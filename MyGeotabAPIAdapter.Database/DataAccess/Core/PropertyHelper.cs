using Dapper.Contrib.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A helper class for evaluating lists of <see cref="PropertyInfo"/> objects of <paramref name="type"/>s that are database model classes associated with corresponding database tables. This class is centered around the <see cref="GetInsertablePropertyNames(Type)"/> method and is intended primarily for use with SQL Server bulk operations.
    /// </summary>
    public static class PropertyHelper
    {
        static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> AllTypeProperties = new();
        static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> ComputedProperties = new();
        static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> KeyProperties = new();
        static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> WriteableTypeProperties = new();

        /// <summary>
        /// Gets a list of all <see cref="PropertyInfo"/> objects for the subject <paramref name="type"/> representing database model properties. These <see cref="PropertyInfo"/>s are added to the <see cref="AllTypeProperties"/> ConcurrentDictionary the first time a specific <paramref name="type"/> is evaluated so that the costly reflection process only needs to be executed once for any given <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The object <see cref="Type"/> to be evaluated.</param>
        /// <returns></returns>
        static List<PropertyInfo> AllTypePropertiesCache(Type type)
        {
            if (AllTypeProperties.TryGetValue(type.TypeHandle, out IEnumerable<PropertyInfo> propertyInfos))
            {
                return propertyInfos.ToList();
            }

            var properties = type.GetProperties().ToArray();
            AllTypeProperties[type.TypeHandle] = properties;
            return properties.ToList();
        }

        /// <summary>
        /// Obtains the data as a list; if it is *already* a list, the original object is returned without
        /// any duplication; otherwise, ToList() is invoked.
        /// </summary>
        /// <typeparam name="T">The type of element in the list.</typeparam>
        /// <param name="source">The enumerable to return as a list.</param>
        static List<T> AsList<T>(this IEnumerable<T> source) =>
            source == null || source is List<T> ? (List<T>)source : source.ToList();

        /// <summary>
        /// Gets a list of <see cref="PropertyInfo"/> objects for the subject <paramref name="type"/> representing database model properties that are marked with <c>[Computed]</c>. These are essentially treated the same as properties with <c>[Write(false)]</c>, but since Dapper supports both, we must handle both. These <see cref="PropertyInfo"/>s for computed properties are added to the <see cref="ComputedProperties"/> ConcurrentDictionary the first time a specific <paramref name="type"/> is evaluated so that the costly reflection process only needs to be executed once for any given <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The object <see cref="Type"/> to be evaluated.</param>
        /// <returns></returns>
        static List<PropertyInfo> ComputedPropertiesCache(Type type)
        {
            if (ComputedProperties.TryGetValue(type.TypeHandle, out IEnumerable<PropertyInfo> propertyInfo))
            {
                return propertyInfo.ToList();
            }

            var computedProperties = WriteableTypePropertiesCache(type).Where(propertyInfo => propertyInfo.GetCustomAttributes(true).Any(attribute => attribute is ComputedAttribute)).ToList();

            ComputedProperties[type.TypeHandle] = computedProperties;
            return computedProperties;
        }

        /// <summary>
        /// Returns an array of all property names associated with the <paramref name="type"/> excluding names of properties marked with the <c>[Write(false)]</c> attribute. Intended for use with SQL Server bulk operations.
        /// </summary>
        /// <param name="type">The object <see cref="Type"/> to be evaluated.</param>
        /// <returns></returns>
        public static string[] GetAllWriteablePropertyNames(Type type)
        {
            var allProperties = WriteableTypePropertiesCache(type);
            var computedProperties = ComputedPropertiesCache(type);
            var allPropertiesExceptComputed = allProperties.Except(computedProperties.ToList());
            var writeablePropertyNames = allPropertiesExceptComputed.Select(propertyInfo => propertyInfo.Name).ToArray();
            return writeablePropertyNames;
        }

        /// <summary>
        /// Returns an array of property names associated with the <paramref name="type"/> including names of properties marked with the <c>[Key]</c> attribute as well as properties named "id" and not marked with <c>[ExplicitKey]</c>. Intended for use with SQL Server bulk operations.
        /// </summary>
        /// <param name="type">The object <see cref="Type"/> to be evaluated.</param>
        /// <returns></returns>
        public static string[] GetKeyPropertyNames(Type type)
        {
            var keyProperties = KeyPropertiesCache(type);
            var keyPropertyNames = keyProperties.Select(propertyInfo => propertyInfo.Name).ToArray();
            return keyPropertyNames;
        }

        /// <summary>
        /// Returns an array of property names associated with the <paramref name="type"/> excluding names of properties marked with <c>[Key]</c>, <c>[Computed]</c> or <c>[Write(false)]</c> attributes as well as properties named "id" and not marked with <c>[ExplicitKey]</c>. This array represents the list of columns in the associated database table that can be inserted into. Intended for use with SQL Server bulk operations.
        /// </summary>
        /// <param name="type">The object <see cref="Type"/> to be evaluated.</param>
        /// <returns></returns>
        public static string[] GetInsertablePropertyNames(Type type)
        {
            var allProperties = WriteableTypePropertiesCache(type);
            var keyProperties = KeyPropertiesCache(type);
            var computedProperties = ComputedPropertiesCache(type);
            var allPropertiesExceptKeyAndComputed = allProperties.Except(keyProperties.Union(computedProperties)).ToList();
            var insertablePropertyNames = allPropertiesExceptKeyAndComputed.Select(propertyInfo => propertyInfo.Name).ToArray();
            return insertablePropertyNames;
        }

        /// <summary>
        /// Returns an array of property names associated with the <paramref name="type"/> excluding names of properties marked with <c>[Computed]</c> or <c>[Write(false)]</c> attributes. Fields marked with <c>[Key]</c> (there should only be one) are included. This array represents the list of columns in the associated database table that can be inserted into. Intended for use with SQL Server bulk operations. Note that the <c>[Key]</c> property is included because temporary tables must have the keys inserted in order for join-based updates of the actual tables to be successful. 
        /// </summary>
        /// <param name="type">The object <see cref="Type"/> to be evaluated.</param>
        /// <returns></returns>
        public static string[] GetKeyAndUpdatablePropertyNames(Type type)
        {
            var allProperties = WriteableTypePropertiesCache(type);
            var keyProperties = KeyPropertiesCache(type);
            var computedProperties = ComputedPropertiesCache(type);
            var allPropertiesIncludingKey = keyProperties.Union(allProperties.Except(computedProperties)).ToList();
            var keyAndUpdatablePropertyNames = allPropertiesIncludingKey.Select(propertyInfo => propertyInfo.Name).ToArray();
            return keyAndUpdatablePropertyNames;
        }

        /// <summary>
        /// Determines whether a property is writeable to database based on the WriteAttribute. If a database model property is marked with <c>[Write(false)]</c>, the property is not writeable. Otherwise it is considered writeable. Note that this does not factor-in other attributes that make properties not writeable, such as the <c>[Key]</c> attribute.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> representing the property to be evaluated.</param>
        /// <returns></returns>
        static bool IsWriteable(PropertyInfo propertyInfo)
        {
            var attributes = propertyInfo.GetCustomAttributes(typeof(WriteAttribute), false).AsList();
            if (attributes.Count != 1) return true;

            var writeAttribute = (WriteAttribute)attributes[0];
            return writeAttribute.Write;
        }

        /// <summary>
        /// Gets a list of <see cref="PropertyInfo"/> objects for the subject <paramref name="type"/> representing database model properties that are marked with <c>[Key]</c>. When evaluating a <paramref name="type"/>, if no property is marked with <c>[Key]</c>, then a secondary check is performed to find a <see cref="PropertyInfo"/> named "id" and not marked with <c>[ExplicitKey]</c>. These <see cref="PropertyInfo"/>s for key properties, represent database table columns that should not be written to, and are added to the <see cref="KeyProperties"/> ConcurrentDictionary the first time a specific <paramref name="type"/> is evaluated so that the costly reflection process only needs to be executed once for any given <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The object <see cref="Type"/> to be evaluated.</param>
        /// <returns></returns>
        static List<PropertyInfo> KeyPropertiesCache(Type type)
        {
            if (KeyProperties.TryGetValue(type.TypeHandle, out IEnumerable<PropertyInfo> propertyInfo))
            {
                return propertyInfo.ToList();
            }

            var allProperties = AllTypePropertiesCache(type);
            var keyProperties = allProperties.Where(propertyInfo => propertyInfo.GetCustomAttributes(true).Any(attribute => attribute is KeyAttribute)).ToList();

            if (keyProperties.Count == 0)
            {
                var idPropertyInfo = allProperties.Find(propertyInfo => string.Equals(propertyInfo.Name, "id", StringComparison.CurrentCultureIgnoreCase));
                if (idPropertyInfo != null && !idPropertyInfo.GetCustomAttributes(true).Any(attribute => attribute is ExplicitKeyAttribute))
                {
                    keyProperties.Add(idPropertyInfo);
                }
            }

            KeyProperties[type.TypeHandle] = keyProperties;
            return keyProperties;
        }

        /// <summary>
        /// Gets a list of all <see cref="IsWriteable(PropertyInfo)"/> <see cref="PropertyInfo"/> objects for the subject <paramref name="type"/> representing database model properties. These <see cref="PropertyInfo"/>s are added to the <see cref="WriteableTypeProperties"/> ConcurrentDictionary the first time a specific <paramref name="type"/> is evaluated so that the costly reflection process only needs to be executed once for any given <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The object <see cref="Type"/> to be evaluated.</param>
        /// <returns></returns>
        static List<PropertyInfo> WriteableTypePropertiesCache(Type type)
        {
            if (WriteableTypeProperties.TryGetValue(type.TypeHandle, out IEnumerable<PropertyInfo> propertyInfos))
            {
                return propertyInfos.ToList();
            }

            var properties = type.GetProperties().Where(IsWriteable).ToArray();
            WriteableTypeProperties[type.TypeHandle] = properties;
            return properties.ToList();
        }
    }
}

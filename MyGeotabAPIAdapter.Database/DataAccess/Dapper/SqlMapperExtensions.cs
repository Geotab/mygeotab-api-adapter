﻿using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
#region Customizations
using System.Threading.Tasks;
#endregion
using System.Collections.Concurrent;
using System.Reflection.Emit;
using Dapper;

namespace Dapper.Contrib.Extensions
{
    /// <summary>
    /// A customized version of the Dapper.Contrib extensions for Dapper. Customizations are contained within "Customizations" regions.
    /// </summary>
    public static partial class SqlMapperExtensions
    {
        /// <summary>
        /// Defined a proxy object with a possibly dirty state.
        /// </summary>
        public interface IProxy //must be kept public
        {
            /// <summary>
            /// Whether the object has been changed.
            /// </summary>
            bool IsDirty { get; set; }
        }

        /// <summary>
        /// Defines a table name mapper for getting table names from types.
        /// </summary>
        public interface ITableNameMapper
        {
            /// <summary>
            /// Gets a table name from a given <see cref="Type"/>.
            /// </summary>
            /// <param name="type">The <see cref="Type"/> to get a name from.</param>
            /// <returns>The table name for the given <paramref name="type"/>.</returns>
            string GetTableName(Type type);
        }

        /// <summary>
        /// The function to get a database type from the given <see cref="IDbConnection"/>.
        /// </summary>
        /// <param name="connection">The connection to get a database type name from.</param>
        public delegate string GetDatabaseTypeDelegate(IDbConnection connection);
        /// <summary>
        /// The function to get a a table name from a given <see cref="Type"/>
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to get a table name for.</param>
        public delegate string TableNameMapperDelegate(Type type);

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> KeyProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> ExplicitKeyProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> ComputedProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();
        #region Customizations
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> ChangeTrackerProperties = new();
        #endregion
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> GetQueries = new ConcurrentDictionary<RuntimeTypeHandle, string>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> TypeTableName = new ConcurrentDictionary<RuntimeTypeHandle, string>();

        private static readonly ISqlAdapter DefaultAdapter = new SqlServerAdapter();
        private static readonly Dictionary<string, ISqlAdapter> AdapterDictionary
            = new Dictionary<string, ISqlAdapter>
            {
                ["sqlconnection"] = new SqlServerAdapter(),
                ["oracleconnection"] = new OracleAdapter(),
                ["sqlceconnection"] = new SqlCeServerAdapter(),
                ["npgsqlconnection"] = new PostgresAdapter(),
                ["mysqlconnection"] = new MySqlAdapter(),
                ["fbconnection"] = new FbAdapter()
            };

        #region Customizations
        private static List<PropertyInfo> ChangeTrackerPropertiesCache(Type type)
        {
            if (ChangeTrackerProperties.TryGetValue(type.TypeHandle, out IEnumerable<PropertyInfo> pi))
            {
                return pi.ToList();
            }

            var changeTrackerProperties = TypePropertiesCache(type).Where(p => p.GetCustomAttributes(true).Any(a => a is ChangeTrackerAttribute)).ToList();

            ChangeTrackerProperties[type.TypeHandle] = changeTrackerProperties;
            return changeTrackerProperties;
        }
        #endregion

        private static List<PropertyInfo> ComputedPropertiesCache(Type type)
        {
            if (ComputedProperties.TryGetValue(type.TypeHandle, out IEnumerable<PropertyInfo> pi))
            {
                return pi.ToList();
            }

            var computedProperties = TypePropertiesCache(type).Where(p => p.GetCustomAttributes(true).Any(a => a is ComputedAttribute)).ToList();

            ComputedProperties[type.TypeHandle] = computedProperties;
            return computedProperties;
        }

        private static List<PropertyInfo> ExplicitKeyPropertiesCache(Type type)
        {
            if (ExplicitKeyProperties.TryGetValue(type.TypeHandle, out IEnumerable<PropertyInfo> pi))
            {
                return pi.ToList();
            }

            var explicitKeyProperties = TypePropertiesCache(type).Where(p => p.GetCustomAttributes(true).Any(a => a is ExplicitKeyAttribute)).ToList();

            ExplicitKeyProperties[type.TypeHandle] = explicitKeyProperties;
            return explicitKeyProperties;
        }

        private static List<PropertyInfo> KeyPropertiesCache(Type type)
        {
            if (KeyProperties.TryGetValue(type.TypeHandle, out IEnumerable<PropertyInfo> pi))
            {
                return pi.ToList();
            }

            var allProperties = TypePropertiesCache(type);
            var keyProperties = allProperties.Where(p => p.GetCustomAttributes(true).Any(a => a is KeyAttribute)).ToList();

            if (keyProperties.Count == 0)
            {
                //var idProp = allProperties.Find(p => string.Equals(p.Name, "id", StringComparison.CurrentCultureIgnoreCase));
                var idProp = allProperties.Find(p => string.Equals(p.Name, "\"id\"", StringComparison.CurrentCultureIgnoreCase));
                if (idProp != null && !idProp.GetCustomAttributes(true).Any(a => a is ExplicitKeyAttribute))
                {
                    keyProperties.Add(idProp);
                }
            }

            KeyProperties[type.TypeHandle] = keyProperties;
            return keyProperties;
        }

        private static List<PropertyInfo> TypePropertiesCache(Type type)
        {
            if (TypeProperties.TryGetValue(type.TypeHandle, out IEnumerable<PropertyInfo> pis))
            {
                return pis.ToList();
            }

            var properties = type.GetProperties().Where(IsWriteable).ToArray();
            TypeProperties[type.TypeHandle] = properties;
            return properties.ToList();
        }

        private static bool IsWriteable(PropertyInfo pi)
        {
            var attributes = pi.GetCustomAttributes(typeof(WriteAttribute), false).AsList();
            if (attributes.Count != 1) return true;

            var writeAttribute = (WriteAttribute)attributes[0];
            return writeAttribute.Write;
        }

        #region Customizations
        /// <summary>
        /// Using Dapper ORM, it is not possible to map strings longer than 4000 characters to Oracle NCLOBs without heavy customization (<see href="https://dapper-tutorial.net/knowledge-base/17832123/dapper---oracle-clob-type">Dapper & Oracle Clob type</see> for more info). Alternatively, <see href="https://github.com/DIPSAS/Dapper.Oracle">Dapper.Oracle</see> could be used to effectively work with all Oracle-specific data types. However, that would require an Oracle-specific version of this project, which is not in-scope. This method is intended for use when working with an Oracle database. It checks all string properties of the <paramref name="entity"/> and replaces any property values greater than 4,000 characters in length with a substitute message. This is an alternative to arbitrarily truncating and corrupting the data. Extraneous processes can be developed to retrieve the afftected values and update them in the database.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="entity"/>.</typeparam>
        /// <param name="entity">The entity to be processed.</param>
        public static void CheckForAndMitigateTruncationOfOracleNCLOB<T>(T entity)
        {
            const int CharacterLimit = 4000;
            const string TruncatedValueSubstituteString = "Entity value exceeds 4000 character limit and would be truncated. Please use other means to obtain and update this placeholder with the actual value.";

            var type = typeof(T);
            var allProperties = TypePropertiesCache(type);
            var keyProperties = KeyPropertiesCache(type);
            var computedProperties = ComputedPropertiesCache(type);
            var allPropertiesExceptKeyAndComputed = allProperties.Except(keyProperties.Union(computedProperties)).ToList();

            for (var i = 0; i < allPropertiesExceptKeyAndComputed.Count; i++)
            {
                var property = allPropertiesExceptKeyAndComputed[i];
                if (property.PropertyType == typeof(string))
                {
                    var propertyValue = property.GetValue(entity);
                    if (propertyValue is not null)
                    {
                        var propertyValueString = propertyValue.ToString();
                        if (propertyValueString.Length > CharacterLimit)
                        {
                            property.SetValue(entity, TruncatedValueSubstituteString);
                        }
                    }
                }
            }
        }
        #endregion

        private static PropertyInfo GetSingleKey<T>(string method)
        {
            var type = typeof(T);
            var keys = KeyPropertiesCache(type);
            var explicitKeys = ExplicitKeyPropertiesCache(type);
            var keyCount = keys.Count + explicitKeys.Count;
            if (keyCount > 1)
            {
                //throw new DataException($"{method}<T> only supports an entity with a single [Key] or [ExplicitKey] property. [Key] Count: {keys.Count}, [ExplicitKey] Count: {explicitKeys.Count}");
                #region Customizations
                // If there is an ExplicitKey and another column named "id", which may be a surrogate key, drop the "id" column from the list of keys as it is the ExplicitKey that we want to use. 
                if (keys.Count == 1 && keys[0].Name == "id")
                {
                    keys.Clear();
                    keyCount = keys.Count + explicitKeys.Count;
                }
                else
                {
                    throw new DataException($"{method}<T> only supports an entity with a single [Key] or [ExplicitKey] property. [Key] Count: {keys.Count}, [ExplicitKey] Count: {explicitKeys.Count}");
                }
                #endregion

            }
            if (keyCount == 0)
                throw new DataException($"{method}<T> only supports an entity with a [Key] or an [ExplicitKey] property");

            return keys.Any() ? keys[0] : explicitKeys[0];
        }

        #region Customizations
        private static PropertyInfo GetSortKey<T>(string sortColumnName)
        {
            var type = typeof(T);

            if (type.IsArray)
            {
                type = type.GetElementType();
            }
            else if (type.IsGenericType)
            {
                var typeInfo = type.GetTypeInfo();
                bool implementsGenericIEnumerableOrIsGenericIEnumerable =
                    typeInfo.ImplementedInterfaces.Any(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
                    typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>);

                if (implementsGenericIEnumerableOrIsGenericIEnumerable)
                {
                    type = type.GetGenericArguments()[0];
                }
            }

            var allProperties = TypePropertiesCache(type);

            if (allProperties.Any())
            {
                var sortKeyProp = allProperties.Find(p => string.Equals(p.Name, sortColumnName, StringComparison.CurrentCultureIgnoreCase));
                if (sortKeyProp != null)
                {
                    return sortKeyProp;
                }
            }

            throw new DataException($"GetSortKey<T> cannot find a property named {sortColumnName} for type {type}.");
        }
        #endregion

        #region Customizations
        ///// <summary>
        ///// Returns a single entity by a single id from table "Ts".  
        ///// Id must be marked with [Key] attribute.
        ///// Entities created from interfaces are tracked/intercepted for changes and used by the Update() extension
        ///// for optimal performance. 
        ///// </summary>
        ///// <typeparam name="T">Interface or type to create and populate</typeparam>
        ///// <param name="connection">Open SqlConnection</param>
        ///// <param name="id">Id of the entity to get, must be marked with [Key] attribute</param>
        ///// <param name="transaction">The transaction to run under, null (the default) if none</param>
        ///// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        ///// <returns>Entity of T</returns>
        //public static T Get<T>(this IDbConnection connection, dynamic id, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        //{
        //    var type = typeof(T);

        //    if (!GetQueries.TryGetValue(type.TypeHandle, out string sql))
        //    {
        //        var key = GetSingleKey<T>(nameof(Get));
        //        var name = GetTableName(type);

        //        sql = $"select * from {name} where {key.Name} = @id";
        //        GetQueries[type.TypeHandle] = sql;
        //    }

        //    var dynParms = new DynamicParameters();
        //    dynParms.Add("@id", id);

        //    T obj;

        //    if (type.IsInterface)
        //    {
        //        var res = connection.Query(sql, dynParms).FirstOrDefault() as IDictionary<string, object>;

        //        if (res == null)
        //            return null;

        //        obj = ProxyGenerator.GetInterfaceProxy<T>();

        //        foreach (var property in TypePropertiesCache(type))
        //        {
        //            var val = res[property.Name];
        //            if (val == null) continue;
        //            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        //            {
        //                var genericType = Nullable.GetUnderlyingType(property.PropertyType);
        //                if (genericType != null) property.SetValue(obj, Convert.ChangeType(val, genericType), null);
        //            }
        //            else
        //            {
        //                property.SetValue(obj, Convert.ChangeType(val, property.PropertyType), null);
        //            }
        //        }

        //        ((IProxy)obj).IsDirty = false;   //reset change tracking and return
        //    }
        //    else
        //    {
        //        obj = connection.Query<T>(sql, dynParms, transaction, commandTimeout: commandTimeout).FirstOrDefault();
        //    }
        //    return obj;
        //}

        /// <summary>
        /// Returns a single entity by a single id from table "Ts".  
        /// Id must be marked with [Key] attribute.
        /// Entities created from interfaces are tracked/intercepted for changes and used by the Update() extension
        /// for optimal performance. 
        /// </summary>
        /// <typeparam name="T">Interface or type to create and populate</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="id">Id of the entity to get, must be marked with [Key] attribute</param>
        /// <param name="transaction">The transaction to run under, null (the default) if none</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Entity of T</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "<Pending>")]
        public static async Task<T> GetAsync<T>(this IDbConnection connection, dynamic id, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);

            if (!GetQueries.TryGetValue(type.TypeHandle, out string sql))
            {
                var key = GetSingleKey<T>(nameof(GetAsync));
                var name = GetTableName(type);

                sql = $"select * from \"{name}\" where \"{key.Name}\" = \'@id\'";
                GetQueries[type.TypeHandle] = sql;
            }

            var dynParms = new DynamicParameters();
            dynParms.Add("@id", id);

            T obj;

            if (type.IsInterface)
            {
                var res = await connection.QueryAsync(sql, dynParms).Result.FirstOrDefault() as IDictionary<string, object>;

                if (res == null)
                    return null;

                obj = ProxyGenerator.GetInterfaceProxy<T>();

                foreach (var property in TypePropertiesCache(type))
                {
                    var val = res[property.Name];
                    if (val == null) continue;
                    if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        var genericType = Nullable.GetUnderlyingType(property.PropertyType);
                        if (genericType != null) property.SetValue(obj, Convert.ChangeType(val, genericType), null);
                    }
                    else
                    {
                        property.SetValue(obj, Convert.ChangeType(val, property.PropertyType), null);
                    }
                }

                ((IProxy)obj).IsDirty = false;   //reset change tracking and return
            }
            else
            {
                //obj = connection.Query<T>(sql, dynParms, transaction, commandTimeout: commandTimeout).FirstOrDefault();
                var result = await connection.QueryAsync<T>(sql, dynParms, transaction, commandTimeout: commandTimeout);
                obj = result.FirstOrDefault();
            }
            return obj;
        }
        #endregion

        #region Customizations
        ///// <summary>
        ///// Returns a list of entites from table "Ts".  
        ///// Id of T must be marked with [Key] attribute.
        ///// Entities created from interfaces are tracked/intercepted for changes and used by the Update() extension
        ///// for optimal performance. 
        ///// </summary>
        ///// <typeparam name="T">Interface or type to create and populate</typeparam>
        ///// <param name="connection">Open SqlConnection</param>
        ///// <param name="transaction">The transaction to run under, null (the default) if none</param>
        ///// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        ///// <returns>Entity of T</returns>
        //public static IEnumerable<T> GetAll<T>(this IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        //{
        //    var type = typeof(T);
        //    var cacheType = typeof(List<T>);

        //    if (!GetQueries.TryGetValue(cacheType.TypeHandle, out string sql))
        //    {
        //        GetSingleKey<T>(nameof(GetAll));
        //        var name = GetTableName(type);

        //        sql = "select * from " + name;
        //        GetQueries[cacheType.TypeHandle] = sql;
        //    }

        //    try
        //    {
        //        if (!type.IsInterface) return connection.Query<T>(sql, null, transaction, commandTimeout: commandTimeout);
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }
        //    if (!type.IsInterface) return connection.Query<T>(sql, null, transaction, commandTimeout: commandTimeout);

        //    var result = connection.Query(sql);
        //    var list = new List<T>();
        //    foreach (IDictionary<string, object> res in result)
        //    {
        //        var obj = ProxyGenerator.GetInterfaceProxy<T>();
        //        foreach (var property in TypePropertiesCache(type))
        //        {
        //            var val = res[property.Name];
        //            if (val == null) continue;
        //            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        //            {
        //                var genericType = Nullable.GetUnderlyingType(property.PropertyType);
        //                if (genericType != null) property.SetValue(obj, Convert.ChangeType(val, genericType), null);
        //            }
        //            else
        //            {
        //                property.SetValue(obj, Convert.ChangeType(val, property.PropertyType), null);
        //            }
        //        }
        //        ((IProxy)obj).IsDirty = false;   //reset change tracking and return
        //        list.Add(obj);
        //    }
        //    return list;
        //}

        /// <summary>
        /// Returns a list of entites from table "Ts".  
        /// Id of T must be marked with [Key] attribute.
        /// Entities created from interfaces are tracked/intercepted for changes and used by the Update() extension
        /// for optimal performance. 
        /// </summary>
        /// <typeparam name="T">Interface or type to create and populate</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="transaction">The transaction to run under, null (the default) if none</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <param name="resultsLimit">The maximum number of entities to return. If null, no limit is applied.</param>
        /// <param name="changedSince">Only select entities where the <see cref="ChangeTrackerAttribute"/> property has a value greater than this <see cref="DateTime"/>. If null, no <see cref="DateTime"/> filter is applied.</param>
        /// <param name="sortColumnName">If a valid column name is supplied, the query to retrieve entities will sort on this column instead of the column marked with the [Key] attribute.</param>
        /// <returns>Entity of T</returns>
        public static async Task<IEnumerable<T>> GetAllAsync<T>(this IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null, int? resultsLimit = null, DateTime? changedSince = null, string sortColumnName = "") where T : class
        {
            var type = typeof(T);
            var cacheType = typeof(List<T>);
            var changedSinceDateTime = DateTime.MinValue;

            if (!GetQueries.TryGetValue(cacheType.TypeHandle, out string sql))
            {
                var tableName = GetTableName(type);
                var key = GetSingleKey<T>(nameof(GetAllAsync));
                if (sortColumnName != "")
                {
                    key = GetSortKey<T>(sortColumnName);
                }
                var changeTrackerProperties = ChangeTrackerPropertiesCache(type);
                PropertyInfo changedSinceProperty = null;
#nullable enable
                string? changedSinceColumnName = null;
#nullable disable
                string connectionProviderType = connection.GetType().Namespace;

                if (changedSince != null && changeTrackerProperties.Any())
                {
                    changedSinceProperty = changeTrackerProperties[0];
                    changedSinceColumnName = changedSinceProperty.Name;
                }

                sql = SqlMapperSqlBuilder.GetSqlForGetAllAsync(tableName, key.Name, changedSinceColumnName, connectionProviderType, resultsLimit, changedSince);
                GetQueries[cacheType.TypeHandle] = sql;
            }

            try
            {
                if (!type.IsInterface) return await connection.QueryAsync<T>(sql, null, transaction, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                throw;
            }
            if (!type.IsInterface) return await connection.QueryAsync<T>(sql, null, transaction, commandTimeout: commandTimeout);

            var result = await connection.QueryAsync(sql);
            var list = new List<T>();
            foreach (IDictionary<string, object> res in result)
            {
                var obj = ProxyGenerator.GetInterfaceProxy<T>();
                foreach (var property in TypePropertiesCache(type))
                {
                    var val = res[property.Name];
                    if (val == null) continue;
                    if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        var genericType = Nullable.GetUnderlyingType(property.PropertyType);
                        if (genericType != null) property.SetValue(obj, Convert.ChangeType(val, genericType), null);
                    }
                    else
                    {
                        property.SetValue(obj, Convert.ChangeType(val, property.PropertyType), null);
                    }
                }
                ((IProxy)obj).IsDirty = false;   //reset change tracking and return
                list.Add(obj);
            }
            return list;
        }
        #endregion

        /// <summary>
        /// Specify a custom table name mapper based on the POCO type name
        /// </summary>
        public static TableNameMapperDelegate TableNameMapper;

        private static string GetTableName(Type type)
        {
            if (TypeTableName.TryGetValue(type.TypeHandle, out string name)) return name;

            if (TableNameMapper != null)
            {
                name = TableNameMapper(type);
            }
            else
            {
                //NOTE: This as dynamic trick falls back to handle both our own Table-attribute as well as the one in EntityFramework 
                var tableAttrName =
                    type.GetCustomAttribute<TableAttribute>(false)?.Name
                    ?? (type.GetCustomAttributes(false).FirstOrDefault(attr => attr.GetType().Name == "TableAttribute") as dynamic)?.Name;

                if (tableAttrName != null)
                {
                    name = tableAttrName;
                }
                else
                {
                    name = type.Name + "s";
                    if (type.IsInterface && name.StartsWith("I"))
                        name = name.Substring(1);
                }
            }

            TypeTableName[type.TypeHandle] = name;
            return name;
        }

        #region Customizations
        ///// <summary>
        ///// Inserts an entity into table "Ts" and returns identity id or number of inserted rows if inserting a list.
        ///// </summary>
        ///// <typeparam name="T">The type to insert.</typeparam>
        ///// <param name="connection">Open SqlConnection</param>
        ///// <param name="entityToInsert">Entity to insert, can be list of entities</param>
        ///// <param name="transaction">The transaction to run under, null (the default) if none</param>
        ///// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        ///// <returns>Identity of inserted entity, or number of inserted rows if inserting a list</returns>
        //public static long Insert<T>(this IDbConnection connection, T entityToInsert, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        //{
        //    var isList = false;

        //    var type = typeof(T);

        //    if (type.IsArray)
        //    {
        //        isList = true;
        //        type = type.GetElementType();
        //    }
        //    else if (type.IsGenericType)
        //    {
        //        var typeInfo = type.GetTypeInfo();
        //        bool implementsGenericIEnumerableOrIsGenericIEnumerable =
        //            typeInfo.ImplementedInterfaces.Any(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
        //            typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>);

        //        if (implementsGenericIEnumerableOrIsGenericIEnumerable)
        //        {
        //            isList = true;
        //            type = type.GetGenericArguments()[0];
        //        }
        //    }

        //    var name = GetTableName(type);
        //    var sbColumnList = new StringBuilder(null);
        //    var allProperties = TypePropertiesCache(type);
        //    var keyProperties = KeyPropertiesCache(type);
        //    var computedProperties = ComputedPropertiesCache(type);
        //    var allPropertiesExceptKeyAndComputed = allProperties.Except(keyProperties.Union(computedProperties)).ToList();

        //    var adapter = GetFormatter(connection);

        //    for (var i = 0; i < allPropertiesExceptKeyAndComputed.Count; i++)
        //    {
        //        var property = allPropertiesExceptKeyAndComputed[i];
        //        adapter.AppendColumnName(sbColumnList, property.Name);  //fix for issue #336
        //        if (i < allPropertiesExceptKeyAndComputed.Count - 1)
        //            sbColumnList.Append(", ");
        //    }

        //    var sbParameterList = new StringBuilder(null);
        //    for (var i = 0; i < allPropertiesExceptKeyAndComputed.Count; i++)
        //    {
        //        var property = allPropertiesExceptKeyAndComputed[i];
        //        sbParameterList.AppendFormat("@{0}", property.Name);
        //        if (i < allPropertiesExceptKeyAndComputed.Count - 1)
        //            sbParameterList.Append(", ");
        //    }

        //    int returnVal;
        //    var wasClosed = connection.State == ConnectionState.Closed;
        //    if (wasClosed) connection.Open();

        //    if (!isList)    //single entity
        //    {
        //        returnVal = adapter.Insert(connection, transaction, commandTimeout, name, sbColumnList.ToString(),
        //            sbParameterList.ToString(), keyProperties, entityToInsert);
        //    }
        //    else
        //    {
        //        //insert list of entities
        //        var cmd = $"insert into {name} ({sbColumnList}) values ({sbParameterList})";
        //        returnVal = connection.Execute(cmd, entityToInsert, transaction, commandTimeout);
        //    }
        //    if (wasClosed) connection.Close();
        //    return returnVal;
        //}

        /// <summary>
        /// Inserts an entity into table "Ts" and returns identity id or number of inserted rows if inserting a list.
        /// </summary>
        /// <typeparam name="T">The type to insert.</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToInsert">Entity to insert, can be list of entities</param>
        /// <param name="transaction">The transaction to run under, null (the default) if none</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Identity of inserted entity, or number of inserted rows if inserting a list</returns>
        public static async Task<long> InsertAsync<T>(this IDbConnection connection, T entityToInsert, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            string connectionProviderType = connection.GetType().Namespace;
            if (connectionProviderType == "Oracle.ManagedDataAccess.Client")
            {
                SqlMapper.AddTypeMap(typeof(bool), DbType.Int32);
                CheckForAndMitigateTruncationOfOracleNCLOB(entityToInsert);
            }

            var isList = false;

            var type = typeof(T);

            if (type.IsArray)
            {
                isList = true;
                type = type.GetElementType();
            }
            else if (type.IsGenericType)
            {
                var typeInfo = type.GetTypeInfo();
                bool implementsGenericIEnumerableOrIsGenericIEnumerable =
                    typeInfo.ImplementedInterfaces.Any(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
                    typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>);

                if (implementsGenericIEnumerableOrIsGenericIEnumerable)
                {
                    isList = true;
                    type = type.GetGenericArguments()[0];
                }
            }

            var name = GetTableName(type);
            var sbColumnList = new StringBuilder(null);
            var allProperties = TypePropertiesCache(type);
            var keyProperties = KeyPropertiesCache(type);
            var computedProperties = ComputedPropertiesCache(type);
            var allPropertiesExceptKeyAndComputed = allProperties.Except(keyProperties.Union(computedProperties)).ToList();

            var adapter = GetFormatter(connection);

            for (var i = 0; i < allPropertiesExceptKeyAndComputed.Count; i++)
            {
                var property = allPropertiesExceptKeyAndComputed[i];
                adapter.AppendColumnName(sbColumnList, property.Name);  //fix for issue #336

                //adapter.AppendColumnName(sbColumnList, $"["{property.Name}"]");  //fix for issue #336
                if (i < allPropertiesExceptKeyAndComputed.Count - 1)
                    sbColumnList.Append(", ");
            }

            var sbParameterList = new StringBuilder(null);
            

            for (var i = 0; i < allPropertiesExceptKeyAndComputed.Count; i++)
            {
                var property = allPropertiesExceptKeyAndComputed[i];
                //sbParameterList.AppendFormat("@{0}", property.Name);

                if (connectionProviderType == "Oracle.ManagedDataAccess.Client")
                {
                    //sbParameterList.AppendFormat(":{0}", property.Name);

                    //sbParameterList.AppendFormat(":[{0}]", property.Name);  CommentOracle

                    string sbColumnPropertyName = property.Name;
                    if (sbColumnPropertyName == "Comment" || sbColumnPropertyName == "Start" )
                    {

                        sbParameterList.AppendFormat(":{0}", property.Name + "Oracle");

                    }
                    else
                    {
                        sbParameterList.AppendFormat(":{0}", property.Name);
                    }


                }
                else 
                { 
                    sbParameterList.AppendFormat("@{0}", property.Name); 
                }

                if (i < allPropertiesExceptKeyAndComputed.Count - 1)
                    sbParameterList.Append(", ");
            }

            long returnVal;
            var wasClosed = connection.State == ConnectionState.Closed;
            if (wasClosed) connection.Open();

            if (!isList)    //single entity
            {
                
                returnVal = adapter.Insert(connection, transaction, commandTimeout, name, sbColumnList.ToString(),
                    sbParameterList.ToString(), keyProperties, entityToInsert);
            }
            else
            {
                //insert list of entities
                var cmd = $"insert into {name} ({sbColumnList}) values ({sbParameterList})";
                returnVal = await connection.ExecuteAsync(cmd, entityToInsert, transaction, commandTimeout);
            }
            if (wasClosed) connection.Close();
            return returnVal;
        }
        #endregion

        #region Customizations
        ///// <summary>
        ///// Updates entity in table "Ts", checks if the entity is modified if the entity is tracked by the Get() extension.
        ///// </summary>
        ///// <typeparam name="T">Type to be updated</typeparam>
        ///// <param name="connection">Open SqlConnection</param>
        ///// <param name="entityToUpdate">Entity to be updated</param>
        ///// <param name="transaction">The transaction to run under, null (the default) if none</param>
        ///// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        ///// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
        //public static bool Update<T>(this IDbConnection connection, T entityToUpdate, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        //{
        //    if (entityToUpdate is IProxy proxy && !proxy.IsDirty)
        //    {
        //        return false;
        //    }

        //    var type = typeof(T);

        //    if (type.IsArray)
        //    {
        //        type = type.GetElementType();
        //    }
        //    else if (type.IsGenericType)
        //    {
        //        var typeInfo = type.GetTypeInfo();
        //        bool implementsGenericIEnumerableOrIsGenericIEnumerable =
        //            typeInfo.ImplementedInterfaces.Any(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
        //            typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>);

        //        if (implementsGenericIEnumerableOrIsGenericIEnumerable)
        //        {
        //            type = type.GetGenericArguments()[0];
        //        }
        //    }

        //    var keyProperties = KeyPropertiesCache(type).ToList();  //added ToList() due to issue #418, must work on a list copy
        //    var explicitKeyProperties = ExplicitKeyPropertiesCache(type);
        //    if (keyProperties.Count == 0 && explicitKeyProperties.Count == 0)
        //        throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

        //    var name = GetTableName(type);

        //    var sb = new StringBuilder();
        //    sb.AppendFormat("update {0} set ", name);

        //    var allProperties = TypePropertiesCache(type);
        //    keyProperties.AddRange(explicitKeyProperties);
        //    var computedProperties = ComputedPropertiesCache(type);
        //    var nonIdProps = allProperties.Except(keyProperties.Union(computedProperties)).ToList();

        //    var adapter = GetFormatter(connection);

        //    for (var i = 0; i < nonIdProps.Count; i++)
        //    {
        //        var property = nonIdProps[i];
        //        adapter.AppendColumnNameEqualsValue(sb, property.Name);  //fix for issue #336
        //        if (i < nonIdProps.Count - 1)
        //            sb.Append(", ");
        //    }
        //    sb.Append(" where ");
        //    for (var i = 0; i < keyProperties.Count; i++)
        //    {
        //        var property = keyProperties[i];
        //        adapter.AppendColumnNameEqualsValue(sb, property.Name);  //fix for issue #336
        //        if (i < keyProperties.Count - 1)
        //            sb.Append(" and ");
        //    }
        //    var updated = connection.Execute(sb.ToString(), entityToUpdate, commandTimeout: commandTimeout, transaction: transaction);
        //    return updated > 0;
        //}

        /// <summary>
        /// Updates entity in table "Ts", checks if the entity is modified if the entity is tracked by the Get() extension.
        /// </summary>
        /// <typeparam name="T">Type to be updated</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToUpdate">Entity to be updated</param>
        /// <param name="transaction">The transaction to run under, null (the default) if none</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
        public static async Task<bool> UpdateAsync<T>(this IDbConnection connection, T entityToUpdate, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {

            string connectionProviderType = connection.GetType().Namespace;
            if (connectionProviderType == "Oracle.ManagedDataAccess.Client")
            {
                SqlMapper.AddTypeMap(typeof(bool), DbType.Int32);
                CheckForAndMitigateTruncationOfOracleNCLOB(entityToUpdate);
            }

            if (entityToUpdate is IProxy proxy && !proxy.IsDirty)
            {
                return false;
            }

            var type = typeof(T);

            if (type.IsArray)
            {
                type = type.GetElementType();
            }
            else if (type.IsGenericType)
            {
                var typeInfo = type.GetTypeInfo();
                bool implementsGenericIEnumerableOrIsGenericIEnumerable =
                    typeInfo.ImplementedInterfaces.Any(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
                    typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>);

                if (implementsGenericIEnumerableOrIsGenericIEnumerable)
                {
                    type = type.GetGenericArguments()[0];
                }
            }

            var keyProperties = KeyPropertiesCache(type).ToList();  //added ToList() due to issue #418, must work on a list copy
            var explicitKeyProperties = ExplicitKeyPropertiesCache(type);
            if (keyProperties.Count == 0 && explicitKeyProperties.Count == 0)
                throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

            var name = GetTableName(type);

            var sb = new StringBuilder();
            sb.AppendFormat("update \"{0}\" set ", name);

            var allProperties = TypePropertiesCache(type);
            keyProperties.AddRange(explicitKeyProperties);
            var computedProperties = ComputedPropertiesCache(type);
            var nonIdProps = allProperties.Except(keyProperties.Union(computedProperties)).ToList();

            var adapter = GetFormatter(connection);

            for (var i = 0; i < nonIdProps.Count; i++)
            {
                var property = nonIdProps[i];
                adapter.AppendColumnNameEqualsValue(sb, property.Name);  //fix for issue #336
                if (i < nonIdProps.Count - 1)
                    sb.Append(", ");
            }
            sb.Append(" where ");
            for (var i = 0; i < keyProperties.Count; i++)
            {
                var property = keyProperties[i];
                adapter.AppendColumnNameEqualsValue(sb, property.Name);  //fix for issue #336
                if (i < keyProperties.Count - 1)
                    sb.Append(" and ");
            }
            var updated = await connection.ExecuteAsync(sb.ToString(), entityToUpdate, commandTimeout: commandTimeout, transaction: transaction);
            return updated > 0;
        }
        #endregion

        #region Customizations
        ///// <summary>
        ///// Delete entity in table "Ts".
        ///// </summary>
        ///// <typeparam name="T">Type of entity</typeparam>
        ///// <param name="connection">Open SqlConnection</param>
        ///// <param name="entityToDelete">Entity to delete</param>
        ///// <param name="transaction">The transaction to run under, null (the default) if none</param>
        ///// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        ///// <returns>true if deleted, false if not found</returns>
        //public static bool Delete<T>(this IDbConnection connection, T entityToDelete, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        //{
        //    if (entityToDelete == null)
        //        throw new ArgumentException("Cannot Delete null Object", nameof(entityToDelete));

        //    var type = typeof(T);

        //    if (type.IsArray)
        //    {
        //        type = type.GetElementType();
        //    }
        //    else if (type.IsGenericType)
        //    {
        //        var typeInfo = type.GetTypeInfo();
        //        bool implementsGenericIEnumerableOrIsGenericIEnumerable =
        //            typeInfo.ImplementedInterfaces.Any(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
        //            typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>);

        //        if (implementsGenericIEnumerableOrIsGenericIEnumerable)
        //        {
        //            type = type.GetGenericArguments()[0];
        //        }
        //    }

        //    var keyProperties = KeyPropertiesCache(type).ToList();  //added ToList() due to issue #418, must work on a list copy
        //    var explicitKeyProperties = ExplicitKeyPropertiesCache(type);
        //    if (keyProperties.Count == 0 && explicitKeyProperties.Count == 0)
        //        throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

        //    var name = GetTableName(type);
        //    keyProperties.AddRange(explicitKeyProperties);

        //    var sb = new StringBuilder();
        //    sb.AppendFormat("delete from {0} where ", name);

        //    var adapter = GetFormatter(connection);

        //    for (var i = 0; i < keyProperties.Count; i++)
        //    {
        //        var property = keyProperties[i];
        //        adapter.AppendColumnNameEqualsValue(sb, property.Name);  //fix for issue #336
        //        if (i < keyProperties.Count - 1)
        //            sb.Append(" and ");
        //    }
        //    var deleted = connection.Execute(sb.ToString(), entityToDelete, transaction, commandTimeout);
        //    return deleted > 0;
        //}

        /// <summary>
        /// Delete entity in table "Ts".
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entityToDelete">Entity to delete</param>
        /// <param name="transaction">The transaction to run under, null (the default) if none</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>true if deleted, false if not found</returns>
        public static async Task<bool> DeleteAsync<T>(this IDbConnection connection, T entityToDelete, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            string connectionProviderType = connection.GetType().Namespace;
            if (connectionProviderType == "Oracle.ManagedDataAccess.Client")
            {
                SqlMapper.AddTypeMap(typeof(bool), DbType.Int32);
            }

            if (entityToDelete == null)
                throw new ArgumentException("Cannot Delete null Object", nameof(entityToDelete));

            var type = typeof(T);

            if (type.IsArray)
            {
                type = type.GetElementType();
            }
            else if (type.IsGenericType)
            {
                var typeInfo = type.GetTypeInfo();
                bool implementsGenericIEnumerableOrIsGenericIEnumerable =
                    typeInfo.ImplementedInterfaces.Any(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
                    typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>);

                if (implementsGenericIEnumerableOrIsGenericIEnumerable)
                {
                    type = type.GetGenericArguments()[0];
                }
            }

            var keyProperties = KeyPropertiesCache(type).ToList();  //added ToList() due to issue #418, must work on a list copy
            var explicitKeyProperties = ExplicitKeyPropertiesCache(type);
            if (keyProperties.Count == 0 && explicitKeyProperties.Count == 0)
                throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

            var name = GetTableName(type);
            keyProperties.AddRange(explicitKeyProperties);

            var sb = new StringBuilder();
            sb.AppendFormat("delete from \"{0}\" where ", name);

            var adapter = GetFormatter(connection);

            for (var i = 0; i < keyProperties.Count; i++)
            {
                var property = keyProperties[i];
                adapter.AppendColumnNameEqualsValue(sb, property.Name);  //fix for issue #336
                if (i < keyProperties.Count - 1)
                    sb.Append(" and ");
            }
            var deleted = await connection.ExecuteAsync(sb.ToString(), entityToDelete, transaction, commandTimeout);
            return deleted > 0;
        }
        #endregion

        #region Customizations
        ///// <summary>
        ///// Delete all entities in the table related to the type T.
        ///// </summary>
        ///// <typeparam name="T">Type of entity</typeparam>
        ///// <param name="connection">Open SqlConnection</param>
        ///// <param name="transaction">The transaction to run under, null (the default) if none</param>
        ///// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        ///// <returns>true if deleted, false if none found</returns>
        //public static bool DeleteAll<T>(this IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        //{
        //    var type = typeof(T);
        //    var name = GetTableName(type);
        //    var statement = $"delete from {name}";
        //    var deleted = connection.Execute(statement, null, transaction, commandTimeout);
        //    return deleted > 0;
        //}

        /// <summary>
        /// Delete all entities in the table related to the type T.
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="transaction">The transaction to run under, null (the default) if none</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>true if deleted, false if none found</returns>
        public static async Task<bool> DeleteAllAsync<T>(this IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var name = GetTableName(type);
            var statement = $"delete from \"{name}\"";
            var deleted = await connection.ExecuteAsync(statement, null, transaction, commandTimeout);
            return deleted > 0;
        }
        #endregion

        /// <summary>
        /// Specifies a custom callback that detects the database type instead of relying on the default strategy (the name of the connection type object).
        /// Please note that this callback is global and will be used by all the calls that require a database specific adapter.
        /// </summary>
        public static GetDatabaseTypeDelegate GetDatabaseType;

        private static ISqlAdapter GetFormatter(IDbConnection connection)
        {
            var name = GetDatabaseType?.Invoke(connection).ToLower()
                       ?? connection.GetType().Name.ToLower();

            return AdapterDictionary.TryGetValue(name, out var adapter)
                ? adapter
                : DefaultAdapter;
        }

        private static class ProxyGenerator
        {
            private static readonly Dictionary<Type, Type> TypeCache = new Dictionary<Type, Type>();

            private static AssemblyBuilder GetAsmBuilder(string name)
            {
#if NETSTANDARD2_0
                return AssemblyBuilder.DefineDynamicAssembly(new AssemblyName { Name = name }, AssemblyBuilderAccess.Run);
#else
                #region Customizations
                //return Thread.GetDomain().DefineDynamicAssembly(new AssemblyName { Name = name }, AssemblyBuilderAccess.Run);
                return AssemblyBuilder.DefineDynamicAssembly(new AssemblyName { Name = name }, AssemblyBuilderAccess.Run);
                #endregion
#endif
            }

            public static T GetInterfaceProxy<T>()
            {
                Type typeOfT = typeof(T);

                if (TypeCache.TryGetValue(typeOfT, out Type k))
                {
                    return (T)Activator.CreateInstance(k);
                }
                var assemblyBuilder = GetAsmBuilder(typeOfT.Name);

                var moduleBuilder = assemblyBuilder.DefineDynamicModule("SqlMapperExtensions." + typeOfT.Name); //NOTE: to save, add "asdasd.dll" parameter

                var interfaceType = typeof(IProxy);
                var typeBuilder = moduleBuilder.DefineType(typeOfT.Name + "_" + Guid.NewGuid(),
                    TypeAttributes.Public | TypeAttributes.Class);
                typeBuilder.AddInterfaceImplementation(typeOfT);
                typeBuilder.AddInterfaceImplementation(interfaceType);

                //create our _isDirty field, which implements IProxy
                var setIsDirtyMethod = CreateIsDirtyProperty(typeBuilder);

                // Generate a field for each property, which implements the T
                foreach (var property in typeof(T).GetProperties())
                {
                    var isId = property.GetCustomAttributes(true).Any(a => a is KeyAttribute);
                    CreateProperty<T>(typeBuilder, property.Name, property.PropertyType, setIsDirtyMethod, isId);
                }

#if NETSTANDARD2_0
                var generatedType = typeBuilder.CreateTypeInfo().AsType();
#else
                var generatedType = typeBuilder.CreateType();
#endif

                TypeCache.Add(typeOfT, generatedType);
                return (T)Activator.CreateInstance(generatedType);
            }

            private static MethodInfo CreateIsDirtyProperty(TypeBuilder typeBuilder)
            {
                var propType = typeof(bool);
                var field = typeBuilder.DefineField("_" + nameof(IProxy.IsDirty), propType, FieldAttributes.Private);
                var property = typeBuilder.DefineProperty(nameof(IProxy.IsDirty),
                                               System.Reflection.PropertyAttributes.None,
                                               propType,
                                               new[] { propType });

                const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.SpecialName
                                                  | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig;

                // Define the "get" and "set" accessor methods
                var currGetPropMthdBldr = typeBuilder.DefineMethod("get_" + nameof(IProxy.IsDirty),
                                             getSetAttr,
                                             propType,
                                             Type.EmptyTypes);
                var currGetIl = currGetPropMthdBldr.GetILGenerator();
                currGetIl.Emit(OpCodes.Ldarg_0);
                currGetIl.Emit(OpCodes.Ldfld, field);
                currGetIl.Emit(OpCodes.Ret);
                var currSetPropMthdBldr = typeBuilder.DefineMethod("set_" + nameof(IProxy.IsDirty),
                                             getSetAttr,
                                             null,
                                             new[] { propType });
                var currSetIl = currSetPropMthdBldr.GetILGenerator();
                currSetIl.Emit(OpCodes.Ldarg_0);
                currSetIl.Emit(OpCodes.Ldarg_1);
                currSetIl.Emit(OpCodes.Stfld, field);
                currSetIl.Emit(OpCodes.Ret);

                property.SetGetMethod(currGetPropMthdBldr);
                property.SetSetMethod(currSetPropMthdBldr);
                var getMethod = typeof(IProxy).GetMethod("get_" + nameof(IProxy.IsDirty));
                var setMethod = typeof(IProxy).GetMethod("set_" + nameof(IProxy.IsDirty));
                typeBuilder.DefineMethodOverride(currGetPropMthdBldr, getMethod);
                typeBuilder.DefineMethodOverride(currSetPropMthdBldr, setMethod);

                return currSetPropMthdBldr;
            }

            private static void CreateProperty<T>(TypeBuilder typeBuilder, string propertyName, Type propType, MethodInfo setIsDirtyMethod, bool isIdentity)
            {
                //Define the field and the property 
                var field = typeBuilder.DefineField("_" + propertyName, propType, FieldAttributes.Private);
                var property = typeBuilder.DefineProperty(propertyName,
                                               System.Reflection.PropertyAttributes.None,
                                               propType,
                                               new[] { propType });

                const MethodAttributes getSetAttr = MethodAttributes.Public
                                                    | MethodAttributes.Virtual
                                                    | MethodAttributes.HideBySig;

                // Define the "get" and "set" accessor methods
                var currGetPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName,
                                             getSetAttr,
                                             propType,
                                             Type.EmptyTypes);

                var currGetIl = currGetPropMthdBldr.GetILGenerator();
                currGetIl.Emit(OpCodes.Ldarg_0);
                currGetIl.Emit(OpCodes.Ldfld, field);
                currGetIl.Emit(OpCodes.Ret);

                var currSetPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName,
                                             getSetAttr,
                                             null,
                                             new[] { propType });

                //store value in private field and set the isdirty flag
                var currSetIl = currSetPropMthdBldr.GetILGenerator();
                currSetIl.Emit(OpCodes.Ldarg_0);
                currSetIl.Emit(OpCodes.Ldarg_1);
                currSetIl.Emit(OpCodes.Stfld, field);
                currSetIl.Emit(OpCodes.Ldarg_0);
                currSetIl.Emit(OpCodes.Ldc_I4_1);
                currSetIl.Emit(OpCodes.Call, setIsDirtyMethod);
                currSetIl.Emit(OpCodes.Ret);

                //TODO: Should copy all attributes defined by the interface?
                if (isIdentity)
                {
                    var keyAttribute = typeof(KeyAttribute);
                    var myConstructorInfo = keyAttribute.GetConstructor(new Type[] { });
                    var attributeBuilder = new CustomAttributeBuilder(myConstructorInfo, new object[] { });
                    property.SetCustomAttribute(attributeBuilder);
                }

                property.SetGetMethod(currGetPropMthdBldr);
                property.SetSetMethod(currSetPropMthdBldr);
                var getMethod = typeof(T).GetMethod("get_" + propertyName);
                var setMethod = typeof(T).GetMethod("set_" + propertyName);
                typeBuilder.DefineMethodOverride(currGetPropMthdBldr, getMethod);
                typeBuilder.DefineMethodOverride(currSetPropMthdBldr, setMethod);
            }
        }

        #region Customizations
        /// <summary>
        /// Returns a collection of objects representing records in a database table that match the specified search criteria.
        /// </summary>
        /// <typeparam name="T">The type of entity to be used for representation of database records and for which CRUD operations are to be performed.</typeparam>
        /// <param name="connection">The database connection to be used to perform the subject operation.</param>
        /// <param name="parms">The dynamic parameters to be used comprise the WHERE clause of the subject operation.</param>
        /// <param name="transaction">The database transaction within which to perform the subject operation.</param>
        /// <param name="commandTimeout">The command timeout setting.</param>
        /// <returns></returns>
        public static IEnumerable<T> GetByParam<T>(this IDbConnection connection, dynamic parms, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            string sqlStmt;

            var name = GetTableName(type);
            //ISqlAdapter adapter = GetFormatter(connection);
            #region Customizations
            //char sqlParameterChar = adapter.SqlParameterChar();

            //Sunan
            //string sqlParameterChar = ":";

            string sqlParameterChar = "@";

            #endregion
            var sb = new StringBuilder();
            sb.AppendFormat("SELECT * FROM {0} WHERE ", name);

            var allProperties = parms.GetType().GetProperties();

            var dynParms = new DynamicParameters();

            for (var i = 0; i < allProperties.Length; i++)
            {
                // Create the SQL statement with where clause based on dynamic parameters
                var property = allProperties[i];
                sb.AppendFormat("{0} = {1}{2}", property.Name, sqlParameterChar, property.Name);
                if (i < allProperties.Length - 1)
                    sb.AppendFormat(" and ");

                // Create the DynamicParameters
                dynParms.Add(String.Format("{0}{1}", sqlParameterChar, property.Name), property.GetValue(parms, null));
            }
            sqlStmt = sb.ToString();
            return connection.Query<T>(sqlStmt, dynParms, transaction: transaction, commandTimeout: commandTimeout);
        }
        #endregion

        #region Customizations
        /// <summary>
        /// Returns a collection of objects representing records in a database table that match the specified search criteria.
        /// </summary>
        /// <typeparam name="T">The type of entity to be used for representation of database records and for which CRUD operations are to be performed.</typeparam>
        /// <param name="connection">The database connection to be used to perform the subject operation.</param>
        /// <param name="parms">The dynamic parameters to be used comprise the WHERE clause of the subject operation.</param>
        /// <param name="transaction">The database transaction within which to perform the subject operation.</param>
        /// <param name="commandTimeout">The command timeout setting.</param>
        /// <param name="resultsLimit">The maximum number of entities to return. If null, no limit is applied.</param>
        /// <param name="changedSince">Only select entities where the <see cref="ChangeTrackerAttribute"/> property has a value greater than this <see cref="DateTime"/>. If null, no <see cref="DateTime"/> filter is applied.</param>
        /// <param name="sortColumnName">If a valid column name is supplied, the query to retrieve entities will sort on this column instead of the column marked with the [Key] attribute.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<T>> GetByParamAsync<T>(this IDbConnection connection, dynamic parms, IDbTransaction transaction = null, int? commandTimeout = null, int? resultsLimit = null, DateTime? changedSince = null, string sortColumnName = "") where T : class
        {
            var type = typeof(T);
            string connectionProviderType = connection.GetType().Namespace;
            var changedSinceDateTime = DateTime.MinValue;
            string sqlStmt;
            DynamicParameters dynParms;

            var key = GetSingleKey<T>(nameof(GetByParamAsync));
            if (sortColumnName != "")
            {
                key = GetSortKey<T>(sortColumnName);
            }
            var changeTrackerProperties = ChangeTrackerPropertiesCache(type);
            var tableName = GetTableName(type);
            PropertyInfo changedSinceProperty = null;
#nullable enable
            string? changedSinceColumnName = null;
#nullable disable

            if (changedSince != null && changeTrackerProperties.Any())
            {
                changedSinceProperty = changeTrackerProperties[0];
                changedSinceColumnName = changedSinceProperty.Name;
            }

            (sqlStmt, dynParms) = ((string, DynamicParameters))SqlMapperSqlBuilder.GetSqlForGetByParamAsync(tableName, key.Name, parms, changedSinceColumnName, connectionProviderType, resultsLimit, changedSince);

            return await connection.QueryAsync<T>(sqlStmt, dynParms, transaction: transaction, commandTimeout: commandTimeout);
        }
        #endregion
    }

    /// <summary>
    /// Defines the name of a table to use in Dapper.Contrib commands.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        /// Creates a table mapping to a specific name for Dapper.Contrib commands
        /// </summary>
        /// <param name="tableName">The name of this table in the database.</param>
        public TableAttribute(string tableName)
        {
            Name = tableName;
        }

        /// <summary>
        /// The name of the table in the database
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Specifies that this field is a primary key in the database
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifies that this field is a explicitly set primary key in the database
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ExplicitKeyAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifies whether a field is writable in the database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class WriteAttribute : Attribute
    {
        /// <summary>
        /// Specifies whether a field is writable in the database.
        /// </summary>
        /// <param name="write">Whether a field is writable in the database.</param>
        public WriteAttribute(bool write)
        {
            Write = write;
        }

        /// <summary>
        /// Whether a field is writable in the database.
        /// </summary>
        public bool Write { get; }
    }

    /// <summary>
    /// Specifies that this is a computed column.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ComputedAttribute : Attribute
    {
    }

}

/// <summary>
/// The interface for all Dapper.Contrib database operations
/// Implementing this is each provider's model.
/// </summary>
public partial interface ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>The Id of the row created.</returns>
    long Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert);

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    void AppendColumnName(StringBuilder sb, string columnName);
    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    void AppendColumnNameEqualsValue(StringBuilder sb, string columnName);
}

/// <summary>
/// The SQL Server database adapter.
/// </summary>
public partial class SqlServerAdapter : ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>The Id of the row created.</returns>
    public long Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert)
    {
        var cmd = $"insert into {tableName} ({columnList}) values ({parameterList});select SCOPE_IDENTITY() id";
        var multi = connection.QueryMultiple(cmd, entityToInsert, transaction, commandTimeout);

        var first = multi.Read().FirstOrDefault();
        if (first == null || first.id == null) return 0;

        var id = (long)first.id;
        var propertyInfos = keyProperties as PropertyInfo[] ?? keyProperties.ToArray();
        if (propertyInfos.Length == 0) return id;

        var idProperty = propertyInfos[0];
        idProperty.SetValue(entityToInsert, Convert.ChangeType(id, idProperty.PropertyType), null);

        return id;
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("[{0}]", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("[{0}] = @{1}", columnName, columnName);
    }
}

/// <summary>
/// The SQL Server Compact Edition database adapter.
/// </summary>
public partial class SqlCeServerAdapter : ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>The Id of the row created.</returns>
    public long Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert)
    {
        var cmd = $"insert into {tableName} ({columnList}) values ({parameterList})";
        connection.Execute(cmd, entityToInsert, transaction, commandTimeout);
        var r = connection.Query("select @@IDENTITY id", transaction: transaction, commandTimeout: commandTimeout).ToList();

        if (r[0].id == null) return 0;
        var id = (long)r[0].id;

        var propertyInfos = keyProperties as PropertyInfo[] ?? keyProperties.ToArray();
        if (propertyInfos.Length == 0) return id;

        var idProperty = propertyInfos[0];
        idProperty.SetValue(entityToInsert, Convert.ChangeType(id, idProperty.PropertyType), null);

        return id;
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("[{0}]", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("[{0}] = @{1}", columnName, columnName);
    }
}

/// <summary>
/// The MySQL database adapter.
/// </summary>
public partial class MySqlAdapter : ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>The Id of the row created.</returns>
    public long Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert)
    {
        var cmd = $"insert into {tableName} ({columnList}) values ({parameterList})";
        connection.Execute(cmd, entityToInsert, transaction, commandTimeout);
        var r = connection.Query("Select LAST_INSERT_ID() id", transaction: transaction, commandTimeout: commandTimeout);

        var id = r.First().id;
        if (id == null) return 0;
        var propertyInfos = keyProperties as PropertyInfo[] ?? keyProperties.ToArray();
        if (propertyInfos.Length == 0) return Convert.ToInt64(id);

        var idp = propertyInfos[0];
        idp.SetValue(entityToInsert, Convert.ChangeType(id, idp.PropertyType), null);

        return Convert.ToInt64(id);
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("`{0}`", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("`{0}` = @{1}", columnName, columnName);
    }
}

/// <summary>
/// The Postgres database adapter.
/// </summary>
public partial class PostgresAdapter : ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>The Id of the row created.</returns>
    public long Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert)
    {
        tableName = $"\"{tableName}\"";
        var sb = new StringBuilder();
        sb.AppendFormat("insert into {0} ({1}) values ({2})", tableName, columnList, parameterList);

        // If no primary key then safe to assume a join table with not too much data to return
        var propertyInfos = keyProperties as PropertyInfo[] ?? keyProperties.ToArray();
        if (propertyInfos.Length == 0)
        {
            sb.Append(" RETURNING *");
        }
        else
        {
            sb.Append(" RETURNING ");
            var first = true;
            foreach (var property in propertyInfos)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append(property.Name);
            }
        }

        var results = connection.Query(sb.ToString(), entityToInsert, transaction, commandTimeout: commandTimeout).ToList();

        // Return the key by assinging the corresponding property in the object - by product is that it supports compound primary keys
        long id = 0;
        foreach (var p in propertyInfos)
        {
            var value = ((IDictionary<string, object>)results[0])[p.Name.ToLower()];
            p.SetValue(entityToInsert, value, null);
            if (id == 0)
                id = Convert.ToInt64(value);
        }
        return id;
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("\"{0}\"", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("\"{0}\" = @{1}", columnName, columnName);
    }
}

/// <summary>
/// The Firebase SQL adapeter.
/// </summary>
public partial class FbAdapter : ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>The Id of the row created.</returns>
    public long Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert)
    {
        var cmd = $"insert into {tableName} ({columnList}) values ({parameterList})";
        connection.Execute(cmd, entityToInsert, transaction, commandTimeout);

        var propertyInfos = keyProperties as PropertyInfo[] ?? keyProperties.ToArray();
        var keyName = propertyInfos[0].Name;
        var r = connection.Query($"SELECT FIRST 1 {keyName} ID FROM {tableName} ORDER BY {keyName} DESC", transaction: transaction, commandTimeout: commandTimeout);

        var id = r.First().ID;
        if (id == null) return 0;
        if (propertyInfos.Length == 0) return Convert.ToInt64(id);

        var idp = propertyInfos[0];
        idp.SetValue(entityToInsert, Convert.ChangeType(id, idp.PropertyType), null);

        return Convert.ToInt64(id);
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("{0}", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("{0} = @{1}", columnName, columnName);
    }
}


/// <summary>
/// The Oracle database adapter.
/// </summary>
public partial class OracleAdapter : ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>The Id of the row created.</returns>
    public long Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert)
    {

        var cmd = $"insert into \"{tableName}\" ({columnList}) values ({parameterList})";
        
        connection.Execute(cmd, entityToInsert, transaction, commandTimeout);

        //var r = connection.Query($"Select id from {tableName} where rownum = 1 ORDER BY id DESC", transaction: transaction, commandTimeout: commandTimeout);

        var r = connection.Query($"Select \"id\" from \"{tableName}\" where ROWNUM = 1 ORDER BY \"id\" DESC", transaction: transaction, commandTimeout: commandTimeout);

        var id = r.First().id;
        if (id == null) return 0;
        var propertyInfos = keyProperties as PropertyInfo[] ?? keyProperties.ToArray();
        if (propertyInfos.Length == 0) return Convert.ToInt64(id);

        var idp = propertyInfos[0];
        idp.SetValue(entityToInsert, Convert.ChangeType(id, idp.PropertyType), null);

        return Convert.ToInt64(id);

    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        //columnName = $"[{columnName}]";

        sb.AppendFormat("\"{0}\"", columnName);
        //sb.AppendFormat("{0}", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        //sb.AppendFormat("\"{0}\" = :{1}", columnName, columnName);

        if (columnName == "Comment" || columnName == "Start")
        {
            sb.AppendFormat("\"{0}\" = :{1}", columnName, columnName + "Oracle");
        }
        else
        {
            sb.AppendFormat("\"{0}\" = :{1}", columnName, columnName);
        }

    }
}

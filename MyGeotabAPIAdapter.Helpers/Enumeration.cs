using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

namespace MyGeotabAPIAdapter.Helpers
{
    /// <summary>
    /// A base class for creating Enumerations that are not integer-based. See <see href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types">Use enumeration classes instead of enum types</see> for details.
    /// </summary>
    public abstract class Enumeration : IComparable
    {
        public string Name { get; private set; }

        public int Id { get; private set; }

        protected Enumeration(int id, string name) => (Id, Name) = (id, name);

        public override string ToString() => Name;

        public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
            typeof(T).GetFields(BindingFlags.Public |
                                BindingFlags.Static |
                                BindingFlags.DeclaredOnly)
                     .Select(f => f.GetValue(null))
                     .Cast<T>();

        public override bool Equals(object obj)
        {
            if (obj is not Enumeration otherValue)
            {
                return false;
            }

            var typeMatches = GetType().Equals(obj.GetType());
            var valueMatches = Id.Equals(otherValue.Id);

            return typeMatches && valueMatches;
        }

        public int CompareTo(object other) => Id.CompareTo(((Enumeration)other).Id);

        public override int GetHashCode() => Name.GetHashCode();

        public static int AbsoluteDifference(Enumeration firstValue, Enumeration secondValue)
        {
            var absoluteDifference = Math.Abs(firstValue.Id - secondValue.Id);
            return absoluteDifference;
        }

        public static T FromId<T>(int id) where T : Enumeration, new()
        {
            var matchingItem = parse<T, int>(id, "id", item => item.Id == id);
            return matchingItem;
        }

        public static T FromName<T>(string name) where T : Enumeration, new()
        {
            var matchingItem = parse<T, string>(name, "name", item => item.Name == name);
            return matchingItem;
        }

        private static T parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration, new()
        {
            var matchingItem = GetAll<T>().FirstOrDefault(predicate);

            if (matchingItem == null)
            {
                var message = string.Format("'{0}' is not a valid {1} in {2}", value, description, typeof(T));
                throw new ApplicationException(message);
            }

            return matchingItem;
        }
    }
}

using Geotab.Checkmate.ObjectModel;
using System;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class that compares two Geotab <typeparamref name="T"/> objects based on their <see cref="IDateTimeProvider.DateTime"/> values for sorting purposes.
    /// </summary>
    /// <typeparam name="T">The type of Geotab object to compare.</typeparam>
    internal class GeotabDateTimeProviderComparer<T> : IGeotabDateTimeProviderComparer<T> where T : IDateTimeProvider
    {
        // Obtain the type parameter type (for logging purposes).
        readonly Type typeParameterType = typeof(T);

        static string CurrentClassName { get => $"{nameof(GeotabDateTimeProviderComparer<T>)}<{typeof(T).Name}>"; }

        /// <summary>
        /// Compares two <typeparamref name="T"/> objects based on their <see cref="IDateTimeProvider.DateTime"/> values. If one or both objects are null, an <see cref="ArgumentException"/> is thrown.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        public int Compare(T? x, T? y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentException($"{CurrentClassName}.{nameof(Compare)} could not be executed because one or both objects to be compared are null.");
            }

            // If both objects have a DateTime value, compare them. If one has a DateTime value and the other does not, the one with the DateTime value is considered greater.
            if (x.DateTime.HasValue && y.DateTime.HasValue)
            {
                return x.DateTime.Value.CompareTo(y.DateTime.Value);
            }
            else if (x.DateTime.HasValue)
            {
                // x is greater because it has a DateTime value.
                return 1; 
            }
            else if (y.DateTime.HasValue)
            {
                // y is greater because it has a DateTime value.
                return -1; 
            }
            else
            {
                // Both are considered equal if neither has a DateTime value.
                return 0; 
            }
        }
    }
}

using Geotab.Checkmate.ObjectModel;
using System.Collections.Generic;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for class that compares two Geotab <typeparamref name="T"/> objects based on their <see cref="IDateTimeProvider.DateTime"/> values for sorting purposes.
    /// </summary>
    /// <typeparam name="T">The type of Geotab object to compare.</typeparam>
    internal interface IGeotabDateTimeProviderComparer<T> : IComparer<T> where T : IDateTimeProvider
    {
    }
}

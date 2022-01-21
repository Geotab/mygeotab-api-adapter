using Geotab.Checkmate;

namespace MyGeotabAPIAdapter.MyGeotabAPI
{
    /// <summary>
    /// Interface for a container class that holds a <see cref="API"/> instance.
    /// </summary>
    public interface IMyGeotabAPIContainer
    {
        /// <summary>
        /// A <see cref="API"/> instance to be used for interfacing with the MyGeotab platform.
        /// </summary>
        API MyGeotabAPI { get; }
    }
}

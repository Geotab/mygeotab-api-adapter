using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Interface for a container class to hold <see cref="ConnectionInfo"/> instances for use throughout this project.
    /// </summary>
    public interface IConnectionInfoContainer
    {
        /// <summary>
        /// An instantiated <see cref="ConnectionInfo"/> to be used when working with the adapter database.
        /// </summary>
        ConnectionInfo AdapterDatabaseConnectionInfo { get; }

        /// <summary>
        /// An instantiated <see cref="ConnectionInfo"/> to be used when working with the optimizer database.
        /// </summary>
        ConnectionInfo OptimizerDatabaseConnectionInfo { get; }
    }
}

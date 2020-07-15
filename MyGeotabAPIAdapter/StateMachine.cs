using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Logic;
using System;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Helps to keep track of the overall application state with regard to MyGeotab API and database connectivity.
    /// </summary>
    public static class StateMachine
    {

        /// <summary>
        /// The current <see cref="State"/> of the <see cref="StateMachine"/> instance.
        /// </summary>
        public static State CurrentState { get; set; }

        /// <summary>
        /// The <see cref="StateReason"/> for the current <see cref="State"/> of the <see cref="StateMachine"/> instance.
        /// </summary>
        public static StateReason Reason { get; set; }

        /// <summary>
        /// Indicates whether the database is accessible.
        /// </summary>
        /// <param name="connectionInfo">The database connecton information for use in testing connectivity.</param>
        /// <returns></returns>
        public static async Task<bool> IsDatabaseAccessibleAsync(ConnectionInfo connectionInfo)
        {
            try
            {
                // Attenpt to retrieve the list of DbMyGeotabVersionInfo entities from the database (which will always return data after Worker.StartAsync() has been executed.
                var dbMyGeotabVersionInfos = await DbMyGeotabVersionInfoService.GetAllAsync(connectionInfo, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                return true;
            }
            catch (Exception)
            {
                // Basic API call not possible.
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the MyGeotab API is accessible.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> IsMyGeotabAccessibleAsync()
        {
            try
            {
                // Attenpt authentication to the MyGeotab API.
                await Globals.AuthenticateMyGeotabApiAsync();
                return true;
            }
            catch (Exception)
            {
                // Basic API call not possible.
                return false;
            }
        }
    }

    /// <summary>
    /// A list of possible application states for use by the <see cref="StateMachine"/>.
    /// </summary>
    public enum State { Normal, Waiting }

    /// <summary>
    /// A list of possible reasons for the current <see cref="State"/> of the <see cref="StateMachine"/>.
    /// </summary>
    public enum StateReason { MyGeotabNotAvailable, DatabaseNotAvailable }
}

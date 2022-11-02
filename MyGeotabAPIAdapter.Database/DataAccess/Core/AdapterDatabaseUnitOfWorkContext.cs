using Microsoft.Data.SqlClient;
using NLog;
using System;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;
using static MyGeotabAPIAdapter.Database.ConnectionInfo;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A class that facilitates interaction between repository classes (that perform database CRUD operations) and <see cref="UnitOfWork"/> instances (that wrap transactions associated with the CRUD operations). This class should be registered with DI services (i.e. in Program.cs) using the <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.servicecollectionserviceextensions.addtransient?view=dotnet-plat-ext-5.0">AddTransient</see> method.
    /// </summary>
    public class AdapterDatabaseUnitOfWorkContext : IDatabaseUnitOfWorkContext
    {
        readonly IAdapterDatabaseConnectionInfoContainer connectionInfoContainer;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        DbConnection connection;
        SqlConnection sqlConnection;
        [ThreadStatic] UnitOfWork unitOfWork;

        /// <inheritdoc/>
        public Databases Database { get; private set; }

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <summary>
        /// Returns <c>true</c> if the <see cref="UnitOfWork"/> associated with the current <see cref="AdapterDatabaseUnitOfWorkContext"/> instance is neither null nor disposed.
        /// </summary>
        private bool IsUnitOfWorkOpen
        {
            get
            {
                if (unitOfWork == null)
                {
                    return false;
                }
                else
                {
                    return !unitOfWork.IsDisposed;
                }
            }
        }

        /// <inheritdoc/>
        public DataAccessProviderType ProviderType
        {
            get
            {
                return connectionInfoContainer.AdapterDatabaseConnectionInfo.ProviderType;
            }
        }

        /// <inheritdoc/>
        public int TimeoutSecondsForDatabaseTasks { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterDatabaseUnitOfWorkContext"/> class.
        /// </summary>
        /// <param name="connectionInfoContainer">The <see cref="IAdapterDatabaseConnectionInfoContainer"/> containing the information required to create and open a <see cref="DbConnection"/> that will be supplied to the <see cref="UnitOfWork"/> instance to be created and associated with the current <see cref="AdapterDatabaseUnitOfWorkContext"/> instance</param>
        public AdapterDatabaseUnitOfWorkContext(IAdapterDatabaseConnectionInfoContainer connectionInfoContainer)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            Id = System.Guid.NewGuid().ToString();
            this.connectionInfoContainer = connectionInfoContainer;
            this.TimeoutSecondsForDatabaseTasks = connectionInfoContainer.AdapterDatabaseConnectionInfo.TimeoutSecondsForDatabaseTasks;

            logger.Trace($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {Id}] created.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public UnitOfWork CreateUnitOfWork(Databases database)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var uowOpen = IsUnitOfWorkOpen;
            if (uowOpen)
            {
                // Wait up to 2 seconds for UnitOfWork disposal to be completed.
                var currentDateTime = DateTime.UtcNow;
                var endDelayDateTime = currentDateTime.AddSeconds(2);
                while (currentDateTime < endDelayDateTime && uowOpen == true)
                {
                    Task.Delay(25);
                    uowOpen = IsUnitOfWorkOpen;
                }

                if (uowOpen)
                {
                    throw new InvalidOperationException(
                        $"Cannot create a new {nameof(UnitOfWork)} within this {nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {Id}] before the existing {nameof(UnitOfWork)} [Id: {unitOfWork.Id}] is disposed");
                }
            }

            Database = database;

            // Use standard DbConnection by default. If bulk operations are required as part of the UOW, an additional SqlConection can be created.
            InitializeDbConnectionAsync().Wait();
            unitOfWork = new UnitOfWork(connection, this.TimeoutSecondsForDatabaseTasks);

            logger.Debug($"{nameof(UnitOfWork)} [Id: {unitOfWork.Id}] created by {nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {Id}] and database transaction initiated.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return unitOfWork;
        }

        /// <inheritdoc/>
        public DbConnection GetConnection()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (!IsUnitOfWorkOpen)
            {
                throw new InvalidOperationException(
                    $"There is no {nameof(UnitOfWork)} currently associated with this {nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {Id}]. The {nameof(CreateUnitOfWork)} method must be called to create a new {nameof(UnitOfWork)} before calling the {nameof(GetConnection)} method.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return unitOfWork.Connection;
        }

        /// <inheritdoc/>
        public SqlConnection GetSqlConnection()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (!IsUnitOfWorkOpen)
            {
                throw new InvalidOperationException(
                    $"There is no {nameof(UnitOfWork)} currently associated with this {nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {Id}]. The {nameof(CreateUnitOfWork)} method must be called to create a new {nameof(UnitOfWork)} before calling the {nameof(GetSqlConnection)} method.");
            }

            // Open a SqlConnection if one has not yet been opened. 
            if (unitOfWork.SqlConnection == null)
            {
                InitializeSqlConnectionAsync().Wait();
                unitOfWork.SqlConnection = sqlConnection;
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return unitOfWork.SqlConnection;
        }

        /// <inheritdoc/>
        public SqlTransaction GetSqlTransaction()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (!IsUnitOfWorkOpen)
            {
                throw new InvalidOperationException(
                    $"There is no {nameof(UnitOfWork)} currently associated with this {nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {Id}]. The {nameof(CreateUnitOfWork)} method must be called to create a new {nameof(UnitOfWork)} before calling the {nameof(GetSqlTransaction)} method.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return unitOfWork.SqlTransaction;
        }

        /// <inheritdoc/>
        public DbTransaction GetTransaction()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (!IsUnitOfWorkOpen)
            {
                throw new InvalidOperationException(
                    $"There is no {nameof(UnitOfWork)} currently associated with this {nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {Id}]. The {nameof(CreateUnitOfWork)} method must be called to create a new {nameof(UnitOfWork)} before calling the {nameof(GetTransaction)} method.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return unitOfWork.Transaction;
        }

        /// <inheritdoc/>
        public string GetUnitOfWorkId()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (!IsUnitOfWorkOpen)
            {
                throw new InvalidOperationException(
                    $"There is no {nameof(UnitOfWork)} currently associated with this {nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {Id}]. The {nameof(CreateUnitOfWork)} method must be called to create a new {nameof(UnitOfWork)} before calling the {nameof(GetUnitOfWorkId)} method.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return unitOfWork.Id;
        }

        /// <summary>
        /// Creates and opens a <see cref="DbConnection"/> using information from the <see cref="IDataAdapterDatabaseConnectionInfoContainer"/> supplied upon instantiation of the current <see cref="AdapterDatabaseUnitOfWorkContext"/> instance. The <see cref="ConnectionInfo"/> used will be that which is associated with the <see cref="Database"/> identified when the <see cref="CreateUnitOfWork(Databases, int)"/> is called.
        /// </summary>
        /// <returns></returns>
        async Task InitializeDbConnectionAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            ConnectionProvider connectionProvider;
            switch (Database)
            {
                case Databases.AdapterDatabase:
                    connectionProvider = new ConnectionProvider(connectionInfoContainer.AdapterDatabaseConnectionInfo);
                    Database = Databases.AdapterDatabase;
                    break;
                default:
                    throw new NotSupportedException($"The {nameof(Database)} Database is not supported by this method.");
            }

            connection = await connectionProvider.GetOpenConnectionAsync();

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Creates and opens a <see cref="SqlConnection"/> using information from the <see cref="IDataAdapterDatabaseConnectionInfoContainer"/> supplied upon instantiation of the current <see cref="AdapterDatabaseUnitOfWorkContext"/> instance. The <see cref="ConnectionInfo"/> used will be that which is associated with the <see cref="Database"/> identified when the <see cref="CreateUnitOfWork(Databases, int)"/> is called.
        /// </summary>
        /// <returns></returns>
        async Task InitializeSqlConnectionAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            ConnectionProvider connectionProvider;
            switch (Database)
            {
                case Databases.AdapterDatabase:
                    connectionProvider = new ConnectionProvider(connectionInfoContainer.AdapterDatabaseConnectionInfo);
                    Database = Databases.AdapterDatabase;
                    break;
                default:
                    throw new NotSupportedException($"The {nameof(Database)} Database is not supported by this method.");
            }

            sqlConnection = await connectionProvider.GetOpenSqlConnectionAsync();

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}

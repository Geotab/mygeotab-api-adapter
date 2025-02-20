#!/bin/bash
set -e

# Start SQL Server
/opt/mssql/bin/sqlservr &

# Wait for SQL Server to be ready
until /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" -C &> /dev/null
do
  echo "Waiting for SQL Server to start..."
  sleep 1
done

echo "SQL Server is ready"

# Check if database exists - improved query and output handling
DB_EXISTS=$(/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
    -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('geotabadapterdb') IS NOT NULL THEN 1 ELSE 0 END;" \
    -h -1 | tr -d '[:space:]')

if [ "$DB_EXISTS" = "1" ]; then
    echo "Database already exists, skipping initialization"
else
    echo "Database does not exist. Running initialization scripts..."
    
    # Run database creation first
    echo "Running 01-create-database.sql"
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" \
        -i "/docker-entrypoint-initdb.d/01-create-database.sql" -C
    
    # Run user creation with password variable
    echo "Running 02-create-user.sql"
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" \
        -i "/docker-entrypoint-initdb.d/02-create-user.sql" \
        -v DB_PASSWORD="$DB_PASSWORD" -C
    
    # Run schema creation
    echo "Running geotabadapterdb-DatabaseCreationScript.sql"
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" \
        -i "/docker-entrypoint-initdb.d/geotabadapterdb-DatabaseCreationScript.sql" -C
    
    echo "Database initialization complete"
fi

# Keep container running
tail -f /dev/null

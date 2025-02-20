# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first to leverage Docker cache for package restore. They're not all needed, but logic can be switched for different projects.
COPY *.sln .
COPY MyGeotabAPIAdapter/*.csproj MyGeotabAPIAdapter/
COPY MyGeotabAPIAdapter.Configuration/*.csproj MyGeotabAPIAdapter.Configuration/
COPY MyGeotabAPIAdapter.Database/*.csproj MyGeotabAPIAdapter.Database/
COPY MyGeotabAPIAdapter.Database.EntityPersisters/*.csproj MyGeotabAPIAdapter.Database.EntityPersisters/
COPY MyGeotabAPIAdapter.DataOptimizer/*.csproj MyGeotabAPIAdapter.DataOptimizer/
COPY MyGeotabAPIAdapter.Exceptions/*.csproj MyGeotabAPIAdapter.Exceptions/
COPY MyGeotabAPIAdapter.GeotabObjectMappers/*.csproj MyGeotabAPIAdapter.GeotabObjectMappers/
COPY MyGeotabAPIAdapter.Geospatial/*.csproj MyGeotabAPIAdapter.Geospatial/
COPY MyGeotabAPIAdapter.Helpers/*.csproj MyGeotabAPIAdapter.Helpers/
COPY MyGeotabAPIAdapter.Logging/*.csproj MyGeotabAPIAdapter.Logging/
COPY MyGeotabAPIAdapter.MyGeotabAPI/*.csproj MyGeotabAPIAdapter.MyGeotabAPI/
COPY MyGeotabAPIAdapter.Tests/*.csproj MyGeotabAPIAdapter.Tests/

# Restore NuGet packages
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build and publish the DataOptimizer project
RUN dotnet publish MyGeotabAPIAdapter/MyGeotabAPIAdapter.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variables if needed
ENV DOTNET_ENVIRONMENT=Production

# Create a non-root user
RUN useradd -M -s /bin/bash appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "MyGeotabAPIAdapter.dll"]

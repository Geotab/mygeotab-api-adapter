namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class with methods that map database foreign key constraint names to prerequisite background services responsible for providing the related data. This facilitates determination of which service to potentially wait for when a specific foreign key violation occurs due to timing issues between services.
    /// </summary>
    public interface IForeignKeyServiceDependencyMap
    {
        /// <summary>
        /// Attempts to retrieve the prerequisite <see cref="AdapterService"/> associated with
        /// the specified foreign key constraint name.
        /// </summary>
        /// <param name="constraintName">The name of the database foreign key constraint for which to find the associated prerequisite <see cref="AdapterService"/>.</param>
        /// <param name="prerequisiteService">When this method returns <c>true</c>, contains the <see cref="AdapterService"/> identified as the prerequisite (i.e., the service that provides the needed data) for the specified foriegn key constraint.
        /// </param>
        /// <returns>
        /// <c>true</c> if a prerequisite service mapping was found for the specified
        /// <paramref name="constraintName"/>; otherwise, <c>false</c>.
        /// </returns>
        bool TryGetDependency(string constraintName, out AdapterService prerequisiteService);
    }
}
namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class that represents the association of a database foreign key constraint with a prerequisite <see cref="AdapterService"/>.
    /// </summary>
    public class ForeignKeyServiceDependency
    {
        public string ConstraintName { get; }
        public AdapterService PrerequisiteService { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignKeyServiceDependency"/> class.
        /// </summary>
        /// <param name="constraintName">The name of the database foreign key constraint.</param>
        /// <param name="prerequisiteService">The prerequisite <see cref="AdapterService"/> associated with the <paramref name="constraintName"/>.</param>
        public ForeignKeyServiceDependency(string constraintName, AdapterService prerequisiteService)
        {
            ConstraintName = constraintName;
            PrerequisiteService = prerequisiteService;
        }
    }
}

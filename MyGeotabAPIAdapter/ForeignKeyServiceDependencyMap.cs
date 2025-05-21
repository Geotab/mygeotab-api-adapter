using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class with methods that map database foreign key constraint names with prerequisite background services responsible for providing the related data. This facilitates determination of which service to potentially wait for when a specific foreign key violation occurs due to timing issues between services.
    /// </summary>
    public class ForeignKeyServiceDependencyMap : IForeignKeyServiceDependencyMap
    {
        readonly ConcurrentDictionary<string, AdapterService> map =
        new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignKeyServiceDependencyMap"/> class.
        /// </summary>
        /// <param name="foreignKeyServiceDependencies">The list of database foreign key constrain names mapped to prerequisite background services.</param>
        public ForeignKeyServiceDependencyMap(IEnumerable<ForeignKeyServiceDependency> foreignKeyServiceDependencies)
        {
            ArgumentNullException.ThrowIfNull(foreignKeyServiceDependencies);

            foreach (var foreignKeyServiceDependency in foreignKeyServiceDependencies)
            {
                if (foreignKeyServiceDependency == null || string.IsNullOrEmpty(foreignKeyServiceDependency.ConstraintName)) continue;

                map.AddOrUpdate(
                    foreignKeyServiceDependency.ConstraintName,
                    foreignKeyServiceDependency.PrerequisiteService,
                    (key, oldValue) => foreignKeyServiceDependency.PrerequisiteService);
            }
        }

        /// <inheritdoc/>
        public bool TryGetDependency(string constraintName, out AdapterService prerequisiteService)
        {
            return map.TryGetValue(constraintName, out prerequisiteService) && prerequisiteService != AdapterService.NullValue;
        }
    }
}

using Geotab.Checkmate.ObjectModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A container for an in-memory cache with associated attributes.
    /// </summary>
    public class CacheContainer
    {
        // According to <see href="https://geotab.github.io/sdk/software/api/reference/#M:Geotab.Checkmate.Database.DataStore.GetFeed1">GetFeed(...)</see>, 5000 is the lowest feed result limit; thus, it is used here as the default, but should be overridden with the correct limit for the subject entity type.
        const int DefaultFeedResultsLimit = 5000;

        int feedResultsLimit = DefaultFeedResultsLimit;
        long? lastFeedVersion = 0;
        IDictionary cache;
        DateTime lastPropagatedToDatabaseTimeUTC;
        DateTime lastRefreshedTimeUTC;
        DateTime lastUpdatedTimeUTC;

        /// <summary>
        /// The current cache. 
        /// </summary>
        public IDictionary Cache
        {
            get => cache;
            set => cache = value;
        }

        /// <summary>
        /// The results limit to be supplied to the GetFeed() method for the subject <see cref="Entity"/> type.
        /// </summary>
        public int FeedResultsLimit
        {
            get => feedResultsLimit;
            set => feedResultsLimit = value;
        }

        /// <summary>
        /// Indicates whether the <see cref="Cache"/> has had its initial population completed.
        /// </summary>
        public bool InitialCacheRetrievalCompleted
        {
            get
            {
                if (lastRefreshedTimeUTC == DateTime.MinValue && lastUpdatedTimeUTC == DateTime.MinValue)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// The FeedVersion of the latest <see cref="Cache"/>; applies only if the <see cref="Cache"/> is populated via the GetFeed() method.
        /// </summary>
        public long? LastFeedVersion
        {
            get => lastFeedVersion;
            set => lastFeedVersion = value;
        }

        /// <summary>
        /// The last time when the <see cref="Cache"/> was propagated to the database.
        /// </summary>
        public DateTime LastPropagatedToDatabaseTimeUtc
        {
            get => lastPropagatedToDatabaseTimeUTC;
            set => lastPropagatedToDatabaseTimeUTC = value;
        }

        /// <summary>
        /// The last time when the <see cref="Cache"/> was refreshed (i.e. purged and fully re-populated to remove items that have been deleted).
        /// </summary>
        public DateTime LastRefreshedTimeUtc
        {
            get => lastRefreshedTimeUTC;
            set => lastRefreshedTimeUTC = value;
        }

        /// <summary>
        /// The last time when the <see cref="Cache"/> was updated (i.e. items added and/or updated).
        /// </summary>
        public DateTime LastUpdatedTimeUtc
        {
            get => lastUpdatedTimeUTC;
            set => lastUpdatedTimeUTC = value;
        }
    }
}

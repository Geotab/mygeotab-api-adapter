using Geotab.Checkmate.ObjectModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A container for Geotab data feed information related to a single <see cref="Entity"/> type. 
    /// </summary>
    public class FeedContainer
    {
        // According to <see href="https://geotab.github.io/sdk/software/api/reference/#M:Geotab.Checkmate.Database.DataStore.GetFeed1">GetFeed(...)</see>, 5000 is the lowest feed result limit; thus, it is used here as the default, but should be overridden with the correct limit for the subject entity type.
        const int DefaultFeedResultsLimit = 5000;

        bool feedCurrent;
        bool feedEnabled;
        int feedPollingIntervalSeconds;
        IDictionary feedResultData;
        int feedResultsLimit = DefaultFeedResultsLimit;
        Globals.FeedStartOption feedStartOption = Globals.FeedStartOption.FeedVersion;
        DateTime feedStartTimeUTC = DateTime.MinValue;
        DateTime lastFeedRetrievalTimeUTC;
        long? lastFeedVersion = 0;

        /// <summary>
        /// Indicates whether the data feed for the subject <see cref="Entity"/> is up-to-date. 
        /// </summary>
        public bool FeedCurrent
        {
            get => feedCurrent;
            set => feedCurrent = value;
        }

        /// <summary>
        /// Indicates whether data feed polling for the subject <see cref="Entity"/> type is enabled.
        /// </summary>
        public bool FeedEnabled
        {
            get => feedEnabled;
            set => feedEnabled = value;
        }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for the subject <see cref="Entity"/> type.
        /// </summary>
        public int FeedPollingIntervalSeconds
        {
            get => feedPollingIntervalSeconds;
            set => feedPollingIntervalSeconds = value;
        }

        /// <summary>
        /// The latest batch of <see cref="FeedResult{T}"/> data that has been retrieved for the subject <see cref="Entity"/> type.
        /// </summary>
        public IDictionary FeedResultData
        {
            get => feedResultData;
            set => feedResultData = value;
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
        /// Determines whether the next GetFeed() call for the subject <see cref="Entity"/> type will use FromDate or FromVersion.
        /// </summary>
        public Globals.FeedStartOption FeedStartOption
        {
            get => feedStartOption;
            set => feedStartOption = value;
        }

        /// <summary>
        /// The <see cref="DateTime"/> to use as the FromDate when making the next GetFeed() call for the subject <see cref="Entity"/> type if <see cref="FeedStartOption"/> is set to <see cref="Common.FeedStartOption.CurrentTime"/> or <see cref="Common.FeedStartOption.SpecificTime"/>.
        /// </summary>
        public DateTime FeedStartTimeUtc
        {
            get => feedStartTimeUTC;
            set => feedStartTimeUTC = value;
        }

        /// <summary>
        /// The last time the GetFeed() call was executed for the subject <see cref="Entity"/> type. Used in conjunction with <see cref="FeedPollingIntervalSeconds"/> to regulate GetFeed() call frequency.
        /// </summary>
        public DateTime LastFeedRetrievalTimeUtc
        {
            get => lastFeedRetrievalTimeUTC;
            set => lastFeedRetrievalTimeUTC = value;
        }

        /// <summary>
        /// The <see cref="FeedResult{T}.ToVersion"/> returned by the latest GetFeed() call.
        /// </summary>
        public long? LastFeedVersion
        {
            get => lastFeedVersion;
            set => lastFeedVersion = value;
        }

        /// <summary>
        /// Returns the <see cref="FeedResultData"/> values of this <see cref="FeedContainer"/> as a list.
        /// </summary>
        /// <typeparam name="T">Type of entity to return in the list.</typeparam>
        /// <returns></returns>
        public List<T> GetFeedResultDataValuesList<T>() where T : class
        {
            var feedResultDataValues = new List<T>();
            foreach (var item in feedResultData.Values)
            {
                T value = (T)item;
                feedResultDataValues.Add(value);
            }
            return feedResultDataValues;
        }
    }
}

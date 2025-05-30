﻿using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a generic class that handles processing of Geotab <see cref="Entity"/>s that can be classified as "feed data". These object types typically represent data points generated by the telematics devices and vehicles. Examples include <see cref="LogRecord"/>, <see cref="StatusData"/> and <see cref="FaultData"/>.
    /// </summary>
    /// <typeparam name="T">The type of Geotab <see cref="Entity"/> to be processed by this <see cref="IGenericGeotabObjectFeeder{T}"/> instance.</typeparam>
    internal interface IGenericGeotabObjectFeeder<T> where T : class, IEntity
    {
        /// <summary>
        /// The <see cref="AdapterService"/> member associated with <see cref="T"/>.
        /// </summary>
        AdapterService AdapterServiceType { get; }

        /// <summary>
        /// The FeedStartOption configured in appsettings.json. If <see cref="FeedStartOption.CurrentTime"/> or <see cref="FeedStartOption.SpecificTime"/> is specified, that setting is used only the first time data is requested for a given <see cref="Entity"/> type. Once data has been written to the adapter database, this setting will be ignored and <see cref="FeedStartOption.FeedVersion"/> will be used for all subsequent requests. The <see cref="FeedStartOption"/> property shows the <see cref="Configuration.FeedStartOption"/> that is actually in-use at any given point in time.
        /// </summary>
        FeedStartOption ConfiguredFeedStartOption { get; }

        /// <summary>
        /// Indicates whether the data feed for the subject <see cref="Entity"/> is up-to-date. 
        /// </summary>
        bool FeedCurrent { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for the subject <see cref="Entity"/> type.
        /// </summary>
        int FeedPollingIntervalSeconds { get; }

        /// <summary>
        /// The latest batch of <see cref="FeedResult{T}"/> data that has been retrieved for the subject <see cref="Entity"/> type.
        /// </summary>
        ConcurrentDictionary<Id, T> FeedResultData { get; }

        /// <summary>
        /// The results limit to be supplied to the GetFeed() method for the subject <see cref="Entity"/> type.
        /// </summary>
        int FeedResultsLimit { get; }

        /// <summary>
        /// Determines whether the next GetFeed() call for the subject <see cref="Entity"/> type will use FromDate or FromVersion.
        /// </summary>
        FeedStartOption FeedStartOption { get; }

        /// <summary>
        /// The <see cref="DateTime"/> to use as the FromDate when making the next GetFeed() call for the subject <see cref="Entity"/> type if <see cref="FeedStartOption"/> is set to <see cref="FeedStartOption.CurrentTime"/> or <see cref="FeedStartOption.SpecificTime"/>.
        /// </summary>
        DateTime FeedStartTimeUtc { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Indicates whether the <see cref="InitializeAsync(CancellationTokenSource, int, int, FeedStartOption, DateTime, long)"/> method has been invoked since the current class instance was created.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// The last time the GetFeed() call was executed for the subject <see cref="Entity"/> type. Used in conjunction with <see cref="FeedPollingIntervalSeconds"/> to regulate GetFeed() call frequency.
        /// </summary>
        DateTime LastFeedRetrievalTimeUtc { get; set; }

        /// <summary>
        /// The <see cref="FeedResult{T}.ToVersion"/> returned by the latest GetFeed() call.
        /// </summary>
        long? LastFeedVersion { get; set; }

        /// <summary>
        /// Retrieves a batch of data from the MyGeotab database via a GetFeed() call and updates the <see cref="FeedResultData"/> with the returned data.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        Task GetFeedDataBatchAsync(CancellationTokenSource cancellationTokenSource);

        /// <summary>
        /// Returns the <see cref="FeedResultData"/> values of this <see cref="IGenericGeotabObjectFeeder"/> as a list.
        /// </summary>
        /// <returns></returns>
        List<T> GetFeedResultDataValuesList();

        /// <summary>
        /// If not already initialized, initializes the current <see cref="IGenericGeotabObjectFeeder<T>"/> instance.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="feedPollingIntervalSeconds">The minimum number of seconds to wait between GetFeed() calls for the subject <see cref="Entity"/> type.</param>
        /// <param name="feedResultsLimit">The results limit to be supplied to the GetFeed() method for the subject <see cref="Entity"/> type.</param>
        /// <param name="lastFeedVersion">The <see cref="FeedResult{T}.ToVersion"/> returned by the latest GetFeed() call.</param>
        /// <returns></returns>
        Task InitializeAsync(CancellationTokenSource cancellationTokenSource, int feedPollingIntervalSeconds, int feedResultsLimit, long? lastFeedVersion);

        /// <summary>
        /// Re-executes the previous GetFeed() call for the subject <see cref="Entity"/> type to retrieve the same batch of entities.
        /// This is typically used when a database rollback operation has occurred and the last batch of entities needs to be reprocessed.
        /// When this method is called, the following actions are taken:
        /// <list type="bullet">
        /// <item>
        /// <description><see cref="LastFeedVersion"/> is updated to the value of <paramref name="lastProcessedFeedVersion"/>.</description>
        /// </item>
        /// <item>
        /// <description><see cref="LastFeedRetrievalTimeUtc"/> is updated to <see cref="DateTime.MinValue"/>.</description>
        /// </item>
        /// <item>
        /// <description>If <paramref name="lastProcessedFeedVersion"/> is null and <see cref="FeedStartOption"/> differs from <see cref="ConfiguredFeedStartOption"/>, <see cref="FeedStartOption"/> is changed back to <see cref="ConfiguredFeedStartOption"/>.</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="lastProcessedFeedVersion">The last feed version that was successfully processed before the rollback. This value will be used to update <see cref="LastFeedVersion"/>.</param>

        void Rollback(long? lastProcessedFeedVersion);
    }
}

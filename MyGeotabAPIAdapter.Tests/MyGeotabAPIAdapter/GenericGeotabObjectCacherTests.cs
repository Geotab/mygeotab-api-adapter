using Geotab.Checkmate.ObjectModel;
using Moq;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.MyGeotabAPI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Tests covering the feed-paging behaviour of <see cref="GenericGeotabObjectCacher{T}"/> - specifically the
    /// conditions under which cache population stops.
    /// </summary>
    public class GenericGeotabObjectCacherTests
    {
        const int TestFeedResultsLimit = 5;
        const int TestCacheUpdateIntervalMinutes = 60;
        const int TestCacheRefreshIntervalMinutes = 1440;

        static User BuildUser(string id) => new() { Id = Id.Create(id) };

        /// <summary>
        /// Builds a <see cref="FeedResult{T}"/> containing <paramref name="recordCount"/> distinct <see cref="User"/>s.
        /// </summary>
        static FeedResult<User> BuildFeedResult(long toVersion, int recordCount, int firstIdNumber)
        {
            var data = new List<User>();
            for (var i = 0; i < recordCount; i++)
            {
                data.Add(BuildUser($"b{firstIdNumber + i}"));
            }
            return new FeedResult<User> { Data = data, ToVersion = toVersion };
        }

        /// <summary>
        /// Creates a cacher whose GetFeed calls return <paramref name="feedResults"/> in order. Once exhausted, further
        /// calls return an empty batch so that a correctly-terminating loop always ends.
        /// </summary>
        static (GenericGeotabObjectCacher<User> Cacher, Mock<IMyGeotabAPIHelper> ApiHelper) CreateCacher(
            IEnumerable<FeedResult<User>> feedResults, IList<User> getResults = null)
        {
            var adapterConfiguration = new Mock<IAdapterConfiguration>();
            adapterConfiguration.Setup(configuration => configuration.TimeoutSecondsForMyGeotabTasks).Returns(30);
            adapterConfiguration.Setup(configuration => configuration.TrackZoneStops).Returns(true);

            // Always report the minimum inter-call interval as elapsed so that tests do not incur real delays.
            var dateTimeHelper = new Mock<IDateTimeHelper>();
            dateTimeHelper
                .Setup(helper => helper.TimeIntervalHasElapsed(It.IsAny<DateTime>(), It.IsAny<DateTimeIntervalType>(), It.IsAny<int>()))
                .Returns(true);

            var remainingFeedResults = new Queue<FeedResult<User>>(feedResults);
            var apiHelper = new Mock<IMyGeotabAPIHelper>();
            apiHelper
                .Setup(helper => helper.GetFeedAsync<User>(It.IsAny<long?>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(() => remainingFeedResults.Count > 0
                    ? remainingFeedResults.Dequeue()
                    : BuildFeedResult(long.MaxValue, 0, 0));
            apiHelper
                .Setup(helper => helper.GetAsync<User>(It.IsAny<Search>(), It.IsAny<int>()))
                .ReturnsAsync(() => getResults ?? new List<User>());

            var cacher = new GenericGeotabObjectCacher<User>(adapterConfiguration.Object, dateTimeHelper.Object, apiHelper.Object);
            return (cacher, apiHelper);
        }

        static Task InitializeAsync(GenericGeotabObjectCacher<User> cacher, CancellationTokenSource cancellationTokenSource, bool useGetFeed) =>
            cacher.InitializeAsync(cancellationTokenSource, DateTime.UtcNow.Date, TestCacheUpdateIntervalMinutes,
                TestCacheRefreshIntervalMinutes, TestFeedResultsLimit, useGetFeed);

        /// <summary>
        /// A batch containing fewer records than the results limit does not mean the feed has been exhausted, because
        /// MyGeotab may apply the results limit before filtering the records it returns. Population must continue until
        /// an empty batch is received, otherwise the cache is silently left incomplete.
        /// </summary>
        [Fact(Timeout = 30000)]
        public async Task InitializeAsync_ShortBatchBeforeExhaustion_ContinuesUntilFeedIsEmpty()
        {
            var feedResults = new[]
            {
                BuildFeedResult(100, 3, 1),   // Short batch (3 < 5) that is NOT the end of the feed.
                BuildFeedResult(200, 2, 4),
                BuildFeedResult(300, 0, 0)
            };
            var (cacher, apiHelper) = CreateCacher(feedResults);
            using var cancellationTokenSource = new CancellationTokenSource();

            await InitializeAsync(cacher, cancellationTokenSource, useGetFeed: true);

            Assert.Equal(5, cacher.GeotabObjectCache.Count);
            apiHelper.Verify(
                helper => helper.GetFeedAsync<User>(It.IsAny<long?>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Exactly(3));
        }

        /// <summary>
        /// A batch exactly at the results limit must not be treated as the end of the feed either.
        /// </summary>
        [Fact(Timeout = 30000)]
        public async Task InitializeAsync_BatchAtResultsLimit_ContinuesUntilFeedIsEmpty()
        {
            var feedResults = new[]
            {
                BuildFeedResult(100, TestFeedResultsLimit, 1),
                BuildFeedResult(200, 0, 0)
            };
            var (cacher, apiHelper) = CreateCacher(feedResults);
            using var cancellationTokenSource = new CancellationTokenSource();

            await InitializeAsync(cacher, cancellationTokenSource, useGetFeed: true);

            Assert.Equal(TestFeedResultsLimit, cacher.GeotabObjectCache.Count);
            apiHelper.Verify(
                helper => helper.GetFeedAsync<User>(It.IsAny<long?>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Exactly(2));
        }

        /// <summary>
        /// The Get retrieval path returns every object in a single call and has no version cursor to advance, so it must
        /// stop after one call rather than repeating the same request.
        /// </summary>
        [Fact(Timeout = 30000)]
        public async Task InitializeAsync_WhenNotUsingGetFeed_RetrievesOnceAndStops()
        {
            var getResults = new List<User> { BuildUser("b1"), BuildUser("b2") };
            var (cacher, apiHelper) = CreateCacher(Array.Empty<FeedResult<User>>(), getResults);
            using var cancellationTokenSource = new CancellationTokenSource();

            await InitializeAsync(cacher, cancellationTokenSource, useGetFeed: false);

            Assert.Equal(2, cacher.GeotabObjectCache.Count);
            apiHelper.Verify(helper => helper.GetAsync<User>(It.IsAny<Search>(), It.IsAny<int>()), Times.Once);
            apiHelper.Verify(
                helper => helper.GetFeedAsync<User>(It.IsAny<long?>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        /// <summary>
        /// If the feed returns records without advancing its version, continuing would repeat the same request
        /// indefinitely. Population must stop instead.
        /// </summary>
        [Fact(Timeout = 30000)]
        public async Task InitializeAsync_WhenFeedVersionDoesNotAdvance_StopsInsteadOfLoopingIndefinitely()
        {
            var feedResults = new[]
            {
                BuildFeedResult(100, 2, 1),
                BuildFeedResult(100, 2, 3)    // Same version as the previous batch.
            };
            var (cacher, apiHelper) = CreateCacher(feedResults);
            using var cancellationTokenSource = new CancellationTokenSource();

            await InitializeAsync(cacher, cancellationTokenSource, useGetFeed: true);

            Assert.Equal(4, cacher.GeotabObjectCache.Count);
            apiHelper.Verify(
                helper => helper.GetFeedAsync<User>(It.IsAny<long?>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Exactly(2));
        }

        /// <summary>
        /// An empty first batch means there is nothing to cache and only one call should be made.
        /// </summary>
        [Fact(Timeout = 30000)]
        public async Task InitializeAsync_WhenFeedIsEmpty_CachesNothing()
        {
            var feedResults = new[] { BuildFeedResult(100, 0, 0) };
            var (cacher, apiHelper) = CreateCacher(feedResults);
            using var cancellationTokenSource = new CancellationTokenSource();

            await InitializeAsync(cacher, cancellationTokenSource, useGetFeed: true);

            Assert.Empty(cacher.GeotabObjectCache);
            apiHelper.Verify(
                helper => helper.GetFeedAsync<User>(It.IsAny<long?>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Once);
        }
    }
}

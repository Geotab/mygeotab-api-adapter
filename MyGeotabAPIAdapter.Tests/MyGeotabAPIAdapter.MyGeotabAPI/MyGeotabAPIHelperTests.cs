using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Moq;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyGeotabAPI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="MyGeotabAPIHelper"/> class, focused on the handling of null results
    /// returned by MyGeotab API calls (observed in the field during MyGeotab-side maintenance/failover
    /// windows). A null result must surface as a <see cref="MyGeotabConnectionException"/> so that the
    /// adapter's connectivity-restoration logic responds; a bare <see cref="Exception"/> would instead be
    /// treated as fatal and terminate the adapter process.
    /// </summary>
    public class MyGeotabAPIHelperTests
    {
        private readonly Mock<IApi> _mockApi;
        private readonly MyGeotabAPIHelper _helper;

        public MyGeotabAPIHelperTests()
        {
            _mockApi = new Mock<IApi>();
            _helper = new MyGeotabAPIHelper(new ExceptionHelper())
            {
                MyGeotabAPI = _mockApi.Object
            };
        }

        [Fact]
        public async Task GetAsync_ThrowsMyGeotabConnectionException_WhenResultIsNull()
        {
            _mockApi.Setup(api => api.CallAsync<IList<Device>>("Get", typeof(Device), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IList<Device>)null);

            var exception = await Assert.ThrowsAsync<MyGeotabConnectionException>(() => _helper.GetAsync<Device>());
            Assert.Contains("failed to return a result", exception.Message);
        }

        [Fact]
        public async Task GetFeedAsync_FromDate_ThrowsMyGeotabConnectionException_WhenResultIsNull()
        {
            _mockApi.Setup(api => api.CallAsync<FeedResult<StatusData>>("GetFeed", typeof(StatusData), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FeedResult<StatusData>)null);

            await Assert.ThrowsAsync<MyGeotabConnectionException>(() => _helper.GetFeedAsync<StatusData>(fromDate: DateTime.UtcNow));
        }

        [Fact]
        public async Task GetFeedAsync_FromVersion_ThrowsMyGeotabConnectionException_WhenResultIsNull()
        {
            _mockApi.Setup(api => api.CallAsync<FeedResult<StatusData>>("GetFeed", typeof(StatusData), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FeedResult<StatusData>)null);

            var exception = await Assert.ThrowsAsync<MyGeotabConnectionException>(() => _helper.GetFeedAsync<StatusData>(fromVersion: 12345L));
            Assert.Contains("GetFeedAsync<T>(fromVersion...) method failed to return a result for entity type 'StatusData'", exception.Message);
        }

        [Fact]
        public async Task GetVersionInformationAsync_ThrowsMyGeotabConnectionException_WhenResultIsNull()
        {
            _mockApi.Setup(api => api.CallAsync<VersionInformation>("GetVersionInformation", It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((VersionInformation)null);

            await Assert.ThrowsAsync<MyGeotabConnectionException>(() => _helper.GetVersionInformationAsync());
        }

        [Fact]
        public async Task AddAsync_ThrowsMyGeotabConnectionException_WhenResultIsNull()
        {
            _mockApi.Setup(api => api.CallAsync<Id>("Add", typeof(StatusData), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Id)null);

            await Assert.ThrowsAsync<MyGeotabConnectionException>(() => _helper.AddAsync(new StatusData()));
        }

        [Fact]
        public async Task GetAsync_ReturnsResult_WhenResultIsNotNull()
        {
            IList<Device> devices = new List<Device>();
            _mockApi.Setup(api => api.CallAsync<IList<Device>>("Get", typeof(Device), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(devices);

            var result = await _helper.GetAsync<Device>();

            Assert.Same(devices, result);
        }

        [Fact]
        public async Task GetFeedAsync_FromVersion_ReturnsResult_WhenResultIsNotNull()
        {
            var feedResult = new FeedResult<StatusData>();
            _mockApi.Setup(api => api.CallAsync<FeedResult<StatusData>>("GetFeed", typeof(StatusData), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(feedResult);

            var result = await _helper.GetFeedAsync<StatusData>(fromVersion: 12345L);

            Assert.Same(feedResult, result);
        }
    }
}

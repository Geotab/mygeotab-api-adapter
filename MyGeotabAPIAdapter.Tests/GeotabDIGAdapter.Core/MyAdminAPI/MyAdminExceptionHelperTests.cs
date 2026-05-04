using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Sockets;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyAdminAPI;
using Xunit;

namespace MyGeotabAPIAdapter.Tests.GeotabDIGAdapter.Core.MyAdminAPI
{
    public class MyAdminExceptionHelperTests
    {
        readonly MyAdminExceptionHelper sut = new();

        // --- ExceptionIsRelatedToMyAdminConnectivityLoss: should return TRUE ---

        [Fact]
        public void ExceptionIsRelatedToMyAdminConnectivityLoss_HttpRequestException_ReturnsTrue()
        {
            var exception = new HttpRequestException("Connection refused");
            Assert.True(sut.ExceptionIsRelatedToMyAdminConnectivityLoss(exception));
        }

        [Fact]
        public void ExceptionIsRelatedToMyAdminConnectivityLoss_TaskCanceledException_ReturnsTrue()
        {
            var exception = new TaskCanceledException("The operation was canceled.");
            Assert.True(sut.ExceptionIsRelatedToMyAdminConnectivityLoss(exception));
        }

        [Fact]
        public void ExceptionIsRelatedToMyAdminConnectivityLoss_OperationCanceledException_ReturnsTrue()
        {
            var exception = new OperationCanceledException("The operation was canceled.");
            Assert.True(sut.ExceptionIsRelatedToMyAdminConnectivityLoss(exception));
        }

        [Fact]
        public void ExceptionIsRelatedToMyAdminConnectivityLoss_IOExceptionWithSocketException_ReturnsTrue()
        {
            var innerException = new SocketException((int)SocketError.ConnectionRefused);
            var exception = new IOException("Unable to read data from the transport connection.", innerException);
            Assert.True(sut.ExceptionIsRelatedToMyAdminConnectivityLoss(exception));
        }

        [Fact]
        public void ExceptionIsRelatedToMyAdminConnectivityLoss_IOExceptionWithEndOfStreamException_ReturnsTrue()
        {
            var innerException = new EndOfStreamException("Attempted to read past the end of the stream.");
            var exception = new IOException("Unable to read data from the transport connection.", innerException);
            Assert.True(sut.ExceptionIsRelatedToMyAdminConnectivityLoss(exception));
        }

        [Fact]
        public void ExceptionIsRelatedToMyAdminConnectivityLoss_ServiceUnavailableMessage_ReturnsTrue()
        {
            var exception = new Exception("Service temporarily unavailable");
            Assert.True(sut.ExceptionIsRelatedToMyAdminConnectivityLoss(exception));
        }

        // --- ExceptionIsRelatedToMyAdminConnectivityLoss: should return FALSE ---

        [Fact]
        public void ExceptionIsRelatedToMyAdminConnectivityLoss_ArgumentException_ReturnsFalse()
        {
            var exception = new ArgumentException("Invalid argument");
            Assert.False(sut.ExceptionIsRelatedToMyAdminConnectivityLoss(exception));
        }

        [Fact]
        public void ExceptionIsRelatedToMyAdminConnectivityLoss_InvalidOperationException_ReturnsFalse()
        {
            var exception = new InvalidOperationException("Invalid operation");
            Assert.False(sut.ExceptionIsRelatedToMyAdminConnectivityLoss(exception));
        }

        [Fact]
        public void ExceptionIsRelatedToMyAdminConnectivityLoss_GenericException_ReturnsFalse()
        {
            var exception = new Exception("Some unexpected error");
            Assert.False(sut.ExceptionIsRelatedToMyAdminConnectivityLoss(exception));
        }

        [Fact]
        public void ExceptionIsRelatedToMyAdminConnectivityLoss_MyAdminExceptionMessage_ReturnsFalse()
        {
            var exception = new Exception("MyAdminException: illegal_field_value");
            Assert.False(sut.ExceptionIsRelatedToMyAdminConnectivityLoss(exception));
        }

        [Fact]
        public void ExceptionIsRelatedToMyAdminConnectivityLoss_IOExceptionWithoutRelevantInner_ReturnsFalse()
        {
            var exception = new IOException("Some I/O error");
            Assert.False(sut.ExceptionIsRelatedToMyAdminConnectivityLoss(exception));
        }

        // --- ConnectivityIssueDetectedIncludingMyAdmin ---

        [Fact]
        public void ConnectivityIssueDetectedIncludingMyAdmin_WithMyAdminConnectionException_ReturnsTrue()
        {
            var aggregateException = new AggregateException(
                new MyAdminConnectionException("MyAdmin API connectivity lost."));
            Assert.True(sut.ConnectivityIssueDetectedIncludingMyAdmin(aggregateException));
        }

        [Fact]
        public void ConnectivityIssueDetectedIncludingMyAdmin_WithAdapterDatabaseConnectionException_ReturnsTrue()
        {
            var aggregateException = new AggregateException(
                new AdapterDatabaseConnectionException("Database connectivity lost."));
            Assert.True(sut.ConnectivityIssueDetectedIncludingMyAdmin(aggregateException));
        }

        [Fact]
        public void ConnectivityIssueDetectedIncludingMyAdmin_WithGenericException_ReturnsFalse()
        {
            var aggregateException = new AggregateException(
                new Exception("Some unexpected error"));
            Assert.False(sut.ConnectivityIssueDetectedIncludingMyAdmin(aggregateException));
        }

        // --- IsNonRetryableException (via MyAdminAPIResilienceHelper) ---

        [Fact]
        public void IsNonRetryableException_ArgumentException_ReturnsTrue()
        {
            var exception = new ArgumentException("Invalid argument");
            Assert.True(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_InvalidOperationException_ReturnsTrue()
        {
            var exception = new InvalidOperationException("Invalid operation");
            Assert.True(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_BadRequestMessage_ReturnsTrue()
        {
            var exception = new Exception("400 Bad Request: illegal_field_value");
            Assert.True(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_UnauthorizedMessage_ReturnsTrue()
        {
            var exception = new Exception("401 Unauthorized");
            Assert.True(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_ForbiddenMessage_ReturnsTrue()
        {
            var exception = new Exception("403 Forbidden");
            Assert.True(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_DuplicateEntityErrorMessage_ReturnsTrue()
        {
            var exception = new Exception("MyAdminException: duplicate_entity_error");
            Assert.True(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_TransientError_ReturnsFalse()
        {
            var exception = new HttpRequestException("Connection refused");
            Assert.False(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_TimeoutMessage_ReturnsFalse()
        {
            var exception = new Exception("The operation has timed out.");
            Assert.False(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_GenericException_ReturnsFalse()
        {
            var exception = new Exception("Some unknown error from SDK");
            Assert.False(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        // --- IsNonRetryableException: MYA WHITELISTED_TYPES ---

        [Fact]
        public void IsNonRetryableException_SecurityExceptionMessage_ReturnsTrue()
        {
            var exception = new Exception("SecurityException: caller lacks permission");
            Assert.True(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_DuplicateExceptionMessage_ReturnsTrue()
        {
            var exception = new Exception("DuplicateException: entity already exists");
            Assert.True(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_InvalidDataExceptionMessage_ReturnsTrue()
        {
            var exception = new Exception("InvalidDataException: malformed data in request");
            Assert.True(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_UserAuthenticationExceptionMessage_ReturnsTrue()
        {
            var exception = new Exception("UserAuthenticationException: auth failure");
            Assert.True(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }

        [Fact]
        public void IsNonRetryableException_GenericRequestErrorMessage_ReturnsFalse()
        {
            // GenericRequestError is transient (wraps NpgsqlException, IOException, etc.) and SHOULD be retried.
            var exception = new Exception("GenericRequestError: something went wrong");
            Assert.False(MyGeotabAPIAdapter.MyAdminAPI.MyAdminAPIResilienceHelper.IsNonRetryableException(exception));
        }
    }
}

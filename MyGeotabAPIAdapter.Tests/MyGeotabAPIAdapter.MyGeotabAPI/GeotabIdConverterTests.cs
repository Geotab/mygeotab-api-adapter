using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyGeotabAPI;
using System;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="GeotabIdConverter"/> class.
    /// </summary>
    public class GeotabIdConverterTests
    {
        private readonly IExceptionHelper _exceptionHelper;
        private readonly GeotabIdConverter _converter;

        public GeotabIdConverterTests()
        {
            _exceptionHelper = new ExceptionHelper();
            _converter = new GeotabIdConverter(_exceptionHelper);
        }

        [Fact]
        public void ToGuid_FromId_ThrowsArgumentNullException_ForNullOrEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => _converter.ToGuid((Id.Null)));
        }

        [Fact]
        public void ToGuid_FromString_ThrowsArgumentNullException_ForNullOrEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => _converter.ToGuid((string)null));
            Assert.Throws<ArgumentNullException>(() => _converter.ToGuid(string.Empty));
        }

        [Fact]
        public void ToGuid_FromId_ThrowsArgumentOutOfRangeException_ForInvalidPrefix()
        {
            var invalidId = Id.Create(long.MaxValue);
            Assert.Throws<ArgumentException>(() => _converter.ToGuid(invalidId));
        }

        [Fact]
        public void ToGuid_FromString_ThrowsArgumentOutOfRangeException_ForInvalidPrefix()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _converter.ToGuid("b1234567890123456789012"));
        }

        [Fact]
        public void ToGuid_FromId_ThrowsArgumentException_ForInvalidLength()
        {
            var invalidIdShort = Id.Create("a12345678901234567890");
            var invalidIdLong = Id.Create("a12345678901234567890123");
            var invalidId = Id.Create(long.MaxValue);

            Assert.Throws<ArgumentException>(() => _converter.ToGuid(invalidIdShort));
            Assert.Throws<ArgumentException>(() => _converter.ToGuid(invalidIdLong));
            Assert.Throws<ArgumentException>(() => _converter.ToGuid(invalidId));
        }

        [Fact]
        public void ToGuid_FromString_ThrowsArgumentException_ForInvalidLength()
        {
            Assert.Throws<ArgumentException>(() => _converter.ToGuid("a12345678901234567890")); // Length 21
            Assert.Throws<ArgumentException>(() => _converter.ToGuid("a12345678901234567890123")); // Length 24
        }

        [Fact]
        public void ToGuid_ReturnsCorrectGuid_ForValidInput()
        {
            var validId = Id.Create("aMTIzNDU2Nzg5MDEyMzQ1Ng");
            var expectedGuid = new Guid("34333231-3635-3837-3930-313233343536");
            var result = _converter.ToGuid(validId);
            Assert.Equal(expectedGuid, result);
        }

        [Fact]
        public void ToLong_FromId_ThrowsArgumentNullException_ForNullOrEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => _converter.ToLong(Id.Null));
        }

        [Fact]
        public void ToLong_FromString_ThrowsArgumentNullException_ForNullOrEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => _converter.ToLong((string)null));
            Assert.Throws<ArgumentNullException>(() => _converter.ToLong(string.Empty));
        }

        [Fact]
        public void ToLong_FromId_ThrowsArgumentOutOfRangeException_ForInvalidPrefix()
        {
            var invalidId = Id.Create("a1234567890abcdef");
            Assert.Throws<ArgumentOutOfRangeException>(() => _converter.ToLong(invalidId));
        }

        [Fact]
        public void ToLong_FromString_ThrowsArgumentOutOfRangeException_ForInvalidPrefix()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _converter.ToLong("a1234567890abcdef"));
        }

        [Fact]
        public void ToLong_ReturnsCorrectLong_ForValidInput()
        {
            var validId = Id.Create("b123456789ABCDEF");
            var expectedLong = 0x123456789ABCDEF;
            var result = _converter.ToLong(validId);
            Assert.Equal(expectedLong, result);
        }

        [Fact]
        public void TryToGuid_FromId_ThrowsArgumentNullException_ForNull()
        {
            Assert.Throws<ArgumentNullException>(() => _converter.TryToGuid((Id.Null)));
        }

        [Fact]
        public void TryToGuid_FromId_ReturnsGuid_ForValidInput()
        {
            var validId = Id.Create("aMTIzNDU2Nzg5MDEyMzQ1Ng");
            var expectedGuid = new Guid("34333231-3635-3837-3930-313233343536");
            var result = _converter.TryToGuid(validId);
            Assert.Equal(expectedGuid, result);
        }

        [Fact]
        public void TryToGuid_FromString_ThrowsArgumentNullException_ForNullOrEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => _converter.TryToGuid((string)null));
            Assert.Throws<ArgumentNullException>(() => _converter.TryToGuid(string.Empty));
        }

        [Fact]
        public void TryToGuid_FromString_ReturnsNull_ForInvalidPrefix()
        {
            var result = _converter.TryToGuid("bMTIzNDU2Nzg5MDEyMzQ1Ng");
            Assert.Null(result);
        }

        [Fact]
        public void TryToGuid_FromString_ReturnsNull_ForInvalidLength()
        {
            var result = _converter.TryToGuid("aMTIzNDU");
            Assert.Null(result);
        }

        [Fact]
        public void TryToGuid_FromString_ReturnsNull_ForExceptionDuringConversion()
        {
            // Invalid character "!"
            var invalidId = "aMTIzNDU2Nzg5MDEyMzQ1N!"; 
            var result = _converter.TryToGuid(invalidId);
            Assert.Null(result);
        }
    }
}

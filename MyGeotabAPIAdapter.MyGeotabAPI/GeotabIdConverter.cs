using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Logging;
using NLog;
using System;
using System.Globalization;

namespace MyGeotabAPIAdapter.MyGeotabAPI
{
    /// <summary>
    /// A class containing methods for converting Geotab <see cref="Id"/>s between various representative types. 
    /// </summary>
    public class GeotabIdConverter : IGeotabIdConverter
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        readonly IExceptionHelper exceptionHelper;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        public GeotabIdConverter(IExceptionHelper exceptionHelper)
        {
            this.exceptionHelper = exceptionHelper;
        }

        /// <inheritdoc/>
        public GeotabIdType GetGeotabIdType(Id geotabId)
        {
            var geotabIdTypeName = geotabId.GetType().Name;
            if (geotabIdTypeName == GeotabIdType.GuidId.Name)
            {
                return GeotabIdType.GuidId;
            }
            else if (geotabIdTypeName == GeotabIdType.LongId.Name)
            {
                return GeotabIdType.LongId;
            }
            else if (geotabIdTypeName == GeotabIdType.NamedGuidId.Name)
            {
                return GeotabIdType.NamedGuidId;
            }
            else if (geotabIdTypeName == GeotabIdType.ShimId.Name)
            {
                return GeotabIdType.ShimId;
            }
            else
            {
                throw new ArgumentException($"The {nameof(geotabId)} type '{geotabIdTypeName}' is not a valid {nameof(GeotabIdType)}.");
            }
        }

        /// <inheritdoc/>
        public Guid ToGuid(Id geotabId)
        {
            if (geotabId == Id.Null)
            {
                throw new ArgumentNullException($"{nameof(geotabId)} cannot be null.");
            }

            var geotabIdType = GetGeotabIdType(geotabId);
            if (geotabIdType != GeotabIdType.GuidId && geotabIdType != GeotabIdType.NamedGuidId)
            {
                throw new ArgumentException($"The {nameof(geotabId)} type '{geotabIdType.Name}' is not a valid {nameof(GeotabIdType)} for converting to a {nameof(Guid)}.");
            }

            return (Guid)geotabId.GetValue();
        }

        /// <inheritdoc/>
        public Guid ToGuid(string geotabId)
        {
            const int GuidIdLength = 23;
            const char GuidIdPrefix = 'a';

            if (string.IsNullOrEmpty(geotabId))
            {
                throw new ArgumentNullException($"{nameof(geotabId)} cannot be null or empty.");
            }

            if (geotabId[0] != GuidIdPrefix)
            {
                throw new ArgumentOutOfRangeException($"The {nameof(geotabId)} '{geotabId}' cannot be converted to a {nameof(Guid)} because it does not begin with '{GuidIdPrefix}'.");
            }

            if (geotabId.Length != GuidIdLength)
            {
                throw new ArgumentException($"The {nameof(geotabId)} '{geotabId}' cannot be converted to a {nameof(Guid)} because its length is not {GuidIdLength}.");
            }

            // Convert the GeotabId to Base64 characters.
            Span<char> chars = stackalloc char[24];
            int i = 0;
            for (; i < 22; i++)
            {
                chars[i] = geotabId[i + 1] switch
                {
                    '_' => '/',
                    '-' => '+',
                    _ => geotabId[i + 1],
                };
            }
            chars[i++] = '=';
            chars[i] = '=';

            // Convert the Base64 characters to a byte array.
            Span<byte> bytes = stackalloc byte[16];
            //Convert.TryFromBase64Chars(chars, bytes, out _);
            if (!Convert.TryFromBase64Chars(chars, bytes, out int bytesWritten) || bytesWritten != 16)
            {
                throw new FormatException("The input string is not a valid Base64 encoded GUID.");
            }

            // Constructs a Guid from the byte array and returns it.
            return new Guid(bytes);
        }

        /// <inheritdoc/>
        public string ToGuidString(Id geotabId)
        {
            if (geotabId == Id.Null)
            {
                throw new ArgumentNullException($"{nameof(geotabId)} cannot be null.");
            }

            return geotabId.GetValue().ToString();
        }

        /// <inheritdoc/>
        public long ToLong(Id geotabId)
        {
            if (geotabId == Id.Null)
            {
                throw new ArgumentNullException($"{nameof(geotabId)} cannot be null.");
            }

            return ToLong(geotabId.ToString());
        }

        /// <inheritdoc/>
        public long ToLong(string geotabId)
        {
            const char GuidIdPrefix = 'b';

            if (string.IsNullOrEmpty(geotabId))
            {
                throw new ArgumentNullException($"{nameof(geotabId)} cannot be null or empty.");
            }

            if (geotabId[0] != GuidIdPrefix)
            {
                throw new ArgumentOutOfRangeException($"The {nameof(geotabId)} '{geotabId}' cannot be converted to a long because it does not begin with '{GuidIdPrefix}'.");
            }

            return long.Parse(geotabId.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public Guid? TryToGuid(Id geotabId)
        {
            if (geotabId == Id.Null)
            {
                throw new ArgumentNullException($"{nameof(geotabId)} cannot be null.");
            }

            return TryToGuid(geotabId.ToString());
        }

        /// <inheritdoc/>
        public Guid? TryToGuid(string geotabId)
        {
            const int GuidIdLength = 23;
            const char GuidIdPrefix = 'a';

            if (string.IsNullOrEmpty(geotabId))
            {
                throw new ArgumentNullException($"{nameof(geotabId)} cannot be null or empty.");
            }

            // If the geotabId starts with 'a' and is the correct length, try to convert it to a Guid. Since it's possible that the geotabId is a Diagnostic with a ShimId, it's also possible that it can't be converted to a Guid even though these two checks passed. In that case, log the exception and return null.
            if (geotabId[0] == GuidIdPrefix && geotabId.Length == GuidIdLength)
            {
                try
                {
                    return ToGuid(geotabId);
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
            }
            return null;
        }
    }
}

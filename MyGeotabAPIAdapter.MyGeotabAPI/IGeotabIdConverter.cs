using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using System;

namespace MyGeotabAPIAdapter.MyGeotabAPI
{
    /// <summary>
    /// Interface for a class containing methods for converting Geotab <see cref="Id"/>s between various representative types. 
    /// </summary>
    public interface IGeotabIdConverter
    {
        /// <summary>
        /// Gets the <see cref="GeotabIdType"/> of the <paramref name="geotabId"/>.
        /// </summary>
        /// <param name="geotabId">The <see cref="Id"/> for which to retrieve the <see cref="GeotabIdType"/>.</param>
        /// <returns></returns>
        GeotabIdType GetGeotabIdType(Id geotabId);

        /// <summary>
        /// Decodes the <paramref name="geotabId"/> into its <see cref="Guid"/> format. NOTE: Only <paramref name="geotabId"/> values with a leading "a" can be decoded into <see cref="Guid"/>s.
        /// </summary>
        /// <param name="geotabId">The <see cref="Id"/> to be decoded.</param>
        /// <returns>
        /// The decoded <see cref="Guid"/> <see cref="Id"/> value. If <paramref name="geotabId"/> cannot be converted to a <see cref="Guid"/>, an exception is thrown.
        /// </returns>
        Guid ToGuid(Id geotabId);

        /// <summary>
        /// Decodes the <paramref name="geotabId"/> into its <see cref="Guid"/> format. NOTE: Only <paramref name="geotabId"/> values with a leading "a" can be decoded into <see cref="Guid"/>s.
        /// </summary>
        /// <param name="geotabId">The string-encoded <see cref="Id"/> to be decoded.</param>
        /// <returns>
        /// The decoded <see cref="Guid"/> <see cref="Id"/> value. If <paramref name="geotabId"/> cannot be converted to a <see cref="Guid"/>, an exception is thrown.
        /// </returns>
        Guid ToGuid(string geotabId);

        /// <summary>
        /// Intended for use with <see cref="Diagnostic"/>s if TryToGuid or TryToGuid2 returns null (in which case the <see cref="Id"/> is likely a <see cref="ShimId"/>).
        /// </summary>
        /// <param name="geotabId">The string-encoded <see cref="Id"/> to be decoded.</param>
        /// <returns>The value of the <paramref name="geotabId"/> as a <see cref="string"/>.</returns>
        string ToGuidString(Id geotabId);

        /// <summary>
        /// Decodes the <paramref name="geotabId"/> into its <see cref="long"/> format. NOTE: Only <paramref name="geotabId"/> values with a leading "b" can be decoded into <see cref="long"/>s.
        /// </summary>
        /// <param name="geotabId">The <see cref="Id"/> to be decoded.</param>
        /// <returns>
        /// The decoded <see cref="long"/> <see cref="Id"/> value. If <paramref name="geotabId"/> cannot be converted to a <see cref="long"/>, an exception is thrown.
        /// </returns>
        long ToLong(Id geotabId);

        /// <summary>
        /// Decodes the <paramref name="geotabId"/> into its <see cref="long"/> format. NOTE: Only <paramref name="geotabId"/> values with a leading "b" can be decoded into <see cref="long"/>s.
        /// </summary>
        /// <param name="geotabId">The string-encoded <see cref="Id"/> to be decoded.</param>
        /// <returns>
        /// The decoded <see cref="long"/> <see cref="Id"/> value. If <paramref name="geotabId"/> cannot be converted to a <see cref="long"/>, an exception is thrown.
        /// </returns>
        long ToLong(string geotabId);

        /// <summary>
        /// Attempts to decode the <paramref name="geotabId"/> into its <see cref="Guid"/> format. NOTE: Only <paramref name="geotabId"/> values with a leading "a" can be decoded into <see cref="Guid"/>s.
        /// </summary>
        /// <param name="geotabId">The <see cref="Id"/> to be decoded.</param>
        /// <returns>
        /// The decoded <see cref="Guid"/> <see cref="Id"/> value or null if decoding is not possible. If <paramref name="geotabId"/> is null or empty, an exception is thrown.
        /// </returns>
        Guid? TryToGuid(Id geotabId);

        /// <summary>
        /// Attempts to decode the <paramref name="geotabId"/> into its <see cref="Guid"/> format. NOTE: Only <paramref name="geotabId"/> values with a leading "a" can be decoded into <see cref="Guid"/>s.
        /// </summary>
        /// <param name="geotabId">The string-encoded <see cref="Id"/> to be decoded.</param>
        /// <returns>
        /// The decoded <see cref="Guid"/> <see cref="Id"/> value or null if decoding is not possible. If <paramref name="geotabId"/> is null or empty, an exception is thrown.
        /// </returns>
        Guid? TryToGuid(string geotabId);
    }
}

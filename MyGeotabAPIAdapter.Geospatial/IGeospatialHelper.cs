#nullable enable
using System;

namespace MyGeotabAPIAdapter.Geospatial
{
    /// <summary>
    /// Interface for a helper class to assist in working with geospatial data.
    /// </summary>
    public interface IGeospatialHelper
    {
        /// <summary>
        /// Calculates and returns the <see href="https://en.wikipedia.org/wiki/Bearing_(angle)">bearing</see> between the supplied origin and destination points.
        /// </summary>
        /// <param name="originLongitude">The longitude coordinate of the origin point.</param>
        /// <param name="originLatitude">The latitude coordinate of the origin point.</param>
        /// <param name="destinationLongitude">The longitude coordinate of the destination point.</param>
        /// <param name="destinationLatitude">The latitude coordinate of the destination point.</param>
        /// <returns></returns>
        double GetBearingBetweenPoints(double originLongitude, double originLatitude, double destinationLongitude, double destinationLatitude);

        /// <summary>
        /// Returns, for the supplied <paramref name="bearing"/>, the <see cref="CompassDirection.Direction"/> of the <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see> associated with the <paramref name="compassRoseType"/> or null if one cannot be found.
        /// </summary>
        /// <param name="bearing">The bearing for which to retrieve the associated <see cref="CompassDirection.Direction"/>.</param>
        /// <param name="compassRoseType">The <see cref="CompassRoseType"/> for which to retrieve the <see cref="CompassDirection.Direction"/> associated with the supplied <paramref name="bearing"/>.</param>
        /// <returns></returns>
        string? GetCompassDirection(double bearing, CompassRoseType compassRoseType);

        /// <summary>
        /// Returns, for the supplied <paramref name="bearing"/>, the <see cref="CompassDirection.Direction"/> of the <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see> associated with the <paramref name="compassRoseType"/> or null if one cannot be found.
        /// </summary>
        /// <param name="bearing">The bearing for which to retrieve the associated <see cref="CompassDirection.Direction"/>.</param>
        /// <param name="numberOfCompassDirections">The desired number of cardinal directions on the <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see>. Determines the <see cref="CompassRoseType"/> that will be used to retrieve the <see cref="CompassDirection.Direction"/> associated with the supplied <paramref name="bearing"/>.</param>
        /// <returns></returns>
        string? GetCompassDirection(double bearing, int numberOfCompassDirections);

        /// <summary>
        /// Calculates and returns the latitude and longitude coordinates for a point that is <paramref name="distanceToDestinationInMetres"/> metres away from the point defined by the <paramref name="originLongitude"/>/<paramref name="originLatitude"/> coordinates and using the specified <paramref name="azimuth"/>.
        /// </summary>
        /// <param name="originLongitude">The longitude coordinate of the origin point.</param>
        /// <param name="originLatitude">The latitude coordinate of the origin point.</param>
        /// <param name="azimuth">The azimuth to use when calcualting the destination coordinates.</param>
        /// <param name="distanceToDestinationInMetres">The distance in metres from the origin point that the destination point should be.</param>
        /// <returns></returns>
        (double destinationLongitude, double destinationLatitude) GetDestinationPointCoordinates(double originLongitude, double originLatitude, double azimuth, double distanceToDestinationInMetres);

        /// <summary>
        /// Calculates and returns the <see href="https://en.wikipedia.org/wiki/Great-circle_distance#:~:text=The%20great%2Dcircle%20distance%2C%20orthodromic,line%20through%20the%20sphere's%20interior).">great-circle distance</see> in metres between the supplied origin and destination points.
        /// </summary>
        /// <param name="originLongitude">The longitude coordinate of the origin point.</param>
        /// <param name="originLatitude">The latitude coordinate of the origin point.</param>
        /// <param name="destinationLongitude">The longitude coordinate of the destination point.</param>
        /// <param name="destinationLatitude">The latitude coordinate of the destination point.</param>
        /// <returns></returns>
        double GetDistanceBetweenPointsInMetres(double originLongitude, double originLatitude, double destinationLongitude, double destinationLatitude);
    }
}

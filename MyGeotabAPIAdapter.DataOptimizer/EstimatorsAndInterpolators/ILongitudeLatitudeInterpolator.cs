using MyGeotabAPIAdapter.Geospatial;
using System;

namespace MyGeotabAPIAdapter.DataOptimizer.EstimatorsAndInterpolators
{
    /// <summary>
    /// Interface for a class that helps with interpolation of longitude and latitude coordinates.
    /// </summary>
    public interface ILongitudeLatitudeInterpolator
    {
        /// <summary>
        /// Uses the lag and lead parameter values to derive and return interpolated longitude and latitude coordinates for the specified <paramref name="targetDateTime"/>
        /// </summary>
        /// <param name="targetDateTime">The <see cref="DateTime"/> for which to interpolate coordinates. Must be between the <paramref name="lagDateTime"/> and the <paramref name="leadDateTime"/> (inclusive).</param>
        /// <param name="lagDateTime">The <see cref="DateTime"/> associated with the <paramref name="lagLongitude"/> and <paramref name="lagLatitude"/> coordinates.</param>
        /// <param name="lagLongitude">The longitude at the <paramref name="lagDateTime"/>.</param>
        /// <param name="lagLatitude">The latitude at the <paramref name="lagDateTime"/>.</param>
        /// <param name="leadDateTime">The <see cref="DateTime"/> associated with the <paramref name="leadLongitude"/> and <paramref name="leadLatitude"/> coordinates.</param>
        /// <param name="leadLongitude">The longitude at the <paramref name="leadDateTime"/>.</param>
        /// <param name="leadLatitude">The latitude at the <paramref name="leadDateTime"/>.</param>
        /// <param name="numberOfCompassDirections">The desired number of cardinal directions on the <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see>. Determines the <see cref="CompassRoseType"/> that will be used to retrieve the <see cref="CompassDirection.Direction"/> associated with the supplied <paramref name="bearing"/>.</param>
        /// <returns></returns>
        LongitudeLatitudeInterpolationResult InterpolateCoordinates(DateTime targetDateTime, DateTime lagDateTime, double lagLongitude, double lagLatitude, DateTime leadDateTime, double leadLongitude, double leadLatitude, int numberOfCompassDirections);
    }
}

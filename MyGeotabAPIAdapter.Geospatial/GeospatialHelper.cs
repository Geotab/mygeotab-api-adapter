#nullable enable
using MyGeotabAPIAdapter.Logging;
using System;

namespace MyGeotabAPIAdapter.Geospatial
{
    /// <summary>
    /// A helper class to assist in working with geospatial data.
    /// </summary>
    public class GeospatialHelper : IGeospatialHelper
    {
        const double EarthRadiusInKilometres = 6376.5;

        readonly ICompassRose compassRose;
        readonly IExceptionHelper exceptionHelper;

        static double ConvertRadiansToDegrees(double radians)
        {
            return 180 / Math.PI * radians;
        }

        static double ConvertToRadians(double angle)
        {
            return Math.PI / 180 * angle;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeospatialHelper"/> class.
        /// </summary>
        /// <param name="compassRose">The <see cref="ICompassRose"/> to use. If not supplied, a new <see cref="CompassRose"/> instance will be created and used.</param>
        public GeospatialHelper(IExceptionHelper exceptionHelper, ICompassRose? compassRose = null)
        {
            this.exceptionHelper = exceptionHelper;
            if (compassRose == null)
            {
                compassRose = new CompassRose();
            }
            this.compassRose = compassRose;
        }

        /// <inheritdoc/>
        public double GetBearingBetweenPoints(double originLongitude, double originLatitude, double destinationLongitude, double destinationLatitude)
        {
            var startLat = ConvertToRadians(originLatitude);
            var startLong = ConvertToRadians(originLongitude);
            var endLat = ConvertToRadians(destinationLatitude);
            var endLong = ConvertToRadians(destinationLongitude);

            var dLong = endLong - startLong;
            var dPhi = Math.Log(Math.Tan((endLat / 2)
                                + (Math.PI / 4)) / Math.Tan((startLat / 2)
                                                            + (Math.PI / 4)));

            if (Math.Abs(dLong) > Math.PI)
            {
                dLong = dLong > 0
                    ? ((2 * Math.PI) - dLong) * -1
                    : (2 * Math.PI) + dLong;
            }

            var bearing = (ConvertRadiansToDegrees(Math.Atan2(dLong, dPhi)) + 360) % 360;
            return bearing;
        }

        /// <inheritdoc/>
        public string? GetCompassDirection(double bearing, CompassRoseType compassRoseType)
        {
            try
            {
                compassRose.CompassRoseType = compassRoseType;
                var compassDirection = compassRose.GetCompassDirection(bearing);
                return compassDirection;
            }
            catch (ArgumentException ex)
            {
                exceptionHelper.LogException(ex, NLogLogLevelName.Warn, $"An exception was encountered while attempting to get the compass direction associated with a bearing.");
            }
            return null;
        }

        /// <inheritdoc/>
        public string? GetCompassDirection(double bearing, int numberOfCompassDirections)
        {
            try
            {
                var compassRoseType = CompassRoseType.SixteenPoint;
                switch (numberOfCompassDirections)
                {
                    case 4:
                        compassRoseType = CompassRoseType.FourPoint;
                        break;
                    case 8:
                        compassRoseType = CompassRoseType.EightPoint;
                        break;
                    case 16:
                        break;
                    default:
                        throw new ArgumentException($"The value of {numberOfCompassDirections} supplied for the {nameof(numberOfCompassDirections)} parameter is not valid. Value must be one of 4, 8 or 16.");
                }
                var compassDirection = GetCompassDirection(bearing, compassRoseType);
                return compassDirection;
            }
            catch (Exception ex)
            {
                exceptionHelper.LogException(ex, NLogLogLevelName.Warn, $"An exception was encountered while attempting to get the compass direction associated with a bearing.");
            }
            return null;
        }

        /// <inheritdoc/>
        public (double destinationLongitude, double destinationLatitude) GetDestinationPointCoordinates(double originLongitude, double originLatitude, double azimuth, double distanceToDestinationInMetres)
        {
            // Convert bearing to radians:
            var bearing = ConvertToRadians(azimuth);

            var distanceToDestinationInKilometres = distanceToDestinationInMetres / 1000;
            var startLat = ConvertToRadians(originLatitude);
            var startLong = ConvertToRadians(originLongitude);
            
            var sinStartLat = Math.Sin(startLat);
            var cosDistanceToDestinationInKilometres = Math.Cos(distanceToDestinationInKilometres / EarthRadiusInKilometres);
            var cosStartLat = Math.Cos(startLat);
            var sinDistanceToDestinationInKilometres = Math.Sin(distanceToDestinationInKilometres / EarthRadiusInKilometres);

            double destinationLat = Math.Asin((sinStartLat * cosDistanceToDestinationInKilometres) + (cosStartLat * sinDistanceToDestinationInKilometres * Math.Cos(bearing)));
            double destinationLong = startLong + Math.Atan2(Math.Sin(bearing) * (sinDistanceToDestinationInKilometres * cosStartLat), cosDistanceToDestinationInKilometres - (sinStartLat * Math.Sin(destinationLat)));

            // Convert to degrees:
            destinationLat = ConvertRadiansToDegrees(destinationLat);
            destinationLong = ConvertRadiansToDegrees(destinationLong);
            
            return new (destinationLong, destinationLat);
        }

        /// <inheritdoc/>
        public double GetDistanceBetweenPointsInMetres(double originLongitude, double originLatitude, double destinationLongitude, double destinationLatitude)
        {
            var startLat = ConvertToRadians(originLatitude);
            var startLong = ConvertToRadians(originLongitude);
            var endLat = ConvertToRadians(destinationLatitude);
            var endLong = ConvertToRadians(destinationLongitude);
            var dLat = endLat - startLat;
            var dLong = endLong - startLong;

            var a = Math.Pow(Math.Sin(dLat / 2.0), 2.0) +
                       Math.Cos(startLat) * Math.Cos(endLat) *
                       Math.Pow(Math.Sin(dLong / 2.0), 2.0);

            var greatCircleDistanceRadians = 2.0 * Math.Asin(Math.Sqrt(a));
            var greatCircleDistanceMetres = EarthRadiusInKilometres * greatCircleDistanceRadians * 1000;
            return greatCircleDistanceMetres;
        }
    }
}

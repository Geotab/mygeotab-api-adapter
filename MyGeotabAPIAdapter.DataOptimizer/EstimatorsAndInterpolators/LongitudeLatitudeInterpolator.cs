using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Geospatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGeotabAPIAdapter.DataOptimizer.EstimatorsAndInterpolators
{
    /// <summary>
    /// A class that helps with interpolation of longitude and latitude coordinates.
    /// </summary>
    public class LongitudeLatitudeInterpolator : ILongitudeLatitudeInterpolator
    {
        readonly IGeospatialHelper geospatialHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="LongitudeLatitudeInterpolator"/> class.
        /// </summary>
        public LongitudeLatitudeInterpolator(IGeospatialHelper geospatialHelper)
        {
            this.geospatialHelper = geospatialHelper;
        }

        /// <summary>
        /// Enumerates the <paramref name="sortedDbLogRecordTs"/> and returns the ones with the closest preceding (<see cref="lagDbLogRecordT"/>) and succeeding (<see cref="leadDbLogRecordT"/>) <see cref="DbLogRecordT.DateTime"/> values relative to the <paramref name="targetDateTime"/>. If a preceeding/succeeding <see cref="DbLogRecordT.DateTime"/> cannot be found, the respective <see cref="lagDbLogRecordT"/>/<see cref="leadDbLogRecordT"/> will be <c>null</c>. Also returns the <see cref="lagDbLogRecordTIndex"/> - the index of the <see cref="lagDbLogRecordT"/> in the the <paramref name="sortedDbLogRecordTs"/> (or null if no <see cref="lagDbLogRecordT"/> is found).
        /// </summary>
        /// <param name="targetDateTime">The <see cref="DateTime"/> around which to find lag/lead <see cref="DbLogRecordT"/>s.</param>
        /// <param name="sortedDbLogRecordTs">The list of <see cref="DbLogRecordT"/>s from which to extract lag/lead values. IMPORTANT: These must be ordered chronologically on <see cref="DbLogRecordT.DateTime"/> or else an exception will be thrown.</param>
        /// <param name="startDbLogRecordTIndex">The index of the item in <paramref name="sortedDbLogRecordTs"/> to start from when searching for lag and lead <see cref="DbLogRecordT"/>s. This can speed-up processing since the <paramref name="sortedDbLogRecordTs"/> must be sorted chronologically and it is recommended that the same "batch" of <see cref="DbLogRecordT"/>s be used repeatedly when processing a batch of objects for which interpolation is being performed.</param>
        /// <returns></returns>
        (DbLogRecordT lagDbLogRecordT, DbLogRecordT leadDbLogRecordT, int lagDbLogRecordTIndex) GetLagAndLeadDbLogRecordTs(DateTime targetDateTime, IEnumerable<DbLogRecordT> sortedDbLogRecordTs, int startDbLogRecordTIndex = 0)
        {
            if (startDbLogRecordTIndex < 0)
            {
                startDbLogRecordTIndex = 0;
            }

            // Validate chronological order.
            var previousEntityDateTime = sortedDbLogRecordTs.First().DateTime;
            foreach (var dbLogRecordT in sortedDbLogRecordTs)
            {
                if (dbLogRecordT.DateTime < previousEntityDateTime)
                {
                    throw new Exception($"{nameof(DbLogRecordT)}s are not in chronological order. The {nameof(GetLagAndLeadDbLogRecordTs)} method requires that the input {nameof(sortedDbLogRecordTs)} be in chronological order.");
                }
                previousEntityDateTime = dbLogRecordT.DateTime;
            }

            // Find lag/lead dbLogRecordTs.
            DbLogRecordT lagDbLogRecordT = null;
            DbLogRecordT leadDbLogRecordT = null;
            int currentDbLogRecordTIndex = 0;

            foreach (var dbLogRecordT in sortedDbLogRecordTs)
            {
                // Skip until arriving at the startDbLogRecordTIndex.
                if (currentDbLogRecordTIndex < startDbLogRecordTIndex)
                {
                    currentDbLogRecordTIndex++;
                    continue;
                }

                // Evaluate and capture lag/lead entities.
                if (dbLogRecordT.DateTime < targetDateTime)
                {
                    lagDbLogRecordT = dbLogRecordT;
                }
                else if (dbLogRecordT.DateTime == targetDateTime)
                {
                    lagDbLogRecordT = dbLogRecordT;
                    leadDbLogRecordT = dbLogRecordT;
                }
                else
                {
                    leadDbLogRecordT = dbLogRecordT;
                    break;
                }

                currentDbLogRecordTIndex++;
            }
            return (lagDbLogRecordT, leadDbLogRecordT, currentDbLogRecordTIndex-1);
        }

        /// <inheritdoc/>
        public LongitudeLatitudeInterpolationResult InterpolateCoordinates(DateTime targetDateTime, DateTime lagDateTime, double lagLongitude, double lagLatitude, DateTime leadDateTime, double leadLongitude, double leadLatitude, int numberOfCompassDirections)
        {
            // Ensure that leadDateTime >= lagDateTime and that targetDateTime is between lagDateTime and leadDateTime (inclusive).
            if (leadDateTime < lagDateTime)
            {
                return new LongitudeLatitudeInterpolationResult(false, LongitudeLatitudeInterpolationResultReason.LeadDateTimeLessThanLagDateTime);
            }
            if (targetDateTime < lagDateTime)
            {
                return new LongitudeLatitudeInterpolationResult(false, LongitudeLatitudeInterpolationResultReason.TargetDateTimeLessThanLagDateTime);
            }
            if (targetDateTime > leadDateTime)
            {
                return new LongitudeLatitudeInterpolationResult(false, LongitudeLatitudeInterpolationResultReason.TargetDateTimeGreaterThanLeadDateTime);
            }

            // Get the duration, in ticks, between the lagDateTime and the leadDateTime.
            var lagToLeadDurationTicks = lagDateTime.Ticks - leadDateTime.Ticks;
            if (lagToLeadDurationTicks == 0)
            {
                // The duration between the lagDateTime and the leadDateTime is zero. Simply return the lag coordinates.
                return new LongitudeLatitudeInterpolationResult(true, LongitudeLatitudeInterpolationResultReason.None, lagLongitude, lagLatitude);
            }

            // Get the duration, in ticks, between the lagDateTime and the targetDateTime.
            var lagToTargetDurationTicks = targetDateTime.Ticks - lagDateTime.Ticks;
            if (lagToTargetDurationTicks == 0)
            {
                // The duration between the lagDateTime and the targetDateTime is zero. Simply return the lag coordinates.
                return new LongitudeLatitudeInterpolationResult(true, LongitudeLatitudeInterpolationResultReason.None, lagLongitude, lagLatitude);
            }

            // Calculate the distance between the lag and lead coordinates.
            var lagToLeadDistanceMetres = geospatialHelper.GetDistanceBetweenPointsInMetres(lagLongitude, lagLatitude, leadLongitude, leadLatitude);
            if (lagToLeadDistanceMetres == 0)
            {
                // The distance between the lag and lead coordinates is zero. Simply return the lag coordinates.
                return new LongitudeLatitudeInterpolationResult(true, LongitudeLatitudeInterpolationResultReason.None, lagLongitude, lagLatitude);
            }

            // Calculate the lag-to-targetDateTime duration as a proportion of the lag-to-lead duration.
            var proportion = (double)lagToTargetDurationTicks / (double)lagToLeadDurationTicks;

            // Calculate the distance that the interpolated longitude/latitude coordinates should be away from the lag coordinates using the previously-calculated lag-to-targetDateTime duration proportion of the lag-to-lead duration. This assumes a constant speed between lag and lead coordinates.  
            var lagToTargetDateTimeDistanceMetres = proportion * lagToLeadDistanceMetres;

            // Calculate the bearing and compass direction between the lag and lead coordinates.
            var lagToLeadBearing = geospatialHelper.GetBearingBetweenPoints(lagLongitude, lagLatitude, leadLongitude, leadLatitude);
            var lagToLeadDirection = geospatialHelper.GetCompassDirection(lagToLeadBearing, numberOfCompassDirections);
          
            // Using the calculated bearing and distance values, derive the interpolated coordinates for the targetDateTime.
            var (targetDateTimeLong, targetDateTimeLat) = geospatialHelper.GetDestinationPointCoordinates(lagLongitude, lagLatitude, lagToLeadBearing, lagToTargetDateTimeDistanceMetres);

            return new LongitudeLatitudeInterpolationResult(true, LongitudeLatitudeInterpolationResultReason.None, targetDateTimeLong, targetDateTimeLat, lagToLeadBearing, lagToLeadDirection);
        }
    }
}

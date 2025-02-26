using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Geospatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGeotabAPIAdapter.DataEnhancement
{
    public class LocationInterpolator : ILocationInterpolator
    {
        readonly IGeospatialHelper geospatialHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationInterpolator"/> class.
        /// </summary>
        public LocationInterpolator(IGeospatialHelper geospatialHelper)
        {
            this.geospatialHelper = geospatialHelper;
        }

        /// <summary>
        /// Enumerates the <paramref name="sortedDbLogRecord2s"/> and returns the ones with the closest preceding (<see cref="lagDbLogRecord2"/>) and succeeding (<see cref="leadDbLogRecord2"/>) <see cref="DbLogRecord2.DateTime"/> values relative to the <paramref name="targetDateTime"/>. If a preceeding/succeeding <see cref="DbLogRecord2.DateTime"/> cannot be found, the respective <see cref="lagDbLogRecord2"/>/<see cref="leadDbLogRecord2"/> will be <c>null</c>. Also returns the <see cref="lagDbLogRecord2Index"/> - the index of the <see cref="lagDbLogRecord2"/> in the the <paramref name="sortedDbLogRecord2s"/> (or null if no <see cref="lagDbLogRecord2"/> is found).
        /// </summary>
        /// <param name="targetDateTime">The <see cref="DateTime"/> around which to find lag/lead <see cref="DbLogRecord2"/>s.</param>
        /// <param name="sortedDbLogRecord2s">The list of <see cref="DbLogRecord2"/>s from which to extract lag/lead values. IMPORTANT: These must be ordered chronologically on <see cref="DbLogRecord2.DateTime"/> or else an exception will be thrown.</param>
        /// <param name="startDbLogRecord2Index">The index of the item in <paramref name="sortedDbLogRecord2s"/> to start from when searching for lag and lead <see cref="DbLogRecord2"/>s. This can speed-up processing since the <paramref name="sortedDbLogRecord2s"/> must be sorted chronologically and it is recommended that the same "batch" of <see cref="DbLogRecord2"/>s be used repeatedly when processing a batch of objects for which interpolation is being performed.</param>
        /// <returns></returns>
        (DbLogRecord2 lagDbLogRecord2, DbLogRecord2 leadDbLogRecord2, int lagDbLogRecord2Index) GetLagAndLeadDbLogRecord2s(DateTime targetDateTime, IEnumerable<DbLogRecord2> sortedDbLogRecord2s, int startDbLogRecord2Index = 0)
        {
            if (startDbLogRecord2Index < 0)
            {
                startDbLogRecord2Index = 0;
            }

            // Validate chronological order.
            var previousEntityDateTime = sortedDbLogRecord2s.First().DateTime;
            foreach (var dbLogRecord2 in sortedDbLogRecord2s)
            {
                if (dbLogRecord2.DateTime < previousEntityDateTime)
                {
                    throw new Exception($"{nameof(DbLogRecord2)}s are not in chronological order. The {nameof(GetLagAndLeadDbLogRecord2s)} method requires that the input {nameof(sortedDbLogRecord2s)} be in chronological order.");
                }
                previousEntityDateTime = dbLogRecord2.DateTime;
            }

            // Find lag/lead dbLogRecord2s.
            DbLogRecord2 lagDbLogRecord2 = null;
            DbLogRecord2 leadDbLogRecord2 = null;
            int currentDbLogRecord2Index = 0;

            foreach (var dbLogRecord2 in sortedDbLogRecord2s)
            {
                // Skip until arriving at the startDbLogRecord2Index.
                if (currentDbLogRecord2Index < startDbLogRecord2Index)
                {
                    currentDbLogRecord2Index++;
                    continue;
                }

                // Evaluate and capture lag/lead entities.
                if (dbLogRecord2.DateTime < targetDateTime)
                {
                    lagDbLogRecord2 = dbLogRecord2;
                }
                else if (dbLogRecord2.DateTime == targetDateTime)
                {
                    lagDbLogRecord2 = dbLogRecord2;
                    leadDbLogRecord2 = dbLogRecord2;
                }
                else
                {
                    leadDbLogRecord2 = dbLogRecord2;
                    break;
                }

                currentDbLogRecord2Index++;
            }
            return (lagDbLogRecord2, leadDbLogRecord2, currentDbLogRecord2Index - 1);
        }

        /// <inheritdoc/>
        public LocationInterpolationResult InterpolateCoordinates(DateTime targetDateTime, DateTime lagDateTime, double lagLongitude, double lagLatitude, DateTime leadDateTime, double leadLongitude, double leadLatitude, int numberOfCompassDirections)
        {
            // Ensure that leadDateTime >= lagDateTime and that targetDateTime is between lagDateTime and leadDateTime (inclusive).
            if (leadDateTime < lagDateTime)
            {
                return new LocationInterpolationResult(false, LocationInterpolationResultReason.LeadDateTimeLessThanLagDateTime);
            }
            if (targetDateTime < lagDateTime)
            {
                return new LocationInterpolationResult(false, LocationInterpolationResultReason.TargetDateTimeLessThanLagDateTime);
            }
            if (targetDateTime > leadDateTime)
            {
                return new LocationInterpolationResult(false, LocationInterpolationResultReason.TargetDateTimeGreaterThanLeadDateTime);
            }

            // Get the duration, in ticks, between the lagDateTime and the leadDateTime.
            var lagToLeadDurationTicks = leadDateTime.Ticks - lagDateTime.Ticks;
            if (lagToLeadDurationTicks == 0)
            {
                // The duration between the lagDateTime and the leadDateTime is zero. Simply return the lag coordinates.
                return new LocationInterpolationResult(true, LocationInterpolationResultReason.None, lagLongitude, lagLatitude);
            }

            // Get the duration, in ticks, between the lagDateTime and the targetDateTime.
            var lagToTargetDurationTicks = targetDateTime.Ticks - lagDateTime.Ticks;
            if (lagToTargetDurationTicks == 0)
            {
                // The duration between the lagDateTime and the targetDateTime is zero. Simply return the lag coordinates.
                return new LocationInterpolationResult(true, LocationInterpolationResultReason.None, lagLongitude, lagLatitude);
            }

            // Calculate the distance between the lag and lead coordinates.
            var lagToLeadDistanceMetres = geospatialHelper.GetDistanceBetweenPointsInMetres(lagLongitude, lagLatitude, leadLongitude, leadLatitude);
            if (lagToLeadDistanceMetres == 0)
            {
                // The distance between the lag and lead coordinates is zero. Simply return the lag coordinates.
                return new LocationInterpolationResult(true, LocationInterpolationResultReason.None, lagLongitude, lagLatitude);
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

            return new LocationInterpolationResult(true, LocationInterpolationResultReason.None, targetDateTimeLong, targetDateTimeLat, (float?)lagToLeadBearing, lagToLeadDirection);
        }
    }
}

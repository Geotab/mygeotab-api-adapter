using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGeotabAPIAdapter.Geospatial
{
    /// <summary>
    /// A class that represents a <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see>. Supports all <see cref="Geospatial.CompassRoseType"/>s and exists primarily to convert bearing values into associated compass directions via the <see cref="GetCompassDirection(double)"/> method.
    /// </summary>
    public class CompassRose : ICompassRose
    {
        const CompassRoseType DefaultCompassRoseType = CompassRoseType.SixteenPoint;
        const double MaxDegrees = 360;
        readonly IList<CompassDirection> compassDirections;
        CompassRoseType compassRoseType;

        /// <inheritdoc/>
        public CompassRoseType CompassRoseType 
        { 
            get => compassRoseType;
            set 
            {
                if (compassRoseType != value)
                {
                    compassRoseType = value;
                    RecalibrateForCurrentCompassRoseType();
                }
            }
        }

        /// <inheritdoc/>
        public IList<CompassDirection> CompassDirections { get => compassDirections; }

        /// <inheritdoc/>
        public double DegreesPerDirection { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompassRose"/> class.
        /// </summary>
        public CompassRose()
        {
            compassDirections = new List<CompassDirection>();
            CompassRoseType = DefaultCompassRoseType;
        }

        /// <inheritdoc/>
        public string GetCompassDirection(double bearing)
        {
            if (bearing < 0 || bearing > 360)
            {
                throw new ArgumentException($"The value of {bearing} supplied for the bearing parameter does not fall within the allowed range of 0 through 360.");
            }

            var compassDirection = CompassDirections.Where(compassDirection => bearing >= compassDirection.MinDegrees && bearing < compassDirection.MaxDegrees).FirstOrDefault();

            // If compassDirection is null, it's likely north.
            if (compassDirection == null)
            {
                compassDirection = CompassDirections.Where(compassDirection => 
                    compassDirection.IsNorth == true && compassDirection.MinDegrees > compassDirection.MaxDegrees 
                    && 
                    (
                        (bearing < compassDirection.MaxDegrees && bearing >= 0)
                        || 
                        (bearing >= compassDirection.MinDegrees && bearing <= MaxDegrees)
                    )).FirstOrDefault();
            }

            return compassDirection.Direction;
        }

        /// <summary>
        /// Re-builds the <see cref="CompassDirections"/> and re-sets the <see cref="DegreesPerDirection"/> of the current <see cref="CompassRose"/> instance based on the <see cref="CompassRoseType"/>.
        /// </summary>
        void RecalibrateForCurrentCompassRoseType()
        {
            compassDirections.Clear();
            string[] directionNames = CompassRoseType switch
            {
                CompassRoseType.FourPoint => Enum.GetNames(typeof(CompassDirection4Point)),
                CompassRoseType.EightPoint => Enum.GetNames(typeof(CompassDirection8Point)),
                CompassRoseType.SixteenPoint => Enum.GetNames(typeof(CompassDirection16Point)),
                _ => throw new NotSupportedException($"The {nameof(CompassRoseType)} CompassRoseType is not supported by this method."),
            };
            DegreesPerDirection = MaxDegrees / directionNames.Length;

            // Determine min/max degrees for N:
            var currentDirectionMinDegrees = MaxDegrees - (DegreesPerDirection / 2);
            var currentDirectionMaxDegrees = DegreesPerDirection / 2;
            var currentCompassDirection = new CompassDirection(directionNames[0], currentDirectionMinDegrees, currentDirectionMaxDegrees, true);
            CompassDirections.Add(currentCompassDirection);

            // Determine min/max degrees for all other directions (moving clockwise).
            for (int i = 1; i < directionNames.Length; i++)
            {
                currentDirectionMinDegrees = currentDirectionMaxDegrees;
                currentDirectionMaxDegrees = currentDirectionMinDegrees + DegreesPerDirection;
                currentCompassDirection = new CompassDirection(directionNames[i], currentDirectionMinDegrees, currentDirectionMaxDegrees);
                CompassDirections.Add(currentCompassDirection);
            }
        }
    }
}

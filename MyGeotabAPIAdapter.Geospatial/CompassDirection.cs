namespace MyGeotabAPIAdapter.Geospatial
{
    /// <summary>
    /// A container for information related to one of the cardinal directions on a <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see>.
    /// </summary>
    public class CompassDirection
    {
        readonly bool isNorth;

        /// <summary>
        /// A string representation of the compass direction (e.g. "N", "SE", "WSW", etc.)
        /// </summary>
        public string Direction { get; }

        /// <summary>
        /// Indicates whether the current direction has been determined to be north.
        /// </summary>
        public bool IsNorth { get => isNorth; }

        /// <summary>
        /// The maximum value in the degree range represented by the current direction.
        /// </summary>
        public double MaxDegrees { get; }

        /// <summary>
        /// The minimum value in the degree range represented by the current direction.
        /// </summary>
        public double MinDegrees { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompassDirection"/> class.
        /// </summary>
        /// <param name="direction">The value to apply to the <see cref="Direction"/> property.</param>
        /// <param name="minDegrees">The value to apply to the <see cref="MinDegrees"/> property.</param>
        /// <param name="maxDegrees">The value to apply to the <see cref="MaxDegrees"/> property.</param>
        /// <param name="isNorth">The value to apply to the <see cref="IsNorth"/> property.</param>
        public CompassDirection(string direction, double minDegrees, double maxDegrees, bool isNorth = false)
        {
            Direction = direction;
            MinDegrees = minDegrees;
            MaxDegrees = maxDegrees;
            this.isNorth = isNorth;
        }
    }
}

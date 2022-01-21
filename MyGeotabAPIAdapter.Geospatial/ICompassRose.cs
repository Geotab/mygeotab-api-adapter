using System;
using System.Collections.Generic;

namespace MyGeotabAPIAdapter.Geospatial
{
    /// <summary>
    /// Interface for a class that represents a <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see>. Supports all <see cref="Geospatial.CompassRoseType"/>s and exists primarily to convert bearing values into associated compass directions via the <see cref="GetCompassDirection(double)"/> method.
    /// </summary>
    public interface ICompassRose
    {
        /// <summary>
        /// The <see cref="Geospatial.CompassRoseType"/> represented by the current instance of the <see cref="ICompassRose"/> implementing class.
        /// </summary>
        CompassRoseType CompassRoseType { get; set; }

        /// <summary>
        /// A list of <see cref="CompassDirection"/>s associated with the <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see> represented by the current instance of the <see cref="ICompassRose"/> implementing class.
        /// </summary>
        IList<CompassDirection> CompassDirections { get; }

        /// <summary>
        /// The number of degrees per cardinal direction of the <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see> represented by the current instance of the <see cref="ICompassRose"/> implementing class.
        /// </summary>
        double DegreesPerDirection { get; }

        /// <summary>
        /// Returns, for the supplied <paramref name="bearing"/>, the <see cref="CompassDirection.Direction"/> of the <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see> represented by the current instance of the <see cref="ICompassRose"/> implementing class.
        /// </summary>
        /// <param name="bearing">The bearing for which to retrieve the associated <see cref="CompassDirection.Direction"/>. Must be a value between 0 and 360 or an <see cref="ArgumentException"/> will be thrown.</param>
        /// <returns></returns>
        string GetCompassDirection(double bearing);
    }
}

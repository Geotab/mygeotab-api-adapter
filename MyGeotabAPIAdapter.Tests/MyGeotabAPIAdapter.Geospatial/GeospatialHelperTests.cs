#nullable enable
using MyGeotabAPIAdapter.Logging;
using System;
using Xunit;
using Xunit.Abstractions;
using MyGeotabAPIAdapter.Geospatial;

namespace MyGeotabAPIAdapter.Tests
{
    public class GetCompassDirectionTestData : TheoryData<CompassRoseType, double, string?>
    {
        public GetCompassDirectionTestData()
        {
            #region Test code to generate output for supported compass rose types
            ////Sample code to generate output for all degrees for all supported compass rose types. Can be copied and pasted to run elsewhere in a class that utilizes the GeospatialHelper:
            //var geospatialHelper = new MyGeotabAPIAdapter.Geospatial.GeospatialHelper();
            //const double MaxBearing = 360.00;
            //double bearing = 0.00;
            //double increment = 0.01;
            //while (bearing < MaxBearing)
            //{
            //    var direction = geospatialHelper.GetCompassDirection(bearing, Geospatial.CompassRoseType.FourPoint);
            //    logger.Info($"Bearing: {bearing}, Direction: {direction}");
            //    bearing += increment;
            //}
            //while (bearing < MaxBearing)
            //{
            //    var direction = geospatialHelper.GetCompassDirection(bearing, Geospatial.CompassRoseType.EightPoint);
            //    logger.Info($"Bearing: {bearing}, Direction: {direction}");
            //    bearing += increment;
            //}
            //while (bearing < MaxBearing)
            //{
            //    var direction = geospatialHelper.GetCompassDirection(bearing, Geospatial.CompassRoseType.SixteenPoint);
            //    logger.Info($"Bearing: {bearing}, Direction: {direction}");
            //    bearing += increment;
            //}
            #endregion

            // Invalid bearing values.
            Add(CompassRoseType.FourPoint, -1, null);
            Add(CompassRoseType.FourPoint, 361, null);

            // Four point compass rose:
            Add(CompassRoseType.FourPoint, 315, Enum.GetName(typeof(CompassDirection4Point), CompassDirection4Point.N));
            Add(CompassRoseType.FourPoint, 360, Enum.GetName(typeof(CompassDirection4Point), CompassDirection4Point.N));
            Add(CompassRoseType.FourPoint, 0, Enum.GetName(typeof(CompassDirection4Point), CompassDirection4Point.N));
            Add(CompassRoseType.FourPoint, 44.999, Enum.GetName(typeof(CompassDirection4Point), CompassDirection4Point.N));
            Add(CompassRoseType.FourPoint, 45, Enum.GetName(typeof(CompassDirection4Point), CompassDirection4Point.E));
            Add(CompassRoseType.FourPoint, 134.999, Enum.GetName(typeof(CompassDirection4Point), CompassDirection4Point.E));
            Add(CompassRoseType.FourPoint, 135, Enum.GetName(typeof(CompassDirection4Point), CompassDirection4Point.S));
            Add(CompassRoseType.FourPoint, 224.999, Enum.GetName(typeof(CompassDirection4Point), CompassDirection4Point.S));
            Add(CompassRoseType.FourPoint, 225, Enum.GetName(typeof(CompassDirection4Point), CompassDirection4Point.W));
            Add(CompassRoseType.FourPoint, 314.999, Enum.GetName(typeof(CompassDirection4Point), CompassDirection4Point.W));

            // Eight point compass rose:
            Add(CompassRoseType.EightPoint, 337.5, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.N));
            Add(CompassRoseType.EightPoint, 360, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.N));
            Add(CompassRoseType.EightPoint, 0, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.N));
            Add(CompassRoseType.EightPoint, 22.499, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.N));
            Add(CompassRoseType.EightPoint, 22.5, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.NE));
            Add(CompassRoseType.EightPoint, 67.499, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.NE));
            Add(CompassRoseType.EightPoint, 67.5, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.E));
            Add(CompassRoseType.EightPoint, 112.499, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.E));
            Add(CompassRoseType.EightPoint, 112.5, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.SE));
            Add(CompassRoseType.EightPoint, 157.499, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.SE));
            Add(CompassRoseType.EightPoint, 157.5, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.S));
            Add(CompassRoseType.EightPoint, 202.499, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.S));
            Add(CompassRoseType.EightPoint, 202.5, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.SW));
            Add(CompassRoseType.EightPoint, 247.499, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.SW));
            Add(CompassRoseType.EightPoint, 247.5, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.W));
            Add(CompassRoseType.EightPoint, 292.499, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.W));
            Add(CompassRoseType.EightPoint, 292.5, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.NW));
            Add(CompassRoseType.EightPoint, 337.499, Enum.GetName(typeof(CompassDirection8Point), CompassDirection8Point.NW));
            
            // Sixteen point compass rose:
            Add(CompassRoseType.SixteenPoint, 348.75, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.N));
            Add(CompassRoseType.SixteenPoint, 360, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.N));
            Add(CompassRoseType.SixteenPoint, 0, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.N));
            Add(CompassRoseType.SixteenPoint, 11.249, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.N));
            Add(CompassRoseType.SixteenPoint, 11.25, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.NNE));
            Add(CompassRoseType.SixteenPoint, 33.749, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.NNE));
            Add(CompassRoseType.SixteenPoint, 33.75, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.NE));
            Add(CompassRoseType.SixteenPoint, 56.249, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.NE));
            Add(CompassRoseType.SixteenPoint, 56.25, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.ENE));
            Add(CompassRoseType.SixteenPoint, 78.749, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.ENE));
            Add(CompassRoseType.SixteenPoint, 78.75, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.E));
            Add(CompassRoseType.SixteenPoint, 101.249, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.E));
            Add(CompassRoseType.SixteenPoint, 101.25, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.ESE));
            Add(CompassRoseType.SixteenPoint, 123.749, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.ESE));
            Add(CompassRoseType.SixteenPoint, 123.75, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.SE));
            Add(CompassRoseType.SixteenPoint, 146.249, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.SE));
            Add(CompassRoseType.SixteenPoint, 146.25, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.SSE));
            Add(CompassRoseType.SixteenPoint, 168.749, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.SSE));
            Add(CompassRoseType.SixteenPoint, 168.75, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.S));
            Add(CompassRoseType.SixteenPoint, 191.249, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.S));
            Add(CompassRoseType.SixteenPoint, 191.25, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.SSW));
            Add(CompassRoseType.SixteenPoint, 213.749, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.SSW));
            Add(CompassRoseType.SixteenPoint, 213.75, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.SW));
            Add(CompassRoseType.SixteenPoint, 236.249, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.SW));
            Add(CompassRoseType.SixteenPoint, 236.25, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.WSW));
            Add(CompassRoseType.SixteenPoint, 258.749, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.WSW));
            Add(CompassRoseType.SixteenPoint, 258.75, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.W));
            Add(CompassRoseType.SixteenPoint, 281.249, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.W));
            Add(CompassRoseType.SixteenPoint, 281.25, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.WNW));
            Add(CompassRoseType.SixteenPoint, 303.749, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.WNW));
            Add(CompassRoseType.SixteenPoint, 303.75, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.NW));
            Add(CompassRoseType.SixteenPoint, 326.249, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.NW));
            Add(CompassRoseType.SixteenPoint, 326.25, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.NNW));
            Add(CompassRoseType.SixteenPoint, 348.749, Enum.GetName(typeof(CompassDirection16Point), CompassDirection16Point.NNW));
        }
    }

    public class GeospatialHelperTests
    {
        readonly ExceptionHelper exceptionHelper = new();
        private readonly ITestOutputHelper output;

        public GeospatialHelperTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [ClassData(typeof(GetCompassDirectionTestData))]
        public void GetCompassDirectionTests(CompassRoseType compassRoseType, double bearing, string? expected)
        {
            var geospatialHelper = new GeospatialHelper(exceptionHelper);
            var result = geospatialHelper.GetCompassDirection(bearing, compassRoseType);
            output.WriteLine($"Bearing: {bearing}, Direction: {result}");
            Assert.Equal(expected, result);
        }
    }
}

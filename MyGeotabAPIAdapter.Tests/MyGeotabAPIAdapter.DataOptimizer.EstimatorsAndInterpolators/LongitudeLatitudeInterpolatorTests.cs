using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DataOptimizer.EstimatorsAndInterpolators;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.Geospatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    public class GetLagAndLeadDbLogRecordTsTestData : TheoryData<DateTime, IEnumerable<DbLogRecordT>, int, (DbLogRecordT, DbLogRecordT, int)>
    {
        public GetLagAndLeadDbLogRecordTsTestData()
        {
            var targetDateTime = DateTime.Now;
            var dbLogRecordTs = new List<DbLogRecordT>();

            var dbLogRecordT1 = new DbLogRecordT()
            {
                id = 1,
                DateTime = DateTime.Now.AddMinutes(-2)
            };
            dbLogRecordTs.Add(dbLogRecordT1);

            var dbLogRecordT2 = new DbLogRecordT()
            {
                id = 2,
                DateTime = DateTime.Now.AddMinutes(-1)
            };
            dbLogRecordTs.Add(dbLogRecordT2);

            var dbLogRecordT3 = new DbLogRecordT()
            {
                id = 3,
                DateTime = DateTime.Now.AddMinutes(1)
            };
            dbLogRecordTs.Add(dbLogRecordT3);

            var dbLogRecordT4 = new DbLogRecordT()
            {
                id = 4,
                DateTime = DateTime.Now.AddMinutes(2)
            };
            dbLogRecordTs.Add(dbLogRecordT4);

            // DbLogRecordTs ordered chronologically and targetDateTime falls within DateTime range of the DbLogRecordTs.
            Add(targetDateTime, dbLogRecordTs, 0, (dbLogRecordT2, dbLogRecordT3, 0));
        }
    }

    public class GetLagAndLeadDbLogRecordTsTestData_DbLogRecordTsNotInChronologicalOrder : TheoryData<DateTime, IEnumerable<DbLogRecordT>, int>
    {
        public GetLagAndLeadDbLogRecordTsTestData_DbLogRecordTsNotInChronologicalOrder()
        {
            var targetDateTime = DateTime.Now;
            var dbLogRecordTs = new List<DbLogRecordT>();

            var dbLogRecordT1 = new DbLogRecordT()
            {
                id = 1,
                DateTime = DateTime.Now.AddMinutes(-2)
            };
            dbLogRecordTs.Add(dbLogRecordT1);

            var dbLogRecordT2 = new DbLogRecordT()
            {
                id = 2,
                DateTime = DateTime.Now.AddMinutes(-1)
            };
            dbLogRecordTs.Add(dbLogRecordT2);

            var dbLogRecordT4 = new DbLogRecordT()
            {
                id = 4,
                DateTime = DateTime.Now.AddMinutes(2)
            };
            dbLogRecordTs.Add(dbLogRecordT4);

            var dbLogRecordT3 = new DbLogRecordT()
            {
                id = 3,
                DateTime = DateTime.Now.AddMinutes(1)
            };
            dbLogRecordTs.Add(dbLogRecordT3);

            // DbLogRecordTs NOT ordered chronologically.
            Add(targetDateTime, dbLogRecordTs, 0);
        }
    }

    public class LongitudeLatitudeInterpolatorTests
    {
        readonly ExceptionHelper exceptionHelper = new();

        [Theory]
        [ClassData(typeof(GetLagAndLeadDbLogRecordTsTestData))]
        public void GetLagAndLeadDbLogRecordTs(DateTime targetDateTime, IEnumerable<DbLogRecordT> sortedDbLogRecordTs, int startDbLogRecordTIndex, (DbLogRecordT, DbLogRecordT, int) expected)
        {
            Type type = typeof(LongitudeLatitudeInterpolator);
            var longitudeLatitudeInterpolator = Activator.CreateInstance(type, new GeospatialHelper(exceptionHelper));

            MethodInfo method = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "GetLagAndLeadDbLogRecordTs" && !(x.IsPublic))
                .First();

            var returnValue = method.Invoke(longitudeLatitudeInterpolator, new object[] { targetDateTime, sortedDbLogRecordTs, startDbLogRecordTIndex });
            var(lagDbLogRecordT, leadDbLogRecordT, lagDbLogRecordTIndex) = (ValueTuple<DbLogRecordT, DbLogRecordT, int>)returnValue;
            Assert.Equal(expected.Item1.id, lagDbLogRecordT.id);
            Assert.Equal(expected.Item2.id, leadDbLogRecordT.id);
        }

        [Theory]
        [ClassData(typeof(GetLagAndLeadDbLogRecordTsTestData_DbLogRecordTsNotInChronologicalOrder))]
        public void GetLagAndLeadDbLogRecordTs_DbLogRecordTsNotInChronologicalOrder(DateTime targetDateTime, IEnumerable<DbLogRecordT> sortedDbLogRecordTs, int startDbLogRecordTIndex)
        {
            Type type = typeof(LongitudeLatitudeInterpolator);
            var longitudeLatitudeInterpolator = Activator.CreateInstance(type, new GeospatialHelper(exceptionHelper));

            MethodInfo method = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "GetLagAndLeadDbLogRecordTs" && !(x.IsPublic))
                .First();

            var exception = Assert.ThrowsAny<Exception>(() => method.Invoke(longitudeLatitudeInterpolator, new object[] { targetDateTime, sortedDbLogRecordTs, startDbLogRecordTIndex }));
            var innerException = exception.InnerException;
            Assert.Contains("DbLogRecordTs are not in chronological order. The GetLagAndLeadDbLogRecordTs method requires that the input sortedDbLogRecordTs be in chronological order.", innerException.Message);
        }
    }
}

using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    public class RuleObjectTest
    {
        [Fact]
        public void BuildRuleObject_Test()
        {
            //arrange
            Rule rule = GetTestRule();
            DbRuleObject ruleObject = new DbRuleObject();

            //act 
            ruleObject.BuildRuleObject(rule, (int)Common.DatabaseRecordStatus.Active, DateTime.UtcNow,
                        Common.DatabaseWriteOperationType.Insert);
            DbRule dbrule = ruleObject.DbRule;
            IList<DbCondition> dbConditions = ruleObject.DbConditions;

            //assert
            Assert.True(dbrule.Name == "test");
            Assert.NotNull(dbConditions);
            Assert.True(dbConditions.Count == 7);
        }

        private Rule GetTestRule()
        {
            //test device
            Device device = new Device(Id.Create("dev1"))
            {
                SerialNumber = "G9CXXXXXB65E",
                Name = "Test Device"
            };

            Rule rule = new Rule
            {
                Id = Id.Create("test"),
                Name = "test",
                Version = 11111,
                ActiveFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month - 1, DateTime.Now.Day),
                ActiveTo = new DateTime(DateTime.Now.Year, DateTime.Now.Month + 1, DateTime.Now.Day),
                BaseType = ExceptionRuleBaseType.Custom
            };

            //create test conditions
            Condition condition = new Condition(Id.Create("cond1"))
            {
                Value = 60,
                ConditionType = ConditionType.SpeedLimit
            };

            //Add children conditions to main condition
            IList<Condition> childConditions = new List<Condition>();
            Condition conditionChild = new Condition(Id.Create("sub1"), ConditionType.IsValueMoreThan, null, 21, null, null, null, null, null, null);
            childConditions.Add(conditionChild);
            childConditions.Add(new Condition(Id.Create("sub2"), ConditionType.IsValueEqualTo, null, 50, device, null, null, null, null, null));
            childConditions.Add(new Condition(Id.Create("sub3"), ConditionType.IsDriving, null, 1, null, null, null, null, null, null));
            childConditions.Add(new Condition(Id.Create("sub4"), ConditionType.Speed, null, 40, device, null, null, null, null, null));
            condition.AddChildren(childConditions, false);

            //Add children conditions of a child condition
            IList<Condition> childChildConditions = new List<Condition>
            {
                new Condition(Id.Create("subSub1"), ConditionType.And, null, null, device, null, null, null, null, null),
                new Condition(Id.Create("subSub2"), ConditionType.AnyData, null, 50, null, null, null, null, null, null)
            };
            conditionChild.AddChildren(childChildConditions, false);

            rule.Condition = condition;

            return rule;
        }
    }
}

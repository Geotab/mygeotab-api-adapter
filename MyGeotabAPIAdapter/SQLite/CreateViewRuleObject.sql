drop view vwRuleObject;

create view vwRuleObject as 
SELECT 
r.*,
c.Id as Cond_Id,
c.ParentId as Cond_ParentId,
c.RuleId as Cond_RuleId,
c.ConditionType as Cond_ConditionType,
c.DeviceId as Cond_DeviceId,
c.DiagnosticId as Cond_DiagnosticId,
c.DriverId as Cond_DriverId,
c.Value as Cond_Value,
c.WorkTimeId as Cond_WorkTimeId,
c.ZoneId as Cond_ZoneId,
c.EntityStatus as Cond_EntityStatus,
c.RecordLastChangedUtc as Cond_RecordLastChangedUtc
FROM Rules r
INNER JOIN Conditions c
ON  r.Id  = c.RuleId
ORDER BY r.Id, c.Id



select * from vwRuleObject;
SELECT 
r.*,
c.*
FROM Rules r
INNER JOIN Conditions c
ON  r.Id  = c.RuleId
ORDER BY r.Id, c.Id

SELECT count(Id)
FROM Conditions 
WHERE RuleId IS NULL

SELECT 
r.*,
c.*,
subCond.*
FROM Rules r
LEFT OUTER JOIN Conditions c
ON r.Id = c.RuleId
LEFT OUTER JOIN Conditions  subCond
ON c.Id = subCond.ParentId;
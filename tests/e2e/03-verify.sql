-- ============================================================
-- Verification: row counts per table per RecordID
-- ============================================================
USE TestCompareDB;
GO

SELECT 'Customer'  AS TableName, RecordID, COUNT(*) AS Cnt FROM dbo.Customer   GROUP BY RecordID ORDER BY RecordID;
SELECT 'Order'     AS TableName, RecordID, COUNT(*) AS Cnt FROM dbo.[Order]    GROUP BY RecordID ORDER BY RecordID;
SELECT 'OrderLine' AS TableName, RecordID, COUNT(*) AS Cnt FROM dbo.OrderLine  GROUP BY RecordID ORDER BY RecordID;
SELECT 'OrderTag'  AS TableName, RecordID, COUNT(*) AS Cnt FROM dbo.OrderTag   GROUP BY RecordID ORDER BY RecordID;
SELECT 'AuditLog'  AS TableName, RecordID, COUNT(*) AS Cnt FROM dbo.AuditLog   GROUP BY RecordID ORDER BY RecordID;

SELECT 'Payment (via junction)' AS TableName, op.RecordID, COUNT(*) AS Cnt
FROM dbo.OrderPayment op
JOIN dbo.Payment p ON p.PaymentID = op.PaymentID
GROUP BY op.RecordID ORDER BY op.RecordID;

SELECT 'AuditLog (Financial only)' AS TableName, RecordID, COUNT(*) AS Cnt
FROM dbo.AuditLog WHERE Category = 'Financial'
GROUP BY RecordID ORDER BY RecordID;

PRINT '=== Verification complete ===';
GO

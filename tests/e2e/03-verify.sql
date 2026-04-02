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

SELECT 'Product'   AS TableName, RecordID, COUNT(*) AS Cnt FROM dbo.Product    GROUP BY RecordID ORDER BY RecordID;
SELECT 'Shipment'  AS TableName, RecordID, COUNT(*) AS Cnt FROM dbo.Shipment   GROUP BY RecordID ORDER BY RecordID;
SELECT 'ProductCategory' AS TableName, CategoryCode, COUNT(*) AS Cnt FROM dbo.ProductCategory GROUP BY CategoryCode ORDER BY CategoryCode;
SELECT 'InventorySnapshot' AS TableName, RecordID, COUNT(*) AS Cnt FROM dbo.InventorySnapshot GROUP BY RecordID ORDER BY RecordID;

SELECT 'ProductCategory (via subquery)' AS TableName, p.RecordID, COUNT(*) AS Cnt
FROM dbo.ProductCategory pc
JOIN dbo.Product p ON p.CategoryID = pc.CategoryID
GROUP BY p.RecordID ORDER BY p.RecordID;

SELECT 'InventorySnapshot (LEFT JOIN)' AS TableName, inv.RecordID, COUNT(*) AS Cnt
FROM dbo.InventorySnapshot inv
LEFT JOIN dbo.Warehouse wh ON inv.WarehouseID = wh.WarehouseID
GROUP BY inv.RecordID ORDER BY inv.RecordID;

PRINT '=== Verification complete ===';
GO

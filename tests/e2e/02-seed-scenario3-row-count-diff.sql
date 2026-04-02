-- ============================================================
-- Scenario 3: Row Count Differences (OLD-3001 / NEW-3001)
-- Missing rows in new, extra rows in new, mixed matched/unmatched
-- ============================================================
USE TestCompareDB;
GO

-- Customer: simple match (no row-count issue on single-row)
INSERT INTO dbo.Customer (RecordID, FullName, Email, Phone, Address, IsActive, Notes, CreatedBy)
VALUES
    ('OLD-3001', 'Alice Johnson',  'alice@test.com', '555-3001', '789 Pine Rd',  1, NULL, 'OldSystem'),
    ('NEW-3001', 'Alice Johnson',  'alice@test.com', '555-3001', '789 Pine Rd',  1, NULL, 'NewSystem');

-- Order: simple match
INSERT INTO dbo.[Order] (RecordID, CustomerRecordID, OrderDate, OrderStatus, SubTotal, TaxRate, TaxAmount, TotalAmount, DiscountPercent, Notes, ProcessedFlag, CreatedBy)
VALUES
    ('OLD-3001', 'OLD-3001', '2025-07-01', 'Shipped', 150.00, 0.08, 12.00, 162.00, 0.0, NULL, '1', 'OldSystem'),
    ('NEW-3001', 'NEW-3001', '2025-07-01', 'Shipped', 150.00, 8.0,  12.00, 162.00, 0.0, NULL, '1', 'NewSystem');

-- OrderLine: Old=4 rows, New=3 rows
--   Matched: SKU-A/LG, SKU-C/SM
--   Missing in new: SKU-B/MD, SKU-D/XL
--   Extra in new: SKU-E/LG
INSERT INTO dbo.OrderLine (RecordID, ProductCode, SizeCode, Quantity, UnitPrice, LineTotal, Description)
VALUES
    ('OLD-3001', 'SKU-A', 'LG', 3, 25.00, 75.00,  'Alpha Large'),
    ('OLD-3001', 'SKU-B', 'MD', 2, 30.00, 60.00,  'Beta Medium'),
    ('OLD-3001', 'SKU-C', 'SM', 1, 15.00, 15.00,  'Charlie Small'),
    ('OLD-3001', 'SKU-D', 'XL', 1, 50.00, 50.00,  'Delta Extra-Large');

INSERT INTO dbo.OrderLine (RecordID, ProductCode, SizeCode, Quantity, UnitPrice, LineTotal, Description)
VALUES
    ('NEW-3001', 'SKU-A', 'LG', 3, 25.00, 75.00,  'Alpha Large'),
    ('NEW-3001', 'SKU-C', 'SM', 1, 15.00, 15.00,  'Charlie Small'),
    ('NEW-3001', 'SKU-E', 'LG', 4, 20.00, 80.00,  'Echo Large');

-- Payment: Old=2, New=3 (extra GiftCard in new)
DECLARE @pay1Old INT, @pay2Old INT, @pay1New INT, @pay2New INT, @pay3New INT;

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('CreditCard', 100.00, '2025-07-01 09:00:00', 'CC-3001', 0, 'OldProc');
SET @pay1Old = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('Cash', 62.00, '2025-07-01 09:01:00', 'CA-3001', 0, 'OldProc');
SET @pay2Old = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('CreditCard', 100.00, '2025-07-01 09:00:00', 'CC-3001', 0, 'NewProc');
SET @pay1New = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('Cash', 62.00, '2025-07-01 09:01:00', 'CA-3001', 0, 'NewProc');
SET @pay2New = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('GiftCard', 50.00, '2025-07-01 09:02:00', 'GC-3001', 0, 'NewProc');
SET @pay3New = SCOPE_IDENTITY();

INSERT INTO dbo.OrderPayment (RecordID, PaymentID) VALUES ('OLD-3001', @pay1Old), ('OLD-3001', @pay2Old);
INSERT INTO dbo.OrderPayment (RecordID, PaymentID) VALUES ('NEW-3001', @pay1New), ('NEW-3001', @pay2New), ('NEW-3001', @pay3New);

-- AuditLog: Old=2 Financial, New=1 Financial (missing Approve in new)
INSERT INTO dbo.AuditLog (RecordID, Category, Action, Details, UserName)
VALUES
    ('OLD-3001', 'Financial', 'Create',  'Order created', 'OldSystem'),
    ('OLD-3001', 'Financial', 'Approve', 'Payment approved', 'OldSystem');

INSERT INTO dbo.AuditLog (RecordID, Category, Action, Details, UserName)
VALUES
    ('NEW-3001', 'Financial', 'Create',  'Order created', 'NewSystem');

-- OrderTag: Old=1 tag, New=0 tags (all unmatched)
INSERT INTO dbo.OrderTag (RecordID, TagName, TagValue)
VALUES ('OLD-3001', 'Urgent', 'Yes');

-- Product: simple match (single row table)
INSERT INTO dbo.Product (RecordID, ProductGuid, ProductCode, ProductName, ShortCode, Description,
    WeightKg, VolumeLiters, UnitCostPrecise, ListPrice, ClearancePrice,
    Specifications, TagsVarchar, TagsNvarchar)
VALUES
    ('OLD-3001', 'C1C2C3C4-D5D6-7890-ABCD-EF1234567890', 'SKU-DELTA', 'Basic Widget', 'BWDG',
     'A basic widget.', 0.5, 0.2, 3.500000, 9.99, 4.99,
     '<specs><size>small</size></specs>', 'basic', N'basic'),
    ('NEW-3001', 'c1c2c3c4-d5d6-7890-abcd-ef1234567890', 'SKU-DELTA', 'Basic Widget', 'BWDG',
     'A basic widget.', 0.5, 0.2, 3.500000, 9.99, 4.99,
     '<specs><size>small</size></specs>', 'basic', N'basic');

-- Shipment: Old=3, New=2 (missing TRK-3001-C), New has extra TRK-3001-D
INSERT INTO dbo.Shipment (RecordID, TrackingNumber, ShipDate, DispatchTime, ShippedAtOffset,
    PackageCount, TotalItems, ShipmentWeight, AdjustmentAmount, InsuredValue,
    WeightAsText, CarrierNotes, SpecialInstructions)
VALUES
    ('OLD-3001', 'TRK-3001-A', '2025-07-01', '10:00:00', '2025-07-01 10:00:00 +00:00',
     1, 5, 5000, 0.00, 100.00, '5.0', 'Standard', 'None'),
    ('OLD-3001', 'TRK-3001-B', '2025-07-02', '11:00:00', '2025-07-02 11:00:00 +00:00',
     2, 10, 8000, -5.00, 200.00, '8.0', 'Standard', 'None'),
    ('OLD-3001', 'TRK-3001-C', '2025-07-03', '12:00:00', '2025-07-03 12:00:00 +00:00',
     1, 3, 2000, 0.00, 50.00, '2.0', 'Express', 'Signature required');

INSERT INTO dbo.Shipment (RecordID, TrackingNumber, ShipDate, DispatchTime, ShippedAtOffset,
    PackageCount, TotalItems, ShipmentWeight, AdjustmentAmount, InsuredValue,
    WeightAsText, CarrierNotes, SpecialInstructions)
VALUES
    ('NEW-3001', 'TRK-3001-A', '2025-07-01', '10:00:00', '2025-07-01 10:00:00 +00:00',
     1, 5, 5000, 0.00, 100.00, '5.0', 'Standard', 'None'),
    ('NEW-3001', 'TRK-3001-B', '2025-07-02', '11:00:00', '2025-07-02 11:00:00 +00:00',
     2, 10, 8000, -5.00, 200.00, '8.0', 'Standard', 'None'),
    ('NEW-3001', 'TRK-3001-D', '2025-07-04', '14:00:00', '2025-07-04 14:00:00 +00:00',
     3, 20, 15000, 0.00, 400.00, '15.0', 'Freight', 'Dock delivery');

-- InventorySnapshot: Old=2 warehouses, New=3 (extra warehouse 3)
INSERT INTO dbo.InventorySnapshot (RecordID, WarehouseID, SKU, QuantityOnHand, QuantityReserved,
    ReorderPoint, UnitCost, TotalValue, LastCountedAt, SnapshotTime, BatchId, Notes)
VALUES
    ('OLD-3001', 1, 'SKU-DELTA', 100, 10, 20, 3.500000, 350.00, '2025-07-01 08:00:00 +00:00', '08:00:00',
     'E1E2E3E4-F5F6-7890-ABCD-EF1234567890', 'Count'),
    ('OLD-3001', 2, 'SKU-DELTA', 50, 5, 10, 3.500000, 175.00, '2025-07-01 11:00:00 -08:00', '11:00:00',
     'E1E2E3E4-F5F6-7890-ABCD-EF1234567891', 'Count');

INSERT INTO dbo.InventorySnapshot (RecordID, WarehouseID, SKU, QuantityOnHand, QuantityReserved,
    ReorderPoint, UnitCost, TotalValue, LastCountedAt, SnapshotTime, BatchId, Notes)
VALUES
    ('NEW-3001', 1, 'SKU-DELTA', 100, 10, 20, 3.500000, 350.00, '2025-07-01 08:00:00 +00:00', '08:00:00',
     'e1e2e3e4-f5f6-7890-abcd-ef1234567890', 'Count'),
    ('NEW-3001', 2, 'SKU-DELTA', 50, 5, 10, 3.500000, 175.00, '2025-07-01 11:00:00 -08:00', '11:00:00',
     'e1e2e3e4-f5f6-7890-abcd-ef1234567891', 'Count'),
    ('NEW-3001', 3, 'SKU-DELTA', 25, 0, 5, 3.500000, 87.50, '2025-07-01 12:00:00 +00:00', '12:00:00',
     'e1e2e3e4-f5f6-7890-abcd-ef1234567892', 'New warehouse');

PRINT '=== Scenario 3 (Row Count Differences) seeded ===';
GO

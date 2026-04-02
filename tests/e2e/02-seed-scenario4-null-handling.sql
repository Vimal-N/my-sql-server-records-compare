-- ============================================================
-- Scenario 4: Null Handling (OLD-4001 / NEW-4001)
-- NULL==NULL match, NULL vs value mismatch, across rule types
-- ============================================================
USE TestCompareDB;
GO

-- Customer: Notes both NULL (match), Phone NULL vs value (mismatch), Address value vs NULL (mismatch)
INSERT INTO dbo.Customer (RecordID, FullName, Email, Phone, Address, IsActive, Notes, CreatedBy)
VALUES
    ('OLD-4001', 'Bob Wilson',  'bob@test.com', NULL,       '123 Main St', 1, NULL, 'OldSystem'),
    ('NEW-4001', 'Bob Wilson',  'bob@test.com', '555-1234', NULL,          1, NULL, 'NewSystem');

-- Order: Notes NULL vs value (ignore rule — should pass), TotalAmount NULL vs value (mismatch),
--        TaxRate both NULL (match), SubTotal both have values (match)
INSERT INTO dbo.[Order] (RecordID, CustomerRecordID, OrderDate, OrderStatus, SubTotal, TaxRate, TaxAmount, TotalAmount, DiscountPercent, Notes, ProcessedFlag, CreatedBy)
VALUES
    ('OLD-4001', 'OLD-4001', '2025-08-01', 'Pending', 50.00, NULL, NULL, NULL,    0.0,  NULL,        'Y', 'OldSystem'),
    ('NEW-4001', 'NEW-4001', '2025-08-01', 'Pending', 50.00, NULL, NULL, 100.00,  0.0,  'Some note', 'Y', 'NewSystem');

-- OrderLine: 2 rows
--   Row 1: Description both NULL (fuzzy NULL==NULL = match)
--   Row 2: Description value vs NULL (fuzzy mismatch)
INSERT INTO dbo.OrderLine (RecordID, ProductCode, SizeCode, Quantity, UnitPrice, LineTotal, Description)
VALUES
    ('OLD-4001', 'SKU-X', 'LG', 1, 25.00, 25.00, NULL),
    ('OLD-4001', 'SKU-Y', 'MD', 2, 12.50, 25.00, 'Standard Widget');

INSERT INTO dbo.OrderLine (RecordID, ProductCode, SizeCode, Quantity, UnitPrice, LineTotal, Description)
VALUES
    ('NEW-4001', 'SKU-X', 'LG', 1, 25.00, 25.00, NULL),
    ('NEW-4001', 'SKU-Y', 'MD', 2, 12.50, 25.00, NULL);            -- Description mismatch: value vs NULL

-- Payment: ReferenceNumber both NULL (contains NULL==NULL = match), ProcessedBy both NULL (exact match)
DECLARE @pay1Old INT, @pay1New INT;

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('CreditCard', 50.00, '2025-08-01 12:00:00', NULL, 0, NULL);
SET @pay1Old = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('CreditCard', 50.00, '2025-08-01 12:00:00', NULL, 0, NULL);
SET @pay1New = SCOPE_IDENTITY();

INSERT INTO dbo.OrderPayment (RecordID, PaymentID) VALUES ('OLD-4001', @pay1Old);
INSERT INTO dbo.OrderPayment (RecordID, PaymentID) VALUES ('NEW-4001', @pay1New);

-- AuditLog: 1 entry, Details NULL vs value (fuzzy mismatch)
INSERT INTO dbo.AuditLog (RecordID, Category, Action, Details, UserName)
VALUES
    ('OLD-4001', 'Financial', 'Create', NULL,        'OldSystem'),
    ('NEW-4001', 'Financial', 'Create', 'Processed', 'NewSystem');

-- OrderTag: 1 tag, TagValue value vs NULL (exact mismatch)
INSERT INTO dbo.OrderTag (RecordID, TagName, TagValue)
VALUES
    ('OLD-4001', 'Status', 'Active'),
    ('NEW-4001', 'Status', NULL);

-- Product: GUID NULL==NULL, MONEY NULL vs value, XML both NULL, FLOAT NULL vs value
INSERT INTO dbo.Product (RecordID, ProductGuid, ProductCode, ProductName, ShortCode, Description,
    WeightKg, VolumeLiters, UnitCostPrecise, ListPrice, ClearancePrice,
    Specifications, TagsVarchar, TagsNvarchar)
VALUES
    ('OLD-4001', NULL, 'SKU-NULL', 'Null Test Product', 'NULL',
     NULL, NULL, 0.5, 1.000000, NULL, 4.99,
     NULL, NULL, NULL),
    ('NEW-4001', NULL, 'SKU-NULL', 'Null Test Product', 'NULL',
     NULL, 1.5, 0.5, 1.000000, 19.99, 4.99,     -- WeightKg NULL→value, ListPrice NULL→value
     NULL, NULL, N'has-value');                     -- TagsNvarchar NULL→value

-- Shipment: zero vs NULL, empty string vs NULL, whitespace vs empty
INSERT INTO dbo.Shipment (RecordID, TrackingNumber, ShipDate, DispatchTime, ShippedAtOffset,
    PackageCount, TotalItems, ShipmentWeight, AdjustmentAmount, InsuredValue,
    WeightAsText, CarrierNotes, SpecialInstructions)
VALUES
    ('OLD-4001', 'TRK-4001-A', '2025-08-01', '10:00:00', '2025-08-01 10:00:00 +00:00',
     0, 5, 1000, 0.00, 100.00,
     NULL, '', '   ');        -- WeightAsText NULL, CarrierNotes empty string, SpecialInstructions whitespace

INSERT INTO dbo.Shipment (RecordID, TrackingNumber, ShipDate, DispatchTime, ShippedAtOffset,
    PackageCount, TotalItems, ShipmentWeight, AdjustmentAmount, InsuredValue,
    WeightAsText, CarrierNotes, SpecialInstructions)
VALUES
    ('NEW-4001', 'TRK-4001-A', '2025-08-01', '10:00:00', '2025-08-01 10:00:00 +00:00',
     NULL, 5, 1000, 0.00, 100.00,
     '0.0', NULL, '');        -- PackageCount 0→NULL, WeightAsText NULL→value, CarrierNotes ''→NULL, whitespace→empty

-- InventorySnapshot: QuantityReserved 0 vs NULL, BatchId both NULL, TotalValue NULL==NULL
INSERT INTO dbo.InventorySnapshot (RecordID, WarehouseID, SKU, QuantityOnHand, QuantityReserved,
    ReorderPoint, UnitCost, TotalValue, LastCountedAt, SnapshotTime, BatchId, Notes)
VALUES
    ('OLD-4001', 1, 'SKU-NULL', 20, 0, 5, 1.000000, NULL, '2025-08-01 08:00:00 +00:00', '08:00:00',
     NULL, NULL),
    ('NEW-4001', 1, 'SKU-NULL', 20, NULL, 5, 1.000000, NULL, '2025-08-01 08:00:00 +00:00', '08:00:00',
     NULL, NULL);    -- QuantityReserved 0→NULL mismatch, TotalValue NULL==NULL match, BatchId NULL==NULL match

PRINT '=== Scenario 4 (Null Handling) seeded ===';
GO

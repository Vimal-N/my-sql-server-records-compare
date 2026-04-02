-- ============================================================
-- Scenario 2: Known Mismatches (OLD-2001 / NEW-2001)
-- Deliberate differences across every comparison rule type
-- ============================================================
USE TestCompareDB;
GO

-- Customer: fuzzy fail (below 0.90), exact-ci fail, boolean fail, contains fail
INSERT INTO dbo.Customer (RecordID, FullName, Email, Phone, Address, IsActive, Notes, CreatedBy)
VALUES
    ('OLD-2001', 'John Smith',    'john@test.com', '555-1111', '123 Main St',  1, 'Same notes',  'OldSystem'),
    ('NEW-2001', 'Jonathan Q. Smithson', 'jane@test.com', '555-1111', '456 Oak Ave',   0, 'Same notes',  'NewSystem');

-- Order: date off by 1 day, currency over tolerance, percentage over tolerance, boolean mismatch
-- SubTotal within tolerance (should pass), DiscountPercent matches
INSERT INTO dbo.[Order] (RecordID, CustomerRecordID, OrderDate, OrderStatus, SubTotal, TaxRate, TaxAmount, TotalAmount, DiscountPercent, Notes, ProcessedFlag, CreatedBy)
VALUES
    ('OLD-2001', 'OLD-2001', '2025-03-15', 'Completed', 90.00,  0.075,  6.75,  100.00, 0.10, 'Old notes', 'Y',     'OldSystem'),
    ('NEW-2001', 'NEW-2001', '2025-03-16', 'Completed', 90.005, 0.08,   7.20,  100.50, 0.10, 'New notes', 'false', 'NewSystem');

-- OrderLine: 3 matched rows with mismatches in different columns
INSERT INTO dbo.OrderLine (RecordID, ProductCode, SizeCode, Quantity, UnitPrice, LineTotal, Description)
VALUES
    ('OLD-2001', 'SKU-100', 'LG', 5,  29.99, 149.95, 'Large Blue Widget'),
    ('OLD-2001', 'SKU-200', 'MD', 2,  29.99,  59.98, 'Medium Red Gadget'),
    ('OLD-2001', 'SKU-300', 'SM', 1,  19.99,  19.99, 'Small Green Sprocket');

INSERT INTO dbo.OrderLine (RecordID, ProductCode, SizeCode, Quantity, UnitPrice, LineTotal, Description)
VALUES
    ('NEW-2001', 'SKU-100', 'LG', 7,  29.99, 209.93, 'Large Blue Widget'),       -- Quantity mismatch: 5→7, LineTotal mismatch
    ('NEW-2001', 'SKU-200', 'MD', 2,  35.00,  70.00, 'Medium Red Gadget'),       -- UnitPrice mismatch: 29.99→35.00, LineTotal mismatch
    ('NEW-2001', 'SKU-300', 'SM', 1,  19.99,  19.99, 'Tiny Purple Gizmo');       -- Description fuzzy fail

-- Payment: amount mismatch, contains fail
DECLARE @pay1Old INT, @pay2Old INT, @pay1New INT, @pay2New INT;

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('CreditCard', 75.00, '2025-03-15 14:00:00', 'ABC123', 0, 'Processor1');
SET @pay1Old = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('Cash', 25.00, '2025-03-15 14:05:00', 'DEF456', 0, 'Processor1');
SET @pay2Old = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('CreditCard', 80.00, '2025-03-15 14:00:00', 'XYZ789', 0, 'Processor2');   -- Amount mismatch, contains fail
SET @pay1New = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('Cash', 25.00, '2025-03-15 14:05:00', 'DEF456', 0, 'Processor2');          -- matches
SET @pay2New = SCOPE_IDENTITY();

INSERT INTO dbo.OrderPayment (RecordID, PaymentID) VALUES ('OLD-2001', @pay1Old), ('OLD-2001', @pay2Old);
INSERT INTO dbo.OrderPayment (RecordID, PaymentID) VALUES ('NEW-2001', @pay1New), ('NEW-2001', @pay2New);

-- AuditLog: one entry with substantial detail difference
INSERT INTO dbo.AuditLog (RecordID, Category, Action, Details, UserName)
VALUES
    ('OLD-2001', 'Financial', 'Create',  'Order created for customer John Smith total $100',   'OldSystem'),
    ('OLD-2001', 'Financial', 'Approve', 'Manager approved payment batch',                     'OldSystem');

INSERT INTO dbo.AuditLog (RecordID, Category, Action, Details, UserName)
VALUES
    ('NEW-2001', 'Financial', 'Create',  'Order created for customer John Smith total $100',   'NewSystem'),
    ('NEW-2001', 'Financial', 'Approve', 'Automated system approved after validation check',   'NewSystem');  -- fuzzy fail

-- OrderTag: one value mismatch
INSERT INTO dbo.OrderTag (RecordID, TagName, TagValue)
VALUES
    ('OLD-2001', 'Priority', 'High'),
    ('OLD-2001', 'Region',   'West');

INSERT INTO dbo.OrderTag (RecordID, TagName, TagValue)
VALUES
    ('NEW-2001', 'Priority', 'Low'),     -- exact mismatch
    ('NEW-2001', 'Region',   'West');

-- Product: GUID mismatch, FLOAT precision mismatch, MONEY mismatch, XML diff, Unicode mismatch
INSERT INTO dbo.ProductCategory (CategoryCode, CategoryName, ParentCategoryID, SortOrder, IsVisible, MetadataXml, IconUrl, DisplayNotes)
VALUES ('TOOLS     ', 'Tools', NULL, 2, 1, '<meta><color>red</color></meta>', '/icons/tools.png', 'Tool category');

DECLARE @catId2 INT = SCOPE_IDENTITY();

INSERT INTO dbo.Product (RecordID, ProductGuid, ProductCode, ProductName, ShortCode, Description,
    WeightKg, VolumeLiters, UnitCostPrecise, ListPrice, ClearancePrice,
    Specifications, TagsVarchar, TagsNvarchar, CategoryID)
VALUES
    ('OLD-2001',
     'AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE',
     'SKU-BETA',
     'Standard Wrench',
     'WRNCH',
     'A standard wrench for general use.',
     1.5, 0.8, 5.123456, 15.99, 9.99,
     '<specs><size>medium</size></specs>',
     'tools,hand-tools',
     N'Stra' + NCHAR(0x00DF) + N'e',        -- Straße (German)
     @catId2);

INSERT INTO dbo.Product (RecordID, ProductGuid, ProductCode, ProductName, ShortCode, Description,
    WeightKg, VolumeLiters, UnitCostPrecise, ListPrice, ClearancePrice,
    Specifications, TagsVarchar, TagsNvarchar, CategoryID)
VALUES
    ('NEW-2001',
     'FFFFFFFF-BBBB-CCCC-DDDD-EEEEEEEEEEEE',  -- GUID mismatch (different value entirely)
     'SKU-GAMMA',                               -- CHAR mismatch
     'Standard Wrench',
     'WRNCH',
     'A standard wrench for general use.',
     1.50001,                                   -- FLOAT: 0.00001 diff (exceeds 0.0001 tolerance)
     0.8,
     5.123457,                                  -- DECIMAL(18,6): 0.000001 diff (at tolerance boundary)
     16.50,                                     -- MONEY mismatch
     9.99,
     '<specs><size>large</size></specs>',        -- XML mismatch
     'tools,hand-tools',
     N'Strasse',                                -- Unicode mismatch (no ß)
     @catId2);

-- Shipment: TIME mismatch, DATETIMEOFFSET mismatch, BIGINT large number mismatch, negative number mismatch
INSERT INTO dbo.Shipment (RecordID, TrackingNumber, ShipDate, DispatchTime, ShippedAtOffset,
    PackageCount, TotalItems, ShipmentWeight, AdjustmentAmount, InsuredValue,
    WeightAsText, CarrierNotes, SpecialInstructions)
VALUES
    ('OLD-2001', 'TRK-2001-A', '2025-03-15', '14:30:00', '2025-03-15 14:30:00 +05:30',
     3, 15, 9223372036854775000, -15.50, 500.00,
     '12.5', 'Handle with care', 'Ring doorbell');

INSERT INTO dbo.Shipment (RecordID, TrackingNumber, ShipDate, DispatchTime, ShippedAtOffset,
    PackageCount, TotalItems, ShipmentWeight, AdjustmentAmount, InsuredValue,
    WeightAsText, CarrierNotes, SpecialInstructions)
VALUES
    ('NEW-2001', 'TRK-2001-A', '2025-03-15', '15:45:00', '2025-03-15 16:00:00 +05:30',  -- TIME + DATETIMEOFFSET mismatch
     3, 20, 9223372036854770000, -100.00, 500.00,    -- SMALLINT, BIGINT, negative mismatch
     '15.0', 'Fragile', 'Ring doorbell');             -- WeightAsText, CarrierNotes mismatch

-- InventorySnapshot: LEFT JOIN, amount mismatches
INSERT INTO dbo.InventorySnapshot (RecordID, WarehouseID, SKU, QuantityOnHand, QuantityReserved,
    ReorderPoint, UnitCost, TotalValue, LastCountedAt, SnapshotTime, BatchId, Notes)
VALUES
    ('OLD-2001', 1, 'SKU-200', 100, 10, 20, 5.123456, 512.35, '2025-03-15 08:00:00 +00:00', '08:00:00',
     'D1D2D3D4-E5E6-7890-ABCD-EF1234567890', 'Stock check');

INSERT INTO dbo.InventorySnapshot (RecordID, WarehouseID, SKU, QuantityOnHand, QuantityReserved,
    ReorderPoint, UnitCost, TotalValue, LastCountedAt, SnapshotTime, BatchId, Notes)
VALUES
    ('NEW-2001', 1, 'SKU-200', 95, 10, 20, 5.200000, 494.00, '2025-03-15 08:00:00 +00:00', '09:30:00',
     'D1D2D3D4-E5E6-7890-ABCD-EF1234567890', 'Stock check');  -- Qty, UnitCost, TotalValue, SnapshotTime mismatch

PRINT '=== Scenario 2 (Known Mismatches) seeded ===';
GO

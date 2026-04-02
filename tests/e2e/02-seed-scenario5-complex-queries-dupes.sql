-- ============================================================
-- Scenario 5: Complex Queries & Duplicate Keys (OLD-5001 / NEW-5001)
-- Custom query correctness, filtered subsets, duplicate key detection
-- ============================================================
USE TestCompareDB;
GO

-- Customer: simple match
INSERT INTO dbo.Customer (RecordID, FullName, Email, Phone, Address, IsActive, Notes, CreatedBy)
VALUES
    ('OLD-5001', 'Carol Davis',  'carol@test.com', '555-5001', '321 Elm St',  1, 'VIP', 'OldSystem'),
    ('NEW-5001', 'Carol Davis',  'carol@test.com', '555-5001', '321 Elm St',  1, 'VIP', 'NewSystem');

-- Order: simple match
INSERT INTO dbo.[Order] (RecordID, CustomerRecordID, OrderDate, OrderStatus, SubTotal, TaxRate, TaxAmount, TotalAmount, DiscountPercent, Notes, ProcessedFlag, CreatedBy)
VALUES
    ('OLD-5001', 'OLD-5001', '2025-09-01', 'Completed', 200.00, 0.06, 12.00, 212.00, 0.05, NULL, 'Yes', 'OldSystem'),
    ('NEW-5001', 'NEW-5001', '2025-09-01', 'Completed', 200.00, 6.0,  12.00, 212.00, 5.0,  NULL, 'Yes', 'NewSystem');

-- OrderLine: 2 standard rows, matched, passing
INSERT INTO dbo.OrderLine (RecordID, ProductCode, SizeCode, Quantity, UnitPrice, LineTotal, Description)
VALUES
    ('OLD-5001', 'SKU-P', 'LG', 2, 50.00, 100.00, 'Premium Large Item'),
    ('OLD-5001', 'SKU-Q', 'SM', 4, 25.00, 100.00, 'Standard Small Item');

INSERT INTO dbo.OrderLine (RecordID, ProductCode, SizeCode, Quantity, UnitPrice, LineTotal, Description)
VALUES
    ('NEW-5001', 'SKU-P', 'LG', 2, 50.00, 100.00, 'Premium Large Item'),
    ('NEW-5001', 'SKU-Q', 'SM', 4, 25.00, 100.00, 'Standard Small Item');

-- Payment: 3 per side via junction. CreditCard has amount diff.
-- Also insert an unrelated payment linked to a DIFFERENT RecordID to prove query scoping
DECLARE @pay1Old INT, @pay2Old INT, @pay3Old INT;
DECLARE @pay1New INT, @pay2New INT, @pay3New INT, @payUnrelated INT;

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('CreditCard', 150.00, '2025-09-01 10:00:00', 'CC-5001', 0, 'Proc1');
SET @pay1Old = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('Cash', 50.00, '2025-09-01 10:05:00', 'CA-5001', 0, 'Proc1');
SET @pay2Old = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('GiftCard', 12.00, '2025-09-01 10:10:00', 'GC-5001', 0, 'Proc1');
SET @pay3Old = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('CreditCard', 155.00, '2025-09-01 10:00:00', 'CC-5001', 0, 'Proc2');    -- Amount diff: 150→155
SET @pay1New = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('Cash', 50.00, '2025-09-01 10:05:00', 'CA-5001', 0, 'Proc2');
SET @pay2New = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('GiftCard', 12.00, '2025-09-01 10:10:00', 'GC-5001', 0, 'Proc2');
SET @pay3New = SCOPE_IDENTITY();

-- Unrelated payment for a different order — should NOT appear in results
INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('Wire', 9999.99, '2025-09-01 11:00:00', 'UNRELATED', 0, 'Nobody');
SET @payUnrelated = SCOPE_IDENTITY();

INSERT INTO dbo.OrderPayment (RecordID, PaymentID) VALUES ('OLD-5001', @pay1Old), ('OLD-5001', @pay2Old), ('OLD-5001', @pay3Old);
INSERT INTO dbo.OrderPayment (RecordID, PaymentID) VALUES ('NEW-5001', @pay1New), ('NEW-5001', @pay2New), ('NEW-5001', @pay3New);
INSERT INTO dbo.OrderPayment (RecordID, PaymentID) VALUES ('UNRELATED-9999', @payUnrelated);  -- different RecordID

-- AuditLog: filtered query tests — 2 Financial + 2 Status + 1 System per side
-- Only Financial should appear. Financial entries match.
INSERT INTO dbo.AuditLog (RecordID, Category, Action, Details, UserName)
VALUES
    ('OLD-5001', 'Financial', 'Create',  'Order created',              'OldSystem'),
    ('OLD-5001', 'Financial', 'Approve', 'Payment verified',           'OldSystem'),
    ('OLD-5001', 'Status',   'Submit',   'Submitted to warehouse',     'OldSystem'),
    ('OLD-5001', 'Status',   'Ship',     'Shipped via FedEx',          'OldSystem'),
    ('OLD-5001', 'System',   'Log',      'Background job completed',   'OldSystem');

INSERT INTO dbo.AuditLog (RecordID, Category, Action, Details, UserName)
VALUES
    ('NEW-5001', 'Financial', 'Create',  'Order created',              'NewSystem'),
    ('NEW-5001', 'Financial', 'Approve', 'Payment verified',           'NewSystem'),
    ('NEW-5001', 'Status',   'Submit',   'Submitted to warehouse',     'NewSystem'),
    ('NEW-5001', 'Status',   'Ship',     'Shipped via FedEx',          'NewSystem'),
    ('NEW-5001', 'System',   'Log',      'Background job completed',   'NewSystem');

-- OrderTag: DUPLICATE KEY scenario
-- Old has 3 tags, two share TagName='Priority' → duplicate key warning
-- New also has 2 tags with TagName='Priority'
INSERT INTO dbo.OrderTag (RecordID, TagName, TagValue)
VALUES
    ('OLD-5001', 'Priority', 'High'),
    ('OLD-5001', 'Priority', 'Urgent'),     -- duplicate TagName
    ('OLD-5001', 'Region',   'East');

INSERT INTO dbo.OrderTag (RecordID, TagName, TagValue)
VALUES
    ('NEW-5001', 'Priority', 'High'),
    ('NEW-5001', 'Priority', 'Critical'),   -- duplicate TagName (different value from old's 2nd)
    ('NEW-5001', 'Region',   'East');

-- Product + ProductCategory: test subquery custom query pattern
-- Both systems reference the same category
DECLARE @catId5 INT;
INSERT INTO dbo.ProductCategory (CategoryCode, CategoryName, ParentCategoryID, SortOrder, IsVisible, MetadataXml, IconUrl, DisplayNotes)
VALUES ('PREMIUM   ', 'Premium Items', NULL, 3, 1, '<meta><tier>gold</tier></meta>', '/icons/premium.png', 'Premium tier products');
SET @catId5 = SCOPE_IDENTITY();

INSERT INTO dbo.Product (RecordID, ProductGuid, ProductCode, ProductName, ShortCode, Description,
    WeightKg, VolumeLiters, UnitCostPrecise, ListPrice, ClearancePrice,
    Specifications, TagsVarchar, TagsNvarchar, CategoryID)
VALUES
    ('OLD-5001', 'F1F2F3F4-A5A6-7890-ABCD-EF1234567890', 'SKU-PREM', 'Premium Deluxe', 'PREM',
     'A premium product.', 3.0, 2.0, 25.000000, 99.99, 79.99,
     '<specs><material>titanium</material></specs>', 'premium', N'premium', @catId5),
    ('NEW-5001', 'f1f2f3f4-a5a6-7890-abcd-ef1234567890', 'SKU-PREM', 'Premium Deluxe', 'PREM',
     'A premium product.', 3.0, 2.0, 25.000000, 99.99, 79.99,
     '<specs><material>titanium</material></specs>', 'premium', N'premium', @catId5);

-- Shipment: standard match for complex scenario
INSERT INTO dbo.Shipment (RecordID, TrackingNumber, ShipDate, DispatchTime, ShippedAtOffset,
    PackageCount, TotalItems, ShipmentWeight, AdjustmentAmount, InsuredValue,
    WeightAsText, CarrierNotes, SpecialInstructions)
VALUES
    ('OLD-5001', 'TRK-5001-A', '2025-09-01', '16:00:00', '2025-09-01 16:00:00 +00:00',
     2, 8, 20000, 0.00, 800.00, '20.0', 'Insured shipment', 'VIP customer'),
    ('NEW-5001', 'TRK-5001-A', '2025-09-01', '16:00:00', '2025-09-01 16:00:00 +00:00',
     2, 8, 20000, 0.00, 800.00, '20.0', 'Insured shipment', 'VIP customer');

-- InventorySnapshot: LEFT JOIN, including a row with WarehouseID=999 (no matching warehouse → NULL from LEFT JOIN)
INSERT INTO dbo.InventorySnapshot (RecordID, WarehouseID, SKU, QuantityOnHand, QuantityReserved,
    ReorderPoint, UnitCost, TotalValue, LastCountedAt, SnapshotTime, BatchId, Notes)
VALUES
    ('OLD-5001', 1, 'SKU-PREM', 20, 2, 5, 25.000000, 500.00, '2025-09-01 08:00:00 +00:00', '08:00:00',
     'A1A1A1A1-B2B2-C3C3-D4D4-E5E5E5E5E5E5', 'Premium stock'),
    ('OLD-5001', 999, 'SKU-PREM', 5, 0, 1, 25.000000, 125.00, '2025-09-01 10:00:00 +00:00', '10:00:00',
     'A1A1A1A1-B2B2-C3C3-D4D4-E5E5E5E5E5E6', 'Overflow');   -- WarehouseID=999 → LEFT JOIN gives NULL WarehouseName

INSERT INTO dbo.InventorySnapshot (RecordID, WarehouseID, SKU, QuantityOnHand, QuantityReserved,
    ReorderPoint, UnitCost, TotalValue, LastCountedAt, SnapshotTime, BatchId, Notes)
VALUES
    ('NEW-5001', 1, 'SKU-PREM', 20, 2, 5, 25.000000, 500.00, '2025-09-01 08:00:00 +00:00', '08:00:00',
     'a1a1a1a1-b2b2-c3c3-d4d4-e5e5e5e5e5e5', 'Premium stock'),
    ('NEW-5001', 999, 'SKU-PREM', 5, 0, 1, 25.000000, 125.00, '2025-09-01 10:00:00 +00:00', '10:00:00',
     'a1a1a1a1-b2b2-c3c3-d4d4-e5e5e5e5e5e6', 'Overflow');

PRINT '=== Scenario 5 (Complex Queries & Duplicate Keys) seeded ===';
GO

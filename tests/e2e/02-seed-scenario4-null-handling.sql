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

PRINT '=== Scenario 4 (Null Handling) seeded ===';
GO

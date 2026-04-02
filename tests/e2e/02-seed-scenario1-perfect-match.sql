-- ============================================================
-- Scenario 1: Perfect Match (OLD-1001 / NEW-1001)
-- Every rule type sees equivalent data → zero mismatches
-- ============================================================
USE TestCompareDB;
GO

-- Customer: exact-ci email, fuzzy name (high similarity), boolean bit, contains address
INSERT INTO dbo.Customer (RecordID, FullName, Email, Phone, Address, IsActive, Notes, CreatedBy)
VALUES
    ('OLD-1001', 'John A. Smith',  'John.Smith@Example.COM', '555-1234', '123 Main St',          1, 'Preferred customer', 'OldSystem'),
    ('NEW-1001', 'John A. Smith',  'john.smith@example.com', '555-1234', '123 Main St, Suite 5', 1, 'Preferred customer', 'NewSystem');

-- Order: date match, currency within tolerance, percentage normalization, boolean Y/true, ignore Notes
INSERT INTO dbo.[Order] (RecordID, CustomerRecordID, OrderDate, OrderStatus, SubTotal, TaxRate, TaxAmount, TotalAmount, DiscountPercent, Notes, ProcessedFlag, CreatedBy)
VALUES
    ('OLD-1001', 'OLD-1001', '2025-06-15', 'Completed', 250.00, 0.075,  18.75, 268.75, 0.10,  'Old system notes here',     'Y',    'OldSystem'),
    ('NEW-1001', 'NEW-1001', '2025-06-15', 'Completed', 250.00, 7.5,    18.75, 268.75, 10.0,  'New system notes differ',   'true', 'NewSystem');

-- OrderLine: 3 rows, different insert order, descriptions similar (above 0.85 fuzzy)
INSERT INTO dbo.OrderLine (RecordID, ProductCode, SizeCode, Quantity, UnitPrice, LineTotal, Description)
VALUES
    ('OLD-1001', 'SKU-100', 'LG', 5, 29.99, 149.95, 'Large Blue Widget'),
    ('OLD-1001', 'SKU-200', 'MD', 2, 49.99,  99.98, 'Medium Red Gadget'),
    ('OLD-1001', 'SKU-300', 'SM', 1, 19.99,  19.99, 'Small Green Sprocket');

INSERT INTO dbo.OrderLine (RecordID, ProductCode, SizeCode, Quantity, UnitPrice, LineTotal, Description)
VALUES
    ('NEW-1001', 'SKU-300', 'SM', 1, 19.99,  19.99, 'Small Green Sprocket'),
    ('NEW-1001', 'SKU-100', 'LG', 5, 29.99, 149.95, 'Large Blue Widget'),
    ('NEW-1001', 'SKU-200', 'MD', 2, 49.99,  99.98, 'Medium Red Gadget');

-- Payment: via junction, boolean 0/false, contains REF- prefix, currency match
DECLARE @pay1Old INT, @pay2Old INT, @pay1New INT, @pay2New INT;

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('CreditCard', 200.00, '2025-06-15 10:30:00', '12345', 0, 'OldProcessor');
SET @pay1Old = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('Cash', 68.75, '2025-06-15 10:31:00', '12346', 0, 'OldProcessor');
SET @pay2Old = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('CreditCard', 200.00, '2025-06-15 10:30:00', 'REF-12345', 0, 'NewProcessor');
SET @pay1New = SCOPE_IDENTITY();

INSERT INTO dbo.Payment (PaymentType, Amount, PaymentDate, ReferenceNumber, IsRefund, ProcessedBy)
VALUES ('Cash', 68.75, '2025-06-15 10:31:00', 'REF-12346', 0, 'NewProcessor');
SET @pay2New = SCOPE_IDENTITY();

INSERT INTO dbo.OrderPayment (RecordID, PaymentID) VALUES ('OLD-1001', @pay1Old), ('OLD-1001', @pay2Old);
INSERT INTO dbo.OrderPayment (RecordID, PaymentID) VALUES ('NEW-1001', @pay1New), ('NEW-1001', @pay2New);

-- AuditLog: 2 Financial (matched), 1 Status (filtered out by custom query)
INSERT INTO dbo.AuditLog (RecordID, Category, Action, Details, UserName)
VALUES
    ('OLD-1001', 'Financial', 'Create',  'Order created with total 268.75',  'OldSystem'),
    ('OLD-1001', 'Financial', 'Approve', 'Payment approved',                 'OldSystem'),
    ('OLD-1001', 'Status',   'Submit',   'Order submitted for processing',   'OldSystem');

INSERT INTO dbo.AuditLog (RecordID, Category, Action, Details, UserName)
VALUES
    ('NEW-1001', 'Financial', 'Create',  'Order created with total 268.75',  'NewSystem'),
    ('NEW-1001', 'Financial', 'Approve', 'Payment approved',                 'NewSystem'),
    ('NEW-1001', 'Status',   'Submit',   'Order submitted for processing',   'NewSystem');

-- OrderTag: 2 unique tags, exact match
INSERT INTO dbo.OrderTag (RecordID, TagName, TagValue)
VALUES
    ('OLD-1001', 'Priority', 'High'),
    ('OLD-1001', 'Region',   'West');

INSERT INTO dbo.OrderTag (RecordID, TagName, TagValue)
VALUES
    ('NEW-1001', 'Priority', 'High'),
    ('NEW-1001', 'Region',   'West');

PRINT '=== Scenario 1 (Perfect Match) seeded ===';
GO

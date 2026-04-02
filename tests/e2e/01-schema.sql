-- ============================================================
-- TestCompareDB — Schema for E2E testing
-- ============================================================
USE master;
GO

IF DB_ID('TestCompareDB') IS NOT NULL
BEGIN
    ALTER DATABASE TestCompareDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TestCompareDB;
END
GO

CREATE DATABASE TestCompareDB;
GO

USE TestCompareDB;
GO

-- ----------------------------------------------------------
-- Customer: single-row header (simple query mode)
-- Tests: exact, exact-ci, fuzzy, boolean, contains rules
-- ----------------------------------------------------------
CREATE TABLE dbo.Customer (
    CustomerID    INT IDENTITY(1,1) PRIMARY KEY,
    RecordID      NVARCHAR(50)  NOT NULL,
    FullName      NVARCHAR(200),
    Email         NVARCHAR(200),
    Phone         NVARCHAR(50),
    Address       NVARCHAR(500),
    IsActive      BIT,
    Notes         NVARCHAR(MAX),
    CreatedDate   DATETIME2     DEFAULT SYSUTCDATETIME(),
    CreatedBy     NVARCHAR(100),
    ModifiedDate  DATETIME2     DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_Customer_RecordID ON dbo.Customer(RecordID);

-- ----------------------------------------------------------
-- Order: single-row header (simple query mode)
-- Tests: date, currency, percentage, numeric, boolean, ignore
-- ----------------------------------------------------------
CREATE TABLE dbo.[Order] (
    OrderID         INT IDENTITY(1,1) PRIMARY KEY,
    RecordID        NVARCHAR(50)   NOT NULL,
    CustomerRecordID NVARCHAR(50),
    OrderDate       DATE,
    OrderStatus     NVARCHAR(50),
    SubTotal        DECIMAL(18,2),
    TaxRate         DECIMAL(8,4),
    TaxAmount       DECIMAL(18,2),
    TotalAmount     DECIMAL(18,2),
    DiscountPercent DECIMAL(8,4),
    Notes           NVARCHAR(MAX),
    ProcessedFlag   NVARCHAR(10),
    CreatedDate     DATETIME2      DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(100),
    ModifiedDate    DATETIME2      DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_Order_RecordID ON dbo.[Order](RecordID);

-- ----------------------------------------------------------
-- OrderLine: multi-row child (simple query, RowMatchColumns)
-- Tests: row matching, currency, fuzzy, unmatched rows
-- ----------------------------------------------------------
CREATE TABLE dbo.OrderLine (
    OrderLineID   INT IDENTITY(1,1) PRIMARY KEY,
    RecordID      NVARCHAR(50)  NOT NULL,
    ProductCode   NVARCHAR(50),
    SizeCode      NVARCHAR(20),
    Quantity      INT,
    UnitPrice     DECIMAL(18,2),
    LineTotal     DECIMAL(18,2),
    Description   NVARCHAR(500),
    CreatedDate   DATETIME2     DEFAULT SYSUTCDATETIME(),
    ModifiedDate  DATETIME2     DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_OrderLine_RecordID ON dbo.OrderLine(RecordID);

-- ----------------------------------------------------------
-- Payment: multi-row, NO RecordID (custom query via junction)
-- Tests: CustomQuery JOIN, currency, datetime, contains, boolean
-- ----------------------------------------------------------
CREATE TABLE dbo.Payment (
    PaymentID       INT IDENTITY(1,1) PRIMARY KEY,
    PaymentType     NVARCHAR(50),
    Amount          DECIMAL(18,2),
    PaymentDate     DATETIME2,
    ReferenceNumber NVARCHAR(100),
    IsRefund        BIT,
    ProcessedBy     NVARCHAR(100)
);

-- ----------------------------------------------------------
-- OrderPayment: junction table (not compared directly)
-- ----------------------------------------------------------
CREATE TABLE dbo.OrderPayment (
    OrderPaymentID  INT IDENTITY(1,1) PRIMARY KEY,
    RecordID        NVARCHAR(50)  NOT NULL,
    PaymentID       INT           NOT NULL
);
CREATE INDEX IX_OrderPayment_RecordID ON dbo.OrderPayment(RecordID);

-- ----------------------------------------------------------
-- AuditLog: multi-row, custom query with WHERE filter
-- Tests: filtered CustomQuery, fuzzy
-- ----------------------------------------------------------
CREATE TABLE dbo.AuditLog (
    AuditLogID    INT IDENTITY(1,1) PRIMARY KEY,
    RecordID      NVARCHAR(50)  NOT NULL,
    Category      NVARCHAR(50),
    Action        NVARCHAR(100),
    Details       NVARCHAR(MAX),
    LogTimestamp  DATETIME2     DEFAULT SYSUTCDATETIME(),
    UserName      NVARCHAR(100)
);
CREATE INDEX IX_AuditLog_RecordID ON dbo.AuditLog(RecordID);

-- ----------------------------------------------------------
-- OrderTag: multi-row, simple query, duplicate key testing
-- ----------------------------------------------------------
CREATE TABLE dbo.OrderTag (
    OrderTagID    INT IDENTITY(1,1) PRIMARY KEY,
    RecordID      NVARCHAR(50)  NOT NULL,
    TagName       NVARCHAR(100),
    TagValue      NVARCHAR(200)
);
CREATE INDEX IX_OrderTag_RecordID ON dbo.OrderTag(RecordID);

-- ----------------------------------------------------------
-- Product: single-row (simple query mode)
-- Tests: GUID, CHAR/NCHAR, FLOAT/REAL, DECIMAL(18,6),
--        MONEY/SMALLMONEY, XML, VARCHAR vs NVARCHAR,
--        very long strings, Unicode
-- ----------------------------------------------------------
CREATE TABLE dbo.Product (
    ProductID       INT IDENTITY(1,1) PRIMARY KEY,
    RecordID        NVARCHAR(50)    NOT NULL,
    ProductGuid     UNIQUEIDENTIFIER,
    ProductCode     CHAR(20),
    ProductName     NVARCHAR(200),
    ShortCode       NCHAR(10),
    Description     NVARCHAR(MAX),
    WeightKg        FLOAT,
    VolumeLiters    REAL,
    UnitCostPrecise DECIMAL(18,6),
    ListPrice       MONEY,
    ClearancePrice  SMALLMONEY,
    Specifications  XML,
    TagsVarchar     VARCHAR(100),
    TagsNvarchar    NVARCHAR(100),
    CreatedDate     DATETIME2       DEFAULT SYSUTCDATETIME(),
    ModifiedDate    DATETIME2       DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_Product_RecordID ON dbo.Product(RecordID);

-- ----------------------------------------------------------
-- Shipment: multi-row (simple query, RowMatchColumns)
-- Tests: TIME, DATETIMEOFFSET, BIGINT/SMALLINT/TINYINT,
--        negative numbers, zero vs NULL, numbers as varchar,
--        empty string vs NULL, whitespace-only strings
-- ----------------------------------------------------------
CREATE TABLE dbo.Shipment (
    ShipmentID          INT IDENTITY(1,1) PRIMARY KEY,
    RecordID            NVARCHAR(50)    NOT NULL,
    TrackingNumber      NVARCHAR(50),
    ShipDate            DATE,
    DispatchTime        TIME(7),
    ShippedAtOffset     DATETIMEOFFSET(7),
    PackageCount        TINYINT,
    TotalItems          SMALLINT,
    ShipmentWeight      BIGINT,
    AdjustmentAmount    DECIMAL(18,2),
    InsuredValue        DECIMAL(18,2),
    WeightAsText        VARCHAR(50),
    CarrierNotes        NVARCHAR(500),
    SpecialInstructions NVARCHAR(200),
    CreatedDate         DATETIME2       DEFAULT SYSUTCDATETIME(),
    ModifiedDate        DATETIME2       DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_Shipment_RecordID ON dbo.Shipment(RecordID);

-- ----------------------------------------------------------
-- ProductCategory: custom query with subquery pattern
-- Tests: WHERE col IN (SELECT ...), CHAR in custom query, XML
-- ----------------------------------------------------------
CREATE TABLE dbo.ProductCategory (
    CategoryID       INT IDENTITY(1,1) PRIMARY KEY,
    CategoryCode     CHAR(10),
    CategoryName     NVARCHAR(100),
    ParentCategoryID INT NULL,
    SortOrder        SMALLINT,
    IsVisible        BIT,
    MetadataXml      XML NULL,
    IconUrl          VARCHAR(500),
    DisplayNotes     NVARCHAR(MAX)
);

-- Link product to category (column on Product table)
ALTER TABLE dbo.Product ADD CategoryID INT NULL;

-- ----------------------------------------------------------
-- Warehouse: lookup table (not compared directly)
-- ----------------------------------------------------------
CREATE TABLE dbo.Warehouse (
    WarehouseID     INT IDENTITY(1,1) PRIMARY KEY,
    WarehouseName   NVARCHAR(100),
    TimeZoneOffset  NVARCHAR(10)
);

-- Seed warehouses (shared reference data)
INSERT INTO dbo.Warehouse (WarehouseName, TimeZoneOffset) VALUES ('East Coast DC', '+05:00');
INSERT INTO dbo.Warehouse (WarehouseName, TimeZoneOffset) VALUES ('West Coast DC', '-08:00');
INSERT INTO dbo.Warehouse (WarehouseName, TimeZoneOffset) VALUES ('Central Hub',   '+00:00');

-- ----------------------------------------------------------
-- InventorySnapshot: custom query with LEFT JOIN
-- Tests: LEFT JOIN with NULLs, composite row-match key in
--        custom query, GUID + MONEY + TIME + DATETIMEOFFSET
-- ----------------------------------------------------------
CREATE TABLE dbo.InventorySnapshot (
    SnapshotID      INT IDENTITY(1,1) PRIMARY KEY,
    RecordID        NVARCHAR(50)    NOT NULL,
    WarehouseID     INT,
    SKU             NVARCHAR(50),
    QuantityOnHand  INT,
    QuantityReserved INT,
    ReorderPoint    SMALLINT,
    UnitCost        DECIMAL(18,6),
    TotalValue      MONEY,
    LastCountedAt   DATETIMEOFFSET(7),
    SnapshotTime    TIME(7),
    BatchId         UNIQUEIDENTIFIER,
    Notes           NVARCHAR(MAX),
    CreatedDate     DATETIME2       DEFAULT SYSUTCDATETIME(),
    ModifiedDate    DATETIME2       DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_InventorySnapshot_RecordID ON dbo.InventorySnapshot(RecordID);

PRINT '=== Schema created successfully ===';
GO

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

PRINT '=== Schema created successfully ===';
GO

using MsSqlRecordsCompare.Core.Comparison.Models;

namespace MsSqlRecordsCompare.Tests.TestHelpers;

public static class MockComparisonData
{
    public static ComparisonResult CreatePassingResult()
    {
        return new ComparisonResult
        {
            ConfigFile = "test-config.xlsx",
            TableSet = "Tables-Orders",
            ServerName = "TestServer\\SQL",
            DatabaseName = "TestDB",
            UserName = "DOMAIN\\testuser",
            RunTimestamp = new DateTime(2026, 3, 31, 14, 22, 5),
            Scenarios =
            [
                new ScenarioResult
                {
                    Scenario = "Basic-Order",
                    OldRecordId = "100234",
                    NewRecordId = "200891",
                    TableResults =
                    [
                        new TableResult
                        {
                            TableName = "Order",
                            Schema = "dbo",
                            OldRowCount = 1,
                            NewRowCount = 1,
                            RowResults =
                            [
                                new RowComparisonResult
                                {
                                    Matches =
                                    [
                                        new ColumnMatch { ColumnName = "OrderStatus", Value = "Confirmed", CompareRule = "exact" },
                                        new ColumnMatch { ColumnName = "TotalAmount", Value = "1234.56", CompareRule = "currency" }
                                    ]
                                }
                            ],
                            ExcludedColumns = [new ExcludedColumn { ColumnName = "CreatedDate", Reason = "Timestamps" }]
                        }
                    ]
                }
            ]
        };
    }

    public static ComparisonResult CreateResultWithMismatches()
    {
        return new ComparisonResult
        {
            ConfigFile = "test-config.xlsx",
            TableSet = "Tables-Orders",
            ServerName = "TestServer\\SQL",
            DatabaseName = "TestDB",
            UserName = "DOMAIN\\testuser",
            RunTimestamp = new DateTime(2026, 3, 31, 14, 22, 5),
            Scenarios =
            [
                new ScenarioResult
                {
                    Scenario = "Basic-Order",
                    OldRecordId = "100234",
                    NewRecordId = "200891",
                    TableResults =
                    [
                        new TableResult
                        {
                            TableName = "Order",
                            Schema = "dbo",
                            OldRowCount = 1,
                            NewRowCount = 1,
                            RowResults =
                            [
                                new RowComparisonResult
                                {
                                    Mismatches =
                                    [
                                        new ColumnMismatch { ColumnName = "ShippingMethod", OldValue = "Express", NewValue = "EXPRESS", CompareRule = "exact" },
                                        new ColumnMismatch { ColumnName = "DiscountCode", OldValue = "SAVE10", NewValue = "<NULL>", CompareRule = "exact" }
                                    ],
                                    Matches =
                                    [
                                        new ColumnMatch { ColumnName = "OrderStatus", Value = "Confirmed", CompareRule = "exact" }
                                    ]
                                }
                            ],
                            ExcludedColumns = [new ExcludedColumn { ColumnName = "CreatedDate", Reason = "Timestamps" }]
                        },
                        new TableResult
                        {
                            TableName = "OrderLine",
                            Schema = "dbo",
                            OldRowCount = 2,
                            NewRowCount = 2,
                            RowResults =
                            [
                                new RowComparisonResult
                                {
                                    MatchKey = "SKU-100",
                                    Matches = [new ColumnMatch { ColumnName = "Qty", Value = "5", CompareRule = "exact" }]
                                },
                                new RowComparisonResult
                                {
                                    MatchKey = "SKU-200",
                                    Matches = [new ColumnMatch { ColumnName = "Qty", Value = "2", CompareRule = "exact" }]
                                }
                            ]
                        }
                    ]
                },
                new ScenarioResult
                {
                    Scenario = "Express-Shipping",
                    OldRecordId = "100235",
                    NewRecordId = "200892",
                    TableResults =
                    [
                        new TableResult
                        {
                            TableName = "Order",
                            Schema = "dbo",
                            OldRowCount = 1,
                            NewRowCount = 1,
                            RowResults =
                            [
                                new RowComparisonResult
                                {
                                    Matches = [new ColumnMatch { ColumnName = "OrderStatus", Value = "Shipped", CompareRule = "exact" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}

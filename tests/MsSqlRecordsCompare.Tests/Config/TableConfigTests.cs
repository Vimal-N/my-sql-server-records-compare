using FluentAssertions;
using MsSqlRecordsCompare.Core.Config;

namespace MsSqlRecordsCompare.Tests.Config;

public class TableConfigTests
{
    [Fact]
    public void GetQuery_WithRecordIdColumn_GeneratesSelectQuery()
    {
        var table = new TableConfig
        {
            TableName = "Order",
            Schema = "dbo",
            RecordIdColumn = "RecordID"
        };

        table.GetQuery().Should().Be("SELECT * FROM [dbo].[Order] WHERE [RecordID] = @RecordID");
    }

    [Fact]
    public void GetQuery_WithCustomQuery_ReturnsCustomQuery()
    {
        var customSql = "SELECT p.* FROM Payment p INNER JOIN OrderPayment op ON p.PaymentID = op.PaymentID WHERE op.RecordID = @RecordID";
        var table = new TableConfig
        {
            TableName = "Payment",
            CustomQuery = customSql
        };

        table.GetQuery().Should().Be(customSql);
    }

    [Fact]
    public void GetQuery_WithBothColumnsAndCustomQuery_PrefersCustomQuery()
    {
        var table = new TableConfig
        {
            TableName = "Order",
            RecordIdColumn = "RecordID",
            CustomQuery = "SELECT * FROM Orders WHERE ID = @RecordID"
        };

        table.UsesCustomQuery.Should().BeTrue();
        table.GetQuery().Should().Contain("ID = @RecordID");
    }

    [Fact]
    public void GetQuery_NeitherColumnNorCustomQuery_Throws()
    {
        var table = new TableConfig { TableName = "Order" };
        var act = () => table.GetQuery();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Validate_NeitherColumnNorCustomQuery_Throws()
    {
        var table = new TableConfig { TableName = "Order" };
        var act = () => table.Validate();
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*RecordIDColumn or CustomQuery*");
    }

    [Fact]
    public void Validate_EmptyTableName_Throws()
    {
        var table = new TableConfig { TableName = "", RecordIdColumn = "ID" };
        var act = () => table.Validate();
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*TableName*empty*");
    }

    [Fact]
    public void Validate_CustomQueryMissingRecordIdPlaceholder_Throws()
    {
        var table = new TableConfig
        {
            TableName = "Order",
            CustomQuery = "SELECT * FROM Order WHERE ID = @SomeOtherId"
        };

        var act = () => table.Validate();
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*@RecordID*");
    }

    [Fact]
    public void Validate_CustomQueryWithInsert_Throws()
    {
        var table = new TableConfig
        {
            TableName = "Order",
            CustomQuery = "INSERT INTO Temp SELECT * FROM Order WHERE RecordID = @RecordID"
        };

        var act = () => table.Validate();
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*SELECT*INSERT*");
    }

    [Fact]
    public void Validate_CustomQueryWithDelete_Throws()
    {
        var table = new TableConfig
        {
            TableName = "Order",
            CustomQuery = "DELETE FROM Order WHERE RecordID = @RecordID"
        };

        var act = () => table.Validate();
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*SELECT*");
    }

    [Fact]
    public void Validate_CustomQueryWithDrop_Throws()
    {
        var table = new TableConfig
        {
            TableName = "Order",
            CustomQuery = "DROP TABLE Order; SELECT * FROM Order WHERE RecordID = @RecordID"
        };

        var act = () => table.Validate();
        act.Should().Throw<ConfigValidationException>();
    }

    [Fact]
    public void Validate_ValidCustomQuery_DoesNotThrow()
    {
        var table = new TableConfig
        {
            TableName = "Payment",
            CustomQuery = "SELECT p.* FROM Payment p WHERE p.RecordID = @RecordID AND p.Status = 'Active'"
        };

        var act = () => table.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ValidRecordIdColumn_DoesNotThrow()
    {
        var table = new TableConfig
        {
            TableName = "Order",
            Schema = "dbo",
            RecordIdColumn = "RecordID"
        };

        var act = () => table.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void FullTableName_ReturnsSchemaQualifiedName()
    {
        var table = new TableConfig
        {
            TableName = "Order",
            Schema = "billing",
            RecordIdColumn = "ID"
        };

        table.FullTableName.Should().Be("[billing].[Order]");
    }
}

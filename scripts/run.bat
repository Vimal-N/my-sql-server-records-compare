@echo off
dotnet run --project src\MsSqlRecordsCompare.CLI -- --config .\config\ComparisonConfig.xlsx %*

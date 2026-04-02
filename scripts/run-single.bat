@echo off
if "%~1"=="" (
    echo Usage: run-single.bat "ScenarioName"
    exit /b 1
)
dotnet run --project src\MsSqlRecordsCompare.CLI -- --config .\config\ComparisonConfig.xlsx --scenario "%~1"

@echo off
echo Publishing MsSqlRecordsCompare...
dotnet publish src\MsSqlRecordsCompare.CLI\MsSqlRecordsCompare.CLI.csproj -c Release -r win-x64 --self-contained -o .\publish
echo.
echo Published to .\publish
echo Copy the publish folder to distribute the tool.

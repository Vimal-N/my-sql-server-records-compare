#!/usr/bin/env bash
# Run comparison with default config
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx "$@"

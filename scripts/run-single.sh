#!/usr/bin/env bash
# Run a single scenario by name
if [ -z "$1" ]; then
    echo "Usage: ./scripts/run-single.sh \"ScenarioName\""
    exit 1
fi
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx --scenario "$1"

#!/usr/bin/env bash
# Publish self-contained executable for the current platform
set -e

ARCH=$(uname -m)
OS=$(uname -s)

case "$OS" in
    Darwin)
        case "$ARCH" in
            arm64) RID="osx-arm64" ;;
            *)     RID="osx-x64" ;;
        esac
        ;;
    Linux)
        RID="linux-x64"
        ;;
    *)
        echo "Unsupported OS: $OS. Use scripts/publish.bat on Windows."
        exit 1
        ;;
esac

echo "Publishing MsSqlRecordsCompare for $RID..."
dotnet publish src/MsSqlRecordsCompare.CLI -c Release -r "$RID" --self-contained -o ./publish
echo ""
echo "Published to ./publish"
echo "Copy the publish folder to distribute the tool."

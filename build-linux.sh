#!/usr/bin/env bash
#
# build-linux.sh
# Builds a single-file deployctl executable that runs on Linux without
# requiring .NET to be installed on the target machine.
#
# Usage:
#   ./build-linux.sh
#   ./build-linux.sh /my/output/dir
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="${1:-$SCRIPT_DIR/publish/linux}"
CLI_PROJECT="$SCRIPT_DIR/src/Deployment.CLI/Deployment.CLI.csproj"

echo "==> Checking for .NET SDK..."
if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: .NET SDK not found. Install it from https://dotnet.microsoft.com/download"
    exit 1
fi

echo "==> Publishing self-contained single-file executable for linux-x64..."
dotnet publish "$CLI_PROJECT" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o "$OUTPUT_DIR"

chmod +x "$OUTPUT_DIR/deployctl"

echo
echo "==> Build complete!"
echo "    Executable: $OUTPUT_DIR/deployctl"
echo "    Copy this single file to any Linux (x64) machine and run it — no .NET install needed."

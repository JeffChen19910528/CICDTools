#Requires -Version 5.1
<#
    build-windows.ps1
    Builds a single-file deployctl.exe that runs on Windows without requiring
    .NET to be installed on the target machine.

    Usage:
        .\build-windows.ps1
        .\build-windows.ps1 -OutputDir C:\my\output
#>
param(
    [string]$OutputDir = "$PSScriptRoot\publish\windows"
)

$ErrorActionPreference = "Stop"

Write-Host "==> Checking for .NET SDK..." -ForegroundColor Cyan
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: .NET SDK not found. Install it from https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

$cliProject = Join-Path $PSScriptRoot "src\Deployment.CLI\Deployment.CLI.csproj"

Write-Host "==> Publishing self-contained single-file executable for win-x64..." -ForegroundColor Cyan
dotnet publish $cliProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "==> Build complete!" -ForegroundColor Green
Write-Host "    Executable: $OutputDir\deployctl.exe"
Write-Host "    Copy this single file to any Windows machine and run it — no .NET install needed."

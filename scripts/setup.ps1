# Linksy Project Setup Script for Windows (PowerShell)

param(
    [switch]$Help = $false
)

if ($Help) {
    Write-Host "Linksy Setup Script"
    Write-Host ""
    Write-Host "Usage: .\scripts\setup.ps1"
    Write-Host ""
    Write-Host "This script will:"
    Write-Host "  1. Verify .NET 8 SDK is installed"
    Write-Host "  2. Verify Node.js 22 LTS is installed"
    Write-Host "  3. Restore .NET dependencies"
    Write-Host "  4. Install npm dependencies"
    exit 0
}

Write-Host "===================================" -ForegroundColor Cyan
Write-Host "Linksy Project Setup" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

# Check for .NET SDK
Write-Host "Checking for .NET 8 SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($null -eq $dotnetVersion) {
    Write-Host "❌ .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    exit 1
}

if ($dotnetVersion -notmatch "^8\.") {
    Write-Host "❌ .NET 8 SDK is required, but found: $dotnetVersion" -ForegroundColor Red
    Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    exit 1
}
Write-Host "✅ .NET $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Check for Node.js
Write-Host "Checking for Node.js 22 LTS..." -ForegroundColor Yellow
$nodeVersion = node --version
if ($null -eq $nodeVersion) {
    Write-Host "❌ Node.js not found!" -ForegroundColor Red
    Write-Host "Please install Node.js 22 LTS from: https://nodejs.org/" -ForegroundColor Red
    exit 1
}

$nodeMajor = $nodeVersion.Substring(1).Split('.')[0]
if ([int]$nodeMajor -lt 22) {
    Write-Host "❌ Node.js 22 LTS is required, but found: $nodeVersion" -ForegroundColor Red
    Write-Host "Please install Node.js 22 LTS from: https://nodejs.org/" -ForegroundColor Red
    exit 1
}
Write-Host "✅ $nodeVersion" -ForegroundColor Green
Write-Host ""

# Check for npm
Write-Host "Checking for npm..." -ForegroundColor Yellow
$npmVersion = npm --version
if ($null -eq $npmVersion) {
    Write-Host "❌ npm not found!" -ForegroundColor Red
    Write-Host "npm should be included with Node.js. Please reinstall Node.js." -ForegroundColor Red
    exit 1
}
Write-Host "✅ npm $npmVersion" -ForegroundColor Green
Write-Host ""

# Restore .NET dependencies
Write-Host "Restoring .NET dependencies..." -ForegroundColor Yellow
dotnet restore Linksy.sln
Write-Host "✅ .NET dependencies restored" -ForegroundColor Green
Write-Host ""

# Install npm dependencies
Write-Host "Installing npm dependencies..." -ForegroundColor Yellow
npm install --prefix ./src/frontend
Write-Host "✅ npm dependencies installed" -ForegroundColor Green
Write-Host ""

Write-Host "===================================" -ForegroundColor Cyan
Write-Host "Setup Complete! ✅" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Start the Aspire AppHost:" -ForegroundColor White
Write-Host "   dotnet run --project Linksy.AppHost" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Open your browser to:" -ForegroundColor White
Write-Host "   - Frontend: http://localhost:5173" -ForegroundColor Gray
Write-Host "   - API Health: http://localhost:5000/health" -ForegroundColor Gray
Write-Host "   - Aspire Dashboard: http://localhost:15217" -ForegroundColor Gray
Write-Host ""

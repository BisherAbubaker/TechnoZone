# TechnoZone - Automated Installation & Run Script
# Run this script as Administrator in PowerShell

Write-Host "=====================================" -ForegroundColor Green
Write-Host "  TechnoZone - Automated Setup" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
	Write-Host "❌ ERROR: This script must be run as Administrator!" -ForegroundColor Red
	Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
	exit 1
}

# Step 1: Check .NET Installation
Write-Host "Step 1: Checking .NET 8 installation..." -ForegroundColor Cyan
$dotnetVersion = dotnet --version 2>$null
if ($null -eq $dotnetVersion) {
	Write-Host "❌ .NET SDK not found!" -ForegroundColor Red
	Write-Host "📥 Installing .NET 8 SDK..." -ForegroundColor Yellow

	# Download and install .NET 8 SDK
	$installerUrl = "https://dot.net/v1/dotnet-install.ps1"
	$installerPath = "$env:TEMP\dotnet-install.ps1"

	Write-Host "Downloading .NET installer..." -ForegroundColor Yellow
	Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath -UseBasicParsing

	Write-Host "Running installer..." -ForegroundColor Yellow
	& $installerPath -Channel 8.0 -InstallDir "C:\Program Files\dotnet"

	# Add to PATH if needed
	$env:PATH = "C:\Program Files\dotnet;" + $env:PATH
	[Environment]::SetEnvironmentVariable("PATH", "C:\Program Files\dotnet;" + [Environment]::GetEnvironmentVariable("PATH", "Machine"), "Machine")

	$dotnetVersion = dotnet --version 2>$null
}

Write-Host "✅ .NET version: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Step 2: Check SQL Server
Write-Host "Step 2: Checking SQL Server installation..." -ForegroundColor Cyan
$sqlCheck = sqllocaldb info 2>$null
if ($null -eq $sqlCheck) {
	Write-Host "❌ SQL Server LocalDB not found!" -ForegroundColor Red
	Write-Host "📥 Installing SQL Server 2022 Express LocalDB..." -ForegroundColor Yellow

	# Download SQL Server Express
	$sqlInstallerUrl = "https://download.microsoft.com/download/9/1/c/91c5795e-d147-41c5-aea8-8335909fa2b9/SQL2022-SSEI-Express.exe"
	$sqlInstallerPath = "$env:TEMP\sqlserver-installer.exe"

	Write-Host "Downloading SQL Server installer (this may take a few minutes)..." -ForegroundColor Yellow
	Invoke-WebRequest -Uri $sqlInstallerUrl -OutFile $sqlInstallerPath -UseBasicParsing

	Write-Host "Running SQL Server installer..." -ForegroundColor Yellow
	Write-Host "⚠️  IMPORTANT: When the installer opens, select 'LocalDB' and let it complete." -ForegroundColor Yellow
	Write-Host "Press any key when done..." -ForegroundColor Yellow
	Read-Host

	& $sqlInstallerPath /ACTION=Install /FEATURES=LocalDB /QUIET /IACCEPTSQLSERVERLICENSETERMS

	Write-Host "✅ SQL Server LocalDB should now be installed" -ForegroundColor Green
} else {
	Write-Host "✅ SQL Server LocalDB found" -ForegroundColor Green
}
Write-Host ""

# Step 3: Start LocalDB
Write-Host "Step 3: Starting SQL Server LocalDB..." -ForegroundColor Cyan
sqllocaldb start mssqllocaldb 2>$null
if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 50) {
	Write-Host "✅ SQL Server LocalDB started" -ForegroundColor Green
} else {
	Write-Host "⚠️  Could not start LocalDB (may already be running)" -ForegroundColor Yellow
}
Write-Host ""

# Step 4: Navigate to project
Write-Host "Step 4: Preparing project..." -ForegroundColor Cyan
$projectPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectPath
Write-Host "✅ Project path: $projectPath" -ForegroundColor Green
Write-Host ""

# Step 5: Clean previous builds
Write-Host "Step 5: Cleaning previous builds..." -ForegroundColor Cyan
dotnet clean 2>$null
Write-Host "✅ Clean complete" -ForegroundColor Green
Write-Host ""

# Step 6: Restore NuGet packages
Write-Host "Step 6: Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -ne 0) {
	Write-Host "❌ Failed to restore packages" -ForegroundColor Red
	exit 1
}
Write-Host "✅ Packages restored" -ForegroundColor Green
Write-Host ""

# Step 7: Build project
Write-Host "Step 7: Building project..." -ForegroundColor Cyan
dotnet build --configuration Debug
if ($LASTEXITCODE -ne 0) {
	Write-Host "❌ Failed to build project" -ForegroundColor Red
	exit 1
}
Write-Host "✅ Project built successfully" -ForegroundColor Green
Write-Host ""

# Step 8: Run application
Write-Host "Step 8: Starting application..." -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Green
Write-Host "  🚀 Application Starting..." -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "🌐 Once started, open browser to:" -ForegroundColor Yellow
Write-Host "   http://localhost:5243/Auth/Login" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 To register: Click 'Create one now'" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Yellow
Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

# Set environment variable and run
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "  Application Stopped" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

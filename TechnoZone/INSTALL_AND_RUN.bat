@echo off
REM TechnoZone - Automated Installation & Run Script (Batch Version)
REM Right-click and select "Run as Administrator"

setlocal enabledelayedexpansion

echo.
echo =====================================
echo   TechnoZone - Automated Setup
echo =====================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
	echo ERROR: This script must be run as Administrator!
	echo Please right-click and select "Run as Administrator"
	pause
	exit /b 1
)

REM Step 1: Check .NET
echo Step 1: Checking .NET 8 installation...
dotnet --version >nul 2>&1
if errorlevel 1 (
	echo WARNING: .NET SDK may not be installed or not in PATH
	echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download
	echo Then run this script again
	pause
	exit /b 1
)
for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo OK: .NET version %DOTNET_VERSION%
echo.

REM Step 2: Check SQL Server
echo Step 2: Checking SQL Server LocalDB...
sqllocaldb info >nul 2>&1
if errorlevel 1 (
	echo WARNING: SQL Server LocalDB not found
	echo Please install SQL Server 2022 Express from:
	echo https://www.microsoft.com/en-us/sql-server/sql-server-downloads
	echo Select "LocalDB" during installation
	echo Then run this script again
	pause
	exit /b 1
)
echo OK: SQL Server LocalDB found
echo.

REM Step 3: Start LocalDB
echo Step 3: Starting SQL Server LocalDB...
sqllocaldb start mssqllocaldb 2>nul
echo OK: LocalDB started
echo.

REM Step 4: Navigate to project directory
echo Step 4: Preparing project...
cd /d "%~dp0"
set PROJECT_PATH=%cd%
echo OK: Project path: %PROJECT_PATH%
echo.

REM Step 5: Clean
echo Step 5: Cleaning previous builds...
dotnet clean 2>nul
echo OK: Clean complete
echo.

REM Step 6: Restore packages
echo Step 6: Restoring NuGet packages...
dotnet restore
if errorlevel 1 (
	echo ERROR: Failed to restore packages
	pause
	exit /b 1
)
echo OK: Packages restored
echo.

REM Step 7: Build
echo Step 7: Building project...
dotnet build --configuration Debug
if errorlevel 1 (
	echo ERROR: Failed to build project
	pause
	exit /b 1
)
echo OK: Project built successfully
echo.

REM Step 8: Run
echo Step 8: Starting application...
echo.
echo =====================================
echo   Application Starting...
echo =====================================
echo.
echo Browse to: http://localhost:5243/Auth/Login
echo.
echo To register: Click "Create one now"
echo.
echo Press Ctrl+C to stop the application
echo.
echo =====================================
echo.

set ASPNETCORE_ENVIRONMENT=Development
dotnet run

echo.
echo =====================================
echo   Application Stopped
echo =====================================
pause

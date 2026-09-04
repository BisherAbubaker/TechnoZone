# TechnoZone - Complete Setup & Run Guide

## 🔧 Prerequisites

### Required Software
- **.NET 8 SDK** (or later)
- **SQL Server** OR **SQL Server LocalDB**
- **Visual Studio 2022** (recommended) or VS Code

### Check Your Setup

#### 1. Check .NET Version
```bash
dotnet --version
```
Should show 8.0.x or higher.

#### 2. Check SQL Server/LocalDB Installation
```bash
sqllocaldb info
```

If this fails, you need to install SQL Server or LocalDB.

---

## 📥 Installation

### Option A: Using SQL Server LocalDB (Recommended for Development)

#### Step 1: Install SQL Server LocalDB
1. Go to: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Download "SQL Server 2022 Express" (Free)
3. Run installer
4. Choose "Local" installation
5. Accept defaults
6. Finish installation

#### Step 2: Start LocalDB
```bash
sqllocaldb start mssqllocaldb
```

#### Step 3: Verify Connection
```bash
sqllocaldb info mssqllocaldb
```

You should see output with connection info.

---

### Option B: Using Full SQL Server (Development or Production)

#### Edit Connection String
If you have SQL Server running on a different machine or port:

**File:** `appsettings.json`

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=YOUR_SERVER_NAME;Database=TechnoZoneDB;Integrated Security=true;Encrypt=false;"
  }
}
```

Replace `YOUR_SERVER_NAME` with your server name or IP address.

---

### Option C: Using SQL Server in Docker

If you have Docker installed:

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=MyP@ssw0rd123" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

Then update connection string:
```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=localhost,1433;User Id=sa;Password=MyP@ssw0rd123;Database=TechnoZoneDB;Encrypt=false;"
  }
}
```

---

## 🚀 Running the Application

### Step 1: Navigate to Project Directory
```bash
cd path/to/TechnoZone
```

### Step 2: Restore Dependencies
```bash
dotnet restore
```

### Step 3: Build Project
```bash
dotnet build
```

### Step 4: Run Application
```bash
dotnet run
```

You should see output like:
```
info: Microsoft.Hosting.Lifetime[14]
	  Now listening on: https://localhost:7243
	  Now listening on: http://localhost:5243
```

### Step 5: Open in Browser
```
https://localhost:7243
```
or
```
http://localhost:5243
```

---

## ✅ Verify Everything Works

### 1. Check Database Creation
The database should be created automatically on first run.

**SQL Query to verify:**
```sql
SELECT * FROM sys.databases WHERE name = 'TechnoZoneDB'
```

### 2. Test Login Page
1. Navigate to: `http://localhost:5243/Auth/Login`
2. Should see beautiful login form
3. Click "Create one now" link to register

### 3. Create Test Account
1. Go to: `http://localhost:5243/Auth/Register`
2. Fill in form:
   - Username: `testuser`
   - Email: `test@example.com`
   - Password: `TestPassword123`
   - First Name: `Test`
   - Last Name: `User`
3. Click "Create Account"

### 4. Test Login
1. Go to: `http://localhost:5243/Auth/Login`
2. Enter:
   - Username: `testuser`
   - Password: `TestPassword123`
3. Click "Login"
4. Should redirect to home page
5. Check top-right corner - should see "Test User" dropdown

---

## 🐛 Troubleshooting

### ❌ "Cannot connect to database"

**Symptom:** Error message about database connection

**Solution:**

1. **Check LocalDB is running:**
   ```bash
   sqllocaldb start mssqllocaldb
   ```

2. **Check connection string:**
   - Open `appsettings.json`
   - Verify connection string matches your setup

3. **Test connection manually:**
   ```bash
   sqlcmd -S (localdb)\mssqllocaldb
   > SELECT 1
   > GO
   ```

### ❌ Application exits immediately

**Symptom:** App starts then closes without error

**Solution:**

1. Run with logging enabled:
   ```bash
   dotnet run --verbosity Diagnostic
   ```

2. Check debug output for errors

3. Try running in development mode explicitly:
   ```bash
   set ASPNETCORE_ENVIRONMENT=Development
   dotnet run
   ```

### ❌ Port 5243 already in use

**Symptom:** "Address already in use" error

**Solution:**

Edit `Properties/launchSettings.json`:
```json
{
  "profiles": {
	"TechnoZone": {
	  "applicationUrl": "https://localhost:7244;http://localhost:5244"
	}
  }
}
```

Change port numbers (7243→7244, 5243→5244, etc.)

### ❌ "SSL connection error"

**Symptom:** HTTPS connection fails

**Solution:**

Use HTTP instead:
```
http://localhost:5243
```

Or generate new dev certificate:
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### ❌ Can't find login page

**Solution:**

Direct URL:
```
http://localhost:5243/Auth/Login
```

Or check that AuthController exists at:
```
TechnoZone/Controllers/AuthController.cs
```

---

## 📊 Verify Database Was Created

### Using SQL Server Management Studio (SSMS)

1. Open SSMS
2. Connect to: `(localdb)\mssqllocaldb`
3. Expand "Databases"
4. Should see "TechnoZoneDB"
5. Expand it → Tables → "dbo.Users"

### Using sqlcmd

```bash
sqlcmd -S (localdb)\mssqllocaldb

SELECT name FROM sys.databases WHERE name = 'TechnoZoneDB'
GO

USE TechnoZoneDB
GO

SELECT * FROM Users
GO
```

---

## 🔄 Resetting Everything

### Delete Database
```bash
sqlcmd -S (localdb)\mssqllocaldb

DROP DATABASE TechnoZoneDB
GO
```

### Clean Build
```bash
dotnet clean
dotnet build
dotnet run
```

Database will be recreated automatically.

---

## 📋 Checklist Before Running

- [ ] .NET 8 SDK installed (`dotnet --version`)
- [ ] SQL Server or LocalDB installed (`sqllocaldb info`)
- [ ] Connection string correct in `appsettings.json`
- [ ] Project restored (`dotnet restore`)
- [ ] Project builds (`dotnet build`)
- [ ] Port 5243/7243 available
- [ ] ASPNETCORE_ENVIRONMENT set to "Development"

---

## 🎯 Quick Start (TL;DR)

```bash
# 1. Start LocalDB
sqllocaldb start mssqllocaldb

# 2. Go to project
cd path/to/TechnoZone

# 3. Run
dotnet run

# 4. Open browser
http://localhost:5243/Auth/Login

# 5. Register new account or try login
```

---

## 📞 Getting Help

### Check Logs
1. Look at console output from `dotnet run`
2. Check Visual Studio Debug Output window (Debug → Windows → Output)
3. Look in Event Viewer for application errors

### Common Issues Summary

| Issue | Check |
|-------|-------|
| "Cannot connect" | LocalDB running? Connection string correct? |
| App crashes | Any error in console? Run with `--verbosity Diagnostic` |
| Port in use | Change port in `launchSettings.json` |
| Login doesn't work | Account created? Database has users? |
| SSL error | Use HTTP or regenerate dev certs |

---

## 🎉 Success!

Once you see your name in the top-right dropdown after login, everything is working!

Next steps:
1. Explore the application
2. Customize the UI in `Views/Auth/Login.cshtml`
3. Add more features
4. Deploy when ready

Happy coding! 🚀

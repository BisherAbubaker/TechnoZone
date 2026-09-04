# TechnoZone Login System - Quick Start

## 🚀 Quick Start (5 minutes)

### Step 1: Run the Application
```
dotnet run
```

### Step 2: Database Setup
The database will be created automatically when you first run the app. Just wait a moment!

### Step 3: Test Login/Register

#### Option A: Create New Account
1. Go to: `http://localhost:5000/Auth/Register`
2. Fill in the form (username, email, password, etc.)
3. Click "Create Account"

#### Option B: Use Test Account
1. Go to: `http://localhost:5000/Auth/Login`
2. Username: `testuser`
3. Password: `TestUser123`
4. Click "Login"

### Step 4: Check It Works
- You should see your name in the top-right corner
- Click it to see the dropdown menu
- Click "Logout" to log out

---

## 📁 What Was Added

### New Files Created:
```
✅ Controllers/AuthController.cs              - Login logic
✅ Data/DatabaseConnection.cs                 - Database access
✅ Middleware/DatabaseInitializationMiddleware.cs - Auto setup
✅ Models/User.cs                            - User model
✅ Models/AuthViewModels.cs                  - Login/Register forms
✅ Views/Auth/Login.cshtml                   - Login page
✅ Views/Auth/Register.cshtml                - Register page
✅ Database/setup.sql                        - Manual SQL script
✅ appsettings.json                          - Connection strings
✅ appsettings.Development.json              - Dev settings
✅ AUTHENTICATION_SETUP.md                   - Full documentation
```

### Modified Files:
```
✏️ Program.cs                 - Added session support
✏️ Views/Shared/_Layout.cshtml - Added login button
✏️ wwwroot/css/site.css        - Added auth styling
✏️ TechnoZone.csproj           - Added dependencies
```

---

## 🔐 Security Features

✅ **Password Hashing**: SHA-256 encryption  
✅ **SQL Injection Protection**: Parameterized queries  
✅ **CSRF Protection**: Anti-forgery tokens  
✅ **Secure Sessions**: HttpOnly cookies  
✅ **Session Timeout**: 30 minutes of inactivity  

---

## 🗄️ Database Schema

**Table: Users**
```sql
Id                INT (Primary Key)
Username          NVARCHAR(50) - Unique
Email             NVARCHAR(100) - Unique
PasswordHash      NVARCHAR(256) - SHA-256
FirstName         NVARCHAR(100)
LastName          NVARCHAR(100)
CreatedAt         DATETIME
LastLogin         DATETIME
IsActive          BIT (1 = active, 0 = inactive)
```

---

## 🛠️ Troubleshooting

### ❌ "Cannot connect to database"
**Solution**: Make sure SQL Server or LocalDB is installed
```
Check Control Panel → Programs → Programs and Features
Look for "SQL Server" or "LocalDB"
```

### ❌ Login button doesn't work
**Solution**: Clear browser cache
```
Press Ctrl+Shift+Delete to clear cache
Try again
```

### ❌ Can't find login page
**Access it directly**: `http://localhost:5000/Auth/Login`

---

## 📖 Full Documentation

See **AUTHENTICATION_SETUP.md** for:
- Complete setup instructions
- API reference
- Customization guide
- Advanced features

---

## 🎨 Customizing the Pages

### Change Login Page Style
Edit: `Views/Auth/Login.cshtml`
- Modify colors, layout, text

### Change CSS Theme
Edit: `wwwroot/css/site.css`
- Search for `.login-container` or `.register-container`

---

## 💡 Common Tasks

### Access User Info in Your Code

**In a Controller:**
```csharp
var userId = HttpContext.Session.GetInt32("UserId");
var username = HttpContext.Session.GetString("Username");
```

**In a Razor View:**
```razor
@{
	var userId = Context.Session.GetInt32("UserId");
	var username = Context.Session.GetString("Username");
}

@if (userId.HasValue)
{
	<p>Welcome, @username!</p>
}
```

### Require Login for a Page
```csharp
[HttpGet]
public IActionResult MyPage()
{
	var userId = HttpContext.Session.GetInt32("UserId");
	if (!userId.HasValue)
	{
		return RedirectToAction("Login", "Auth", 
			new { returnUrl = Request.Path.Value });
	}
	return View();
}
```

---

## 📞 Support

- Full docs: See `AUTHENTICATION_SETUP.md`
- Database issues: Check SQL Server logs
- Page issues: Check browser console (F12)

---

**Happy Building! 🎉**

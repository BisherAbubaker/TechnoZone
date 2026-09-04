# TechnoZone Authentication System - Setup Guide

## Overview
This guide explains how to set up and use the Login/Registration system for TechnoZone.

## Features Implemented
✅ User Registration with validation
✅ Secure Login with password hashing (SHA-256)
✅ Session management
✅ LocalDB SQL Server integration
✅ Responsive Login/Register pages
✅ User profile dropdown in navigation
✅ Logout functionality
✅ Last login tracking

## Architecture

### Database
- **Database**: TechnoZoneDB (SQL Server LocalDB)
- **Table**: Users
  - Id (Primary Key, Auto-increment)
  - Username (Unique, NVARCHAR(50))
  - Email (Unique, NVARCHAR(100))
  - PasswordHash (NVARCHAR(256) - SHA-256)
  - FirstName (NVARCHAR(100))
  - LastName (NVARCHAR(100))
  - CreatedAt (DateTime)
  - LastLogin (DateTime, Nullable)
  - IsActive (Bit, Default 1)

### Code Structure

```
TechnoZone/
├── Controllers/
│   └── AuthController.cs          # Login/Register/Logout logic
├── Models/
│   ├── User.cs                   # User entity
│   └── AuthViewModels.cs         # LoginViewModel, RegisterViewModel
├── Data/
│   └── DatabaseConnection.cs     # Database access layer
├── Middleware/
│   └── DatabaseInitializationMiddleware.cs  # Auto-create database schema
├── Views/
│   └── Auth/
│       ├── Login.cshtml          # Login page
│       └── Register.cshtml       # Registration page
├── wwwroot/
│   └── css/
│       └── site.css              # Styling (includes auth styles)
├── Database/
│   └── setup.sql                 # Manual database setup script
└── appsettings.json              # Connection string configuration
```

## Setup Instructions

### 1. Initial Setup (Automatic)
When you first run the application:
- The `DatabaseInitializationMiddleware` will automatically create the database and Users table
- No manual SQL scripts are needed
- LocalDB will be used by default

### 2. Connection String
The application uses LocalDB by default. Update the connection string in `appsettings.json` if needed:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TechnoZoneDB;Integrated Security=true;"
}
```

For different servers:
```json
// SQL Server Express
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=TechnoZoneDB;Integrated Security=true;"

// Remote SQL Server
"DefaultConnection": "Server=your-server;Database=TechnoZoneDB;User Id=sa;Password=your-password;"
```

### 3. Session Configuration
Sessions are configured in `Program.cs`:
- Session timeout: 30 minutes
- HttpOnly cookies: Enabled
- Essential cookies: Enabled

## Usage

### Accessing the Login Page
- URL: `http://localhost:xxxx/Auth/Login`
- Click "Login" button in the navigation bar

### Registering a New User
- URL: `http://localhost:xxxx/Auth/Register`
- Or click "Create one now" link from Login page
- Requirements:
  - Username: 3+ characters, must be unique
  - Email: Valid email format, must be unique
  - Password: 6+ characters
  - First/Last Name: Optional

### Logging In
- Enter Username and Password
- Passwords are hashed using SHA-256 before storage
- User info is stored in session

### User Session Data
After login, the following is stored in `HttpContext.Session`:
- `UserId`: User's ID
- `Username`: User's username
- `Email`: User's email
- `FirstName`: User's first name
- `LastName`: User's last name

Access in views/controllers:
```csharp
// In Controller
var userId = HttpContext.Session.GetInt32("UserId");
var username = HttpContext.Session.GetString("Username");

// In Razor View
var userId = Context.Session.GetInt32("UserId");
var username = Context.Session.GetString("Username");
```

### Logging Out
- Click "Logout" from the user dropdown menu
- URL: `http://localhost:xxxx/Auth/Logout`
- Session is cleared

## Security Features

### Password Security
- Passwords are hashed using SHA-256
- Original passwords are NEVER stored
- Each login validates the password hash

### SQL Injection Protection
- Uses parameterized queries
- SQL parameters are used for all user input

### Session Security
- HttpOnly cookies prevent XSS attacks
- Session timeout after 30 minutes of inactivity
- Session data cleared on logout

### CSRF Protection
- All forms use `@Html.AntiForgeryToken()`
- `[ValidateAntiForgeryToken]` attribute on POST actions

## Database Initialization Options

### Option 1: Automatic (Recommended)
- Just run the application
- The middleware will create the database and schema automatically

### Option 2: Manual SQL Setup
If automatic setup fails, run the script manually:

1. Open SQL Server Management Studio
2. Connect to `(localdb)\mssqllocaldb`
3. Open `Database/setup.sql`
4. Execute the script

## Testing the System

### Test User Credentials
After database initialization, test users are available:
- Username: `testuser` | Password: `TestUser123`
- Username: `johndoe` | Password: `TestUser123`

### Manual Testing
1. **Register a new user**
   - Go to `/Auth/Register`
   - Fill in details
   - Click "Create Account"
   - Should redirect to Login page with success message

2. **Login**
   - Enter username and password
   - Should redirect to home page
   - Login button changes to user dropdown

3. **Check Session**
   - After login, user info appears in the dropdown
   - Refresh page - session persists

4. **Logout**
   - Click "Logout" from dropdown
   - Session cleared, redirects to home
   - Login button reappears

## Customization

### Change Session Timeout
In `Program.cs`:
```csharp
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(60); // Change 30 to 60
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});
```

### Customize Login/Register Pages
- Edit `/Views/Auth/Login.cshtml`
- Edit `/Views/Auth/Register.cshtml`
- Modify styles in `wwwroot/css/site.css` (search for `.login-container`, `.register-container`)

### Add Additional User Fields
1. Add column to Users table in SQL
2. Add property to User model
3. Update DatabaseConnection.cs methods
4. Update AuthController.cs
5. Update Register.cshtml form

## Troubleshooting

### "Database initialization error" message appears
**Solution**: 
- Check that SQL Server or LocalDB is installed
- Verify connection string in appsettings.json
- Check Event Viewer for SQL Server errors
- Try running setup.sql manually

### "Invalid username or password"
- Verify username and password are correct
- Check if user account is Active (IsActive = 1)
- Ensure password hasn't expired or been changed

### Login button doesn't disappear after login
- Check browser cookies are enabled
- Verify session is being set: `HttpContext.Session.SetInt32("UserId", ...)`
- Clear browser cache and try again

### Session data is lost after page refresh
- Check session middleware is registered: `app.UseSession();`
- Ensure it's registered BEFORE `app.UseRouting();`
- Verify IDistributedCache is configured if using distributed sessions

## Next Steps

### Future Enhancements
- Add password reset functionality
- Implement email verification
- Add two-factor authentication
- Create admin panel
- Add user roles/permissions
- Implement OAuth (Google, GitHub login)
- Add profile edit page
- Order history/tracking

### Database Expansion
The Users table can be extended with:
- Profile pictures
- Phone numbers
- Addresses
- Preferences
- Account status flags

## Support

For issues or questions:
1. Check this guide for troubleshooting
2. Review SQL Server logs
3. Check application logs in Output window
4. Verify all files are created in correct locations

---

**Last Updated**: 2026
**Version**: 1.0

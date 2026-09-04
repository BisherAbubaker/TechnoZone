# TechnoZone Authentication System - Architecture

## System Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         User's Browser                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌──────────────────────┐      ┌──────────────────────┐                 │
│  │  Login Page          │      │  Register Page       │                 │
│  │  /Auth/Login         │      │  /Auth/Register      │                 │
│  └──────────────────────┘      └──────────────────────┘                 │
│          ↕                                ↕                              │
└─────────────────────────────────────────────────────────────────────────┘
			 ↕ HTTP Request/Response
┌─────────────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core Application                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ Program.cs                                                       │  │
│  │  ├─ Services: AddSession()                                      │  │
│  │  ├─ Middleware: UseSession()                                    │  │
│  │  └─ Middleware: DatabaseInitializationMiddleware()             │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                              ↓                                           │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ AuthController (/Auth)                                           │  │
│  │  ├─ Login() [GET]     → View                                    │  │
│  │  ├─ Login() [POST]    → Authenticate User                       │  │
│  │  ├─ Register() [GET]  → View                                    │  │
│  │  ├─ Register() [POST] → Create User                             │  │
│  │  └─ Logout()          → Clear Session                           │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                              ↓                                           │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ DatabaseConnection (Data Access Layer)                          │  │
│  │  ├─ InitializeDatabase()                                        │  │
│  │  ├─ AuthenticateUser()                                          │  │
│  │  ├─ RegisterUser()                                              │  │
│  │  ├─ UserExists()                                                │  │
│  │  ├─ UpdateLastLogin()                                           │  │
│  │  ├─ HashPassword()                                              │  │
│  │  └─ VerifyPassword()                                            │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                              ↓                                           │
└─────────────────────────────────────────────────────────────────────────┘
			 ↕ SQL Queries (via System.Data.SqlClient)
┌─────────────────────────────────────────────────────────────────────────┐
│                      SQL Server (LocalDB)                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  Database: TechnoZoneDB                                                 │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Table: Users                                                       │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ • Id (PRIMARY KEY)                                                 │ │
│  │ • Username (UNIQUE)                                                │ │
│  │ • Email (UNIQUE)                                                   │ │
│  │ • PasswordHash (SHA-256)                                           │ │
│  │ • FirstName                                                        │ │
│  │ • LastName                                                         │ │
│  │ • CreatedAt                                                        │ │
│  │ • LastLogin                                                        │ │
│  │ • IsActive                                                         │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

## Request Flow - Login Process

```
1. User Opens /Auth/Login
   ↓
2. GET Request → AuthController.Login()
   ↓
3. Controller Returns LoginViewModel + View
   ↓
4. User Enters Credentials & Clicks Login
   ↓
5. POST Request → AuthController.Login(LoginViewModel)
   ↓
6. Validate ModelState
   ├─ Invalid? → Show Errors → Return View
   └─ Valid? → Continue
   ↓
7. Call DatabaseConnection.AuthenticateUser(username, password)
   ↓
8. Database: Query User by Username
   ├─ User Not Found? → Return null
   └─ User Found? → Continue
   ↓
9. Verify Password
   ├─ Hash input password
   ├─ Compare with stored hash
   ├─ No Match? → Return null
   └─ Match? → Continue
   ↓
10. Update LastLogin timestamp
	↓
11. Return User object to Controller
	↓
12. Set Session Variables
	├─ UserId
	├─ Username
	├─ Email
	├─ FirstName
	└─ LastName
	↓
13. Redirect to Home Page
	↓
14. Session Persists Across Requests ✓
```

## Request Flow - Registration Process

```
1. User Opens /Auth/Register
   ↓
2. GET Request → AuthController.Register()
   ↓
3. Controller Returns RegisterViewModel + View
   ↓
4. User Fills Form & Clicks Create Account
   ↓
5. POST Request → AuthController.Register(RegisterViewModel)
   ↓
6. Validate Input
   ├─ Username < 3 chars? → Show Error
   ├─ Email Invalid? → Show Error
   ├─ Passwords Don't Match? → Show Error
   ├─ Password < 6 chars? → Show Error
   └─ All Valid? → Continue
   ↓
7. Call DatabaseConnection.RegisterUser()
   ↓
8. Check if User Already Exists
   ├─ Yes? → Return false
   └─ No? → Continue
   ↓
9. Hash Password (SHA-256)
   ↓
10. Insert New User into Database
	↓
11. Return true
	↓
12. Set Success Message in TempData
	↓
13. Redirect to Login Page
	↓
14. User Sees Success Message ✓
```

## Session Management

```
┌─────────────────────────────────────────────────────────────┐
│ HttpContext.Session (Server-Side)                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│ After Login:                                                │
│ ┌──────────────────────────────────────────────────────┐   │
│ │ Session["UserId"]     = 1                            │   │
│ │ Session["Username"]   = "johndoe"                    │   │
│ │ Session["Email"]      = "john@example.com"          │   │
│ │ Session["FirstName"]  = "John"                       │   │
│ │ Session["LastName"]   = "Doe"                        │   │
│ └──────────────────────────────────────────────────────┘   │
│                                                              │
│ Timeout: 30 minutes of inactivity                           │
│ Storage: Server memory (can be configured for distributed)  │
│                                                              │
│ On Logout:                                                  │
│ HttpContext.Session.Clear() → All data removed ✓            │
│                                                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Browser Cookie (Session ID Only)                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│ Cookie: .AspNetCore.Session = [Session ID GUID]            │
│ - HttpOnly: true (JavaScript cannot access)                │
│ - Secure: true (HTTPS only in production)                  │
│ - SameSite: Lax (CSRF protection)                          │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Security Layers

```
┌─────────────────────────────────────────────────────────────┐
│ Layer 1: Input Validation                                   │
├─────────────────────────────────────────────────────────────┤
│ • Username length check                                     │
│ • Email format validation                                   │
│ • Password strength requirements                            │
│ • ModelState.IsValid check                                  │
└─────────────────────────────────────────────────────────────┘
						  ↓
┌─────────────────────────────────────────────────────────────┐
│ Layer 2: CSRF Protection                                    │
├─────────────────────────────────────────────────────────────┤
│ • @Html.AntiForgeryToken() in views                         │
│ • [ValidateAntiForgeryToken] in controller                  │
│ • Token validation on every POST                            │
└─────────────────────────────────────────────────────────────┘
						  ↓
┌─────────────────────────────────────────────────────────────┐
│ Layer 3: SQL Injection Prevention                           │
├─────────────────────────────────────────────────────────────┤
│ • Parameterized queries (SqlCommand.Parameters)             │
│ • No string concatenation                                   │
│ • No raw SQL input                                          │
└─────────────────────────────────────────────────────────────┘
						  ↓
┌─────────────────────────────────────────────────────────────┐
│ Layer 4: Password Security                                  │
├─────────────────────────────────────────────────────────────┤
│ • SHA-256 hashing algorithm                                 │
│ • One-way encryption (no decryption)                        │
│ • Hash comparison on login                                  │
│ • Original password never stored                            │
└─────────────────────────────────────────────────────────────┘
						  ↓
┌─────────────────────────────────────────────────────────────┐
│ Layer 5: Session Security                                   │
├─────────────────────────────────────────────────────────────┤
│ • HttpOnly cookies (XSS prevention)                         │
│ • Session ID validation                                     │
│ • Server-side session storage                               │
│ • 30-minute inactivity timeout                              │
└─────────────────────────────────────────────────────────────┘
```

## File Structure

```
TechnoZone/
│
├── Controllers/
│   ├── HomeController.cs
│   └── AuthController.cs ........................ [NEW]
│
├── Models/
│   ├── Category.cs
│   ├── Product.cs
│   ├── User.cs .................................. [NEW]
│   ├── AuthViewModels.cs ........................ [NEW]
│   └── ... (other models)
│
├── Data/
│   └── DatabaseConnection.cs ................... [NEW]
│
├── Middleware/
│   └── DatabaseInitializationMiddleware.cs .... [NEW]
│
├── Views/
│   ├── Home/
│   ├── Shared/
│   │   └── _Layout.cshtml ...................... [MODIFIED]
│   └── Auth/ ................................... [NEW]
│       ├── Login.cshtml
│       └── Register.cshtml
│
├── wwwroot/
│   ├── css/
│   │   └── site.css ............................ [MODIFIED]
│   ├── js/
│   └── images/
│
├── Database/
│   └── setup.sql ............................... [NEW]
│
├── Program.cs .................................. [MODIFIED]
├── appsettings.json ............................ [NEW]
├── appsettings.Development.json ............... [NEW]
├── TechnoZone.csproj ........................... [MODIFIED]
├── AUTHENTICATION_SETUP.md .................... [NEW]
└── QUICKSTART.md .............................. [NEW]
```

---

This diagram shows the complete architecture of your authentication system!

# TechnoZone — JavaScript and Database Guide

This is the current, accurate guide for the project. The other markdown files in
this folder were written for an earlier version and are now out of date — most of
them still refer to `Database/setup.sql`, which no longer exists. You can delete
them.

---

## 1. What the project is

An **ASP.NET Core 8 MVC** web application (C# + Razor views) for a custom PC
builder, backed by **Microsoft SQL Server**.

```
TechnoZone/
├── Controllers/
│   ├── AuthController.cs      sign in, register, live checks, profile, sign out
│   └── HomeController.cs      home page + newsletter endpoint
├── Data/
│   └── DatabaseConnection.cs  all SQL access (parameterised)
├── Database/
│   └── TechnoZone_Database.sql   ← run this in SSMS
├── Middleware/
│   └── DatabaseInitializationMiddleware.cs
├── Models/
├── Views/
│   ├── Auth/    Login.cshtml, Register.cshtml, Profile.cshtml
│   ├── Home/    Index.cshtml, Privacy.cshtml, Error.cshtml
│   ├── Shared/  _Layout.cshtml
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
└── wwwroot/
    ├── css/site.css
    └── js/
        ├── validation.js
        ├── auth.js
        └── site.js
```

---

## 2. Setting up the database

1. Open **SQL Server Management Studio** and connect to your server.
2. Open a New Query window.
3. Open `Database/TechnoZone_Database.sql`, paste the whole thing in, press **F5**.

The script is safe to run more than once — it checks for each object before
creating it, so nothing gets duplicated.

### What it creates

| Object | Purpose |
| --- | --- |
| `Users` | Registered accounts. Unique username and email, check constraints, indexes on the columns login searches. |
| `LoginAttempts` | Audit trail of every sign-in attempt, successful or not. |
| `NewsletterSubscribers` | Backs the newsletter form on the home page. |
| `sp_RegisterUser` | Creates an account, returns 0 if username/email taken. |
| `sp_GetUserByUsername` | Fetches a user so the app can compare password hashes. |
| `sp_IsUsernameTaken` / `sp_IsEmailTaken` | Back the live availability checks in the browser. |
| `sp_UpdateLastLogin` | Stamps the time of a successful sign-in. |
| `sp_LogLoginAttempt` | Writes to the audit table. |
| `sp_SubscribeNewsletter` | Adds a subscriber, or reactivates one who left. |
| `vw_UserAccounts` | A view that omits password hashes — safe to show in a demo. |

### Demo accounts

| Username | Password |
| --- | --- |
| `testuser` | `Test@123` |
| `johndoe` | `John@123` |
| `admin` | `Admin@123` |

These hashes are real SHA-256 → Base64 values, so these accounts can actually
sign in. (The hash in the old `setup.sql` was a placeholder, so the old demo
accounts never worked.)

### Connection string

`appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TechnoZoneDB;Integrated Security=true;Encrypt=false;"
}
```

If you use SQL Server Express instead of LocalDB, change the server:

```
Server=.\\SQLEXPRESS;Database=TechnoZoneDB;Integrated Security=true;Encrypt=false;TrustServerCertificate=true;
```

For SQL authentication:

```
Server=localhost;Database=TechnoZoneDB;User Id=sa;Password=YourPassword;Encrypt=false;TrustServerCertificate=true;
```

---

## 3. The JavaScript

Three files, all plain browser JavaScript. No jQuery, no frameworks, no build step.

### `wwwroot/js/validation.js`

A small reusable validation library.

- **`TZValidation.Rules`** — `required`, `minLength`, `maxLength`, `username`
  (letters, numbers, dots, underscores), `email`, `password` (length plus at
  least one letter and one number), and `matches` for confirm-password fields.
  Each rule returns `null` when valid, or a message written for the person
  filling in the form.
- **`TZValidation.Validator`** — attach rules to fields with `addField()`. It
  validates on blur, then re-validates on every keystroke once a field has been
  touched, so people are not shouted at before they have finished typing.
  `validateAll()` checks everything and focuses the first bad field.
- **`TZValidation.scorePassword()`** — scores a password 0–4 based on length,
  mixed case, digits and symbols.

### `wwwroot/js/auth.js`

Runs the sign-in and create-account screens.

- Live field validation using the module above.
- Show / hide password buttons, with correct `aria-pressed` state.
- A password strength meter that fills and changes colour as you type.
- **Live availability checks**: as you type a username or email, a debounced
  `fetch()` (450 ms) asks the server whether it is taken and shows the answer
  under the field. The `debounce` means one request after you stop typing, not
  one per keystroke.
- **AJAX submission**: the form is sent with `fetch()` and the result is shown in
  place, so there is no page reload. Field-level errors returned by the server
  are painted back onto the matching inputs.

### `wwwroot/js/site.js`

Runs on every page.

- Sticky navigation that gains a shadow once you scroll (throttled with
  `requestAnimationFrame`).
- Account dropdown that opens on click and closes on outside click or Escape —
  the CSS already handled hover, this adds keyboard and touch support.
- Mobile hamburger menu.
- Expanding search panel.
- A cart counter kept in `localStorage` so it survives page changes; any element
  marked `data-add-to-cart` increases it, with a small bump animation.
- Newsletter sign-up posted with `fetch()`, answering in place.
- Smooth scrolling for same-page anchor links, which respects
  `prefers-reduced-motion`.

---

## 4. How the JavaScript talks to the server

| Endpoint | Method | Used by | Returns |
| --- | --- | --- | --- |
| `/Auth/Login` | POST | `auth.js` | `{ success, redirectUrl }` or `{ success, message, errors }` |
| `/Auth/Register` | POST | `auth.js` | same shape |
| `/Auth/CheckUsername?value=` | GET | `auth.js` | `{ taken: true/false }` |
| `/Auth/CheckEmail?value=` | GET | `auth.js` | `{ taken: true/false }` |
| `/Home/Subscribe` | POST | `site.js` | `{ success, message }` |

`AuthController` decides how to answer by checking the `X-Requested-With` header:

```csharp
private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
```

A `fetch()` call sets that header and gets JSON back. A plain browser form POST
does not, and gets a redirect or a re-rendered view instead.

**This means the pages still work with JavaScript switched off.** That is called
progressive enhancement, and it is worth saying out loud in your write-up: the
browser checks are there for speed and comfort, but `ValidateRegistration()` in
`AuthController.cs` runs the same rules again on the server, and those are the
ones that actually decide.

---

## 5. Security notes for your report

- **SQL injection**: every query uses `SqlParameter`. User input is never
  concatenated into SQL.
- **CSRF**: every form carries `@Html.AntiForgeryToken()` and every POST action
  is marked `[ValidateAntiForgeryToken]`. `auth.js` reads the hidden token and
  sends it along with the `fetch()` body.
- **Passwords** are stored as SHA-256 hashes encoded as Base64, never as plain
  text. Worth being honest about the limitation: a real site would use a slow,
  salted hash such as PBKDF2 or bcrypt, because SHA-256 is fast enough to brute
  force. It is kept here because it matches the sample hashes in the SQL script
  and keeps the code readable.
- **Open redirect** is guarded with `Url.IsLocalUrl(returnUrl)` before
  redirecting after sign-in.
- **Failed sign-ins** are logged to `LoginAttempts` with the IP address.

---

## 6. Running it

```
dotnet build
dotnet run
```

Or press F5 in Visual Studio. Then visit `/Auth/Register` to create an account,
or `/Auth/Login` and sign in as `testuser` / `Test@123`.

If the database is unreachable the app still starts —
`DatabaseInitializationMiddleware` logs the error and carries on rather than
crashing, so you can see the home page even before SQL Server is set up.

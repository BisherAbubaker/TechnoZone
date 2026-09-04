using Microsoft.AspNetCore.Mvc;
using TechnoZone.Data;
using TechnoZone.Models;

namespace TechnoZone.Controllers
{
    /// <summary>
    /// Sign-in and account creation.
    ///
    /// Each action answers in one of two ways:
    ///   - a normal browser POST gets a redirect or a re-rendered view
    ///   - a fetch() call from auth.js (marked with X-Requested-With) gets JSON
    /// That way the pages still work if JavaScript is switched off.
    /// </summary>
    public class AuthController : Controller
    {
        private readonly DatabaseConnection _db;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _db = new DatabaseConnection(configuration);
            _logger = logger;
        }

        private bool IsAjax =>
            Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        private bool IsSignedIn =>
            HttpContext.Session.GetInt32("UserId").HasValue;

        // =====================================================================
        //  SIGN IN
        // =====================================================================

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (IsSignedIn)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model, string? returnUrl = null)
        {
            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(model.Username))
            {
                errors["Username"] = "Username is required";
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                errors["Password"] = "Password is required";
            }

            if (errors.Count > 0)
            {
                return Fail("Check the highlighted fields and try again.", errors, model);
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                var user = _db.AuthenticateUser(model.Username, model.Password);

                if (user == null)
                {
                    _db.LogLoginAttempt(model.Username, false, ip);
                    _logger.LogWarning("Failed sign-in for {Username}", model.Username);

                    return Fail("That username and password do not match.", errors, model);
                }

                _db.LogLoginAttempt(user.Username, true, ip);

                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("FirstName", user.FirstName);
                HttpContext.Session.SetString("LastName", user.LastName);

                _logger.LogInformation("{Username} signed in", user.Username);

                var target = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : Url.Action("Index", "Home")!;

                if (IsAjax)
                {
                    return Json(new { success = true, redirectUrl = target });
                }

                return Redirect(target);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sign-in failed");
                return Fail("The sign-in service is unavailable. Try again in a moment.", errors, model);
            }
        }

        private IActionResult Fail(string message, Dictionary<string, string> errors, LoginViewModel model)
        {
            if (IsAjax)
            {
                return Json(new { success = false, message, errors });
            }

            foreach (var pair in errors)
            {
                ModelState.AddModelError(pair.Key, pair.Value);
            }

            ModelState.AddModelError(string.Empty, message);
            model.Password = string.Empty;
            return View(model);
        }

        // =====================================================================
        //  CREATE ACCOUNT
        // =====================================================================

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (IsSignedIn)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            var errors = ValidateRegistration(model);

            if (errors.Count > 0)
            {
                return RegisterFail("Check the highlighted fields and try again.", errors, model);
            }

            try
            {
                var created = _db.RegisterUser(
                    model.Username.Trim(),
                    model.Email.Trim(),
                    model.Password,
                    model.FirstName?.Trim() ?? string.Empty,
                    model.LastName?.Trim() ?? string.Empty);

                if (!created)
                {
                    errors["Username"] = "That username or email is already registered";
                    return RegisterFail("That username or email is already registered.", errors, model);
                }

                _logger.LogInformation("New account created: {Username}", model.Username);

                var target = Url.Action("Login", "Auth")!;

                if (IsAjax)
                {
                    return Json(new { success = true, redirectUrl = target });
                }

                TempData["SuccessMessage"] = "Account created. Sign in with your new details.";
                return Redirect(target);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account creation failed");
                return RegisterFail("The account could not be created right now. Try again in a moment.", errors, model);
            }
        }

        /// <summary>
        /// Server-side copy of the rules in wwwroot/js/validation.js.
        /// The browser checks are for speed; these are the ones that count.
        /// </summary>
        private Dictionary<string, string> ValidateRegistration(RegisterViewModel model)
        {
            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(model.FirstName))
            {
                errors["FirstName"] = "First name is required";
            }

            if (string.IsNullOrWhiteSpace(model.LastName))
            {
                errors["LastName"] = "Last name is required";
            }

            if (string.IsNullOrWhiteSpace(model.Username) || model.Username.Trim().Length < 3)
            {
                errors["Username"] = "Username needs at least 3 characters";
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.Username.Trim(), @"^[a-zA-Z0-9._]+$"))
            {
                errors["Username"] = "Use letters, numbers, dots or underscores only";
            }

            if (!IsValidEmail(model.Email))
            {
                errors["Email"] = "Enter an email address in the form name@example.com";
            }

            if (string.IsNullOrEmpty(model.Password) || model.Password.Length < 6)
            {
                errors["Password"] = "Password needs at least 6 characters";
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.Password, "[a-zA-Z]") ||
                     !System.Text.RegularExpressions.Regex.IsMatch(model.Password, "[0-9]"))
            {
                errors["Password"] = "Password needs at least one letter and one number";
            }

            if (model.Password != model.ConfirmPassword)
            {
                errors["ConfirmPassword"] = "The two passwords do not match";
            }

            return errors;
        }

        private IActionResult RegisterFail(string message, Dictionary<string, string> errors, RegisterViewModel model)
        {
            if (IsAjax)
            {
                return Json(new { success = false, message, errors });
            }

            foreach (var pair in errors)
            {
                ModelState.AddModelError(pair.Key, pair.Value);
            }

            ModelState.AddModelError(string.Empty, message);
            model.Password = string.Empty;
            model.ConfirmPassword = string.Empty;
            return View(model);
        }

        // =====================================================================
        //  LIVE CHECKS CALLED BY auth.js
        // =====================================================================

        // GET: /Auth/CheckUsername?value=alice
        [HttpGet]
        public IActionResult CheckUsername(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 3)
            {
                return Json(new { taken = false });
            }

            return Json(new { taken = _db.IsUsernameTaken(value.Trim()) });
        }

        // GET: /Auth/CheckEmail?value=alice@example.com
        [HttpGet]
        public IActionResult CheckEmail(string value)
        {
            if (!IsValidEmail(value))
            {
                return Json(new { taken = false });
            }

            return Json(new { taken = _db.IsEmailTaken(value.Trim()) });
        }

        // =====================================================================
        //  SIGN OUT
        // =====================================================================

        // GET: /Auth/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            var username = HttpContext.Session.GetString("Username");
            HttpContext.Session.Clear();
            _logger.LogInformation("{Username} signed out", username ?? "A visitor");

            return RedirectToAction("Index", "Home");
        }

        // =====================================================================
        //  PROFILE
        // =====================================================================

        // GET: /Auth/Profile
        [HttpGet]
        public IActionResult Profile()
        {
            if (!IsSignedIn)
            {
                return RedirectToAction("Login", new { returnUrl = "/Auth/Profile" });
            }

            var model = new ProfileViewModel
            {
                Username = HttpContext.Session.GetString("Username") ?? string.Empty,
                Email = HttpContext.Session.GetString("Email") ?? string.Empty,
                FirstName = HttpContext.Session.GetString("FirstName") ?? string.Empty,
                LastName = HttpContext.Session.GetString("LastName") ?? string.Empty
            };

            return View(model);
        }

        // =====================================================================

        private static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                var address = new System.Net.Mail.MailAddress(email.Trim());
                return address.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }
    }
}

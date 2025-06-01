using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AichatBot3.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
using AichatBot3.Controllers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;


namespace AichatBot2.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;


        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;

        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = "/")
        {
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };
            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, [FromForm] string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl ?? "/";

            if (!ModelState.IsValid)
                return View(model);

            ApplicationUser user = null;

            // ✅ Determine whether input is email or username
            if (model.LoginIdentifier.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(model.LoginIdentifier);
            }
            else
            {
                user = await _userManager.FindByNameAsync(model.LoginIdentifier);
            }

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // 🔒 Check email confirmation
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "You need to confirm your email before you can log in.");
                return View(model);
            }

            // 🔐 Attempt login with lockout enabled
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true
            );

            if (result.RequiresTwoFactor)
            {
                return RedirectToAction("Choose2FAMethod", "TwoFactor", new { returnUrl, rememberMe = model.RememberMe });
            }

            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                if (lockoutEnd.HasValue)
                {
                    var timeLeft = lockoutEnd.Value - DateTimeOffset.UtcNow;
                    var minutes = Math.Round(timeLeft.TotalMinutes, 1);
                    var seconds = timeLeft.Seconds;

                    ModelState.AddModelError("", $"Your account is locked. Try again in {minutes} minute(s) and {seconds} second(s).");
                }
                else
                {
                    ModelState.AddModelError("", "Your account is locked. Please try again later.");
                }

                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }







        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        public IActionResult RegistrationSuccessful()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var returnUrl = model.ReturnUrl;

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    CreatedAt = DateTime.UtcNow,
                    LockoutEnabled = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Set the lockout enabled flag
                    await _userManager.SetLockoutEnabledAsync(user, true);

                    await _userManager.AddToRoleAsync(user, "User");

                    // Generate email confirmation token
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    // Build confirmation link
                    var confirmationLink = Url.Action("ConfirmEmail", "Account",
                        new { userId = user.Id, token = token }, protocol: HttpContext.Request.Scheme);

                    // Send confirmation email
                    var emailBody = $@"
                        <html>
                            <body style='font-family: Arial, sans-serif; background-color: #f8f9fa; padding: 20px;'>
                                <div style='max-width: 600px; margin: auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); padding: 30px;'>
                                    <h2 style='color: #343a40;'>Welcome to AIChatBot, {user.UserName}!</h2>
                                    <p style='font-size: 16px; color: #495057;'>
                                        Thank you for registering. To complete your registration and verify your email address, please click the button below:
                                    </p>
                                    <p style='text-align: center; margin: 30px 0;'>
                                        <a href='{confirmationLink}' style='background-color: #007bff; color: white; padding: 12px 24px; border-radius: 5px; text-decoration: none; display: inline-block;'>Confirm Your Email</a>
                                    </p>
                                    <p style='font-size: 14px; color: #6c757d;'>
                                        If you didn’t create this account, you can safely ignore this email.
                                    </p>
                                    <hr style='border: none; border-top: 1px solid #dee2e6;' />
                                    <p style='font-size: 12px; color: #adb5bd; text-align: center;'>
                                        &copy; {DateTime.UtcNow.Year} AIChatBot. All rights reserved.
                                    </p>
                                </div>
                            </body>
                        </html>";

                    await _emailSender.SendEmailAsync(user.Email, "Confirm your email address", emailBody);

                    // Redirect to RegistrationSuccessful page after successful registration
                    TempData["Success"] = "User registered successfully. A confirmation email has been sent.";
                    return RedirectToAction("RegistrationSuccessful", "Account");  // This should work properly now
                }

                // If registration fails, add errors to the model
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If the model is invalid, return the view with errors
            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                // Set a success message in TempData to be shown in the view
                TempData["Success"] = "Your email has been successfully verified!";

                await _signInManager.SignInAsync(user, isPersistent: false);

                // ✅ Redirect to your custom Home/Index
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // In case of failure, redirect to an error page
            return RedirectToAction("Error");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/")
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/", string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "Error loading external login information.";
                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
            }

            // Try to sign in the user with the external login info
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);

            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl); // ✅ User already exists
            }

            // User does not exist yet, create account
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email == null)
            {
                TempData["ErrorMessage"] = "Email not received from the external provider.";
                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
            }

            // Check if the user already exists by email
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "An account with this email already exists.";
                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
            }

            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "First";
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "Last";
            var picture = info.Principal.FindFirstValue("urn:google:picture");
            var locale = info.Principal.FindFirstValue("locale");


            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                ProfilePictureUrl = picture,
                Locale = locale
            };

            var createResult = await _userManager.CreateAsync(user);

            if (createResult.Succeeded)
            {
                await _userManager.AddLoginAsync(user, info);
                await _signInManager.SignInAsync(user, isPersistent: false);

                // ✅ Redirect to confirmation or profile setup page on first login
                return RedirectToAction("Index", "Home", new
                {
                    email,
                    returnUrl,
                    provider = info.LoginProvider
                });
            }

            TempData["ErrorMessage"] = "Account creation failed.";
            return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
        }

    }

}

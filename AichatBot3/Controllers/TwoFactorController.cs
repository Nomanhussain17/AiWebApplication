using AichatBot3.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using AichatBot3.ViewModels;
using System.Text;
using OtpNet;
using QRCoder;
using System.Text.Encodings.Web;

namespace AichatBot3.Controllers
{
    public class TwoFactorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public TwoFactorController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        //[HttpGet]
        //public async Task<IActionResult> Enable2FA()
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

        //    var model = new Enable2FAViewModel
        //    {
        //        IsEmailConfirmed = isEmailConfirmed,
        //        TwoFactorEnabled = user.TwoFactorEnabled
        //    };

        //    return View(model);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable2FA()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Toggle TwoFactorEnabled
            user.TwoFactorEnabled = !user.TwoFactorEnabled;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Message"] = $"2FA {(user.TwoFactorEnabled ? "enabled" : "disabled")} successfully.";
                return RedirectToAction("ProfileManage", "ProfileManage");
            }

            TempData["Error"] = "Something went wrong while updating 2FA.";
            return RedirectToAction("Index", "Home");
        }



        [HttpGet]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Unable to load two-factor authentication user.");
            }

            // Send 2FA code via email
            //var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            //await _emailSender.SendEmailAsync(user.Email, "Your 2FA Code", $"Your 2FA code is: {code}");

            return View(new LoginWith2faViewModel
            {
                RememberMe = rememberMe,
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Unable to load user.");
            }

            var code = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            var result = await _signInManager.TwoFactorSignInAsync(TokenOptions.DefaultEmailProvider, code, model.RememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                return Redirect(model.ReturnUrl ?? "/");
            }

            if (result.IsLockedOut)
            {
                return RedirectToAction("Lockout");
            }

            ModelState.AddModelError(string.Empty, "Invalid code.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Resend2FACode()
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            await _emailSender.SendEmailAsync(user.Email, "Your 2FA Code", $"Your 2FA code is: {code}");

            TempData["Message"] = "A new 2FA code has been sent to your email.";
            return RedirectToAction("LoginWith2fa");
        }


        //Google Authenticator

        [HttpGet]
        public async Task<IActionResult> SetupAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Use existing secret if present, else generate
            string secret;
            if (string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                var key = KeyGeneration.GenerateRandomKey(20);
                secret = Base32Encoding.ToString(key);
                user.TwoFactorSecret = secret;
                await _userManager.UpdateAsync(user);
            }
            else
            {
                secret = user.TwoFactorSecret;
            }

            var otpauthUrl = $"otpauth://totp/{UrlEncoder.Default.Encode("YourApp")}:{UrlEncoder.Default.Encode(user.Email)}?secret={secret}&issuer={UrlEncoder.Default.Encode("YourApp")}&digits=6";

            ViewBag.QrCodeImage = GenerateQrCodeImage(otpauthUrl);
            ViewBag.SecretKey = secret;
            return View();
        }


        // Helper: Verify TOTP
        private bool VerifyAuthenticatorCode(ApplicationUser user, string code)
        {
            if (string.IsNullOrEmpty(user.TwoFactorSecret)) return false;
            var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecret));
            return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(previous: 1, future: 1));

        }

        // Helper: Generate QR Code
        private string GenerateQrCodeImage(string otpauthUrl)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(otpauthUrl, QRCodeGenerator.ECCLevel.Q);
            var pngQrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = pngQrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }



        [HttpPost]
        public async Task<IActionResult> VerifyAuthenticatorCode(SetupAuthenticatorViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Verify manually with Otp.NET
            var secretKey = user.TwoFactorSecret;
            if (string.IsNullOrEmpty(secretKey))
            {
                ModelState.AddModelError("Code", "Authenticator is not setup properly.");
                return View("SetupAuthenticator", model);
            }

            var totp = new Totp(Base32Encoding.ToBytes(secretKey));
            bool isValid = totp.VerifyTotp(model.Code.Trim(), out long timeStepMatched, new VerificationWindow(2, 2));

            if (!isValid)
            {
                ModelState.AddModelError("Code", "Invalid verification code.");
                return View("SetupAuthenticator", model);
            }

            // Mark 2FA enabled on your user model (custom property)
            user.TwoFactorEnabled = true;
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Enable2FA");
        }





        //private string FormatKey(string unformattedKey)
        //{
        //    var result = new StringBuilder();
        //    int currentPosition = 0;
        //    while (currentPosition + 4 < unformattedKey.Length)
        //    {
        //        result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
        //        currentPosition += 4;
        //    }
        //    if (currentPosition < unformattedKey.Length)
        //    {
        //        result.Append(unformattedKey.Substring(currentPosition));
        //    }
        //    return result.ToString().ToLowerInvariant();
        //}





        [HttpGet]
        public async Task<IActionResult> Choose2FAMethod(bool rememberMe, string returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Unable to load user.");
            }

            var model = new LoginWith2faViewModel
            {
                ReturnUrl = returnUrl,
                RememberMe = rememberMe
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Choose2FAMethod(string method, LoginWith2faViewModel model)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Unable to load user.");
            }

            if (method == "Email")
            {
                await _userManager.UpdateSecurityStampAsync(user); // Force Identity to invalidate previous tokens

                var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
                var emailBody = $@"
                <p>Hello {user.UserName},</p>
                <p>Your two-factor authentication (2FA) code is:</p>
                <h2>{code}</h2>
                <p>This code will expire shortly. Please do not share it with anyone.</p>
                <p>Thanks,<br/>AIChatBot Security Team</p>";

                await _emailSender.SendEmailAsync(user.Email, "Your 2FA Code", emailBody);

                return RedirectToAction("LoginWith2fa", new { model.RememberMe, model.ReturnUrl });
            }
            else if (method == "Authenticator")
            {
                return RedirectToAction("LoginWithAuthenticator", new { model.RememberMe, model.ReturnUrl });
            }

            ModelState.AddModelError("", "Invalid selection.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> LoginWithAuthenticator(bool rememberMe, string returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Unable to load user.");
            }

            return View("LoginWithAuthenticator", new LoginWith2faViewModel
            {
                RememberMe = rememberMe,
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> LoginWithAuthenticator(LoginWith2faViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get the user who is in the middle of two-factor authentication
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Unable to load two-factor authentication user.");
                return View(model);
            }

            // Clean and sanitize the authenticator code input
            var authenticatorCode = model.TwoFactorCode?.Replace(" ", string.Empty).Replace("-", string.Empty).Trim();

            if (string.IsNullOrEmpty(authenticatorCode))
            {
                ModelState.AddModelError(string.Empty, "Authenticator code is required.");
                return View(model);
            }

            // Get the user's stored authenticator secret key
            var secret = user.TwoFactorSecret;
            if (string.IsNullOrEmpty(secret))
            {
                ModelState.AddModelError(string.Empty, "Authenticator key is missing.");
                return View(model);
            }

            // Verify the TOTP code using Otp.NET
            var totp = new Totp(Base32Encoding.ToBytes(secret));
            bool isValid = totp.VerifyTotp(authenticatorCode, out long timeStepMatched, new VerificationWindow(previous: 2, future: 2));

            if (!isValid)
            {
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return View(model);
            }

            // Code is valid, complete the sign-in with or without remembering the user
            await _signInManager.SignInAsync(user, model.RememberMe);

            // Optionally, you can handle RememberMachine here if you want to remember this device

            // Redirect to the return URL or home page
            return LocalRedirect(model.ReturnUrl ?? "/");
        }






    }
}

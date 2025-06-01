using AichatBot3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AichatBot2.Controllers // Adjust namespace
{
    [Authorize(Roles = "User,Admin")] // Require user to be logged in
    public class ProfileManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileManageController(UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _signInManager = signInManager;
        }

        // Helper to load data
        private async Task<ManageProfileViewModel> LoadViewModelAsync(ApplicationUser user, string? statusMessage = null)
        {
            return new ManageProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.UserName,
                Email = await _userManager.GetEmailAsync(user), // Use GetEmailAsync
                IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user), // Use IsEmailConfirmedAsync
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user), // Use IsEmailConfirmedAsync
                IsPhoneConfirmed = await _userManager.IsPhoneNumberConfirmedAsync(user), // Use IsEmailConfirmedAsync
                TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                ProfilePictureUrl = user.ProfilePictureUrl,
                StatusMessage = statusMessage
            };
        }

        // GET: /ProfileManage/Index (Renders the ProfileManage.cshtml view)
        [HttpGet]
        public async Task<IActionResult> Index(string? statusMessage = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = await LoadViewModelAsync(user, statusMessage);

            // *** Specify the View path since it's in Views/Home/ ***
            return View("~/Views/ProfileManage/ProfileManage.cshtml", model);
            // Or if you moved the view to Views/ProfileManage/Index.cshtml, just use:
            // return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Optional: Delete related data here before removing user

            await _signInManager.SignOutAsync();
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                // Optionally add logging
                ModelState.AddModelError(string.Empty, "Failed to delete account.");
                return RedirectToAction("Index", "ProfileManage");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone))
            {
                ModelState.AddModelError("", "Phone number is required.");
                return View(); // or redirect back with error
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            user.PhoneNumber = phone;
            //user.PhoneNumberConfirmed = true; // Optional: confirm it directly
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Message"] = "Phone number updated successfully!";
            }
            else
            {
                TempData["Message"] = "Failed to update phone number.";
            }

            return RedirectToAction("Index", "ProfileManage"); // or your account details page
        }


        // POST: /ProfileManage/UploadProfilePicture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(ManageProfileViewModel model) // Receives the file via the model
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Reset model state related to non-upload fields before validation
            ModelState.Remove("Username");
            ModelState.Remove("Email");
            ModelState.Remove("IsEmailConfirmed");

            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                // --- Validation ---
                long maxFileSize = 2 * 1024 * 1024; // 2 MB
                string[] permittedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(model.ProfilePictureFile.FileName).ToLowerInvariant();

                if (model.ProfilePictureFile.Length > maxFileSize)
                {
                    ModelState.AddModelError("ProfilePictureFile", $"File size exceeds limit of {maxFileSize / 1024 / 1024} MB.");
                }
                else if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("ProfilePictureFile", "Invalid file type. Allowed types: " + string.Join(", ", permittedExtensions));
                }
                else
                {
                    // --- File Saving Logic ---
                    try
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profilepics");
                        Directory.CreateDirectory(uploadsFolder); // Ensure directory exists
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ProfilePictureFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ProfilePictureFile.CopyToAsync(fileStream);
                        }

                        // (Optional) Delete old file
                        if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && user.ProfilePictureUrl != "/images/profilepics/default-avatar.png")
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                try { System.IO.File.Delete(oldFilePath); } catch (IOException ioEx) { Console.WriteLine($"Error deleting old file: {ioEx.Message}"); }
                            }
                        }

                        // Update user profile URL path
                        user.ProfilePictureUrl = $"/images/profilepics/{uniqueFileName}";
                        var updateResult = await _userManager.UpdateAsync(user);

                        if (updateResult.Succeeded)
                        {
                            return RedirectToAction(nameof(Index), new { statusMessage = "Profile picture updated successfully." });
                        }
                        else
                        {
                            updateResult.Errors.ToList().ForEach(e => ModelState.AddModelError(string.Empty, e.Description));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error uploading file: {ex.ToString()}");
                        ModelState.AddModelError("ProfilePictureFile", "An error occurred uploading the file. Please try again.");
                    }
                }
            }
            else
            {
                ModelState.AddModelError("ProfilePictureFile", "Please select a file to upload.");
            }

            // If ModelState is invalid or upload failed, redisplay form with errors
            var redisplayModel = await LoadViewModelAsync(user); // Reload data
            redisplayModel.ProfilePictureFile = null; // Clear the invalid file input model state
            // *** Specify the View path since it's in Views/Home/ ***
            return View("~/Views/ProfileManage/ProfileManage.cshtml", redisplayModel);
            // Or if you moved the view to Views/ProfileManage/Index.cshtml, just use:
            // return View("Index", redisplayModel);
        }

        // --- Link to Change Password ---
        // No action needed here if linking directly to Identity Pages
        // If you need a custom change password page, add GET/POST actions here.

        public async Task<IActionResult> ProfileManage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Always fetch fresh user info from the DB
            var freshUser = await _userManager.FindByIdAsync(user.Id);

            var model = new ManageProfileViewModel
            {
                FirstName = freshUser.FirstName,
                LastName = freshUser.LastName,
                Username = freshUser.UserName,
                Email = freshUser.Email,
                IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(freshUser),
                PhoneNumber = freshUser.PhoneNumber,
                IsPhoneConfirmed = await _userManager.IsPhoneNumberConfirmedAsync(freshUser),
                TwoFactorEnabled = freshUser.TwoFactorEnabled,
                ProfilePictureUrl = freshUser.ProfilePictureUrl, // If you store it
                StatusMessage = TempData["Message"] as string
            };

            return View(model);
        }


    }
}
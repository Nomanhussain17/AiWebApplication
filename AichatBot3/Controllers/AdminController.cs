using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AichatBot3.ViewModels;

namespace AichatBot3.Admin.Controllers
{
    //[Area("Admin")]
    [Authorize(Roles = "Admin,CEO")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;




        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;

        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term, int page = 1)
        {
            int pageSize = 10;
            // Ensure page is valid
            int pageNumber = page < 1 ? 1 : page;

            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(term))
            {
                term = term.ToLower();
                usersQuery = usersQuery.Where(u => u.UserName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
            }

            // Get total count BEFORE skipping/taking
            int totalItemCount = await usersQuery.CountAsync();

            // Apply ordering THEN skipping/taking
            var usersForPageQuery = usersQuery
                .OrderBy(u => u.UserName)
                .Skip((pageNumber - 1) * pageSize) // Calculate items to skip
                .Take(pageSize); // Take only the items for this page

            // Execute query to get users for the current page
            var usersOnPage = await usersForPageQuery.ToListAsync();

            // Map the current page's users to ViewModel and get roles
            var userWithRolesList = new List<UserWithRolesViewModel>();
            foreach (var user in usersOnPage)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userWithRolesList.Add(new UserWithRolesViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    Roles = roles.ToList()
                });
            }

            // Prepare the ViewModel with manual pagination data
            var tableViewModel = new UserTableViewModel
            {
                Users = userWithRolesList, // The list for the current page
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItemCount = totalItemCount,
                CurrentSearchTerm = term
            };

            return PartialView("_UsersTable", tableViewModel);
        }


        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["Success"] = "User deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Error deleting user.";
                }
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> LockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Lock the user for 100 years (or any long duration)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["Success"] = $"User '{user.UserName}' has been locked.";
            }
            else
            {
                TempData["Error"] = "User not found.";
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null); // unlock immediately
                TempData["Success"] = $"User '{user.UserName}' has been unlocked.";
            }
            else
            {
                TempData["Error"] = "User not found.";
            }

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Dashboard()
        {
            var users = _userManager.Users.ToList();

            // Total Users
            var totalUsers = users.Count;

            // Locked Users
            var lockedUsers = users.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow).Count();

            // New Registrations in the last 24 hours
            var newRegistrations = users.Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-1)).Count();

            // Email Confirmed Users
            var emailConfirmedUsers = users.Where(u => u.EmailConfirmed).Count();

            // Email Not Confirmed Users
            var emailNotConfirmedUsers = users.Where(u => !u.EmailConfirmed).Count();

            // Passing data to the view
            ViewData["TotalUsers"] = totalUsers;
            ViewData["LockedUsers"] = lockedUsers;
            ViewData["NewRegistrations"] = newRegistrations;
            ViewData["EmailConfirmed"] = emailConfirmedUsers;
            ViewData["EmailNotConfirmed"] = emailNotConfirmedUsers;

            return View();
        }

        // GET: /Admin/CreateUser
        [HttpGet]
        public async Task<IActionResult> CreateUser() // Make GET action async
        {
            // --- FIX: Fetch roles dynamically ---
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            // Create SelectList using Role Name for both Value and Text
            ViewBag.RoleList = new SelectList(roles.Select(r => r.Name).ToList());
            // ---------------------------------

            // Return the view (optionally pass a new ViewModel if your view uses @model)
            return View(new CreateUserViewModel()); // Pass new model assuming view uses @model CreateUserViewModel
        }

        // POST: /Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken] // Good practice: Add AntiForgeryToken validation
        public async Task<IActionResult> CreateUser(CreateUserViewModel model) // Assuming model has string Role property
        {
            // --- FIX: Fetch roles dynamically AGAIN ---
            // Required in case ModelState is invalid and we need to return the View
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            ViewBag.RoleList = new SelectList(roles.Select(r => r.Name).ToList());
            // ------------------------------------

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    LockoutEnabled = true, // Sensible default
                    CreatedAt = DateTime.UtcNow // Sensible default
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // 🔐 Assign Role (This part was okay, uses model.Role from dropdown)
                    if (!string.IsNullOrEmpty(model.Role) && await _roleManager.RoleExistsAsync(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }
                    else if (!string.IsNullOrEmpty(model.Role))
                    {
                        // Optional: Log a warning if the submitted role somehow doesn't exist
                        Console.WriteLine($"Warning: Role '{model.Role}' selected in form but not found in database during user creation.");
                        // Consider adding a ModelState error if this is critical
                        // ModelState.AddModelError("Role", "Selected role does not exist.");
                        // return View(model);
                    }

                    // ... (rest of your email confirmation logic is fine) ...
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, protocol: HttpContext.Request.Scheme);
                    await _emailSender.SendEmailAsync(user.Email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");
                    // -------------------------------------------------------

                    TempData["Success"] = "User created successfully. A confirmation email has been sent.";
                    return RedirectToAction("Users");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            // If ModelState invalid, return view with errors AND the dynamically populated RoleList
            return View(model);
        }

        // GET: Admin/EditUser/GUID
        [HttpGet]
        public async Task<IActionResult> EditUser(string id) // Changed action name to EditUser
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("User ID not provided.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Users"); // Or return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                SelectedRoles = userRoles.ToList(), // Pre-select current roles
                AllRoles = allRoles.Select(role => new SelectListItem
                {
                    Value = role.Name,
                    Text = role.Name,
                    // No 'Selected' property needed here if using SelectedRoles for the checkbox check
                }).ToList()
            };

            return View(model); // We will create this View next
        }


        // POST: Admin/EditUser/GUID
        [HttpPost]
        [ValidateAntiForgeryToken] // Good practice for POST actions
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Re-populate AllRoles if validation fails and we return the view
                var allRoles = await _roleManager.Roles.ToListAsync();
                model.AllRoles = allRoles.Select(role => new SelectListItem { Value = role.Name, Text = role.Name }).ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Users"); // Or return NotFound();
            }

            // --- Update User Properties ---
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.EmailConfirmed = model.EmailConfirmed;
            // Note: You usually don't change NormalizedUserName/Email directly, Identity handles it

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                // Re-populate AllRoles before returning view with errors
                var allRolesList = await _roleManager.Roles.ToListAsync();
                model.AllRoles = allRolesList.Select(role => new SelectListItem { Value = role.Name, Text = role.Name }).ToList();
                return View(model);
            }

            // --- Update User Roles ---
            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.SelectedRoles ?? new List<string>(); // Ensure it's not null

            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Error adding roles.");
                    // Consider logging errors: addResult.Errors
                    var allRolesList = await _roleManager.Roles.ToListAsync();
                    model.AllRoles = allRolesList.Select(role => new SelectListItem { Value = role.Name, Text = role.Name }).ToList();
                    return View(model); // Return view with error
                }
            }

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Error removing roles.");
                    // Consider logging errors: removeResult.Errors
                    var allRolesList = await _roleManager.Roles.ToListAsync();
                    model.AllRoles = allRolesList.Select(role => new SelectListItem { Value = role.Name, Text = role.Name }).ToList();
                    return View(model); // Return view with error
                }
            }

            TempData["Success"] = $"User '{user.UserName}' updated successfully.";
            return RedirectToAction("Users");
        }


        // GET: /Admin/ListRoles
        [HttpGet]
        public async Task<IActionResult> ListRoles()
        {
            // Retrieve all roles from the database
            var roles = await _roleManager.Roles.ToListAsync();
            // Pass the list of roles to the view
            return View(roles); // We'll create this ListRoles.cshtml view next
        }

        // GET: /Admin/CreateRole
        [HttpGet]
        public IActionResult CreateRole()
        {
            // Simply display the view to create a new role
            return View(); // We'll create this CreateRole.cshtml view next
        }

        // POST: /Admin/CreateRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(CreateRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if role already exists
                bool roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
                if (roleExists)
                {
                    ModelState.AddModelError("", $"Role '{model.RoleName}' already exists.");
                    return View(model);
                }

                // Create the new role object
                IdentityRole identityRole = new IdentityRole
                {
                    Name = model.RoleName
                };

                // Save the new role to the database
                IdentityResult result = await _roleManager.CreateAsync(identityRole);

                if (result.Succeeded)
                {
                    TempData["Success"] = $"Role '{model.RoleName}' created successfully.";
                    // Redirect to the list of roles after successful creation
                    return RedirectToAction("ListRoles");
                }

                // If creation failed, add errors to ModelState and return the view
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            // If ModelState is invalid, return the view with the model to show validation errors
            return View(model);
        }

        // GET: /Admin/EditRole/roleId
        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("Role ID not provided.");
            }

            // Find the role by ID
            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction("ListRoles"); // Or return NotFound();
            }

            // Populate the ViewModel
            var model = new EditRoleViewModel
            {
                Id = role.Id,
                RoleName = role.Name
            };

            // Pass the ViewModel to the EditRole view
            return View(model); // We'll create this EditRole.cshtml view next
        }

        // POST: /Admin/EditRole/roleId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(EditRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Find the existing role by ID
                var role = await _roleManager.FindByIdAsync(model.Id);

                if (role == null)
                {
                    TempData["Error"] = "Role not found.";
                    return RedirectToAction("ListRoles"); // Or return View(model) with error
                }

                // Check if the new name already exists (and isn't the current role's name)
                var existingRoleWithNewName = await _roleManager.FindByNameAsync(model.RoleName);
                if (existingRoleWithNewName != null && existingRoleWithNewName.Id != role.Id)
                {
                    ModelState.AddModelError("RoleName", $"Role '{model.RoleName}' already exists.");
                    return View(model);
                }


                // Update the role name
                role.Name = model.RoleName;
                // Note: Identity normalizes the name automatically when updating
                // role.NormalizedName = _roleManager.NormalizeKey(model.RoleName); // Usually not needed to set manually

                // Save the updated role
                var result = await _roleManager.UpdateAsync(role);

                if (result.Succeeded)
                {
                    TempData["Success"] = $"Role '{model.RoleName}' updated successfully.";
                    return RedirectToAction("ListRoles");
                }

                // If update failed, add errors and return view
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            // If model state invalid, return view with errors
            return View(model);
        }


        // POST: /Admin/DeleteRole/roleId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string id) // Typically called from a form in the ListRoles view
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Role ID not provided.";
                return RedirectToAction("ListRoles");
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction("ListRoles");
            }

            // **Important Check:** Prevent deleting roles with assigned users (Optional but recommended)
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            if (usersInRole.Any())
            {
                TempData["Error"] = $"Cannot delete role '{role.Name}' because it has users assigned to it. Please remove users from the role first.";
                return RedirectToAction("ListRoles");
            }
            // End Optional Check


            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Role '{role.Name}' deleted successfully.";
            }
            else
            {
                // Log errors or add them to TempData if needed
                TempData["Error"] = $"Error deleting role '{role.Name}'.";
                // Example: TempData["Error"] = $"Error deleting role '{role.Name}': {string.Join(", ", result.Errors.Select(e => e.Description))}";
                Console.WriteLine($"Error deleting role {role.Name}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return RedirectToAction("ListRoles");
        }

    }
}


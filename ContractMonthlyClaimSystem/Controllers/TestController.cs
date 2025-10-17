// Controllers/TestController.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Data;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class TestController : Controller
    {
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _passwordHasher = new PasswordHasher<User>();
            _context = context;
        }

        [HttpGet]
        public IActionResult PasswordHashTester()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GenerateHash(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter a password";
                return View("PasswordHashTester");
            }

            var user = new User { UserName = "test" };

            // Generate hash
            var hash = _passwordHasher.HashPassword(user, password);

            // Verify hash
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, hash, password);

            ViewBag.Password = password;
            ViewBag.Hash = hash;
            ViewBag.VerificationResult = verificationResult.ToString();
            ViewBag.IsSuccess = verificationResult == PasswordVerificationResult.Success ||
                               verificationResult == PasswordVerificationResult.SuccessRehashNeeded;

            return View("PasswordHashTester");
        }

        [HttpPost]
        public IActionResult VerifyHash(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            {
                ViewBag.Error = "Please enter both password and hash";
                return View("PasswordHashTester");
            }

            var user = new User { UserName = "test", PasswordHash = hash };
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, hash, password);

            ViewBag.Password = password;
            ViewBag.Hash = hash;
            ViewBag.VerificationResult = verificationResult.ToString();
            ViewBag.IsSuccess = verificationResult == PasswordVerificationResult.Success ||
                               verificationResult == PasswordVerificationResult.SuccessRehashNeeded;

            return View("PasswordHashTester");
        }

        [HttpPost]
        public async Task<IActionResult> CreateTestUser(string username, string password, string role)
        {
            try
            {
                // Create a new user manager for this operation
                var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
                var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole<int>>>();

                // Check if user exists
                var existingUser = await userManager.FindByNameAsync(username);
                if (existingUser != null)
                {
                    ViewBag.Error = $"User '{username}' already exists!";
                    return View("PasswordHashTester");
                }

                // Create new user
                var newUser = new User
                {
                    UserName = username,
                    Email = $"{username}@university.ac.za",
                    FirstName = "Test",
                    LastName = "User",
                    UserType = role switch
                    {
                        "Lecturer" => UserType.Lecturer,
                        "ProgrammeCoordinator" => UserType.ProgrammeCoordinator,
                        "AcademicManager" => UserType.AcademicManager,
                        _ => UserType.Administrator
                    },
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(newUser, password);

                if (result.Succeeded)
                {
                    // Add to role
                    await userManager.AddToRoleAsync(newUser, role);

                    // Get the hash that was stored
                    var createdUser = await userManager.FindByNameAsync(username);

                    ViewBag.Success = $"User '{username}' created successfully!";
                    ViewBag.CreatedUsername = username;
                    ViewBag.CreatedPassword = password;
                    ViewBag.CreatedHash = createdUser.PasswordHash;
                    ViewBag.CreatedRole = role;
                }
                else
                {
                    ViewBag.Error = $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error creating user: {ex.Message}";
            }

            return View("PasswordHashTester");
        }

        [HttpGet]
        public async Task<JsonResult> GetUserHashes()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.UserName,
                        u.PasswordHash,
                        u.Email
                    })
                    .ToListAsync();

                return Json(users);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
// Controllers/AccountController.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ContractMonthlyClaimSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var model = new LoginModel();
            return View(model);
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserName} logged in.", model.UserName);

                    // Redirect based on user role
                    var user = await _userManager.FindByNameAsync(model.UserName);
                    if (user != null)
                    {
                        return await RedirectToRolePage(user, returnUrl);
                    }

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        // GET: /Account/Lockout
        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task<IActionResult> RedirectToRolePage(User user, string returnUrl)
        {
            if (await _userManager.IsInRoleAsync(user, "Administrator"))
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (await _userManager.IsInRoleAsync(user, "AcademicManager"))
            {
                return RedirectToAction("Index", "Manager");
            }
            else if (await _userManager.IsInRoleAsync(user, "ProgrammeCoordinator"))
            {
                return RedirectToAction("Index", "Coordinator");
            }
            else if (await _userManager.IsInRoleAsync(user, "Lecturer"))
            {
                return RedirectToAction("Index", "Lecturer");
            }

            return LocalRedirect(returnUrl);
        }
    }
}
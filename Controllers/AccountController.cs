using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;

namespace OnlineLearningApp.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<Account> _userManager;
    private readonly SignInManager<Account> _signInManager;
    private readonly OnlineLearningAppDbContext _context;

    public AccountController(
        UserManager<Account> userManager,
        SignInManager<Account> signInManager,
        OnlineLearningAppDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Users()
    {
        var users = await _context.Accounts
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return View(users);
    }

    [AllowAnonymous]
    public IActionResult SignIn() => View(new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> SignIn(LoginViewModel loginVM)
    {
        if (!ModelState.IsValid)
        {
            return View(loginVM);
        }

        var user = await _userManager.FindByEmailAsync(loginVM.Email);
        if (user is null)
        {
            TempData["Error"] = "Invalid email or password.";
            return View(loginVM);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            loginVM.Password,
            loginVM.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (await _userManager.IsInRoleAsync(user, UserRoles.Admin))
            {
                return RedirectToAction("Index", "Course");
            }

            if (await _userManager.IsInRoleAsync(user, UserRoles.Instructor))
            {
                return RedirectToAction("Index", "Instructor");
            }

            if (await _userManager.IsInRoleAsync(user, UserRoles.Student))
            {
                return RedirectToAction("Index", "Learning");
            }
        }

        TempData["Error"] = result.IsLockedOut
            ? "Your account is temporarily locked. Please try again later."
            : "Invalid email or password.";

        return View(loginVM);
    }

    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel registerVM)
    {
        if (!registerVM.AcceptTerms)
        {
            ModelState.AddModelError(nameof(registerVM.AcceptTerms),
                "You must accept the Terms and Privacy Policy to create an account.");
        }

        registerVM.FullName = registerVM.FullName?.Trim() ?? string.Empty;
        registerVM.EmailAddress = registerVM.EmailAddress?.Trim() ?? string.Empty;

        if (!ModelState.IsValid)
        {
            return View(registerVM);
        }

        var normalizedEmail = registerVM.EmailAddress;
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);

        if (existingUser is not null)
        {
            ModelState.AddModelError(nameof(registerVM.EmailAddress), "This email address is already registered.");
            return View(registerVM);
        }

        var newUser = new Account
        {
            FullName = registerVM.FullName.Trim(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            Role = UserRoles.Student,
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(newUser, registerVM.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(registerVM);
        }

        var roleResult = await _userManager.AddToRoleAsync(newUser, UserRoles.Student);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(newUser);

            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(registerVM);
        }

        await _signInManager.SignInAsync(newUser, isPersistent: false);
        return RedirectToAction(nameof(RegisterCompleted));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Course");
    }

    [AllowAnonymous]
    public IActionResult RegisterCompleted() => View();

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}

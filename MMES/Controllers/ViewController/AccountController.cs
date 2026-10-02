using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMES.Data;
using MMES.Models;
using MMES.Services;

namespace MMES.Controllers.ViewController;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly MMesDbContext _context;

    public AccountController(MMesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login(string? tab, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ActiveTab"] = tab == "register" ? "register" : "login";
        ViewData["RegisterSuccess"] = TempData["RegisterSuccess"];
        return View(new AccountPageViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel login, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ActiveTab"] = "login";

        if (!ModelState.IsValid)
        {
            return View(new AccountPageViewModel { Login = login });
        }

        var user = await _context.TbUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserName == login.EmployeeId);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash) ||
            !PasswordHasher.Verify(login.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid employee ID or password.");
            return View(new AccountPageViewModel { Login = login });
        }

        if (!IsActive(user.Active))
        {
            ModelState.AddModelError(string.Empty, "This account has been disabled. Contact your administrator.");
            return View(new AccountPageViewModel { Login = login });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new("FullName", user.FullName ?? user.UserName ?? string.Empty),
            new(ClaimTypes.Role, user.Role?.Dept ?? string.Empty),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = login.RememberMe });

        user.LastLogin = DateTime.Now;
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel register)
    {
        ViewData["ActiveTab"] = "register";

        var dept = await _context.TbDepts.FindAsync(register.DeptId);
        if (dept is null || string.Equals(dept.Authority, "admin", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(register.DeptId), "This department cannot be used for self-registration.");
        }

        if (ModelState.IsValid)
        {
            var existsUser = await _context.TbUsers.AnyAsync(u => u.UserName == register.EmployeeId);
            if (existsUser)
            {
                ModelState.AddModelError(nameof(register.EmployeeId), "This employee ID (username) is already registered.");
            }

            if (!string.IsNullOrWhiteSpace(register.Email))
            {
                var existsEmail = await _context.TbUsers.AnyAsync(u => u.Email == register.Email.Trim());
                if (existsEmail)
                {
                    ModelState.AddModelError(nameof(register.Email), "This email address is already in use.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return View("Login", new AccountPageViewModel { Register = register });
        }

        var user = new TbUser
        {
            UserName = register.EmployeeId,
            FullName = register.FullName,
            Email = register.Email?.Trim(),
            RoleId = dept!.Id,
            PasswordHash = PasswordHasher.Hash(register.Password),
            Active = "Y",
            CreateAt = DateTime.Now,
        };

        try
        {
            _context.TbUsers.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            if (msg.Contains("EMAIL_UNIQUE", StringComparison.OrdinalIgnoreCase) || msg.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(register.Email), "This email address is already in use.");
            }
            else if (msg.Contains("PRIMARY", StringComparison.OrdinalIgnoreCase) || msg.Contains("USERNAME", StringComparison.OrdinalIgnoreCase) || msg.Contains("user_name", StringComparison.OrdinalIgnoreCase) || msg.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(register.EmployeeId), "This employee ID (username) is already registered.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Failed to create account. Please check your information.");
            }

            return View("Login", new AccountPageViewModel { Register = register });
        }

        TempData["RegisterSuccess"] = "Account created. You can sign in now.";
        return RedirectToAction(nameof(Login), new { tab = "login" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // Convention xac nhan: cot active chi co "Y" (active) hoac "N" (khoa tai khoan).
    private static bool IsActive(string? active) =>
        string.Equals(active, "Y", StringComparison.OrdinalIgnoreCase);

    // Bootstrap tai khoan admin dau tien. Tu vo hieu hoa ngay khi tb_user co bat ky row nao,
    // nen khong the dung de tao them admin sau khi he thong da co tai khoan.
    [HttpPost]
    public async Task<IActionResult> BootstrapAdmin()
    {
        if (await _context.TbUsers.AnyAsync())
        {
            return NotFound();
        }

        var adminDept = await _context.TbDepts.FirstOrDefaultAsync(d => d.Dept == "Admin");
        if (adminDept is null)
        {
            return Problem("Khong tim thay dept 'Admin' trong tb_dept.");
        }

        var admin = new TbUser
        {
            UserName = "admin",
            FullName = "Administrator",
            RoleId = adminDept.Id,
            PasswordHash = PasswordHasher.Hash("Aa@309195"),
            Active = "Y",
            CreateAt = DateTime.Now,
        };

        _context.TbUsers.Add(admin);
        await _context.SaveChangesAsync();

        return Content("Admin account created: admin");
    }
}

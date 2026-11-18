using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinnHomestay.Data;
using FinnHomestay.Models;

namespace FinnHomestay.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Auth/Register
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    // POST: /Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var emailNormalized = model.Email.Trim().ToLower();

            // Kiểm tra xem email đã tồn tại hay chưa
            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == emailNormalized);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Địa chỉ Email này đã được đăng ký. Vui lòng sử dụng email khác.");
                return View(model);
            }

            // Tạo người dùng mới với Role mặc định là Customer
            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = emailNormalized,
                Phone = model.Phone.Trim(),
                Password = HashPassword(model.Password),
                Role = "Customer" // Bắt buộc luôn là Customer khi đăng ký công khai
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction(nameof(Login));
        }

        return View(model);
    }

    // GET: /Auth/Login
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        await EnsureDefaultRolesAndUsersAsync();

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var emailNormalized = model.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized);

            // Kiểm tra tính khớp của mật khẩu (hỗ trợ cả mật khẩu hash SHA256 và plain-text để dễ test)
            bool isPasswordCorrect = user != null && (user.Password == model.Password || user.Password == HashPassword(model.Password));

            if (!isPasswordCorrect || user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Tạo các Claim cho phiên làm việc
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
            };

            // Đăng nhập Cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.FullName} ({user.Role}).";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        return View(model);
    }

    // POST: /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống thành công.";
        return RedirectToAction("Index", "Home");
    }

    // GET: /Auth/Logout (hỗ trợ trường hợp người dùng click link đăng xuất trực tiếp)
    [HttpGet]
    public async Task<IActionResult> LogoutGet()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống thành công.";
        return RedirectToAction("Index", "Home");
    }

    // GET: /Auth/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // Hàm băm mật khẩu SHA256 đơn giản, trực quan cho đồ án
    private static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    // Khởi tạo tài khoản mẫu (Admin, Staff, Customer) nếu chưa có trong DB để dễ dàng test đồ án
    private async Task EnsureDefaultRolesAndUsersAsync()
    {
        // 1. Kiểm tra tài khoản Admin
        var adminExists = await _context.Users.AnyAsync(u => u.Role == "Admin");
        if (!adminExists)
        {
            _context.Users.Add(new User
            {
                FullName = "Quản Trị Viên (Admin)",
                Email = "admin@finnhomestay.com",
                Phone = "0900000001",
                Password = "123456", // Có thể đăng nhập bằng 123456
                Role = "Admin"
            });
        }

        // 2. Kiểm tra tài khoản Staff
        var staffExists = await _context.Users.AnyAsync(u => u.Role == "Staff");
        if (!staffExists)
        {
            _context.Users.Add(new User
            {
                FullName = "Nhân Viên Lễ Tân (Staff)",
                Email = "staff@finnhomestay.com",
                Phone = "0900000002",
                Password = "123456",
                Role = "Staff"
            });
        }

        // 3. Kiểm tra tài khoản Customer
        var customerExists = await _context.Users.AnyAsync(u => u.Role == "Customer");
        if (!customerExists)
        {
            _context.Users.Add(new User
            {
                FullName = "Khách Hàng Mẫu",
                Email = "customer@finnhomestay.com",
                Phone = "0900000003",
                Password = "123456",
                Role = "Customer"
            });
        }

        await _context.SaveChangesAsync();
    }
}

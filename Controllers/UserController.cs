using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinnHomestay.Data;
using FinnHomestay.Models;

namespace FinnHomestay.Controllers;

[Authorize(Roles = "Admin")] // Bảo vệ toàn bộ Controller: Chỉ tài khoản Admin mới có quyền truy cập
public class UserController : Controller
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /User hoặc /User/Index?searchString=...
    public async Task<IActionResult> Index(string? searchString)
    {
        ViewData["CurrentFilter"] = searchString;

        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var search = searchString.Trim();
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                u.Email.Contains(search) ||
                u.Phone.Contains(search));
        }

        var users = await query.OrderByDescending(u => u.Id).ToListAsync();
        return View(users);
    }

    // GET: /User/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View(new UserCreateViewModel());
    }

    // POST: /User/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var emailNormalized = model.Email.Trim().ToLower();

            // Kiểm tra trùng lặp email
            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == emailNormalized);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Địa chỉ Email này đã được sử dụng bởi tài khoản khác.");
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = emailNormalized,
                Phone = model.Phone.Trim(),
                Password = HashPassword(model.Password),
                Role = model.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Tạo mới tài khoản \"{user.FullName}\" ({user.Role}) thành công!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: /User/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var viewModel = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role
        };

        return View(viewModel);
    }

    // POST: /User/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserEditViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var emailNormalized = model.Email.Trim().ToLower();

            // Kiểm tra email không được trùng với tài khoản khác
            var emailExists = await _context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == emailNormalized);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Địa chỉ Email này đã thuộc về một tài khoản khác.");
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            user.Email = emailNormalized;
            user.Phone = model.Phone.Trim();
            user.Role = model.Role;

            // Nếu người dùng nhập mật khẩu mới thì mã hóa và cập nhật, nếu để trống thì giữ nguyên mật khẩu cũ
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.Password = HashPassword(model.NewPassword);
            }

            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật tài khoản \"{user.FullName}\" thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Users.AnyAsync(u => u.Id == id);
                if (!exists)
                {
                    return NotFound();
                }
                throw;
            }
        }

        return View(model);
    }

    // POST: /User/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Không cho phép Admin tự xóa chính tài khoản đang đăng nhập
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(currentUserIdStr, out var currentUserId) && currentUserId == id)
        {
            TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản Admin đang đăng nhập của chính mình!";
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra xem User này đã có Booking hay chưa trước khi xóa
        var hasBookings = await _context.Bookings.AnyAsync(b => b.CustomerId == id);
        if (hasBookings)
        {
            TempData["ErrorMessage"] = $"Không thể xóa tài khoản \"{user.FullName}\" vì đã có đơn đặt phòng gắn với tài khoản này. Vui lòng xóa các đơn đặt phòng trước.";
            return RedirectToAction(nameof(Index));
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã xóa tài khoản \"{user.FullName}\" thành công!";

        return RedirectToAction(nameof(Index));
    }

    // Hàm mã hóa mật khẩu SHA-256 đồng bộ với AuthController
    private static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}

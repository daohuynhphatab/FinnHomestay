using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinnHomestay.Data;
using FinnHomestay.Models;

namespace FinnHomestay.Controllers;

public class RoomController : Controller
{
    private readonly AppDbContext _context;

    public RoomController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Room hoặc /Room/Index?searchString=...
    // Cho phép mọi người (khách chưa đăng nhập, khách hàng, nhân viên, admin) xem danh sách phòng
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? searchString)
    {
        ViewData["CurrentFilter"] = searchString;

        var query = _context.Rooms.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(r => r.Name.Contains(searchString));
        }

        var rooms = await query.OrderByDescending(r => r.Id).ToListAsync();
        return View(rooms);
    }

    // GET: /Room/Create
    // Chỉ Admin và Staff mới có quyền thêm phòng
    [Authorize(Roles = "Admin, Staff")]
    public IActionResult Create()
    {
        return View(new Room());
    }

    // POST: /Room/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin, Staff")]
    public async Task<IActionResult> Create(Room room)
    {
        if (ModelState.IsValid)
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm phòng mới thành công!";
            return RedirectToAction(nameof(Index));
        }

        return View(room);
    }

    // GET: /Room/Edit/5
    // Chỉ Admin và Staff mới có quyền sửa thông tin phòng
    [Authorize(Roles = "Admin, Staff")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var room = await _context.Rooms.FindAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        return View(room);
    }

    // POST: /Room/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin, Staff")]
    public async Task<IActionResult> Edit(int id, Room room)
    {
        if (id != room.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(room);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin phòng thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Rooms.AnyAsync(r => r.Id == room.Id);
                if (!exists)
                {
                    return NotFound();
                }
                throw;
            }
        }

        return View(room);
    }

    // POST: /Room/Delete/5
    // Chỉ Admin và Staff mới có quyền xóa phòng
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin, Staff")]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        // Kiểm tra xem phòng đã có đơn đặt phòng nào chưa trước khi xóa
        var hasBookings = await _context.Bookings.AnyAsync(b => b.RoomId == id);
        if (hasBookings)
        {
            TempData["ErrorMessage"] = $"Không thể xóa phòng \"{room.Name}\" vì đã có đơn đặt phòng gắn với phòng này.";
            return RedirectToAction(nameof(Index));
        }

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã xóa phòng \"{room.Name}\" thành công!";

        return RedirectToAction(nameof(Index));
    }
}

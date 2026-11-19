using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinnHomestay.Data;
using FinnHomestay.Models;
using FinnHomestay.Services;

namespace FinnHomestay.Controllers;

public class RoomController : Controller
{
    private readonly AppDbContext _context;
    private readonly IRoomStatusService _roomStatusService;

    public RoomController(AppDbContext context, IRoomStatusService roomStatusService)
    {
        _context = context;
        _roomStatusService = roomStatusService;
    }

    // GET: /Room hoặc /Room/Index?searchString=...
    // Cho phép mọi người xem danh sách phòng (Admin/Staff xem để quản lý)
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? searchString)
    {
        // Tự động kiểm tra và đồng bộ trạng thái phòng (chuyển về Còn trống nếu khách đã check-out)
        await _roomStatusService.SyncAllRoomsStatusAsync();

        ViewData["CurrentFilter"] = searchString;

        var query = _context.Rooms.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(r => r.Name.Contains(searchString));
        }

        var rooms = await query.OrderByDescending(r => r.Id).ToListAsync();
        return View(rooms);
    }

    // GET: /Room/Details/5
    // Xem chi tiết thông tin phòng
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        await _roomStatusService.SyncRoomStatusAsync(id.Value);

        var room = await _context.Rooms
            .Include(r => r.Bookings)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (room == null)
        {
            return NotFound();
        }

        return View(room);
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

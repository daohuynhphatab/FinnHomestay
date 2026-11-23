using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinnHomestay.Models;
using FinnHomestay.Data;
using FinnHomestay.Services;

namespace FinnHomestay.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly IRoomStatusService _roomStatusService;

    public HomeController(AppDbContext context, IRoomStatusService roomStatusService)
    {
        _context = context;
        _roomStatusService = roomStatusService;
    }

    // [GET] Hiển thị trang chủ, phòng nổi bật và các loại phòng
    public async Task<IActionResult> Index()
    {
        // Tự động kiểm tra và chuyển phòng đã check-out về Available
        await _roomStatusService.SyncAllRoomsStatusAsync();

        // Lấy tối đa 6 phòng để hiển thị trên Landing Page (ưu tiên Available)
        var rooms = await _context.Rooms
            .Take(6)
            .ToListAsync();
        ViewBag.RoomTypes = await _context.RoomTypes.OrderBy(t => t.Name).ToListAsync();
        return View(rooms);
    }

    // [GET] Hiển thị thông tin loại phòng và các phòng còn trống thuộc loại đó
    public async Task<IActionResult> RoomTypeDetails(int id)
    {
        var roomType = await _context.RoomTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (roomType == null) return NotFound();

        await _roomStatusService.SyncAllRoomsStatusAsync();
        ViewBag.AvailableRooms = await _context.Rooms
            .Where(r => r.RoomTypeId == id && r.Status == "Available")
            .OrderBy(r => r.Name)
            .ToListAsync();
        return View(roomType);
    }

    // [GET] Hiển thị chính sách quyền riêng tư
    public IActionResult Privacy()
    {
        return View();
    }

    // [GET] Hiển thị trang lỗi chung
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

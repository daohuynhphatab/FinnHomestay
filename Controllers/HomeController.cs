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

    public async Task<IActionResult> Index()
    {
        // Tự động kiểm tra và chuyển phòng đã check-out về Available
        await _roomStatusService.SyncAllRoomsStatusAsync();

        // Lấy tối đa 6 phòng để hiển thị trên Landing Page (ưu tiên Available)
        var rooms = await _context.Rooms
            .Take(6)
            .ToListAsync();
        return View(rooms);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

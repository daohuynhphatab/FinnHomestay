using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinnHomestay.Data;
using FinnHomestay.Models;

namespace FinnHomestay.Controllers;

[Authorize(Roles = "Admin, Staff")]
public class RoomTypeController : Controller
{
    private readonly AppDbContext _context;

    public RoomTypeController(AppDbContext context) => _context = context;

    // [GET] Hiển thị danh sách loại phòng
    public async Task<IActionResult> Index()
    {
        var roomTypes = await _context.RoomTypes.OrderBy(t => t.Name).ToListAsync();
        return View(roomTypes);
    }

    // [GET] Hiển thị biểu mẫu thêm loại phòng
    public IActionResult Create() => View(new RoomType());

    // [POST] Lưu loại phòng mới vào cơ sở dữ liệu
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomType roomType)
    {
        if (!ModelState.IsValid) return View(roomType);

        _context.RoomTypes.Add(roomType);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Thêm loại phòng thành công!";
        return RedirectToAction(nameof(Index));
    }

    // [GET] Tìm loại phòng và hiển thị biểu mẫu chỉnh sửa
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var roomType = await _context.RoomTypes.FindAsync(id);
        return roomType == null ? NotFound() : View(roomType);
    }

    // [POST] Cập nhật thông tin loại phòng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoomType roomType)
    {
        if (id != roomType.Id) return NotFound();
        if (!ModelState.IsValid) return View(roomType);

        _context.RoomTypes.Update(roomType);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Cập nhật loại phòng thành công!";
        return RedirectToAction(nameof(Index));
    }

    // [POST] Xóa loại phòng; các phòng liên quan sẽ giữ lại với loại phòng trống
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var roomType = await _context.RoomTypes.FindAsync(id);
        if (roomType == null) return NotFound();

        _context.RoomTypes.Remove(roomType);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã xóa loại phòng thành công!";
        return RedirectToAction(nameof(Index));
    }
}

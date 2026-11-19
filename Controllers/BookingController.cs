using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinnHomestay.Data;
using FinnHomestay.Models;
using FinnHomestay.Services;

namespace FinnHomestay.Controllers;

[Authorize] // Yêu cầu đăng nhập để truy cập tất cả chức năng Booking
public class BookingController : Controller
{
    private readonly AppDbContext _context;
    private readonly IRoomStatusService _roomStatusService;

    public BookingController(AppDbContext context, IRoomStatusService roomStatusService)
    {
        _context = context;
        _roomStatusService = roomStatusService;
    }

    // GET: /Booking hoặc /Booking/Index?statusFilter=...&searchString=...
    public async Task<IActionResult> Index(string? statusFilter, string? searchString)
    {
        // Tự động quét và cập nhật lại trạng thái phòng nếu khách đã check-out hoặc đơn bị hủy
        await _roomStatusService.SyncAllRoomsStatusAsync();

        ViewData["StatusFilter"] = statusFilter;
        ViewData["SearchString"] = searchString;

        var query = _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.Customer)
            .AsQueryable();

        // Phân quyền: Khách hàng (Customer) chỉ được xem đơn đặt của chính mình
        if (User.IsInRole("Customer"))
        {
            var currentUserId = GetCurrentUserId();
            query = query.Where(b => b.CustomerId == currentUserId);
        }

        // Lọc theo trạng thái đơn
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(b => b.Status == statusFilter);
        }

        // Tìm kiếm theo tên khách hoặc tên phòng
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var search = searchString.Trim();
            query = query.Where(b =>
                (b.Customer != null && b.Customer.FullName.Contains(search)) ||
                (b.Room != null && b.Room.Name.Contains(search)));
        }

        var bookings = await query.OrderByDescending(b => b.Id).ToListAsync();
        return View(bookings);
    }

    // GET: /Booking/Create
    public async Task<IActionResult> Create(int? roomId)
    {
        await EnsureSeedUsersAndRoomsAsync();

        bool isCustomer = User.IsInRole("Customer");
        ViewBag.IsCustomer = isCustomer;

        int currentUserId = GetCurrentUserId() ?? 0;

        if (isCustomer)
        {
            ViewBag.CustomerName = User.Identity?.Name ?? "Khách hàng";
        }

        await PopulateDropDownsAsync(selectedRoomId: roomId, selectedCustomerId: isCustomer ? currentUserId : null);

        var booking = new Booking
        {
            CheckInDate = DateTime.Today,
            CheckOutDate = DateTime.Today.AddDays(1),
            Status = "Pending",
            RoomId = roomId ?? 0,
            CustomerId = isCustomer ? currentUserId : 0
        };

        return View(booking);
    }

    // POST: /Booking/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Booking booking)
    {
        bool isCustomer = User.IsInRole("Customer");
        ViewBag.IsCustomer = isCustomer;

        // Nếu là Customer: tự động trích xuất Id từ User.Claims để gán vào CustomerId (ngăn chặn giả mạo ID từ client)
        if (isCustomer)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Challenge();
            }

            booking.CustomerId = currentUserId.Value;
            ViewBag.CustomerName = User.Identity?.Name;
            ModelState.Remove(nameof(Booking.CustomerId));
        }
        else
        {
            // Nếu là Admin/Staff: kiểm tra xem khách hàng được chọn có tồn tại trong DB không
            var userExists = await _context.Users.AnyAsync(u => u.Id == booking.CustomerId);
            if (!userExists)
            {
                ModelState.AddModelError("CustomerId", "Vui lòng chọn một khách hàng hợp lệ.");
            }
        }

        // Business Rule: Ngày check-out phải sau ngày check-in
        if (booking.CheckOutDate <= booking.CheckInDate)
        {
            ModelState.AddModelError("CheckOutDate", "Ngày trả phòng (Check-out) phải sau ngày nhận phòng (Check-in).");
        }

        // Mặc định Status = "Pending" nếu chưa có
        if (string.IsNullOrWhiteSpace(booking.Status))
        {
            booking.Status = "Pending";
        }

        // Tìm thông tin phòng để tự động tính tổng tiền nếu người dùng chưa nhập hoặc <= 0
        var room = await _context.Rooms.FindAsync(booking.RoomId);
        if (room == null)
        {
            ModelState.AddModelError("RoomId", "Vui lòng chọn một phòng hợp lệ.");
        }
        else
        {
            var nights = (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days;
            if (nights <= 0) nights = 1;

            if (booking.TotalPrice <= 0)
            {
                booking.TotalPrice = room.Price * nights;
            }
        }

        // Business Rule: Ngăn chặn đặt phòng trùng lịch khi đơn ở trạng thái Confirmed
        if (booking.Status == "Confirmed")
        {
            var isOverlap = await _context.Bookings.AnyAsync(b =>
                b.RoomId == booking.RoomId &&
                b.Status == "Confirmed" &&
                b.CheckInDate < booking.CheckOutDate &&
                b.CheckOutDate > booking.CheckInDate);

            if (isOverlap)
            {
                ModelState.AddModelError(string.Empty, "Phòng này đã có đơn đặt phòng được xác nhận (Confirmed) trong khoảng thời gian đã chọn.");
            }
        }

        if (ModelState.IsValid)
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Tự động chuyển Status của phòng sang "Occupied" (Đã đặt)
            await _roomStatusService.MarkRoomAsOccupiedAsync(booking.RoomId);

            TempData["SuccessMessage"] = "Tạo đơn đặt phòng mới thành công!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropDownsAsync(booking.RoomId, booking.CustomerId);
        return View(booking);
    }

    // GET: /Booking/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var booking = await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return NotFound();
        }

        // Phân quyền: Customer chỉ được truy cập đơn đặt của chính mình
        if (User.IsInRole("Customer"))
        {
            var currentUserId = GetCurrentUserId();
            if (booking.CustomerId != currentUserId)
            {
                return Forbid();
            }
        }

        ViewBag.IsCustomer = User.IsInRole("Customer");
        await PopulateDropDownsAsync(booking.RoomId, booking.CustomerId);
        return View(booking);
    }

    // POST: /Booking/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Booking booking)
    {
        if (id != booking.Id)
        {
            return NotFound();
        }

        var existingBooking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (existingBooking == null)
        {
            return NotFound();
        }

        // Phân quyền: Customer chỉ được sửa đơn của chính mình
        if (User.IsInRole("Customer"))
        {
            var currentUserId = GetCurrentUserId();
            if (existingBooking.CustomerId != currentUserId)
            {
                return Forbid();
            }
            // Giữ nguyên CustomerId và Status ban đầu nếu là Customer (không cho phép tự đổi trạng thái thành Confirmed)
            booking.CustomerId = existingBooking.CustomerId;
            booking.Status = existingBooking.Status;
            ModelState.Remove(nameof(Booking.CustomerId));
        }

        if (booking.CheckOutDate <= booking.CheckInDate)
        {
            ModelState.AddModelError("CheckOutDate", "Ngày trả phòng (Check-out) phải sau ngày nhận phòng (Check-in).");
        }

        var room = await _context.Rooms.FindAsync(booking.RoomId);
        if (room == null)
        {
            ModelState.AddModelError("RoomId", "Vui lòng chọn một phòng hợp lệ.");
        }

        // Ngăn chặn trùng lịch phòng nếu trạng thái là Confirmed
        if (booking.Status == "Confirmed")
        {
            var isOverlap = await _context.Bookings.AnyAsync(b =>
                b.Id != booking.Id &&
                b.RoomId == booking.RoomId &&
                b.Status == "Confirmed" &&
                b.CheckInDate < booking.CheckOutDate &&
                b.CheckOutDate > booking.CheckInDate);

            if (isOverlap)
            {
                ModelState.AddModelError(string.Empty, "Phòng này đã có đơn đặt phòng khác được xác nhận trong khoảng thời gian đã chọn.");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                int oldRoomId = existingBooking.RoomId;
                _context.Update(booking);
                await _context.SaveChangesAsync();

                // Cập nhật lại trạng thái phòng nếu đơn bị hủy hoặc đổi phòng/đổi ngày
                await _roomStatusService.SyncRoomStatusAsync(booking.RoomId);
                if (oldRoomId != booking.RoomId)
                {
                    await _roomStatusService.SyncRoomStatusAsync(oldRoomId);
                }

                TempData["SuccessMessage"] = $"Cập nhật đơn đặt phòng #{booking.Id} thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Bookings.AnyAsync(b => b.Id == booking.Id);
                if (!exists)
                {
                    return NotFound();
                }
                throw;
            }
        }

        ViewBag.IsCustomer = User.IsInRole("Customer");
        await PopulateDropDownsAsync(booking.RoomId, booking.CustomerId);
        return View(booking);
    }

    // POST: /Booking/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        // Phân quyền: Customer chỉ được xóa đơn của chính mình
        if (User.IsInRole("Customer"))
        {
            var currentUserId = GetCurrentUserId();
            if (booking.CustomerId != currentUserId)
            {
                return Forbid();
            }
        }

        var roomId = booking.RoomId;
        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();

        // Tự động giải phóng phòng về "Available" nếu không còn đơn đặt nào khác
        await _roomStatusService.SyncRoomStatusAsync(roomId);

        TempData["SuccessMessage"] = $"Đã xóa đơn đặt phòng #{id} thành công!";
        return RedirectToAction(nameof(Index));
    }

    // Lấy Id của user đang đăng nhập từ ClaimTypes.NameIdentifier
    private int? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    // Helper: Nạp dữ liệu vào SelectList
    private async Task PopulateDropDownsAsync(int? selectedRoomId = null, int? selectedCustomerId = null)
    {
        var rooms = await _context.Rooms
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Name, r.Price, r.TiktokLink })
            .ToListAsync();

        var users = await _context.Users
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, DisplayName = $"{u.FullName} ({u.Email})" })
            .ToListAsync();

        ViewBag.Rooms = new SelectList(rooms, "Id", "Name", selectedRoomId);
        ViewBag.Users = new SelectList(users, "Id", "DisplayName", selectedCustomerId);

        ViewBag.RoomPriceMap = rooms.ToDictionary(r => r.Id.ToString(), r => r.Price);
        ViewBag.RoomTiktokMap = rooms.ToDictionary(r => r.Id.ToString(), r => r.TiktokLink ?? string.Empty);

        if (selectedRoomId.HasValue && selectedRoomId.Value > 0)
        {
            var selected = rooms.FirstOrDefault(r => r.Id == selectedRoomId.Value);
            ViewBag.SelectedRoomTiktok = selected?.TiktokLink;
        }
    }

    // Helper: Khởi tạo tài khoản mẫu và phòng mẫu nếu DB trống
    private async Task EnsureSeedUsersAndRoomsAsync()
    {
        if (!await _context.Users.AnyAsync())
        {
            _context.Users.AddRange(
                new User
                {
                    FullName = "Quản Trị Viên (Admin)",
                    Email = "admin@finnhomestay.com",
                    Phone = "0900000001",
                    Password = "123456",
                    Role = "Admin"
                },
                new User
                {
                    FullName = "Nhân Viên Lễ Tân (Staff)",
                    Email = "staff@finnhomestay.com",
                    Phone = "0900000002",
                    Password = "123456",
                    Role = "Staff"
                },
                new User
                {
                    FullName = "Nguyễn Văn An",
                    Email = "customer@finnhomestay.com",
                    Phone = "0912345678",
                    Password = "123456",
                    Role = "Customer"
                }
            );
            await _context.SaveChangesAsync();
        }

        if (!await _context.Rooms.AnyAsync())
        {
            _context.Rooms.AddRange(
                new Room
                {
                    Name = "Phòng Deluxe 101 - Ban công",
                    Description = "Phòng view sân vườn thoáng mát, giường đôi 1m8, máy lạnh, vệ sinh khép kín.",
                    Price = 350000,
                    Status = "Available",
                    ImageUrl = "https://images.unsplash.com/photo-1590490360182-c33d57733427",
                    TiktokLink = "https://www.tiktok.com/@finnhomestay/video/7345678901234567890"
                },
                new Room
                {
                    Name = "Phòng Superior 201 - Cửa sổ lớn",
                    Description = "Phòng rộng rãi cho 2 người, trang bị bàn làm việc, tủ lạnh mini.",
                    Price = 450000,
                    Status = "Available",
                    ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b",
                    TiktokLink = "https://www.tiktok.com/@finnhomestay/video/7345678901234567891"
                }
            );
            await _context.SaveChangesAsync();
        }
    }
}

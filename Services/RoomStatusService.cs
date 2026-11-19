using Microsoft.EntityFrameworkCore;
using FinnHomestay.Data;

namespace FinnHomestay.Services;

public class RoomStatusService : IRoomStatusService
{
    private readonly AppDbContext _context;

    public RoomStatusService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Chuyển trạng thái phòng sang "Occupied" (Đã đặt) khi có đơn đặt mới thành công
    /// </summary>
    public async Task MarkRoomAsOccupiedAsync(int roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room != null && room.Status == "Available")
        {
            room.Status = "Occupied";
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Đồng bộ lại trạng thái của một phòng cụ thể.
    /// Nếu không còn đơn đặt nào đang có hiệu lực (đơn bị hủy hoặc khách đã check-out),
    /// phòng sẽ tự động chuyển về "Available" (Còn trống).
    /// </summary>
    public async Task SyncRoomStatusAsync(int roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null || room.Status == "Maintenance")
        {
            // Không can thiệp nếu phòng không tồn tại hoặc đang bảo trì
            return;
        }

        var today = DateTime.Today;

        // Đơn còn hiệu lực: không bị hủy hoặc đã hoàn tất/trả phòng và ngày check-out lớn hơn ngày hiện tại
        var hasActiveBooking = await _context.Bookings.AnyAsync(b =>
            b.RoomId == roomId &&
            b.Status != "Cancelled" &&
            b.Status != "Completed" &&
            b.CheckOutDate.Date > today);

        if (hasActiveBooking)
        {
            if (room.Status != "Occupied")
            {
                room.Status = "Occupied";
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            // Tất cả đơn đã hủy hoặc khách đã check-out -> trả về Còn trống
            if (room.Status == "Occupied")
            {
                room.Status = "Available";
                await _context.SaveChangesAsync();
            }
        }
    }

    /// <summary>
    /// Quét toàn bộ hệ thống để đồng bộ trạng thái phòng.
    /// Phòng có đơn đặt còn hạn sẽ là "Occupied", phòng không còn đơn nào hoặc khách đã check-out sẽ về "Available".
    /// </summary>
    public async Task SyncAllRoomsStatusAsync()
    {
        var today = DateTime.Today;
        var rooms = await _context.Rooms.ToListAsync();

        // Danh sách các phòng có đơn đặt chưa hủy / chưa trả phòng và ngày check-out lớn hơn hôm nay
        var activeRoomIds = await _context.Bookings
            .Where(b => b.Status != "Cancelled" && b.Status != "Completed" && b.CheckOutDate.Date > today)
            .Select(b => b.RoomId)
            .Distinct()
            .ToListAsync();

        bool hasChanges = false;
        foreach (var room in rooms)
        {
            if (room.Status == "Maintenance") continue;

            if (activeRoomIds.Contains(room.Id))
            {
                if (room.Status != "Occupied")
                {
                    room.Status = "Occupied";
                    hasChanges = true;
                }
            }
            else
            {
                if (room.Status == "Occupied")
                {
                    room.Status = "Available";
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }
    }
}

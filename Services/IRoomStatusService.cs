namespace FinnHomestay.Services;

public interface IRoomStatusService
{
    /// <summary>
    /// Chuyển trạng thái phòng sang "Occupied" (Đã đặt) khi có đơn đặt mới
    /// </summary>
    Task MarkRoomAsOccupiedAsync(int roomId);

    /// <summary>
    /// Đồng bộ lại trạng thái của một phòng cụ thể (kiểm tra đơn hủy hoặc đã check-out)
    /// </summary>
    Task SyncRoomStatusAsync(int roomId);

    /// <summary>
    /// Quét và đồng bộ trạng thái của toàn bộ phòng (tự động giải phóng phòng đã check-out sang "Available")
    /// </summary>
    Task SyncAllRoomsStatusAsync();
}

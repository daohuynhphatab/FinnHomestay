using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FinnHomestay.Models;

public class Booking
{
    public int Id { get; set; }

    [Display(Name = "Khách hàng")]
    [Required(ErrorMessage = "Vui lòng chọn khách hàng.")]
    public int CustomerId { get; set; } // Foreign Key trỏ đến User

    [Display(Name = "Phòng")]
    [Required(ErrorMessage = "Vui lòng chọn phòng.")]
    public int RoomId { get; set; }     // Foreign Key trỏ đến Room

    [Display(Name = "Ngày nhận phòng (Check-in)")]
    [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng.")]
    [DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; } = DateTime.Today;

    [Display(Name = "Ngày trả phòng (Check-out)")]
    [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng.")]
    [DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);

    [Display(Name = "Tổng tiền (VNĐ)")]
    [Required(ErrorMessage = "Vui lòng nhập hoặc tính tổng tiền.")]
    [Range(0, 1000000000, ErrorMessage = "Tổng tiền phải lớn hơn hoặc bằng 0.")]
    public decimal TotalPrice { get; set; }

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Pending"; // "Pending", "Confirmed", "Cancelled"

    // Navigation properties
    [ValidateNever]
    public User? Customer { get; set; }

    [ValidateNever]
    public Room? Room { get; set; }
}

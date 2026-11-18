using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FinnHomestay.Models;

public class Room
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên phòng.")]
    [Display(Name = "Tên phòng")]
    [StringLength(100, ErrorMessage = "Tên phòng không được vượt quá 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Mô tả")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập giá phòng.")]
    [Range(0, 100000000, ErrorMessage = "Giá phòng phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Giá phòng (VNĐ/đêm)")]
    public decimal Price { get; set; }

    [Display(Name = "Đường dẫn hình ảnh")]
    public string ImageUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Available"; // "Available", "Maintenance"

    // Navigation property: Một Room có thể có nhiều Booking
    [ValidateNever]
    public List<Booking> Bookings { get; set; } = new();
}

using System.ComponentModel.DataAnnotations;

namespace FinnHomestay.Models;

public class RoomType
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên loại phòng.")]
    [StringLength(100, ErrorMessage = "Tên loại phòng không được vượt quá 100 ký tự.")]
    [Display(Name = "Tên loại phòng")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Mô tả")]
    public string Description { get; set; } = string.Empty;

    [Range(0, 100000000, ErrorMessage = "Giá cơ bản phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Giá cơ bản (VNĐ/đêm)")]
    public decimal Price { get; set; }

    [Display(Name = "Đường dẫn hình ảnh")]
    public string ImageUrl { get; set; } = string.Empty;

    [Display(Name = "Link video TikTok")]
    public string? TiktokLink { get; set; }

    public ICollection<Room>? Rooms { get; set; }
}

namespace FinnHomestay.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer"; // "Admin", "Staff", "Customer"

    // Navigation property: Một User có thể có nhiều Booking
    public List<Booking> Bookings { get; set; } = new();
}

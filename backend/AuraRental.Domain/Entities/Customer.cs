namespace AuraRental.Domain.Entities;

public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? InstagramHandle { get; set; }
    public string? TiktokHandle { get; set; }
    public string? Address { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
}

namespace AuraRental.Domain.Enums;

public enum OrderStatus
{
    PendingDeposit,
    PendingVerification,
    Confirmed,
    Preparing,
    Renting,
    Inspecting,
    Completed,
    Cancelled
}

namespace AuraRental.Domain.Enums;

public enum PaymentType
{
    SlotDeposit,
    TargetDeposit,
    AdditionalCollection,
    Refund,
    CancellationRefund
}

public enum PaymentStatus
{
    Recorded,
    Confirmed,
    Voided
}

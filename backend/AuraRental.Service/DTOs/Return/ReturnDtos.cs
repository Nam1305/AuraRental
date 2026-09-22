namespace AuraRental.Service.DTOs.Return;

public sealed record ReturnQueueItemDto(
    Guid OrderId,
    string OrderNo,
    string CustomerName,
    DateTimeOffset? ReturnedAt,
    int ItemCount,
    int InspectionCompleted,
    int InspectionTotal,
    Guid? RefundId,
    int? RefundVersion,
    string? RefundStatus,
    decimal? RefundAmount,
    decimal? AdditionalCollection);

public sealed record InspectOrderItemRequest(
    string Condition,
    decimal ActualRentalFee,
    decimal ProcessingFee,
    string? DamageNote,
    IReadOnlyList<string> DamagePhotoPaths,
    string InventoryOutcome);

public sealed record RefundCalculationDto(
    decimal DepositConfirmed,
    decimal RentalFee,
    decimal ProcessingFee,
    decimal RefundAmount,
    decimal AdditionalCollection);

public sealed record InspectionDto(
    Guid OrderItemId,
    string Condition,
    decimal ProcessingFee,
    string InventoryOutcome,
    RefundCalculationDto Calculation);

public sealed record CreateRefundRequest(string? AdjustmentReason);

public sealed record RefundSnapshotItemDto(
    Guid OrderItemId,
    string ProductName,
    string Size,
    string AssetCode,
    string Condition,
    decimal ActualRentalFee,
    decimal ProcessingFee,
    string? DamageNote,
    IReadOnlyList<string> DamagePhotoPaths);

public sealed record RefundDto(
    Guid RefundId,
    Guid OrderId,
    int Version,
    string Status,
    decimal DepositAmount,
    decimal RentalFee,
    decimal ProcessingFee,
    decimal RefundAmount,
    decimal AdditionalCollection,
    IReadOnlyList<RefundSnapshotItemDto> Items,
    string? AdjustmentReason);

public sealed record ReturnForReviewRequest(string Reason);
public sealed record ApproveRefundRequest(int ExpectedVersion);

public sealed record ApproveRefundDto(
    Guid RefundId,
    int Version,
    string Status,
    decimal RefundAmount,
    decimal AdditionalCollection,
    Guid ApprovedBy,
    DateTimeOffset ApprovedAt,
    bool ReceiptReady);

public sealed record CreateRefundRevisionRequest(string Reason);

public sealed record SettleRefundRequest(
    string Method,
    string? TransactionRef,
    string? ProofPath,
    DateTimeOffset PaidAt);

public sealed record SettledInventoryDto(Guid InventoryItemId, string Status, DateTimeOffset? CleaningUntil);

public sealed record SettleRefundDto(
    string SettlementType,
    decimal Amount,
    Guid? PaymentId,
    string OrderStatus,
    DateTimeOffset SettledAt,
    IReadOnlyList<SettledInventoryDto> Inventory);

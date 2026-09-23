namespace AuraRental.Service.DTOs.Return;

public sealed record ReturnQueueItemDto(
    int OrderId,
    string OrderNo,
    string CustomerName,
    DateTimeOffset? ReturnedAt,
    int ItemCount,
    int InspectionCompleted,
    int InspectionTotal,
    int? RefundId,
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
    int OrderItemId,
    string Condition,
    decimal ProcessingFee,
    string InventoryOutcome,
    RefundCalculationDto Calculation);

public sealed record CreateRefundRequest(string? AdjustmentReason);

public sealed record RefundSnapshotItemDto(
    int OrderItemId,
    string ProductName,
    string Size,
    string AssetCode,
    string? PackageCode,
    string Condition,
    decimal ActualRentalFee,
    decimal ProcessingFee,
    string? DamageNote,
    IReadOnlyList<string> DamagePhotoPaths);

public sealed record RefundDto(
    int RefundId,
    int OrderId,
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
    int RefundId,
    int Version,
    string Status,
    decimal RefundAmount,
    decimal AdditionalCollection,
    int ApprovedBy,
    DateTimeOffset ApprovedAt,
    bool ReceiptReady);

public sealed record CreateRefundRevisionRequest(string Reason);

public sealed record SettleRefundRequest(
    string Method,
    string? TransactionRef,
    string? ProofPath,
    DateTimeOffset PaidAt);

public sealed record SettledInventoryDto(int InventoryItemId, string Status);

public sealed record SettleRefundDto(
    string SettlementType,
    decimal Amount,
    int? PaymentId,
    string OrderStatus,
    DateTimeOffset SettledAt,
    IReadOnlyList<SettledInventoryDto> Inventory);

public sealed record RefundReceiptItemDto(
    string ProductName,
    string Size,
    string AssetCode,
    string? PackageCode,
    decimal RentalFee);

public sealed record RefundReceiptDto(
    int RefundId,
    int OrderId,
    string OrderNo,
    string BranchName,
    string CustomerName,
    string Status,
    DateTimeOffset FinalizedAt,
    decimal DepositAmount,
    decimal RentalFee,
    decimal ProcessingFee,
    decimal RefundAmount,
    decimal AdditionalCollection,
    IReadOnlyList<RefundReceiptItemDto> Items);

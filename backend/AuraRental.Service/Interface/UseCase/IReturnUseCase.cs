using AuraRental.Service.DTOs.Return;

namespace AuraRental.Service.Interface.UseCase;

public interface IReturnUseCase
{
    Task<IReadOnlyList<ReturnQueueItemDto>> GetQueue(string? status, int limit, CancellationToken cancellationToken);
    Task<InspectionDto> InspectItem(
        int orderId,
        int orderItemId,
        InspectOrderItemRequest request,
        CancellationToken cancellationToken);
    Task<RefundDto> CreateOrUpdateRefund(int orderId, CreateRefundRequest request, CancellationToken cancellationToken);
    Task<RefundDto> SubmitRefund(int refundId, CancellationToken cancellationToken);
    Task<RefundDto> ReturnForReview(int refundId, ReturnForReviewRequest request, CancellationToken cancellationToken);
    Task<ApproveRefundDto> Approve(int refundId, ApproveRefundRequest request, CancellationToken cancellationToken);
    Task<RefundDto> CreateRevision(int refundId, CreateRefundRevisionRequest request, CancellationToken cancellationToken);
    Task<SettleRefundDto> Settle(int refundId, SettleRefundRequest request, CancellationToken cancellationToken);
    Task<RefundReceiptDto> GetReceipt(int refundId, CancellationToken cancellationToken);
}

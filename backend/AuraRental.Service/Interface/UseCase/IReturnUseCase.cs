using AuraRental.Service.DTOs.Return;

namespace AuraRental.Service.Interface.UseCase;

public interface IReturnUseCase
{
    Task<IReadOnlyList<ReturnQueueItemDto>> GetQueue(string? status, int limit, CancellationToken cancellationToken);
    Task<InspectionDto> InspectItem(
        Guid orderId,
        Guid orderItemId,
        InspectOrderItemRequest request,
        CancellationToken cancellationToken);
    Task<RefundDto> CreateOrUpdateRefund(Guid orderId, CreateRefundRequest request, CancellationToken cancellationToken);
    Task<RefundDto> SubmitRefund(Guid refundId, CancellationToken cancellationToken);
    Task<RefundDto> ReturnForReview(Guid refundId, ReturnForReviewRequest request, CancellationToken cancellationToken);
    Task<ApproveRefundDto> Approve(Guid refundId, ApproveRefundRequest request, CancellationToken cancellationToken);
    Task<RefundDto> CreateRevision(Guid refundId, CreateRefundRevisionRequest request, CancellationToken cancellationToken);
    Task<SettleRefundDto> Settle(Guid refundId, SettleRefundRequest request, CancellationToken cancellationToken);
}

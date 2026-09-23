using AuraRental.Service.DTOs.Reservation;

namespace AuraRental.Service.Interface.UseCase;

public interface IReservationUseCase
{
    Task<ReservationDto> Create(CreateReservationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReservationListItemDto>> Search(
        string? status,
        string? query,
        int limit,
        CancellationToken cancellationToken);
    Task<ReservationDto> Get(Guid reservationId, CancellationToken cancellationToken);
    Task<ReissueOtpDto> ReissueOtp(Guid reservationId, ReissueOtpRequest request, CancellationToken cancellationToken);
    Task<ReservationDto> UpdateRentalSelection(
        Guid reservationId,
        UpdateRentalSelectionRequest request,
        CancellationToken cancellationToken);
    Task<CancelReservationDto> Cancel(
        Guid reservationId,
        CancelReservationRequest request,
        CancellationToken cancellationToken);
    Task<RecordPaymentDto> RecordPayment(
        Guid reservationId,
        RecordPaymentRequest request,
        CancellationToken cancellationToken);
}

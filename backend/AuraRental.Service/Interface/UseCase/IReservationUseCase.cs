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
    Task<ReservationDto> Get(int reservationId, CancellationToken cancellationToken);
    Task<ReissueOtpDto> ReissueOtp(int reservationId, ReissueOtpRequest request, CancellationToken cancellationToken);
    Task<ReservationDto> UpdateRentalSelection(
        int reservationId,
        UpdateRentalSelectionRequest request,
        CancellationToken cancellationToken);
    Task<CancelReservationDto> Cancel(
        int reservationId,
        CancelReservationRequest request,
        CancellationToken cancellationToken);
    Task<RecordPaymentDto> RecordPayment(
        int reservationId,
        RecordPaymentRequest request,
        CancellationToken cancellationToken);
}

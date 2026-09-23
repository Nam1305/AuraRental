using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Reservation;
using AuraRental.Service.Interface.UseCase;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/reservations")]
[Authorize]
[RequireBranch]
public sealed class ReservationsController(IReservationUseCase reservationUseCase) : ApiControllerBase
{
    [HttpPost]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var reservation = await reservationUseCase.Create(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { reservationId = reservation.Id }, ResponseData(reservation));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReservationListItemDto>>>> Search(
        [FromQuery] string? status,
        [FromQuery] string? query,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        ResponseData(await reservationUseCase.Search(status, query, limit, cancellationToken));

    [HttpGet("{reservationId:guid}")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> Get(
        Guid reservationId,
        CancellationToken cancellationToken) =>
        ResponseData(await reservationUseCase.Get(reservationId, cancellationToken));

    [HttpPost("{reservationId:guid}/otp/reissue")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<ReissueOtpDto>>> ReissueOtp(
        Guid reservationId,
        [FromBody] ReissueOtpRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await reservationUseCase.ReissueOtp(reservationId, request, cancellationToken));

    [HttpPut("{reservationId:guid}/rental-selection")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> UpdateRentalSelection(
        Guid reservationId,
        [FromBody] UpdateRentalSelectionRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await reservationUseCase.UpdateRentalSelection(reservationId, request, cancellationToken));

    [HttpPost("{reservationId:guid}/cancel")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<CancelReservationDto>>> Cancel(
        Guid reservationId,
        [FromBody] CancelReservationRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await reservationUseCase.Cancel(reservationId, request, cancellationToken));

    [HttpPost("{reservationId:guid}/payments")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<RecordPaymentDto>>> RecordPayment(
        Guid reservationId,
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await reservationUseCase.RecordPayment(reservationId, request, cancellationToken));
}

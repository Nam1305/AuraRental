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

    [HttpGet("{reservationId:int}")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> Get(
        int reservationId,
        CancellationToken cancellationToken) =>
        ResponseData(await reservationUseCase.Get(reservationId, cancellationToken));

    [HttpPost("{reservationId:int}/otp/reissue")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<ReissueOtpDto>>> ReissueOtp(
        int reservationId,
        [FromBody] ReissueOtpRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await reservationUseCase.ReissueOtp(reservationId, request, cancellationToken));

    [HttpPut("{reservationId:int}/rental-selection")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> UpdateRentalSelection(
        int reservationId,
        [FromBody] UpdateRentalSelectionRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await reservationUseCase.UpdateRentalSelection(reservationId, request, cancellationToken));

    [HttpPost("{reservationId:int}/cancel")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<CancelReservationDto>>> Cancel(
        int reservationId,
        [FromBody] CancelReservationRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await reservationUseCase.Cancel(reservationId, request, cancellationToken));

    [HttpPost("{reservationId:int}/payments")]
    [Idempotent]
    public async Task<ActionResult<ApiResponse<RecordPaymentDto>>> RecordPayment(
        int reservationId,
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await reservationUseCase.RecordPayment(reservationId, request, cancellationToken));
}

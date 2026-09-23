using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Customer;
using AuraRental.Service.Interface.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/customers")]
[Authorize]
public sealed class CustomersController(ICustomerUseCase customerUseCase) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerListItemDto>>>> Search(
        [FromQuery] string? query,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default) =>
        ResponseData(await customerUseCase.Search(query, limit, cancellationToken));

    [HttpGet("{customerId:int}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Get(
        int customerId,
        CancellationToken cancellationToken) =>
        ResponseData(await customerUseCase.Get(customerId, cancellationToken));

    [HttpPost]
    [AuraRental.WebAPI.Security.Idempotent]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await customerUseCase.Create(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { customerId = customer.Id }, ResponseData(customer));
    }

    [HttpPatch("{customerId:int}")]
    [AuraRental.WebAPI.Security.Idempotent]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Update(
        int customerId,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken) =>
        ResponseData(await customerUseCase.Update(customerId, request, cancellationToken));

    [HttpGet("{customerId:int}/orders")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerOrderHistoryDto>>>> GetOrders(
        int customerId,
        [FromQuery] string? status,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        ResponseData(await customerUseCase.GetOrderHistory(customerId, status, limit, cancellationToken));
}

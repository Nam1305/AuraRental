using AuraRental.Service.DTOs.Quote;

namespace AuraRental.Service.Interface.UseCase;

public interface IQuoteUseCase
{
    Task<QuoteDto> Create(CreateQuoteRequest request, CancellationToken cancellationToken);
}

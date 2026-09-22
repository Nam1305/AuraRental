using AuraRental.Service.DTOs.PublicForm;

namespace AuraRental.Service.Interface.UseCase;

public interface IPublicRentalFormUseCase
{
    Task<PublicRentalFormDto> Get(string token, CancellationToken cancellationToken);
    Task<SubmitPublicRentalFormDto> Submit(
        string token,
        SubmitPublicRentalFormRequest request,
        CancellationToken cancellationToken);
}

using AuraRental.Service.Infrastructure.Persistence;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;
using AuraRental.Service.Service;
using AuraRental.Service.UseCase;
using AuraRental.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AuraRental.Service.Options;

namespace AuraRental.Service.Config;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuraRentalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<AuraRentalDbContext>(options => options.UseNpgsql(connectionString));
        services.AddDbContextFactory<AuraRentalDbContext>(options => options.UseNpgsql(connectionString), ServiceLifetime.Scoped);

        services.AddScoped<RequestContext>();
        services.AddScoped<IRequestContext>(provider => provider.GetRequiredService<RequestContext>());
        services.AddScoped<IRequestContextInitializer>(provider => provider.GetRequiredService<RequestContext>());
        services.AddSingleton<ISecureTokenService, SecureTokenService>();
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 600_000);
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.Configure<R2Options>(configuration.GetSection(R2Options.SectionName));
        services.AddScoped<IUploadService, R2UploadService>();
        services.AddScoped<IUnitOfWork, AppUnitOfWork>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IRentalRepository, RentalRepository>();
        services.AddScoped<IReturnRepository, ReturnRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IOperationsRepository, OperationsRepository>();

        services.AddScoped<IAuthUseCase, AuthUseCase>();
        services.AddScoped<IIdentityUseCase, IdentityUseCase>();
        services.AddScoped<ICatalogUseCase, CatalogUseCase>();
        services.AddScoped<IAvailabilityUseCase, AvailabilityUseCase>();
        services.AddScoped<ICustomerUseCase, CustomerUseCase>();
        services.AddScoped<IQuoteUseCase, QuoteUseCase>();
        services.AddScoped<IReservationUseCase, ReservationUseCase>();
        services.AddScoped<IPublicRentalFormUseCase, PublicRentalFormUseCase>();
        services.AddScoped<IOrderUseCase, OrderUseCase>();
        services.AddScoped<IReturnUseCase, ReturnUseCase>();
        services.AddScoped<IAdminUseCase, AdminUseCase>();
        services.AddScoped<IOperationsUseCase, OperationsUseCase>();

        return services;
    }
}

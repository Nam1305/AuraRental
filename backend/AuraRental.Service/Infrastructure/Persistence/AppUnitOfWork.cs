using AuraRental.Service.Interface.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class AppUnitOfWork(AuraRentalDbContext context) : IUnitOfWork
{
    public async Task<IAppTransaction> BeginTransaction(CancellationToken cancellationToken) =>
        new AppTransaction(await context.Database.BeginTransactionAsync(cancellationToken));

    public async Task SaveChanges(CancellationToken cancellationToken) =>
        _ = await context.SaveChangesAsync(cancellationToken);

    private sealed class AppTransaction(IDbContextTransaction transaction) : IAppTransaction
    {
        public Task Commit(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}

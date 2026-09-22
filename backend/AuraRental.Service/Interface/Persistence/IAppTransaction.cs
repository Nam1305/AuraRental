namespace AuraRental.Service.Interface.Persistence;

public interface IAppTransaction : IAsyncDisposable
{
    Task Commit(CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<IAppTransaction> BeginTransaction(CancellationToken cancellationToken);
    Task SaveChanges(CancellationToken cancellationToken);
}

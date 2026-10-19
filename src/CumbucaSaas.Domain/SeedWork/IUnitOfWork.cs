namespace CumbucaSaas.Domain.SeedWork;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Despacha os eventos de domínio pendentes e grava tudo numa única transação.</summary>
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}

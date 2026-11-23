using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.TenantAggregate;

public interface ITenantRepository : IRepository<Tenant>
{
    /// <summary>Com o horário de funcionamento.</summary>
    Task<Tenant?> ObterAsync(int id, CancellationToken cancellationToken = default);
}

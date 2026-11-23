using CumbucaSaas.Domain.Aggregates.TenantAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace CumbucaSaas.Infrastructure.Repositories;

internal sealed class TenantRepository(CumbucaDbContext context) : ITenantRepository
{
    public IUnitOfWork UnitOfWork => context;

    // Os horários são owned: o EF já os carrega junto com o tenant.
    public Task<Tenant?> ObterAsync(int id, CancellationToken cancellationToken = default) =>
        context.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
}

using CumbucaSaas.Domain.Aggregates.ConsumoAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace CumbucaSaas.Infrastructure.Repositories;

internal sealed class RegistroConsumoRepository(CumbucaDbContext context) : IRegistroConsumoRepository
{
    public IUnitOfWork UnitOfWork => context;

    public RegistroConsumo Adicionar(RegistroConsumo consumo) => context.RegistrosConsumo.Add(consumo).Entity;

    public void Remover(RegistroConsumo consumo) => context.RegistrosConsumo.Remove(consumo);

    public Task<RegistroConsumo?> ObterAsync(int id, CancellationToken cancellationToken = default) =>
        context.RegistrosConsumo.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> ExisteNoTurnoAsync(int calendarioRefeicaoId, CancellationToken cancellationToken = default) =>
        context.RegistrosConsumo.AnyAsync(r => r.CalendarioRefeicaoId == calendarioRefeicaoId, cancellationToken);
}

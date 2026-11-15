using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace CumbucaSaas.Infrastructure.Repositories;

internal sealed class CalendarioRefeicaoRepository(CumbucaDbContext context) : ICalendarioRefeicaoRepository
{
    public IUnitOfWork UnitOfWork => context;

    public CalendarioRefeicao Adicionar(CalendarioRefeicao calendario) =>
        context.CalendarioRefeicoes.Add(calendario).Entity;

    public void Remover(CalendarioRefeicao calendario) => context.CalendarioRefeicoes.Remove(calendario);

    // Cardápio, produção e caixa são owned: vêm juntos na mesma consulta, sem Include.
    public Task<CalendarioRefeicao?> ObterAsync(int id, CancellationToken cancellationToken = default) =>
        context.CalendarioRefeicoes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExisteAsync(
        DateOnly data,
        Turno turno,
        int? ignorarId = null,
        CancellationToken cancellationToken = default) =>
        context.CalendarioRefeicoes.AnyAsync(
            c => c.Data == data && c.Turno == turno && (ignorarId == null || c.Id != ignorarId),
            cancellationToken);
}

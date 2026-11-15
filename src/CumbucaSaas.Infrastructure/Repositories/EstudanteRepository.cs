using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace CumbucaSaas.Infrastructure.Repositories;

internal sealed class EstudanteRepository(CumbucaDbContext context) : IEstudanteRepository
{
    public IUnitOfWork UnitOfWork => context;

    public Estudante Adicionar(Estudante estudante) => context.Estudantes.Add(estudante).Entity;

    public Task<Estudante?> ObterAsync(int id, CancellationToken cancellationToken = default) =>
        context.Estudantes.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Estudante?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.Estudantes.FirstOrDefaultAsync(e => e.Email == email, cancellationToken);

    public Task<bool> ExisteMatriculaAsync(string matricula, CancellationToken cancellationToken = default) =>
        context.Estudantes.AnyAsync(e => e.Matricula == matricula, cancellationToken);

    public Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.Estudantes.AnyAsync(e => e.Email == email, cancellationToken);
}

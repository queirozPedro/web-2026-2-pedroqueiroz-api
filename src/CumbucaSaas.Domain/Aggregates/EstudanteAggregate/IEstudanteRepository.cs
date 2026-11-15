using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.EstudanteAggregate;

public interface IEstudanteRepository : IRepository<Estudante>
{
    Estudante Adicionar(Estudante estudante);

    Task<Estudante?> ObterAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Recebe o e-mail já normalizado (Estudante.NormalizarEmail).</summary>
    Task<Estudante?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExisteMatriculaAsync(string matricula, CancellationToken cancellationToken = default);

    /// <summary>Recebe o e-mail já normalizado (Estudante.NormalizarEmail).</summary>
    Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default);
}

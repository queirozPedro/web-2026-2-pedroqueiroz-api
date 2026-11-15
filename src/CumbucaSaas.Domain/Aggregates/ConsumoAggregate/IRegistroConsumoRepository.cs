using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.ConsumoAggregate;

public interface IRegistroConsumoRepository : IRepository<RegistroConsumo>
{
    RegistroConsumo Adicionar(RegistroConsumo consumo);

    void Remover(RegistroConsumo consumo);

    Task<RegistroConsumo?> ObterAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>O turno tem algum consumo registrado? (impede remover o turno do calendário)</summary>
    Task<bool> ExisteNoTurnoAsync(int calendarioRefeicaoId, CancellationToken cancellationToken = default);
}

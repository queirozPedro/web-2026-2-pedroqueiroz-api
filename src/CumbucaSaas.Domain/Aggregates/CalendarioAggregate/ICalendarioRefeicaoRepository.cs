using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.CalendarioAggregate;

public interface ICalendarioRefeicaoRepository : IRepository<CalendarioRefeicao>
{
    CalendarioRefeicao Adicionar(CalendarioRefeicao calendario);

    void Remover(CalendarioRefeicao calendario);

    /// <summary>Com cardápio, produção e caixa carregados.</summary>
    Task<CalendarioRefeicao?> ObterAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Já existe um turno nessa data? <paramref name="ignorarId"/> exclui o próprio registro numa edição.</summary>
    Task<bool> ExisteAsync(DateOnly data, Turno turno, int? ignorarId = null, CancellationToken cancellationToken = default);
}

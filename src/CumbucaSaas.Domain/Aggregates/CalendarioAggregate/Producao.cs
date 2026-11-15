using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.CalendarioAggregate;

/// <summary>
/// Quanto foi produzido em um turno; só existe dentro de um CalendarioRefeicao.
/// O servido não fica aqui: vem da contagem dos registros de consumo, e o
/// excedente é produzido - servido (calculado na leitura).
/// </summary>
public class Producao
{
    public const int ObservacoesTamanhoMaximo = 500;

    // Usado pelo EF Core.
    private Producao()
    {
    }

    internal Producao(int quantidadeProduzida, string? observacoes)
    {
        Atualizar(quantidadeProduzida, observacoes);
    }

    /// <summary>Número de refeições preparadas para o turno.</summary>
    public int QuantidadeProduzida { get; private set; }

    public string? Observacoes { get; private set; }

    internal void Atualizar(int quantidadeProduzida, string? observacoes)
    {
        if (quantidadeProduzida < 0)
        {
            throw new DomainException("A quantidade produzida não pode ser negativa.");
        }

        QuantidadeProduzida = quantidadeProduzida;
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
    }
}

using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.FeedbackAggregate;

/// <summary>
/// Assunto do feedback (Cardápio, Filas e espera...). Tabela de referência com os
/// valores iniciais gravados pela migration; o Feedback guarda só o Id.
/// </summary>
public class CategoriaFeedback : Entity
{
    public const int NomeTamanhoMaximo = 60;
    public const int DescricaoTamanhoMaximo = 200;

    // Usado pelo EF Core.
    private CategoriaFeedback()
    {
        Nome = null!;
        Descricao = null!;
    }

    public string Nome { get; private set; }

    public string Descricao { get; private set; }
}

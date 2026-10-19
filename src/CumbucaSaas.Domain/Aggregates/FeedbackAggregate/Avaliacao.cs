using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.FeedbackAggregate;

/// <summary>Nota (1 a 5) de uma categoria; só existe dentro de um Feedback.</summary>
public class Avaliacao : Entity
{
    public const int NotaMinima = 1;
    public const int NotaMaxima = 5;

    // Usado pelo EF Core.
    private Avaliacao()
    {
        Categoria = null!;
    }

    internal Avaliacao(CategoriaAvaliacao categoria, int nota)
    {
        if (nota is < NotaMinima or > NotaMaxima)
        {
            throw new DomainException($"A nota deve ficar entre {NotaMinima} e {NotaMaxima}.");
        }

        Categoria = categoria;
        Nota = nota;
    }

    public CategoriaAvaliacao Categoria { get; private set; }

    public int Nota { get; private set; }
}

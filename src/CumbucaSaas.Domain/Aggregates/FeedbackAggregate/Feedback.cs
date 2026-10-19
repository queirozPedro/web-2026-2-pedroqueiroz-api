using CumbucaSaas.Domain.Events;
using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.FeedbackAggregate;

/// <summary>Feedback de um estudante, com as avaliações por categoria.</summary>
public class Feedback : AggregateRoot
{
    private readonly List<Avaliacao> _avaliacoes = [];

    // Usado pelo EF Core.
    private Feedback()
    {
        Categoria = null!;
        Comentario = null!;
        Status = null!;
    }

    public Feedback(int estudanteId, DateOnly data, string categoria, string comentario)
    {
        EstudanteId = estudanteId;
        Data = data;
        Categoria = categoria;
        Comentario = comentario;
        Status = StatusFeedback.Novo;
    }

    public int EstudanteId { get; private set; }

    public DateOnly Data { get; private set; }

    public string Categoria { get; private set; }

    public string Comentario { get; private set; }

    public StatusFeedback Status { get; private set; }

    public IReadOnlyCollection<Avaliacao> Avaliacoes => _avaliacoes.AsReadOnly();

    public void Avaliar(CategoriaAvaliacao categoria, int nota)
    {
        if (_avaliacoes.Any(a => a.Categoria == categoria))
        {
            throw new DomainException($"A categoria '{categoria}' já foi avaliada neste feedback.");
        }

        _avaliacoes.Add(new Avaliacao(categoria, nota));
    }

    public void AlterarStatus(StatusFeedback novoStatus)
    {
        if (Status == novoStatus)
        {
            return;
        }

        var anterior = Status;
        Status = novoStatus;
        AddDomainEvent(new StatusFeedbackAlteradoDomainEvent(Id, anterior, novoStatus));
    }
}

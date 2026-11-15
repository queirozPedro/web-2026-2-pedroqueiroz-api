using CumbucaSaas.Domain.Events;
using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.FeedbackAggregate;

/// <summary>Feedback de um estudante, com uma avaliação (1 a 5) para cada categoria.</summary>
public class Feedback : AggregateRoot
{
    public const int ComentarioTamanhoMaximo = 2000;

    private readonly List<Avaliacao> _avaliacoes = [];

    // Usado pelo EF Core.
    private Feedback()
    {
        Comentario = null!;
        Status = null!;
    }

    private Feedback(int estudanteId, DateOnly data, int categoriaFeedbackId, string comentario)
    {
        EstudanteId = estudanteId;
        Data = data;
        CategoriaFeedbackId = categoriaFeedbackId;
        Comentario = comentario;
        Status = StatusFeedback.Novo;
    }

    public int EstudanteId { get; private set; }

    public DateOnly Data { get; private set; }

    /// <summary>Assunto do feedback (tabela CategoriasFeedback).</summary>
    public int CategoriaFeedbackId { get; private set; }

    public string Comentario { get; private set; }

    public StatusFeedback Status { get; private set; }

    public IReadOnlyCollection<Avaliacao> Avaliacoes => _avaliacoes.AsReadOnly();

    /// <summary>
    /// Estudante envia o feedback com nota para todas as categorias de avaliação: o
    /// dashboard calcula a média de cada categoria sobre todos os feedbacks.
    /// </summary>
    public static Feedback Enviar(
        int estudanteId,
        DateOnly data,
        int categoriaFeedbackId,
        string comentario,
        IReadOnlyDictionary<CategoriaAvaliacao, int> notas)
    {
        if (string.IsNullOrWhiteSpace(comentario))
        {
            throw new DomainException("Escreva o comentário do feedback.");
        }

        var faltando = Enumeration.GetAll<CategoriaAvaliacao>().Where(c => !notas.ContainsKey(c)).ToList();
        if (faltando.Count > 0)
        {
            throw new DomainException($"Faltam notas para: {string.Join(", ", faltando)}.");
        }

        var feedback = new Feedback(estudanteId, data, categoriaFeedbackId, comentario.Trim());
        foreach (var (categoria, nota) in notas)
        {
            feedback.Avaliar(categoria, nota);
        }

        return feedback;
    }

    private void Avaliar(CategoriaAvaliacao categoria, int nota)
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

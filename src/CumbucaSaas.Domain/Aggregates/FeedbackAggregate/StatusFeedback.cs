using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.FeedbackAggregate;

public sealed class StatusFeedback : Enumeration
{
    public static readonly StatusFeedback Novo = new(1, "novo");
    public static readonly StatusFeedback EmAnalise = new(2, "em_analise");
    public static readonly StatusFeedback Resolvido = new(3, "resolvido");

    private StatusFeedback(int id, string name) : base(id, name)
    {
    }
}

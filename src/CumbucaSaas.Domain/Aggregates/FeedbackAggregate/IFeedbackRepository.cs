using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.FeedbackAggregate;

public interface IFeedbackRepository : IRepository<Feedback>
{
    Feedback Adicionar(Feedback feedback);

    Task<Feedback?> ObterAsync(int id, CancellationToken cancellationToken = default);
}

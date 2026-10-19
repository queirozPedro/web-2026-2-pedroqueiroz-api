using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace CumbucaSaas.Infrastructure.Repositories;

internal sealed class FeedbackRepository(CumbucaDbContext context) : IFeedbackRepository
{
    public IUnitOfWork UnitOfWork => context;

    public Feedback Adicionar(Feedback feedback) => context.Feedbacks.Add(feedback).Entity;

    public Task<Feedback?> ObterAsync(int id, CancellationToken cancellationToken = default) =>
        context.Feedbacks.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
}

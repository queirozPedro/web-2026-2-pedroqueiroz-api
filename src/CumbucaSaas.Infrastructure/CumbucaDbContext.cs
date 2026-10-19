using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using CumbucaSaas.Domain.SeedWork;
using CumbucaSaas.Infrastructure.EntityConfigurations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CumbucaSaas.Infrastructure;

public class CumbucaDbContext(DbContextOptions<CumbucaDbContext> options, IMediator mediator)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Feedback> Feedbacks => Set<Feedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Registro explícito de cada mapeamento (sem ApplyConfigurationsFromAssembly).
        modelBuilder.ApplyConfiguration(new FeedbackEntityTypeConfiguration());
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // Os eventos saem antes do SaveChanges: os handlers podem alterar outros
        // agregados e tudo é gravado na mesma transação.
        await mediator.DispatchDomainEventsAsync(this, cancellationToken);
        await base.SaveChangesAsync(cancellationToken);
        return true;
    }
}

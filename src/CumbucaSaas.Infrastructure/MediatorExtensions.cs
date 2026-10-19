using CumbucaSaas.Domain.SeedWork;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CumbucaSaas.Infrastructure;

internal static class MediatorExtensions
{
    public static async Task DispatchDomainEventsAsync(
        this IMediator mediator,
        DbContext context,
        CancellationToken cancellationToken = default)
    {
        var entidades = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var eventos = entidades.SelectMany(e => e.DomainEvents).ToList();
        entidades.ForEach(e => e.ClearDomainEvents());

        foreach (var evento in eventos)
        {
            await mediator.Publish(evento, cancellationToken);
        }
    }
}

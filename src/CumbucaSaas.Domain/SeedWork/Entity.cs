using MediatR;

namespace CumbucaSaas.Domain.SeedWork;

/// <summary>Entidade base: identidade por Id e lista de eventos de domínio pendentes.</summary>
public abstract class Entity
{
    private readonly List<INotification> _domainEvents = [];

    public int Id { get; protected set; }

    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    public bool IsTransient() => Id == default;

    public void AddDomainEvent(INotification evento) => _domainEvents.Add(evento);

    public void RemoveDomainEvent(INotification evento) => _domainEvents.Remove(evento);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity outra || GetType() != outra.GetType())
        {
            return false;
        }

        if (ReferenceEquals(this, outra))
        {
            return true;
        }

        // Duas entidades ainda não persistidas nunca são iguais.
        return !IsTransient() && !outra.IsTransient() && Id == outra.Id;
    }

    public override int GetHashCode() =>
        IsTransient() ? base.GetHashCode() : HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? esquerda, Entity? direita) => Equals(esquerda, direita);

    public static bool operator !=(Entity? esquerda, Entity? direita) => !Equals(esquerda, direita);
}

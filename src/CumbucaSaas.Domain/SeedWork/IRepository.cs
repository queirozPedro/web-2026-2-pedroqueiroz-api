namespace CumbucaSaas.Domain.SeedWork;

/// <summary>Repositórios existem só para raízes de agregado.</summary>
public interface IRepository<T> where T : AggregateRoot
{
    IUnitOfWork UnitOfWork { get; }
}

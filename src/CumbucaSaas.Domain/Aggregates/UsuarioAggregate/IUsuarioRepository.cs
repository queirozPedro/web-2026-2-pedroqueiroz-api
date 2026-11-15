using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.UsuarioAggregate;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Usuario Adicionar(Usuario usuario);

    Task<Usuario?> ObterPorIdExternoAsync(string idExterno, CancellationToken cancellationToken = default);
}

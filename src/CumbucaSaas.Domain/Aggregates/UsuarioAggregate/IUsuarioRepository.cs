using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.UsuarioAggregate;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Usuario Adicionar(Usuario usuario);

    /// <summary>Procura em todos os tenants: o login acontece antes de saber o tenant.</summary>
    Task<Usuario?> ObterPorIdExternoAsync(string idExterno, CancellationToken cancellationToken = default);

    /// <summary>O e-mail é único no sistema inteiro (todos os tenants e formas de login).</summary>
    Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default);
}

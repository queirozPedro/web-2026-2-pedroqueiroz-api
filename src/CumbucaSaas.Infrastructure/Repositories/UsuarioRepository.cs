using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace CumbucaSaas.Infrastructure.Repositories;

internal sealed class UsuarioRepository(CumbucaDbContext context) : IUsuarioRepository
{
    public IUnitOfWork UnitOfWork => context;

    public Usuario Adicionar(Usuario usuario) => context.Usuarios.Add(usuario).Entity;

    // Sem o filtro de tenant: é esta consulta que descobre o tenant do usuário.
    public Task<Usuario?> ObterPorIdExternoAsync(string idExterno, CancellationToken cancellationToken = default) =>
        context.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.IdExterno == idExterno, cancellationToken);

    public Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizado = Usuario.NormalizarEmail(email);
        return context.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == normalizado, cancellationToken);
    }
}

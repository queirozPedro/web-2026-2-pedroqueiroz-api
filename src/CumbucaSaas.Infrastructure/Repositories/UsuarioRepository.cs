using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace CumbucaSaas.Infrastructure.Repositories;

internal sealed class UsuarioRepository(CumbucaDbContext context) : IUsuarioRepository
{
    public IUnitOfWork UnitOfWork => context;

    public Usuario Adicionar(Usuario usuario) => context.Usuarios.Add(usuario).Entity;

    public Task<Usuario?> ObterPorIdExternoAsync(string idExterno, CancellationToken cancellationToken = default) =>
        context.Usuarios.FirstOrDefaultAsync(u => u.IdExterno == idExterno, cancellationToken);
}

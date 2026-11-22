using CumbucaSaas.API.PerfilUsuario;
using CumbucaSaas.Infrastructure;

namespace CumbucaSaas.API.Queries;

/// <summary>
/// Perfil do usuário logado (GET /profile). FotoUrl é um link temporário (1 hora) para
/// a foto no S3, ou nulo sem foto. AtualizadoEm (UTC) é nulo enquanto o perfil não foi salvo.
/// </summary>
public sealed record PerfilViewModel(
    string Email,
    string? Nome,
    DateOnly? DataNascimento,
    string? Perfil,
    string? FotoUrl,
    DateTime? AtualizadoEm);

public interface IPerfilQueries
{
    Task<PerfilViewModel?> ObterAsync(string idExterno, int usuarioId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lê o item do DynamoDB; quem ainda não salvou o perfil vê os dados do cadastro
/// (Postgres), sem foto.
/// </summary>
public sealed class PerfilQueries(
    IPerfisUsuario perfis,
    IFotosPerfil fotos,
    IUsuarioQueries usuarios,
    ITenantAtual tenantAtual)
    : IPerfilQueries
{
    public async Task<PerfilViewModel?> ObterAsync(
        string idExterno,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var salvo = await perfis.ObterAsync(idExterno, tenantAtual.Obrigatorio(), cancellationToken);
        if (salvo is not null)
        {
            return new PerfilViewModel(
                salvo.Email,
                salvo.Nome,
                salvo.DataNascimento,
                salvo.Perfil,
                salvo.FotoChave is null ? null : await fotos.LinkTemporarioAsync(salvo.FotoChave),
                DateTime.SpecifyKind(salvo.AtualizadoEmUtc, DateTimeKind.Utc));
        }

        var cadastro = await usuarios.ObterAsync(usuarioId, cancellationToken);
        return cadastro is null
            ? null
            : new PerfilViewModel(cadastro.Email, cadastro.Nome, cadastro.DataNascimento, cadastro.Perfil, null, null);
    }
}

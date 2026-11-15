using CumbucaSaas.Infrastructure;

namespace CumbucaSaas.API.Autenticacao;

/// <summary>Lê o Id do usuário que a UsuarioClaimsTransformation colocou na requisição (para a auditoria).</summary>
internal sealed class UsuarioAtual(IHttpContextAccessor acessor) : IUsuarioAtual
{
    public int? UsuarioId =>
        int.TryParse(acessor.HttpContext?.User.FindFirst(Perfis.ClaimUsuarioId)?.Value, out var id) ? id : null;
}

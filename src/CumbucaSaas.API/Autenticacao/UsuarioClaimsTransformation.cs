using System.Security.Claims;
using CumbucaSaas.API.Commands;
using MediatR;
using Microsoft.AspNetCore.Authentication;

namespace CumbucaSaas.API.Autenticacao;

/// <summary>
/// Depois que o JWT do Cognito é validado, registra o acesso (RegistrarAcessoCommand)
/// e acrescenta ao usuário o perfil (role) e os Ids do banco, usados pelas policies e
/// pelos controllers. Roda uma vez por requisição.
/// </summary>
internal sealed class UsuarioClaimsTransformation(IMediator mediator) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true
            || principal.Identities.Any(i => i.AuthenticationType == Perfis.TipoIdentidade))
        {
            return principal;
        }

        var identidade = new ClaimsIdentity(Perfis.TipoIdentidade, nameType: "email", roleType: ClaimTypes.Role);

        // MapInboundClaims = false: as claims mantêm os nomes do token ("sub", "email", "name").
        var sub = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value;

        if (!string.IsNullOrEmpty(sub) && !string.IsNullOrEmpty(email))
        {
            var resultado = await mediator.Send(new RegistrarAcessoCommand(sub, email, principal.FindFirst("name")?.Value));
            var acesso = resultado.ValueOrDefault;

            if (acesso?.UsuarioId is int usuarioId)
            {
                identidade.AddClaim(new Claim(Perfis.ClaimUsuarioId, usuarioId.ToString()));
            }

            if (acesso?.Perfil is { } perfil)
            {
                identidade.AddClaim(new Claim(ClaimTypes.Role, perfil));
            }

            if (acesso?.EstudanteId is int estudanteId)
            {
                identidade.AddClaim(new Claim(Perfis.ClaimEstudanteId, estudanteId.ToString()));
            }
        }

        var transformado = principal.Clone();
        transformado.AddIdentity(identidade);
        return transformado;
    }
}

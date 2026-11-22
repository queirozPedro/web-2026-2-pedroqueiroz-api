using System.Security.Claims;
using System.Text.Json;
using CumbucaSaas.API.Commands;
using MediatR;
using Microsoft.AspNetCore.Authentication;

namespace CumbucaSaas.API.Autenticacao;

/// <summary>
/// Depois que o JWT do Cognito é validado, registra o acesso (RegistrarAcessoCommand),
/// define o tenant da requisição e acrescenta ao usuário o perfil (role), o status do
/// cadastro e os Ids do banco, usados pelas policies e pelos controllers.
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

        // MapInboundClaims = false: as claims mantêm os nomes do token ("sub", "email", "identities").
        var sub = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value;

        if (!string.IsNullOrEmpty(sub) && !string.IsNullOrEmpty(email))
        {
            var resultado = await mediator.Send(new RegistrarAcessoCommand(sub, email, LoginGoogle(principal)));
            var acesso = resultado.ValueOrDefault;

            if (acesso?.UsuarioId is int usuarioId)
            {
                identidade.AddClaim(new Claim(Perfis.ClaimUsuarioId, usuarioId.ToString()));
            }

            if (acesso?.Perfil is { } perfil)
            {
                identidade.AddClaim(new Claim(ClaimTypes.Role, perfil));
            }

            identidade.AddClaim(new Claim(
                Perfis.ClaimCadastroCompleto,
                acesso?.CadastroCompleto == true ? "true" : "false"));

            if (acesso?.EstudanteId is int estudanteId)
            {
                identidade.AddClaim(new Claim(Perfis.ClaimEstudanteId, estudanteId.ToString()));
            }

            if (acesso?.MotivoSemAcesso is { } motivo)
            {
                identidade.AddClaim(new Claim(Perfis.ClaimMotivoSemAcesso, motivo));
            }
        }

        var transformado = principal.Clone();
        transformado.AddIdentity(identidade);
        return transformado;
    }

    /// <summary>
    /// Quem entra por um provedor externo (Google) traz a claim "identities" com o
    /// providerName; o usuário criado no próprio User Pool (e-mail e senha) não traz.
    /// O "cognito:username" dos federados começa com "google_" (reserva, se faltar a claim).
    /// </summary>
    internal static bool LoginGoogle(ClaimsPrincipal principal) =>
        principal.FindAll("identities").Any(c => ProvedorGoogle(c.Value))
        || (principal.FindFirst("cognito:username")?.Value.StartsWith("google_", StringComparison.OrdinalIgnoreCase)
            ?? false);

    /// <summary>A claim chega como um objeto JSON por identidade (ou como o array inteiro).</summary>
    private static bool ProvedorGoogle(string valor)
    {
        try
        {
            using var json = JsonDocument.Parse(valor);
            var identidades = json.RootElement.ValueKind == JsonValueKind.Array
                ? json.RootElement.EnumerateArray().ToList()
                : new List<JsonElement> { json.RootElement };

            return identidades.Any(i =>
                i.ValueKind == JsonValueKind.Object
                && i.TryGetProperty("providerName", out var provedor)
                && string.Equals(provedor.GetString(), "Google", StringComparison.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

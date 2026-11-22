namespace CumbucaSaas.Infrastructure;

/// <summary>
/// Tenant do usuário logado na requisição atual. A implementação fica na API e é
/// definida no início da requisição (UsuarioClaimsTransformation). Nulo fora de uma
/// requisição autenticada: nesse caso as consultas filtradas não devolvem nada.
/// </summary>
public interface ITenantAtual
{
    int? TenantId { get; }
}

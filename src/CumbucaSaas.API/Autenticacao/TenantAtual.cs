using CumbucaSaas.Infrastructure;

namespace CumbucaSaas.API.Autenticacao;

/// <summary>
/// Tenant da requisição (scoped). Definido pelo RegistrarAcessoCommand assim que o
/// usuário logado é encontrado; a partir daí o EF e as queries filtram por ele.
/// </summary>
public sealed class TenantAtual : ITenantAtual
{
    public int? TenantId { get; private set; }

    public void Definir(int tenantId) => TenantId = tenantId;
}

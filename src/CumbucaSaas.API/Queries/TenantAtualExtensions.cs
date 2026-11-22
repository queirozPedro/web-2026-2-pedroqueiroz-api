using CumbucaSaas.Infrastructure;

namespace CumbucaSaas.API.Queries;

internal static class TenantAtualExtensions
{
    /// <summary>
    /// Tenant para os filtros das queries (parâmetro @tenantId). As rotas que leem dados
    /// exigem usuário com perfil, então o tenant sempre existe; se faltar, é erro de
    /// configuração e a consulta não roda sem filtro.
    /// </summary>
    public static int Obrigatorio(this ITenantAtual tenantAtual) =>
        tenantAtual.TenantId
        ?? throw new InvalidOperationException("Consulta sem tenant: a requisição não tem usuário logado.");
}

using CumbucaSaas.API.Autenticacao;
using CumbucaSaas.API.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

/// <summary>Histórico de alterações (auditoria). Só leitura; as linhas são gravadas automaticamente.</summary>
[ApiController]
[Route("api/auditoria")]
[Authorize(Policy = Perfis.Funcionario)]
public sealed class AuditoriaController(IAuditoriaQueries queries) : ControllerBase
{
    /// <summary>Mais recentes primeiro. Ex.: ?tabela=RegistrosConsumo&amp;inicio=2026-11-01&amp;limite=100.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<HistoricoAlteracaoViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HistoricoAlteracaoViewModel>>> Listar(
        [FromQuery] string? tabela,
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken cancellationToken,
        [FromQuery] int limite = 200) =>
        Ok(await queries.ListarAsync(tabela, inicio, fim, limite, cancellationToken));
}

using CumbucaSaas.API.Queries;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardQueries queries) : ControllerBase
{
    /// <summary>Dados brutos do painel; os indicadores são calculados no front.</summary>
    [HttpGet]
    [ProducesResponseType<DashboardViewModel>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardViewModel>> Obter(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken cancellationToken) =>
        Ok(await queries.ObterAsync(inicio, fim, cancellationToken));
}

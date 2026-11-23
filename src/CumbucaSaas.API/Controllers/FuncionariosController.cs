using CumbucaSaas.API.Autenticacao;
using CumbucaSaas.API.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

/// <summary>Funcionários do restaurante (contas de e-mail e senha), só para consulta.</summary>
[ApiController]
[Authorize(Policy = Perfis.Funcionario)]
[Route("api/funcionarios")]
public sealed class FuncionariosController(IFuncionarioQueries queries) : ControllerBase
{
    /// <summary>Em ordem alfabética.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<FuncionarioViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FuncionarioViewModel>>> Listar(CancellationToken cancellationToken) =>
        Ok(await queries.ListarAsync(cancellationToken));
}

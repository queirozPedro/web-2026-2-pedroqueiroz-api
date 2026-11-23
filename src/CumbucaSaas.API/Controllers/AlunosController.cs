using CumbucaSaas.API.Autenticacao;
using CumbucaSaas.API.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

/// <summary>
/// Lista de alunos da Administração (só consulta): cadastros do RU e contas de estudante
/// criadas no login com Google, inclusive as que ainda não têm cadastro no RU.
/// </summary>
[ApiController]
[Authorize(Policy = Perfis.Funcionario)]
[Route("api/alunos")]
public sealed class AlunosController(IAlunoQueries queries) : ControllerBase
{
    /// <summary>Em ordem alfabética.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AlunoViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlunoViewModel>>> Listar(CancellationToken cancellationToken) =>
        Ok(await queries.ListarAsync(cancellationToken));
}

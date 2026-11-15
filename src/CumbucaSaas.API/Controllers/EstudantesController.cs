using CumbucaSaas.API.Commands;
using CumbucaSaas.API.Queries;
using MediatR;
using CumbucaSaas.API.Autenticacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

/// <summary>Estudantes que consomem no RU com preço subsidiado e enviam feedbacks.</summary>
[ApiController]
[Authorize(Policy = Perfis.Funcionario)]
[Route("api/estudantes")]
public sealed class EstudantesController(IMediator mediator, IEstudanteQueries queries) : ControllerBase
{
    /// <summary>Lista em ordem alfabética; ?vinculo=ativo (ou inativo) filtra.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EstudanteViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EstudanteViewModel>>> Listar(
        [FromQuery] string? vinculo,
        CancellationToken cancellationToken) =>
        Ok(await queries.ListarAsync(vinculo, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<EstudanteViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstudanteViewModel>> Obter(int id, CancellationToken cancellationToken)
    {
        var estudante = await queries.ObterPorIdAsync(id, cancellationToken);
        return estudante is null ? NotFound() : Ok(estudante);
    }

    /// <summary>Identificação no balcão pela matrícula (digitada ou lida do QR Code).</summary>
    [HttpGet("matricula/{matricula}")]
    [ProducesResponseType<EstudanteViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstudanteViewModel>> ObterPorMatricula(
        string matricula,
        CancellationToken cancellationToken)
    {
        var estudante = await queries.ObterPorMatriculaAsync(matricula, cancellationToken);
        return estudante is null ? NotFound() : Ok(estudante);
    }

    /// <param name="Email">Institucional, terminando em @alunos.ufersa.edu.br.</param>
    /// <param name="TipoSubsidio">"padrao" (paga o preço subsidiado) ou "isento".</param>
    public sealed record CadastrarEstudanteRequest(
        string Nome,
        string Email,
        string Matricula,
        string Curso,
        string TipoSubsidio);

    [HttpPost]
    [ProducesResponseType<CriadoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cadastrar(CadastrarEstudanteRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new CadastrarEstudanteCommand(
                request.Nome, request.Email, request.Matricula, request.Curso, request.TipoSubsidio),
            cancellationToken);

        return this.ParaResposta(resultado, () =>
            CreatedAtAction(nameof(Obter), new { id = resultado.Value }, new CriadoResponse(resultado.Value)));
    }

    /// <param name="Vinculo">"ativo" ou "inativo".</param>
    public sealed record AlterarVinculoRequest(string Vinculo);

    [HttpPatch("{id:int}/vinculo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarVinculo(
        int id,
        AlterarVinculoRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new AlterarVinculoEstudanteCommand(id, request.Vinculo), cancellationToken);
        return this.ParaResposta(resultado, NoContent);
    }
}

using CumbucaSaas.API.Autenticacao;
using CumbucaSaas.API.Commands;
using CumbucaSaas.API.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

/// <summary>
/// Feedbacks dos estudantes. Funcionário consulta e muda o status; só o estudante
/// envia (o funcionário não avalia a comida) e vê o próprio histórico.
/// </summary>
[ApiController]
[Authorize]
[Route("api/feedbacks")]
public sealed class FeedbacksController(IMediator mediator, IFeedbackQueries queries) : ControllerBase
{
    /// <summary>Do mais recente para o mais antigo; filtros opcionais ?status=novo&amp;inicio=&amp;fim=.</summary>
    [Authorize(Policy = Perfis.Funcionario)]
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<FeedbackViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FeedbackViewModel>>> Listar(
        [FromQuery] string? status,
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken cancellationToken) =>
        Ok(await queries.ListarAsync(status, inicio, fim, estudanteId: null, cancellationToken));

    [Authorize(Policy = Perfis.Funcionario)]
    [HttpGet("{id:int}")]
    [ProducesResponseType<FeedbackViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedbackViewModel>> Obter(int id, CancellationToken cancellationToken)
    {
        var feedback = await queries.ObterPorIdAsync(id, cancellationToken);
        return feedback is null ? NotFound() : Ok(feedback);
    }

    /// <summary>Histórico de feedbacks enviados por um estudante (visão do funcionário).</summary>
    [Authorize(Policy = Perfis.Funcionario)]
    [HttpGet("/api/estudantes/{estudanteId:int}/feedbacks")]
    [ProducesResponseType<IReadOnlyList<FeedbackViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FeedbackViewModel>>> ListarDoEstudante(
        int estudanteId,
        CancellationToken cancellationToken) =>
        Ok(await queries.ListarAsync(status: null, inicio: null, fim: null, estudanteId, cancellationToken));

    /// <summary>Histórico do estudante logado. Sem cadastro no RU, a lista vem vazia.</summary>
    [Authorize(Policy = Perfis.Estudante)]
    [HttpGet("meus")]
    [ProducesResponseType<IReadOnlyList<FeedbackViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FeedbackViewModel>>> ListarMeus(CancellationToken cancellationToken)
    {
        var estudanteId = EstudanteLogado();
        return estudanteId is null
            ? Ok(Array.Empty<FeedbackViewModel>())
            : Ok(await queries.ListarAsync(status: null, inicio: null, fim: null, estudanteId, cancellationToken));
    }

    /// <summary>Assuntos possíveis de um feedback (Id usado no envio).</summary>
    [HttpGet("/api/categorias-feedback")]
    [ProducesResponseType<IReadOnlyList<CategoriaFeedbackViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoriaFeedbackViewModel>>> ListarCategorias(
        CancellationToken cancellationToken) =>
        Ok(await queries.ListarCategoriasAsync(cancellationToken));

    /// <param name="CategoriaFeedbackId">Id de GET /api/categorias-feedback.</param>
    /// <param name="Notas">
    /// Uma nota de 1 a 5 para cada categoria: qualidade, sabor, temperatura, variedade,
    /// atendimento, limpeza e estrutura.
    /// </param>
    public sealed record EnviarFeedbackRequest(
        int CategoriaFeedbackId,
        string Comentario,
        IReadOnlyDictionary<string, int> Notas);

    /// <summary>
    /// O estudante logado (com vínculo ativo) envia um feedback; começa com status "novo".
    /// 409 se o e-mail dele não estiver cadastrado como estudante no RU.
    /// </summary>
    [Authorize(Policy = Perfis.Estudante)]
    [HttpPost]
    [ProducesResponseType<CriadoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enviar(EnviarFeedbackRequest request, CancellationToken cancellationToken)
    {
        var estudanteId = EstudanteLogado();
        if (estudanteId is null)
        {
            return Problem(
                detail: "Seu e-mail não está cadastrado como estudante no RU. Procure a equipe do restaurante.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflito");
        }

        var resultado = await mediator.Send(
            new EnviarFeedbackCommand(estudanteId.Value, request.CategoriaFeedbackId, request.Comentario, request.Notas),
            cancellationToken);

        return this.ParaResposta(resultado, () =>
            CreatedAtAction(nameof(Obter), new { id = resultado.Value }, new CriadoResponse(resultado.Value)));
    }

    public sealed record AtualizarStatusRequest(string Status);

    [Authorize(Policy = Perfis.Funcionario)]
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarStatus(
        int id,
        AtualizarStatusRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new AtualizarStatusFeedbackCommand(id, request.Status), cancellationToken);
        return this.ParaResposta(resultado, NoContent);
    }

    /// <summary>Id do Estudante ligado ao usuário logado (claim acrescentada pela UsuarioClaimsTransformation).</summary>
    private int? EstudanteLogado() =>
        int.TryParse(User.FindFirst(Perfis.ClaimEstudanteId)?.Value, out var id) ? id : null;
}

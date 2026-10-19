using CumbucaSaas.API.Behaviors;
using CumbucaSaas.API.Commands;
using CumbucaSaas.API.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

[ApiController]
[Route("api/feedbacks")]
public sealed class FeedbacksController(IMediator mediator, IFeedbackQueries queries) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<FeedbackViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedbackViewModel>> Obter(int id, CancellationToken cancellationToken)
    {
        var feedback = await queries.ObterPorIdAsync(id, cancellationToken);
        return feedback is null ? NotFound() : Ok(feedback);
    }

    public sealed record AtualizarStatusRequest(string Status);

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

        if (resultado.IsSuccess)
        {
            return NoContent();
        }

        if (resultado.HasError<NaoEncontradoError>())
        {
            return NotFound();
        }

        foreach (var erro in resultado.Errors)
        {
            ModelState.AddModelError(erro is ValidacaoError v ? v.Propriedade : string.Empty, erro.Message);
        }

        return ValidationProblem(ModelState);
    }
}

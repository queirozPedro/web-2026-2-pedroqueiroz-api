using CumbucaSaas.API.Behaviors;
using CumbucaSaas.API.Commands;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

/// <summary>Converte o Result de um command na resposta HTTP, igual em todos os controllers.</summary>
internal static class ResultadoExtensions
{
    /// <summary>
    /// Sucesso: <paramref name="sucesso"/>. NaoEncontradoError: 404. ConflitoError: 409.
    /// Erros de validação: 400 com os campos (ValidationProblemDetails).
    /// </summary>
    public static IActionResult ParaResposta(
        this ControllerBase controller,
        ResultBase resultado,
        Func<IActionResult> sucesso)
    {
        if (resultado.IsSuccess)
        {
            return sucesso();
        }

        if (resultado.HasError<NaoEncontradoError>())
        {
            return controller.NotFound();
        }

        if (resultado.HasError<ConflitoError>(out var conflitos))
        {
            return controller.Problem(
                detail: conflitos.First().Message,
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflito");
        }

        foreach (var erro in resultado.Errors)
        {
            controller.ModelState.AddModelError(erro is ValidacaoError v ? v.Propriedade : string.Empty, erro.Message);
        }

        return controller.ValidationProblem(controller.ModelState);
    }
}

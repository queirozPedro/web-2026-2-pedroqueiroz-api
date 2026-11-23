using CumbucaSaas.API.Autenticacao;
using CumbucaSaas.API.Commands;
using CumbucaSaas.API.PerfilUsuario;
using CumbucaSaas.API.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

/// <summary>
/// O restaurante (tenant) do usuário logado: nome, local, horário de funcionamento, foto e
/// cores do app. Todos leem (o front aplica as cores); só o funcionário altera.
/// </summary>
[ApiController]
[Authorize]
[Route("api/restaurante")]
public sealed class RestauranteController(IMediator mediator, IRestauranteQueries queries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<RestauranteViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestauranteViewModel>> Obter(CancellationToken cancellationToken)
    {
        var restaurante = await queries.ObterAsync(cancellationToken);
        return restaurante is null ? NotFound() : Ok(restaurante);
    }

    /// <param name="Uf">Sigla do estado, ex.: "RN".</param>
    /// <param name="CorPrincipal">"#RRGGBB" (fundo do menu lateral, com texto branco).</param>
    /// <param name="CorSecundaria">"#RRGGBB" (item ativo, botões e links).</param>
    /// <param name="Horarios">A semana inteira; dia sem horário = fechado.</param>
    public sealed record AtualizarRestauranteRequest(
        string Nome,
        string NomeResponsavel,
        string? Endereco,
        string Cidade,
        string Uf,
        string CorPrincipal,
        string CorSecundaria,
        IReadOnlyList<HorarioFuncionamentoDados> Horarios);

    /// <summary>Altera os dados do restaurante. Devolve o restaurante atualizado.</summary>
    [Authorize(Policy = Perfis.Funcionario)]
    [HttpPut]
    [ProducesResponseType<RestauranteViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(AtualizarRestauranteRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new AtualizarRestauranteCommand(
                request.Nome,
                request.NomeResponsavel,
                request.Endereco,
                request.Cidade,
                request.Uf,
                request.CorPrincipal,
                request.CorSecundaria,
                request.Horarios),
            cancellationToken);

        return resultado.IsFailed
            ? this.ParaResposta(resultado, NoContent)
            : Ok(await queries.ObterAsync(cancellationToken));
    }

    /// <summary>Campo do formulário (multipart/form-data).</summary>
    public sealed class AlterarFotoRequest
    {
        /// <summary>JPG, PNG ou WebP de até 2 MB.</summary>
        public IFormFile? Foto { get; set; }
    }

    /// <summary>Troca a foto do restaurante (vai para o S3). Devolve o restaurante atualizado.</summary>
    [Authorize(Policy = Perfis.Funcionario)]
    [HttpPut("foto")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(PerfilOptions.FotoTamanhoMaximo + 512 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = PerfilOptions.FotoTamanhoMaximo + 512 * 1024)]
    [ProducesResponseType<RestauranteViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AlterarFoto([FromForm] AlterarFotoRequest request, CancellationToken cancellationToken)
    {
        if (request.Foto is null)
        {
            ModelState.AddModelError(nameof(AlterarFotoRequest.Foto), "Escolha a foto do restaurante.");
            return ValidationProblem(ModelState);
        }

        await using var conteudo = request.Foto.OpenReadStream();
        var resultado = await mediator.Send(
            new AlterarFotoRestauranteCommand(new FotoEnviada(conteudo, request.Foto.ContentType, request.Foto.Length)),
            cancellationToken);

        return resultado.IsFailed
            ? this.ParaResposta(resultado, NoContent)
            : Ok(await queries.ObterAsync(cancellationToken));
    }

    /// <summary>Remove a foto do restaurante. Devolve o restaurante atualizado.</summary>
    [Authorize(Policy = Perfis.Funcionario)]
    [HttpDelete("foto")]
    [ProducesResponseType<RestauranteViewModel>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoverFoto(CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new AlterarFotoRestauranteCommand(null), cancellationToken);

        return resultado.IsFailed
            ? this.ParaResposta(resultado, NoContent)
            : Ok(await queries.ObterAsync(cancellationToken));
    }
}

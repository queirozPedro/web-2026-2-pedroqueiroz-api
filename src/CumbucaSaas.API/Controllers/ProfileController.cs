using CumbucaSaas.API.Autenticacao;
using CumbucaSaas.API.Commands;
using CumbucaSaas.API.PerfilUsuario;
using CumbucaSaas.API.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

/// <summary>
/// Perfil do usuário logado (funcionário ou estudante, com o cadastro concluído).
/// Rotas próprias no API Gateway: GET /profile e PUT /profile.
/// </summary>
[ApiController]
[Authorize]
[Route("profile")]
public sealed class ProfileController(IMediator mediator, IPerfilQueries queries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PerfilViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerfilViewModel>> Obter(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst(Perfis.ClaimUsuarioId)?.Value, out var usuarioId))
        {
            return NotFound();
        }

        var perfil = await queries.ObterAsync(User.FindFirst("sub")?.Value ?? "", usuarioId, cancellationToken);
        return perfil is null ? NotFound() : Ok(perfil);
    }

    /// <summary>Campos do formulário (multipart/form-data).</summary>
    public sealed class AtualizarPerfilRequest
    {
        /// <summary>Nome completo (pelo menos 2 palavras de 3 letras ou mais).</summary>
        public string Nome { get; set; } = "";

        /// <summary>Formato "YYYY-MM-DD".</summary>
        public DateOnly DataNascimento { get; set; }

        /// <summary>Opcional: JPG, PNG ou WebP de até 2 MB.</summary>
        public IFormFile? Foto { get; set; }
    }

    /// <summary>
    /// Altera nome, data de nascimento e (opcionalmente) a foto de perfil, que vai para o
    /// S3. Os dados ficam no DynamoDB. Devolve o perfil atualizado.
    /// </summary>
    [HttpPut]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(PerfilOptions.FotoTamanhoMaximo + 512 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = PerfilOptions.FotoTamanhoMaximo + 512 * 1024)]
    [ProducesResponseType<PerfilViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Atualizar(
        [FromForm] AtualizarPerfilRequest request,
        CancellationToken cancellationToken)
    {
        await using var conteudo = request.Foto?.OpenReadStream();
        var foto = request.Foto is null
            ? null
            : new FotoEnviada(conteudo!, request.Foto.ContentType, request.Foto.Length);

        var resultado = await mediator.Send(
            new AtualizarPerfilCommand(User.FindFirst("sub")?.Value ?? "", request.Nome, request.DataNascimento, foto),
            cancellationToken);

        if (resultado.IsFailed)
        {
            return this.ParaResposta(resultado, NoContent);
        }

        var perfil = await queries.ObterAsync(
            User.FindFirst("sub")?.Value ?? "",
            int.Parse(User.FindFirst(Perfis.ClaimUsuarioId)!.Value),
            cancellationToken);

        return Ok(perfil);
    }
}

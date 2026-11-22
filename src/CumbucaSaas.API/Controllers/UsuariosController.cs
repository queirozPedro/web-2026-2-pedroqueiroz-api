using System.Security.Claims;
using CumbucaSaas.API.Autenticacao;
using CumbucaSaas.API.Commands;
using CumbucaSaas.API.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

/// <summary>
/// Cadastro de funcionário (e-mail e senha) e dados do usuário logado. São as únicas
/// rotas abertas a quem ainda não concluiu o cadastro.
/// </summary>
[ApiController]
[Authorize(Policy = Perfis.Logado)]
[Route("api/usuarios")]
public sealed class UsuariosController(IMediator mediator, IUsuarioQueries queries) : ControllerBase
{
    /// <param name="DataNascimento">Formato "YYYY-MM-DD".</param>
    /// <param name="Senha">Mínimo de 8 caracteres, com maiúscula, minúscula, número e símbolo.</param>
    public sealed record CadastroRequest(string Nome, string Email, DateOnly DataNascimento, string Senha);

    /// <summary>
    /// Cria a conta de funcionário (login com e-mail e senha). Não exige token. 409 se
    /// o e-mail já tiver uma conta, por qualquer forma de login.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("/api/cadastro")]
    [ProducesResponseType<CriadoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cadastrar(CadastroRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new CadastrarFuncionarioCommand(request.Nome, request.Email, request.DataNascimento, request.Senha),
            cancellationToken);

        return this.ParaResposta(resultado, () =>
            StatusCode(StatusCodes.Status201Created, new CriadoResponse(resultado.Value)));
    }

    /// <summary>
    /// Quem está logado: perfil, status do cadastro e dados. Sem perfil, vem com o
    /// motivo ("dominio-nao-permitido" ou "email-em-uso").
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType<UsuarioLogadoViewModel>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioLogadoViewModel>> Obter(CancellationToken cancellationToken)
    {
        var email = User.FindFirst("email")?.Value ?? "";
        var semAcesso = new UsuarioLogadoViewModel(
            null, email, null, null, null, false, null, User.FindFirst(Perfis.ClaimMotivoSemAcesso)?.Value);

        if (User.FindFirst(ClaimTypes.Role) is null
            || !int.TryParse(User.FindFirst(Perfis.ClaimUsuarioId)?.Value, out var id))
        {
            return Ok(semAcesso);
        }

        return Ok(await queries.ObterAsync(id, cancellationToken) ?? semAcesso);
    }

    public sealed record ConcluirCadastroRequest(string Nome, DateOnly DataNascimento);

    /// <summary>Nome completo e data de nascimento (obrigatórios para acessar o sistema).</summary>
    [Authorize(Policy = Perfis.ComPerfil)]
    [HttpPut("me/cadastro")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConcluirCadastro(
        ConcluirCadastroRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new ConcluirCadastroCommand(User.FindFirst("sub")?.Value ?? "", request.Nome, request.DataNascimento),
            cancellationToken);

        return this.ParaResposta(resultado, NoContent);
    }
}

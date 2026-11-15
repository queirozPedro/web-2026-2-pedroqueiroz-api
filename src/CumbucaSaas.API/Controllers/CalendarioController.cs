using CumbucaSaas.API.Commands;
using CumbucaSaas.API.Queries;
using MediatR;
using CumbucaSaas.API.Autenticacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

/// <summary>Calendário de refeições: turnos (almoço/janta) de cada dia e seus cardápios.</summary>
[ApiController]
[Authorize]
[Route("api/calendario")]
public sealed class CalendarioController(IMediator mediator, ICalendarioQueries queries) : ControllerBase
{
    /// <summary>Turnos do período, em ordem cronológica. Sem período, todos.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CalendarioRefeicaoViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CalendarioRefeicaoViewModel>>> Listar(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken cancellationToken) =>
        Ok(await queries.ListarAsync(inicio, fim, cancellationToken));

    /// <summary>
    /// Turnos de um dia (ex.: /api/calendario/dia/2026-11-16), para o cardápio do dia.
    /// A data vem do front para valer o dia local, e não o UTC do servidor.
    /// </summary>
    [HttpGet("dia/{data}")]
    [ProducesResponseType<IReadOnlyList<CalendarioRefeicaoViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CalendarioRefeicaoViewModel>>> ListarDoDia(
        DateOnly data,
        CancellationToken cancellationToken) =>
        Ok(await queries.ListarAsync(data, data, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<CalendarioRefeicaoViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarioRefeicaoViewModel>> Obter(int id, CancellationToken cancellationToken)
    {
        var calendario = await queries.ObterPorIdAsync(id, cancellationToken);
        return calendario is null ? NotFound() : Ok(calendario);
    }

    /// <param name="Turno">"almoco" ou "janta".</param>
    /// <param name="Abertura">Formato "HH:mm:ss", ex.: "11:00:00".</param>
    public sealed record CalendarioRefeicaoRequest(
        DateOnly Data,
        string Turno,
        TimeOnly Abertura,
        TimeOnly Fechamento,
        string? Observacoes);

    [Authorize(Policy = Perfis.Funcionario)]
    [HttpPost]
    [ProducesResponseType<CriadoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(CalendarioRefeicaoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new CriarCalendarioRefeicaoCommand(
                request.Data, request.Turno, request.Abertura, request.Fechamento, request.Observacoes),
            cancellationToken);

        return this.ParaResposta(resultado, () =>
            CreatedAtAction(nameof(Obter), new { id = resultado.Value }, new CriadoResponse(resultado.Value)));
    }

    [Authorize(Policy = Perfis.Funcionario)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(
        int id,
        CalendarioRefeicaoRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new AtualizarCalendarioRefeicaoCommand(
                id, request.Data, request.Turno, request.Abertura, request.Fechamento, request.Observacoes),
            cancellationToken);

        return this.ParaResposta(resultado, NoContent);
    }

    /// <summary>Remove o turno. Com produção ou caixa registrados, devolve 409.</summary>
    [Authorize(Policy = Perfis.Funcionario)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new RemoverCalendarioRefeicaoCommand(id), cancellationToken);
        return this.ParaResposta(resultado, NoContent);
    }

    public sealed record CardapioRequest(
        string Itens,
        string CategoriaNutricional,
        decimal PrecoSubsidiado,
        decimal PrecoIntegral);

    /// <summary>Cria ou substitui o cardápio do turno.</summary>
    [Authorize(Policy = Perfis.Funcionario)]
    [HttpPut("{id:int}/cardapio")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DefinirCardapio(
        int id,
        CardapioRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new DefinirCardapioCommand(
                id, request.Itens, request.CategoriaNutricional, request.PrecoSubsidiado, request.PrecoIntegral),
            cancellationToken);

        return this.ParaResposta(resultado, NoContent);
    }

    public sealed record ProducaoRequest(int QuantidadeProduzida, string? Observacoes);

    /// <summary>Cria ou corrige a produção do turno.</summary>
    [Authorize(Policy = Perfis.Funcionario)]
    [HttpPut("{id:int}/producao")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegistrarProducao(
        int id,
        ProducaoRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new RegistrarProducaoCommand(id, request.QuantidadeProduzida, request.Observacoes),
            cancellationToken);

        return this.ParaResposta(resultado, NoContent);
    }

    public sealed record CaixaRequest(decimal ValorDinheiro, decimal ValorPix);

    /// <summary>Fecha (ou corrige) o caixa do turno. Exige cardápio cadastrado (senão 409).</summary>
    [Authorize(Policy = Perfis.Funcionario)]
    [HttpPut("{id:int}/caixa")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FecharCaixa(
        int id,
        CaixaRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new FecharCaixaCommand(id, request.ValorDinheiro, request.ValorPix),
            cancellationToken);

        return this.ParaResposta(resultado, NoContent);
    }
}

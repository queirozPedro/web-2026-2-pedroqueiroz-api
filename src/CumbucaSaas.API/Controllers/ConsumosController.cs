using CumbucaSaas.API.Commands;
using CumbucaSaas.API.Queries;
using MediatR;
using CumbucaSaas.API.Autenticacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CumbucaSaas.API.Controllers;

/// <summary>Registros de consumo: cada refeição servida no ponto de venda.</summary>
[ApiController]
[Authorize(Policy = Perfis.Funcionario)]
[Route("api/consumos")]
public sealed class ConsumosController(IMediator mediator, IConsumoQueries queries) : ControllerBase
{
    /// <summary>Consumos de um turno (?calendarioId=1), em ordem de horário.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ConsumoViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConsumoViewModel>>> ListarPorTurno(
        [FromQuery] int calendarioId,
        CancellationToken cancellationToken) =>
        Ok(await queries.ListarPorTurnoAsync(calendarioId, cancellationToken));

    /// <param name="TipoCliente">"estudante" ou "externo".</param>
    /// <param name="EstudanteId">Obrigatório para estudante; vazio para externo.</param>
    /// <param name="FormaPagamento">"dinheiro" ou "pix". Dispensado para estudante isento.</param>
    /// <param name="FormaIdentificacao">Estudante: "qrcode" ou "matricula" (padrão). Externo: sempre "balcao".</param>
    /// <param name="Horario">"HH:mm:ss". Vazio = agora (horário de Brasília).</param>
    public sealed record RegistrarConsumoRequest(
        int CalendarioRefeicaoId,
        string TipoCliente,
        int? EstudanteId,
        string? FormaPagamento,
        string? FormaIdentificacao,
        TimeOnly? Horario);

    /// <summary>
    /// Registra uma refeição servida. 409 quando o turno não tem cardápio, o horário
    /// está fora do turno ou o estudante está inativo.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<CriadoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(RegistrarConsumoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(
            new RegistrarConsumoCommand(
                request.CalendarioRefeicaoId,
                request.TipoCliente,
                request.EstudanteId,
                request.FormaPagamento,
                request.FormaIdentificacao,
                request.Horario),
            cancellationToken);

        // Não há GET por Id de consumo: o Location aponta para a lista do turno.
        return this.ParaResposta(resultado, () =>
            Created($"/api/consumos?calendarioId={request.CalendarioRefeicaoId}", new CriadoResponse(resultado.Value)));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CancelarConsumoCommand(id), cancellationToken);
        return this.ParaResposta(resultado, NoContent);
    }
}

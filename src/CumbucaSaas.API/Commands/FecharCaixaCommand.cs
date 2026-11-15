using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>
/// Funcionário fecha o caixa do turno com o valor contado em dinheiro e o recebido
/// via Pix (cria ou corrige). O turno precisa ter cardápio.
/// </summary>
public sealed record FecharCaixaCommand(
    int CalendarioRefeicaoId,
    decimal ValorDinheiro,
    decimal ValorPix) : IRequest<Result<Unit>>;

public sealed class FecharCaixaCommandValidator : AbstractValidator<FecharCaixaCommand>
{
    public FecharCaixaCommandValidator()
    {
        RuleFor(c => c.CalendarioRefeicaoId).GreaterThan(0);

        // numeric(10,2) no banco: até 8 dígitos inteiros e 2 casas decimais.
        RuleFor(c => c.ValorDinheiro).GreaterThanOrEqualTo(0).PrecisionScale(10, 2, true);
        RuleFor(c => c.ValorPix).GreaterThanOrEqualTo(0).PrecisionScale(10, 2, true);
    }
}

public sealed class FecharCaixaCommandHandler(
    ICalendarioRefeicaoRepository calendarios,
    INotificadorTempoReal notificador)
    : IRequestHandler<FecharCaixaCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(FecharCaixaCommand command, CancellationToken cancellationToken)
    {
        var calendario = await calendarios.ObterAsync(command.CalendarioRefeicaoId, cancellationToken);
        if (calendario is null)
        {
            return Result.Fail<Unit>(new NaoEncontradoError(
                $"Turno {command.CalendarioRefeicaoId} não encontrado no calendário."));
        }

        // Mesma regra do domínio, devolvida como 409 em vez de exceção.
        if (calendario.Cardapio is null)
        {
            return Result.Fail<Unit>(new ConflitoError("Defina o cardápio do turno antes de fechar o caixa."));
        }

        calendario.FecharCaixa(command.ValorDinheiro, command.ValorPix);
        await calendarios.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        await notificador.NotificarAsync("dashboard-atualizado", cancellationToken);

        return Result.Ok(Unit.Value);
    }
}

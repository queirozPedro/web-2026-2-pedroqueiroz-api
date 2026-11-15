using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>Funcionário informa quantas refeições foram produzidas no turno (cria ou corrige).</summary>
public sealed record RegistrarProducaoCommand(
    int CalendarioRefeicaoId,
    int QuantidadeProduzida,
    string? Observacoes) : IRequest<Result<Unit>>;

public sealed class RegistrarProducaoCommandValidator : AbstractValidator<RegistrarProducaoCommand>
{
    public RegistrarProducaoCommandValidator()
    {
        RuleFor(c => c.CalendarioRefeicaoId).GreaterThan(0);
        RuleFor(c => c.QuantidadeProduzida).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Observacoes).MaximumLength(Producao.ObservacoesTamanhoMaximo);
    }
}

public sealed class RegistrarProducaoCommandHandler(
    ICalendarioRefeicaoRepository calendarios,
    INotificadorTempoReal notificador)
    : IRequestHandler<RegistrarProducaoCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(RegistrarProducaoCommand command, CancellationToken cancellationToken)
    {
        var calendario = await calendarios.ObterAsync(command.CalendarioRefeicaoId, cancellationToken);
        if (calendario is null)
        {
            return Result.Fail<Unit>(new NaoEncontradoError(
                $"Turno {command.CalendarioRefeicaoId} não encontrado no calendário."));
        }

        calendario.RegistrarProducao(command.QuantidadeProduzida, command.Observacoes);
        await calendarios.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        await notificador.NotificarAsync("dashboard-atualizado", cancellationToken);

        return Result.Ok(Unit.Value);
    }
}

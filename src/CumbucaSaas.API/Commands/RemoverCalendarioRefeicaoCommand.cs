using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.Aggregates.ConsumoAggregate;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>
/// Funcionário remove um turno do calendário (o cardápio vai junto). Bloqueado
/// quando o turno já tem produção, caixa ou consumos registrados.
/// </summary>
public sealed record RemoverCalendarioRefeicaoCommand(int CalendarioRefeicaoId) : IRequest<Result<Unit>>;

public sealed class RemoverCalendarioRefeicaoCommandValidator : AbstractValidator<RemoverCalendarioRefeicaoCommand>
{
    public RemoverCalendarioRefeicaoCommandValidator()
    {
        RuleFor(c => c.CalendarioRefeicaoId).GreaterThan(0);
    }
}

public sealed class RemoverCalendarioRefeicaoCommandHandler(
    ICalendarioRefeicaoRepository calendarios,
    IRegistroConsumoRepository consumos,
    INotificadorTempoReal notificador)
    : IRequestHandler<RemoverCalendarioRefeicaoCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(RemoverCalendarioRefeicaoCommand command, CancellationToken cancellationToken)
    {
        var calendario = await calendarios.ObterAsync(command.CalendarioRefeicaoId, cancellationToken);
        if (calendario is null)
        {
            return Result.Fail<Unit>(new NaoEncontradoError(
                $"Turno {command.CalendarioRefeicaoId} não encontrado no calendário."));
        }

        // Consumo é outro agregado: a checagem vem do repositório dele. No banco, a
        // chave estrangeira com Restrict também impede apagar o turno.
        if (calendario.PossuiMovimento || await consumos.ExisteNoTurnoAsync(calendario.Id, cancellationToken))
        {
            return Result.Fail<Unit>(new ConflitoError(
                "O turno já tem produção, caixa ou consumos registrados e não pode ser removido."));
        }

        calendarios.Remover(calendario);
        await calendarios.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        await notificador.NotificarAsync("dashboard-atualizado", cancellationToken);

        return Result.Ok(Unit.Value);
    }
}

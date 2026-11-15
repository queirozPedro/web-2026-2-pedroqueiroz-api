using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Domain.Aggregates.ConsumoAggregate;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>Funcionário desfaz um consumo registrado por engano.</summary>
public sealed record CancelarConsumoCommand(int RegistroConsumoId) : IRequest<Result<Unit>>;

public sealed class CancelarConsumoCommandValidator : AbstractValidator<CancelarConsumoCommand>
{
    public CancelarConsumoCommandValidator()
    {
        RuleFor(c => c.RegistroConsumoId).GreaterThan(0);
    }
}

public sealed class CancelarConsumoCommandHandler(
    IRegistroConsumoRepository consumos,
    INotificadorTempoReal notificador)
    : IRequestHandler<CancelarConsumoCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(CancelarConsumoCommand command, CancellationToken cancellationToken)
    {
        var consumo = await consumos.ObterAsync(command.RegistroConsumoId, cancellationToken);
        if (consumo is null)
        {
            return Result.Fail<Unit>(new NaoEncontradoError(
                $"Registro de consumo {command.RegistroConsumoId} não encontrado."));
        }

        consumos.Remover(consumo);
        await consumos.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        await notificador.NotificarAsync("dashboard-atualizado", cancellationToken);

        return Result.Ok(Unit.Value);
    }
}

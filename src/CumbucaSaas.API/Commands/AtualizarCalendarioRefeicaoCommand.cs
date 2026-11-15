using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>Funcionário corrige data, turno, horários ou observações de um turno já cadastrado.</summary>
public sealed record AtualizarCalendarioRefeicaoCommand(
    int CalendarioRefeicaoId,
    DateOnly Data,
    string Turno,
    TimeOnly Abertura,
    TimeOnly Fechamento,
    string? Observacoes) : IRequest<Result<Unit>>;

public sealed class AtualizarCalendarioRefeicaoCommandValidator : AbstractValidator<AtualizarCalendarioRefeicaoCommand>
{
    public AtualizarCalendarioRefeicaoCommandValidator()
    {
        RuleFor(c => c.CalendarioRefeicaoId).GreaterThan(0);

        RuleFor(c => c.Turno)
            .NotEmpty()
            .Must(turno => Enumeration.TryFromName<Turno>(turno, out _))
            .WithMessage(c => $"Turno '{c.Turno}' inválido. Use: {string.Join(", ", Enumeration.GetAll<Turno>())}.");

        RuleFor(c => c.Fechamento)
            .GreaterThan(c => c.Abertura)
            .WithMessage("O fechamento do turno deve ser depois da abertura.");

        RuleFor(c => c.Observacoes).MaximumLength(CalendarioRefeicao.ObservacoesTamanhoMaximo);
    }
}

public sealed class AtualizarCalendarioRefeicaoCommandHandler(
    ICalendarioRefeicaoRepository calendarios,
    INotificadorTempoReal notificador)
    : IRequestHandler<AtualizarCalendarioRefeicaoCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(AtualizarCalendarioRefeicaoCommand command, CancellationToken cancellationToken)
    {
        var calendario = await calendarios.ObterAsync(command.CalendarioRefeicaoId, cancellationToken);
        if (calendario is null)
        {
            return Result.Fail<Unit>(new NaoEncontradoError(
                $"Turno {command.CalendarioRefeicaoId} não encontrado no calendário."));
        }

        var turno = Enumeration.FromName<Turno>(command.Turno);

        if (await calendarios.ExisteAsync(command.Data, turno, calendario.Id, cancellationToken))
        {
            return Result.Fail<Unit>(new ConflitoError(
                $"Já existe {turno} cadastrado em {command.Data:dd/MM/yyyy}."));
        }

        calendario.Atualizar(command.Data, turno, command.Abertura, command.Fechamento, command.Observacoes);
        await calendarios.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        await notificador.NotificarAsync("dashboard-atualizado", cancellationToken);

        return Result.Ok(Unit.Value);
    }
}

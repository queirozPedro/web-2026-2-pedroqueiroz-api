using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>Funcionário cadastra um turno (almoço ou janta) em uma data. Devolve o Id criado.</summary>
public sealed record CriarCalendarioRefeicaoCommand(
    DateOnly Data,
    string Turno,
    TimeOnly Abertura,
    TimeOnly Fechamento,
    string? Observacoes) : IRequest<Result<int>>;

public sealed class CriarCalendarioRefeicaoCommandValidator : AbstractValidator<CriarCalendarioRefeicaoCommand>
{
    public CriarCalendarioRefeicaoCommandValidator()
    {
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

public sealed class CriarCalendarioRefeicaoCommandHandler(
    ICalendarioRefeicaoRepository calendarios,
    INotificadorTempoReal notificador)
    : IRequestHandler<CriarCalendarioRefeicaoCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CriarCalendarioRefeicaoCommand command, CancellationToken cancellationToken)
    {
        var turno = Enumeration.FromName<Turno>(command.Turno);

        if (await calendarios.ExisteAsync(command.Data, turno, cancellationToken: cancellationToken))
        {
            return Result.Fail<int>(new ConflitoError(
                $"Já existe {turno} cadastrado em {command.Data:dd/MM/yyyy}."));
        }

        var calendario = calendarios.Adicionar(new CalendarioRefeicao(
            command.Data, turno, command.Abertura, command.Fechamento, command.Observacoes));
        await calendarios.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        await notificador.NotificarAsync("dashboard-atualizado", cancellationToken);

        return Result.Ok(calendario.Id);
    }
}

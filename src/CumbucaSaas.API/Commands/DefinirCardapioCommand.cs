using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>Funcionário cadastra ou altera o cardápio (itens e preços) de um turno do calendário.</summary>
public sealed record DefinirCardapioCommand(
    int CalendarioRefeicaoId,
    string Itens,
    string CategoriaNutricional,
    decimal PrecoSubsidiado,
    decimal PrecoIntegral) : IRequest<Result<Unit>>;

public sealed class DefinirCardapioCommandValidator : AbstractValidator<DefinirCardapioCommand>
{
    public DefinirCardapioCommandValidator()
    {
        RuleFor(c => c.CalendarioRefeicaoId).GreaterThan(0);

        RuleFor(c => c.Itens).NotEmpty().MaximumLength(Cardapio.ItensTamanhoMaximo);
        RuleFor(c => c.CategoriaNutricional).NotEmpty().MaximumLength(Cardapio.CategoriaNutricionalTamanhoMaximo);

        // numeric(10,2) no banco: até 8 dígitos inteiros e 2 casas decimais.
        RuleFor(c => c.PrecoSubsidiado).GreaterThan(0).PrecisionScale(10, 2, true);
        RuleFor(c => c.PrecoIntegral).GreaterThan(0).PrecisionScale(10, 2, true);

        RuleFor(c => c.PrecoSubsidiado)
            .LessThanOrEqualTo(c => c.PrecoIntegral)
            .WithMessage("O preço subsidiado não pode ser maior que o integral.");
    }
}

public sealed class DefinirCardapioCommandHandler(
    ICalendarioRefeicaoRepository calendarios,
    INotificadorTempoReal notificador)
    : IRequestHandler<DefinirCardapioCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DefinirCardapioCommand command, CancellationToken cancellationToken)
    {
        var calendario = await calendarios.ObterAsync(command.CalendarioRefeicaoId, cancellationToken);
        if (calendario is null)
        {
            return Result.Fail<Unit>(new NaoEncontradoError(
                $"Turno {command.CalendarioRefeicaoId} não encontrado no calendário."));
        }

        calendario.DefinirCardapio(
            command.Itens, command.CategoriaNutricional, command.PrecoSubsidiado, command.PrecoIntegral);
        await calendarios.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Os preços entram no valor estimado do dashboard.
        await notificador.NotificarAsync("dashboard-atualizado", cancellationToken);

        return Result.Ok(Unit.Value);
    }
}

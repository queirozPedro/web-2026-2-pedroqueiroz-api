using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.SeedWork;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>Funcionário ativa ou inativa o vínculo de um estudante (ex.: formatura, trancamento).</summary>
public sealed record AlterarVinculoEstudanteCommand(int EstudanteId, string Vinculo) : IRequest<Result<Unit>>;

public sealed class AlterarVinculoEstudanteCommandValidator : AbstractValidator<AlterarVinculoEstudanteCommand>
{
    public AlterarVinculoEstudanteCommandValidator()
    {
        RuleFor(c => c.EstudanteId).GreaterThan(0);

        RuleFor(c => c.Vinculo)
            .NotEmpty()
            .Must(vinculo => Enumeration.TryFromName<Vinculo>(vinculo, out _))
            .WithMessage(c =>
                $"Vínculo '{c.Vinculo}' inválido. Use: {string.Join(", ", Enumeration.GetAll<Vinculo>())}.");
    }
}

public sealed class AlterarVinculoEstudanteCommandHandler(IEstudanteRepository estudantes)
    : IRequestHandler<AlterarVinculoEstudanteCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(AlterarVinculoEstudanteCommand command, CancellationToken cancellationToken)
    {
        var estudante = await estudantes.ObterAsync(command.EstudanteId, cancellationToken);
        if (estudante is null)
        {
            return Result.Fail<Unit>(new NaoEncontradoError($"Estudante {command.EstudanteId} não encontrado."));
        }

        estudante.AlterarVinculo(Enumeration.FromName<Vinculo>(command.Vinculo));
        await estudantes.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Ok(Unit.Value);
    }
}

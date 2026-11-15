using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.SeedWork;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>Funcionário cadastra um estudante (começa com vínculo ativo). Devolve o Id criado.</summary>
public sealed record CadastrarEstudanteCommand(
    string Nome,
    string Email,
    string Matricula,
    string Curso,
    string TipoSubsidio) : IRequest<Result<int>>;

public sealed class CadastrarEstudanteCommandValidator : AbstractValidator<CadastrarEstudanteCommand>
{
    public CadastrarEstudanteCommandValidator()
    {
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(Estudante.NomeTamanhoMaximo);
        RuleFor(c => c.Matricula).NotEmpty().MaximumLength(Estudante.MatriculaTamanhoMaximo);
        RuleFor(c => c.Curso).NotEmpty().MaximumLength(Estudante.CursoTamanhoMaximo);

        RuleFor(c => c.Email)
            .NotEmpty()
            .MaximumLength(Estudante.EmailTamanhoMaximo)
            .EmailAddress()
            .Must(email => Estudante.NormalizarEmail(email).EndsWith(Estudante.DominioInstitucional, StringComparison.Ordinal))
            .WithMessage($"O e-mail do estudante deve terminar em {Estudante.DominioInstitucional}.");

        RuleFor(c => c.TipoSubsidio)
            .NotEmpty()
            .Must(tipo => Enumeration.TryFromName<TipoSubsidio>(tipo, out _))
            .WithMessage(c =>
                $"Tipo de subsídio '{c.TipoSubsidio}' inválido. Use: {string.Join(", ", Enumeration.GetAll<TipoSubsidio>())}.");
    }
}

public sealed class CadastrarEstudanteCommandHandler(IEstudanteRepository estudantes)
    : IRequestHandler<CadastrarEstudanteCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CadastrarEstudanteCommand command, CancellationToken cancellationToken)
    {
        var matricula = command.Matricula.Trim();
        if (await estudantes.ExisteMatriculaAsync(matricula, cancellationToken))
        {
            return Result.Fail<int>(new ConflitoError($"Já existe estudante com a matrícula {matricula}."));
        }

        var email = Estudante.NormalizarEmail(command.Email);
        if (await estudantes.ExisteEmailAsync(email, cancellationToken))
        {
            return Result.Fail<int>(new ConflitoError($"Já existe estudante com o e-mail {email}."));
        }

        var estudante = estudantes.Adicionar(new Estudante(
            command.Nome,
            email,
            matricula,
            command.Curso,
            Enumeration.FromName<TipoSubsidio>(command.TipoSubsidio)));
        await estudantes.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Ok(estudante.Id);
    }
}

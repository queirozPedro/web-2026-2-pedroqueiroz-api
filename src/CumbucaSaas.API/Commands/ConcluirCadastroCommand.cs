using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>
/// O usuário logado informa (ou corrige) nome completo e data de nascimento. Até
/// isso acontecer ele não acessa nenhuma tela. O e-mail é o do login e não muda aqui.
/// </summary>
public sealed record ConcluirCadastroCommand(string IdExterno, string Nome, DateOnly DataNascimento)
    : IRequest<Result>;

public sealed class ConcluirCadastroCommandValidator : AbstractValidator<ConcluirCadastroCommand>
{
    public ConcluirCadastroCommandValidator()
    {
        RuleFor(c => c.IdExterno).NotEmpty();
        RuleFor(c => c.Nome).NomeCompleto();
        RuleFor(c => c.DataNascimento).DataNascimento();
    }
}

public sealed class ConcluirCadastroCommandHandler(IUsuarioRepository usuarios)
    : IRequestHandler<ConcluirCadastroCommand, Result>
{
    public async Task<Result> Handle(ConcluirCadastroCommand command, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.ObterPorIdExternoAsync(command.IdExterno, cancellationToken);
        if (usuario is null)
        {
            return Result.Fail(new NaoEncontradoError("Usuário não encontrado."));
        }

        usuario.ConcluirCadastro(command.Nome, command.DataNascimento, HoraLocal.Hoje, DateTime.UtcNow);
        await usuarios.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Ok();
    }
}

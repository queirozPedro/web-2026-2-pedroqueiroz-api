using CumbucaSaas.API.Autenticacao;
using CumbucaSaas.Domain.Aggregates.TenantAggregate;
using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>
/// Cadastro pela tela "Entrar com e-mail e senha": cria a conta no Cognito (sem
/// confirmação de e-mail por enquanto) e o Usuario funcionário, já com o cadastro
/// completo, no tenant padrão. Devolve o Id do usuário. E-mail já usado: 409.
/// </summary>
public sealed record CadastrarFuncionarioCommand(
    string Nome,
    string Email,
    DateOnly DataNascimento,
    string Senha) : IRequest<Result<int>>;

public sealed class CadastrarFuncionarioCommandValidator : AbstractValidator<CadastrarFuncionarioCommand>
{
    public CadastrarFuncionarioCommandValidator()
    {
        RuleFor(c => c.Nome).NomeCompleto();
        RuleFor(c => c.Email).Email();
        RuleFor(c => c.DataNascimento).DataNascimento();
        RuleFor(c => c.Senha).Senha();
    }
}

public sealed class CadastrarFuncionarioCommandHandler(
    IUsuarioRepository usuarios,
    IProvedorIdentidade provedor)
    : IRequestHandler<CadastrarFuncionarioCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CadastrarFuncionarioCommand command, CancellationToken cancellationToken)
    {
        var email = Usuario.NormalizarEmail(command.Email);

        // Confere antes de criar no Cognito: o e-mail pode ser de uma conta do Google.
        if (await usuarios.ExisteEmailAsync(email, cancellationToken))
        {
            return Result.Fail<int>(new ConflitoError("Já existe uma conta com este e-mail."));
        }

        var nome = Usuario.NormalizarNome(command.Nome);
        var criado = await provedor.CriarUsuarioAsync(email, nome, command.DataNascimento, command.Senha, cancellationToken);
        if (criado.IsFailed)
        {
            return Result.Fail<int>(criado.Errors);
        }

        try
        {
            var usuario = usuarios.Adicionar(Usuario.CadastrarFuncionario(
                Tenant.PadraoId, criado.Value.Sub, email, nome, command.DataNascimento, HoraLocal.Hoje, DateTime.UtcNow));
            await usuarios.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return Result.Ok(usuario.Id);
        }
        catch
        {
            // Sem o Usuario no banco, a conta no Cognito não deve ficar para trás.
            await provedor.RemoverUsuarioAsync(criado.Value.NomeUsuario, cancellationToken);
            throw;
        }
    }
}

using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using CumbucaSaas.API.Behaviors;
using CumbucaSaas.API.Commands;
using FluentResults;
using Microsoft.Extensions.Options;

namespace CumbucaSaas.API.Autenticacao;

/// <summary>Conta criada no provedor: o "sub" (vira o IdExterno) e o nome de usuário interno.</summary>
public sealed record ContaCriada(string Sub, string NomeUsuario);

/// <summary>Contas de e-mail e senha no User Pool do Cognito (a senha nunca passa pelo banco).</summary>
public interface IProvedorIdentidade
{
    /// <summary>Cria a conta já confirmada e com senha definitiva.</summary>
    Task<Result<ContaCriada>> CriarUsuarioAsync(
        string email,
        string nome,
        DateOnly dataNascimento,
        string senha,
        CancellationToken cancellationToken = default);

    /// <summary>Desfaz a criação quando a gravação no banco falha.</summary>
    Task RemoverUsuarioAsync(string nomeUsuario, CancellationToken cancellationToken = default);
}

/// <summary>
/// Usa as APIs administrativas do Cognito: precisa de credenciais da AWS com
/// cognito-idp:AdminCreateUser, AdminSetUserPassword e AdminDeleteUser (LabRole na
/// Lambda; o arquivo de credenciais do Learner Lab ao rodar pelo Visual Studio).
/// <para>
/// O User Pool entra com "nome de usuário" e tem o e-mail como alias: o Cognito não
/// aceita nome de usuário em formato de e-mail. Por isso o nome de usuário é um
/// identificador gerado e o e-mail vai como atributo verificado; a pessoa entra
/// digitando o e-mail (o alias), nunca esse identificador.
/// </para>
/// </summary>
internal sealed class CognitoProvedorIdentidade(
    IAmazonCognitoIdentityProvider cognito,
    IOptions<AutenticacaoOptions> opcoes,
    ILogger<CognitoProvedorIdentidade> logger)
    : IProvedorIdentidade
{
    private const string EmailJaCadastrado = "Já existe uma conta com este e-mail.";

    public async Task<Result<ContaCriada>> CriarUsuarioAsync(
        string email,
        string nome,
        DateOnly dataNascimento,
        string senha,
        CancellationToken cancellationToken = default)
    {
        var userPoolId = opcoes.Value.UserPoolId;
        var nomeUsuario = $"func_{Guid.NewGuid():N}";

        AdminCreateUserResponse criado;
        try
        {
            // SUPPRESS: sem e-mail de convite. email_verified = true: sem confirmação por ora,
            // e é o que permite entrar digitando o e-mail (alias só vale com e-mail verificado).
            criado = await cognito.AdminCreateUserAsync(
                new AdminCreateUserRequest
                {
                    UserPoolId = userPoolId,
                    Username = nomeUsuario,
                    MessageAction = MessageActionType.SUPPRESS,
                    UserAttributes =
                    [
                        new AttributeType { Name = "email", Value = email },
                        new AttributeType { Name = "email_verified", Value = "true" },
                        new AttributeType { Name = "name", Value = nome },
                        new AttributeType { Name = "birthdate", Value = dataNascimento.ToString("yyyy-MM-dd") }
                    ]
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is UsernameExistsException or AliasExistsException)
        {
            // AliasExists: outro usuário do pool já tem este e-mail verificado.
            return Result.Fail<ContaCriada>(new ConflitoError(EmailJaCadastrado));
        }

        try
        {
            // Sem isso a conta fica em FORCE_CHANGE_PASSWORD e o login pede outra senha.
            await cognito.AdminSetUserPasswordAsync(
                new AdminSetUserPasswordRequest
                {
                    UserPoolId = userPoolId,
                    Username = nomeUsuario,
                    Password = senha,
                    Permanent = true
                },
                cancellationToken);
        }
        catch (InvalidPasswordException ex)
        {
            await RemoverUsuarioAsync(nomeUsuario, cancellationToken);
            logger.LogInformation(ex, "Senha recusada pela política do User Pool.");
            return Result.Fail<ContaCriada>(new ValidacaoError(
                "Senha",
                "A senha não atende à política do Cognito: mínimo de 8 caracteres, com letra maiúscula, minúscula, número e símbolo."));
        }
        catch
        {
            await RemoverUsuarioAsync(nomeUsuario, cancellationToken);
            throw;
        }

        // No AWSSDK v4 as coleções não preenchidas vêm nulas.
        var sub = criado.User?.Attributes?.FirstOrDefault(a => a.Name == "sub")?.Value;
        if (string.IsNullOrEmpty(sub))
        {
            await RemoverUsuarioAsync(nomeUsuario, cancellationToken);
            throw new InvalidOperationException("O Cognito não devolveu o sub do usuário criado.");
        }

        return Result.Ok(new ContaCriada(sub, nomeUsuario));
    }

    public async Task RemoverUsuarioAsync(string nomeUsuario, CancellationToken cancellationToken = default)
    {
        try
        {
            // CancellationToken.None: a limpeza roda mesmo se a requisição foi cancelada.
            await cognito.AdminDeleteUserAsync(
                new AdminDeleteUserRequest { UserPoolId = opcoes.Value.UserPoolId, Username = nomeUsuario },
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Não esconde o erro original; a conta órfã no Cognito fica registrada no log.
            logger.LogError(ex, "Não foi possível remover do Cognito a conta {NomeUsuario} após falha no cadastro.", nomeUsuario);
        }
    }
}

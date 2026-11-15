using CumbucaSaas.API.Autenticacao;
using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Options;

namespace CumbucaSaas.API.Commands;

/// <summary>O que a API sabe do usuário logado. Perfil nulo = e-mail fora dos domínios (sem acesso).</summary>
public sealed record AcessoUsuario(int? UsuarioId, string? Perfil, int? EstudanteId);

/// <summary>
/// Chamado a cada requisição autenticada (UsuarioClaimsTransformation). No primeiro
/// acesso cria o Usuario; o perfil sai do domínio do e-mail. Estudante é ligado ao
/// seu cadastro no RU pelo e-mail institucional.
/// </summary>
public sealed record RegistrarAcessoCommand(string IdExterno, string Email, string? Nome)
    : IRequest<Result<AcessoUsuario>>;

public sealed class RegistrarAcessoCommandHandler(
    IUsuarioRepository usuarios,
    IEstudanteRepository estudantes,
    IOptions<AutenticacaoOptions> opcoes)
    : IRequestHandler<RegistrarAcessoCommand, Result<AcessoUsuario>>
{
    public async Task<Result<AcessoUsuario>> Handle(RegistrarAcessoCommand command, CancellationToken cancellationToken)
    {
        var email = Estudante.NormalizarEmail(command.Email);
        var perfil = Perfil.PorEmail(email, opcoes.Value.Funcionarios);
        var usuario = await usuarios.ObterPorIdExternoAsync(command.IdExterno, cancellationToken);

        if (perfil is null)
        {
            // Logou com uma conta fora da UFERSA: autenticado, mas sem perfil (403 nas rotas).
            return Result.Ok(new AcessoUsuario(usuario?.Id, null, null));
        }

        if (usuario is null)
        {
            usuario = usuarios.Adicionar(new Usuario(command.IdExterno, email, command.Nome ?? "", perfil, DateTime.UtcNow));
        }
        else
        {
            // Só vira UPDATE se o e-mail, o nome ou o perfil mudaram.
            usuario.Atualizar(email, command.Nome ?? usuario.Nome, perfil);
        }

        await usuarios.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        int? estudanteId = null;
        if (perfil.Id == Perfil.Estudante.Id)
        {
            var estudante = await estudantes.ObterPorEmailAsync(email, cancellationToken);
            if (estudante is not null)
            {
                if (estudante.UsuarioId is null)
                {
                    estudante.VincularUsuario(usuario.Id);
                    await estudantes.UnitOfWork.SaveEntitiesAsync(cancellationToken);
                }

                estudanteId = estudante.Id;
            }
        }

        return Result.Ok(new AcessoUsuario(usuario.Id, perfil.Name, estudanteId));
    }
}

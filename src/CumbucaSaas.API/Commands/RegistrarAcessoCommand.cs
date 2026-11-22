using CumbucaSaas.API.Autenticacao;
using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.Aggregates.TenantAggregate;
using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using FluentResults;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>
/// O que a API sabe do usuário logado. Perfil nulo = sem acesso (o motivo diz por quê).
/// Cadastro incompleto = só pode acessar as rotas de /api/usuarios/me.
/// </summary>
public sealed record AcessoUsuario(
    int? UsuarioId,
    int? TenantId,
    string? Perfil,
    bool CadastroCompleto,
    int? EstudanteId,
    string? MotivoSemAcesso);

/// <summary>
/// Chamado a cada requisição autenticada (UsuarioClaimsTransformation). O perfil sai
/// da forma de login: Google institucional é estudante; e-mail e senha é funcionário.
/// No primeiro acesso cria o Usuario no tenant padrão, com o cadastro incompleto.
/// Define o tenant da requisição e liga o estudante ao cadastro do RU pelo e-mail.
/// </summary>
public sealed record RegistrarAcessoCommand(string IdExterno, string Email, bool LoginGoogle)
    : IRequest<Result<AcessoUsuario>>;

public sealed class RegistrarAcessoCommandHandler(
    IUsuarioRepository usuarios,
    IEstudanteRepository estudantes,
    TenantAtual tenantAtual)
    : IRequestHandler<RegistrarAcessoCommand, Result<AcessoUsuario>>
{
    public async Task<Result<AcessoUsuario>> Handle(RegistrarAcessoCommand command, CancellationToken cancellationToken)
    {
        var email = Usuario.NormalizarEmail(command.Email);
        var perfil = Perfil.PorFormaDeLogin(command.LoginGoogle, email);
        if (perfil is null)
        {
            // Google com conta fora da UFERSA: autenticado, mas sem perfil (403 nas rotas).
            return Result.Ok(SemAcesso(MotivosSemAcesso.DominioNaoPermitido));
        }

        var usuario = await usuarios.ObterPorIdExternoAsync(command.IdExterno, cancellationToken);
        if (usuario is null)
        {
            // O mesmo e-mail não pode ter duas contas (Google e e-mail/senha).
            if (await usuarios.ExisteEmailAsync(email, cancellationToken))
            {
                return Result.Ok(SemAcesso(MotivosSemAcesso.EmailEmUso));
            }

            usuario = usuarios.Adicionar(new Usuario(Tenant.PadraoId, command.IdExterno, email, perfil, DateTime.UtcNow));
        }
        else
        {
            if (usuario.Email != email && await usuarios.ExisteEmailAsync(email, cancellationToken))
            {
                return Result.Ok(SemAcesso(MotivosSemAcesso.EmailEmUso));
            }

            // Só vira UPDATE se o e-mail ou o perfil mudaram.
            usuario.RegistrarAcesso(email, perfil);
        }

        await usuarios.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Daqui em diante (e no resto da requisição) as consultas ficam no tenant do usuário.
        tenantAtual.Definir(usuario.TenantId);

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

        return Result.Ok(new AcessoUsuario(
            usuario.Id, usuario.TenantId, perfil.Name, usuario.CadastroCompleto, estudanteId, null));
    }

    private static AcessoUsuario SemAcesso(string motivo) => new(null, null, null, false, null, motivo);
}

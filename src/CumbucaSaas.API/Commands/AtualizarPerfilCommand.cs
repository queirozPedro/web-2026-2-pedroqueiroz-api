using CumbucaSaas.API.PerfilUsuario;
using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>Foto enviada no PUT /profile (multipart/form-data).</summary>
public sealed record FotoEnviada(Stream Conteudo, string ContentType, long Tamanho);

/// <summary>
/// PUT /profile: o usuário logado altera nome completo e data de nascimento (mesmas
/// regras do cadastro) e, se mandar, a foto de perfil. A foto vai para o S3 e o perfil
/// completo para o DynamoDB; nome e data também são atualizados no Usuario (Postgres),
/// para o app mostrar o mesmo nome em todo lugar.
/// </summary>
public sealed record AtualizarPerfilCommand(
    string IdExterno,
    string Nome,
    DateOnly DataNascimento,
    FotoEnviada? Foto) : IRequest<Result>;

public sealed class AtualizarPerfilCommandValidator : AbstractValidator<AtualizarPerfilCommand>
{
    public AtualizarPerfilCommandValidator()
    {
        RuleFor(c => c.IdExterno).NotEmpty();
        RuleFor(c => c.Nome).NomeCompleto();
        RuleFor(c => c.DataNascimento).DataNascimento();

        When(c => c.Foto is not null, () =>
        {
            RuleFor(c => c.Foto!.ContentType)
                .Must(tipo => PerfilOptions.TiposFoto.ContainsKey(tipo))
                .OverridePropertyName("Foto")
                .WithMessage("A foto precisa ser JPG, PNG ou WebP.");

            RuleFor(c => c.Foto!.Tamanho)
                .InclusiveBetween(1, PerfilOptions.FotoTamanhoMaximo)
                .OverridePropertyName("Foto")
                .WithMessage($"A foto pode ter no máximo {PerfilOptions.FotoTamanhoMaximo / (1024 * 1024)} MB.");
        });
    }
}

public sealed class AtualizarPerfilCommandHandler(
    IUsuarioRepository usuarios,
    IPerfisUsuario perfis,
    IFotosPerfil fotos)
    : IRequestHandler<AtualizarPerfilCommand, Result>
{
    public async Task<Result> Handle(AtualizarPerfilCommand command, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.ObterPorIdExternoAsync(command.IdExterno, cancellationToken);
        if (usuario is null)
        {
            return Result.Fail(new NaoEncontradoError("Usuário não encontrado."));
        }

        var anterior = await perfis.ObterAsync(usuario.IdExterno, usuario.TenantId, cancellationToken);

        // A foto sobe primeiro; se o resto falhar, ela é apagada e nada fica pela metade.
        string? fotoNova = null;
        if (command.Foto is not null)
        {
            fotoNova = await fotos.SalvarAsync(
                usuario.IdExterno, command.Foto.Conteudo, command.Foto.ContentType, cancellationToken);
        }

        try
        {
            usuario.ConcluirCadastro(command.Nome, command.DataNascimento, HoraLocal.Hoje, DateTime.UtcNow);
            await usuarios.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            await perfis.SalvarAsync(
                new PerfilSalvo(
                    usuario.IdExterno,
                    usuario.TenantId,
                    usuario.Email,
                    usuario.Nome!,
                    usuario.DataNascimento!.Value,
                    usuario.Perfil.Name,
                    fotoNova ?? anterior?.FotoChave,
                    DateTime.UtcNow),
                cancellationToken);
        }
        catch
        {
            if (fotoNova is not null)
            {
                await fotos.RemoverAsync(fotoNova);
            }

            throw;
        }

        // A foto antiga só sai depois que a nova está gravada no perfil.
        if (fotoNova is not null && anterior?.FotoChave is { } fotoAntiga && fotoAntiga != fotoNova)
        {
            await fotos.RemoverAsync(fotoAntiga);
        }

        return Result.Ok();
    }
}

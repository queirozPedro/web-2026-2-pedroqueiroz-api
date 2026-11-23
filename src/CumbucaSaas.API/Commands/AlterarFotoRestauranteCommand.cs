using CumbucaSaas.API.PerfilUsuario;
using CumbucaSaas.API.Queries;
using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Domain.Aggregates.TenantAggregate;
using CumbucaSaas.Infrastructure;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>
/// PUT /api/restaurante/foto (com a foto) e DELETE /api/restaurante/foto (Foto nula): troca
/// ou remove a foto do restaurante. A foto vai para o bucket do perfil, em "tenants/{id}".
/// </summary>
public sealed record AlterarFotoRestauranteCommand(FotoEnviada? Foto) : IRequest<Result>;

public sealed class AlterarFotoRestauranteCommandValidator : AbstractValidator<AlterarFotoRestauranteCommand>
{
    public AlterarFotoRestauranteCommandValidator()
    {
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

public sealed class AlterarFotoRestauranteCommandHandler(
    ITenantRepository tenants,
    ITenantAtual tenantAtual,
    IFotosPerfil fotos,
    INotificadorTempoReal notificador)
    : IRequestHandler<AlterarFotoRestauranteCommand, Result>
{
    public async Task<Result> Handle(AlterarFotoRestauranteCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenants.ObterAsync(tenantAtual.Obrigatorio(), cancellationToken);
        if (tenant is null)
        {
            return Result.Fail(new NaoEncontradoError("Restaurante não encontrado."));
        }

        // A foto sobe primeiro; se a gravação no banco falhar, ela é apagada.
        string? fotoNova = null;
        if (command.Foto is not null)
        {
            fotoNova = await fotos.SalvarAsync(
                $"tenants/{tenant.Id}", command.Foto.Conteudo, command.Foto.ContentType, cancellationToken);
        }

        string? fotoAntiga;
        try
        {
            fotoAntiga = tenant.DefinirFoto(fotoNova);
            await tenants.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        catch
        {
            if (fotoNova is not null)
            {
                await fotos.RemoverAsync(fotoNova);
            }

            throw;
        }

        // A antiga só sai depois que a troca está gravada.
        if (fotoAntiga is not null && fotoAntiga != fotoNova)
        {
            await fotos.RemoverAsync(fotoAntiga);
        }

        await notificador.NotificarAsync("restaurante-atualizado", cancellationToken);

        return Result.Ok();
    }
}

using CumbucaSaas.API.Queries;
using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.Aggregates.TenantAggregate;
using CumbucaSaas.Domain.SeedWork;
using CumbucaSaas.Infrastructure;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>Turno servido num dia da semana. <paramref name="DiaSemana"/>: 0 = domingo ... 6 = sábado.</summary>
public sealed record HorarioFuncionamentoDados(int DiaSemana, string Turno, TimeOnly Abertura, TimeOnly Fechamento);

/// <summary>
/// PUT /api/restaurante: o funcionário altera os dados do restaurante (tenant) em que está:
/// nome, responsável, local, cores do app e horário de funcionamento da semana inteira
/// (dia sem horário = fechado).
/// </summary>
public sealed record AtualizarRestauranteCommand(
    string Nome,
    string NomeResponsavel,
    string? Endereco,
    string Cidade,
    string Uf,
    string CorPrincipal,
    string CorSecundaria,
    IReadOnlyList<HorarioFuncionamentoDados> Horarios) : IRequest<Result>;

public sealed class AtualizarRestauranteCommandValidator : AbstractValidator<AtualizarRestauranteCommand>
{
    public AtualizarRestauranteCommandValidator()
    {
        RuleFor(c => c.Nome).NotEmpty().WithMessage("Informe o nome do restaurante.")
            .MaximumLength(Tenant.NomeTamanhoMaximo);
        RuleFor(c => c.NomeResponsavel).NotEmpty().WithMessage("Informe o responsável.")
            .MaximumLength(Tenant.NomeResponsavelTamanhoMaximo);
        RuleFor(c => c.Endereco).MaximumLength(Tenant.EnderecoTamanhoMaximo);
        RuleFor(c => c.Cidade).NotEmpty().WithMessage("Informe a cidade.")
            .MaximumLength(Tenant.CidadeTamanhoMaximo);
        RuleFor(c => c.Uf).Must(Tenant.UfValida).WithMessage("Informe uma UF válida (ex.: RN).");

        RuleFor(c => c.CorPrincipal).Cor("principal");
        RuleFor(c => c.CorSecundaria).Cor("secundária");

        RuleFor(c => c.Horarios).NotNull().WithMessage("Informe o horário de funcionamento.");
        RuleForEach(c => c.Horarios).ChildRules(horario =>
        {
            horario.RuleFor(h => h.DiaSemana).InclusiveBetween(0, 6)
                .WithMessage("Dia da semana inválido (0 = domingo ... 6 = sábado).");
            horario.RuleFor(h => h.Turno)
                .Must(turno => Enumeration.TryFromName<Turno>(turno, out _))
                .WithMessage(h => $"Turno '{h.Turno}' inválido. Use: {string.Join(", ", Enumeration.GetAll<Turno>())}.");
            horario.RuleFor(h => h.Fechamento).GreaterThan(h => h.Abertura)
                .WithMessage("O fechamento do turno deve ser depois da abertura.");
        });

        // Regras entre os horários (um almoço e uma janta por dia, sem sobrepor), quando
        // cada horário já é válido sozinho.
        RuleFor(c => c.Horarios)
            .Must(horarios => Tenant.ErroNosHorarios(Converter(horarios)) is null)
            .When(c => c.Horarios is not null && c.Horarios.All(HorarioValido))
            .WithMessage(c => Tenant.ErroNosHorarios(Converter(c.Horarios))!);
    }

    private static bool HorarioValido(HorarioFuncionamentoDados h) =>
        h.DiaSemana is >= 0 and <= 6
        && Enumeration.TryFromName<Turno>(h.Turno, out _)
        && h.Fechamento > h.Abertura;

    internal static List<HorarioFuncionamento> Converter(IEnumerable<HorarioFuncionamentoDados> horarios) =>
        horarios
            .Select(h => new HorarioFuncionamento(
                (DayOfWeek)h.DiaSemana, Enumeration.FromName<Turno>(h.Turno), h.Abertura, h.Fechamento))
            .ToList();
}

internal static class RegrasRestaurante
{
    public static IRuleBuilderOptions<T, string> Cor<T>(this IRuleBuilder<T, string> regra, string qual) =>
        regra
            .Must(Tenant.CorValida)
            .WithMessage($"A cor {qual} precisa estar no formato #RRGGBB.")
            // Só avalia o contraste quando o formato está certo (sem repetir o erro de formato).
            .Must(cor => !Tenant.CorValida(cor) || Tenant.ContrasteSuficiente(cor))
            .WithMessage(
                $"A cor {qual} é clara demais: o texto branco sobre ela precisa de contraste de pelo menos " +
                $"{Tenant.ContrasteMinimoComBranco:0.#}:1. Escolha um tom mais escuro.");
}

public sealed class AtualizarRestauranteCommandHandler(
    ITenantRepository tenants,
    ITenantAtual tenantAtual,
    INotificadorTempoReal notificador)
    : IRequestHandler<AtualizarRestauranteCommand, Result>
{
    public async Task<Result> Handle(AtualizarRestauranteCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenants.ObterAsync(tenantAtual.Obrigatorio(), cancellationToken);
        if (tenant is null)
        {
            return Result.Fail(new NaoEncontradoError("Restaurante não encontrado."));
        }

        tenant.AtualizarDados(command.Nome, command.NomeResponsavel, command.Endereco, command.Cidade, command.Uf);
        tenant.DefinirCores(command.CorPrincipal, command.CorSecundaria);
        tenant.DefinirHorarios(AtualizarRestauranteCommandValidator.Converter(command.Horarios));
        await tenants.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        await notificador.NotificarAsync("restaurante-atualizado", cancellationToken);

        return Result.Ok();
    }
}

using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.Aggregates.ConsumoAggregate;
using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.SeedWork;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>
/// Funcionário registra uma refeição servida no ponto de venda. O valor pago é
/// calculado pelo cardápio do turno; o horário, se não vier, é o atual (Brasília).
/// Devolve o Id criado.
/// </summary>
public sealed record RegistrarConsumoCommand(
    int CalendarioRefeicaoId,
    string TipoCliente,
    int? EstudanteId,
    string? FormaPagamento,
    string? FormaIdentificacao,
    TimeOnly? Horario) : IRequest<Result<int>>;

public sealed class RegistrarConsumoCommandValidator : AbstractValidator<RegistrarConsumoCommand>
{
    public RegistrarConsumoCommandValidator()
    {
        RuleFor(c => c.CalendarioRefeicaoId).GreaterThan(0);

        RuleFor(c => c.TipoCliente)
            .NotEmpty()
            .Must(tipo => Enumeration.TryFromName<TipoCliente>(tipo, out _))
            .WithMessage(c =>
                $"Tipo de cliente '{c.TipoCliente}' inválido. Use: {string.Join(", ", Enumeration.GetAll<TipoCliente>())}.");

        RuleFor(c => c.FormaPagamento)
            .Must(forma => Enumeration.TryFromName<FormaPagamento>(forma, out _))
            .When(c => !string.IsNullOrEmpty(c.FormaPagamento))
            .WithMessage(c =>
                $"Forma de pagamento '{c.FormaPagamento}' inválida. Use: {string.Join(", ", Enumeration.GetAll<FormaPagamento>())}.");

        RuleFor(c => c.FormaIdentificacao)
            .Must(forma => Enumeration.TryFromName<FormaIdentificacao>(forma, out _))
            .When(c => !string.IsNullOrEmpty(c.FormaIdentificacao))
            .WithMessage(c =>
                $"Forma de identificação '{c.FormaIdentificacao}' inválida. Use: {string.Join(", ", Enumeration.GetAll<FormaIdentificacao>())}.");

        When(c => EhTipo(c.TipoCliente, TipoCliente.Estudante), () =>
        {
            RuleFor(c => c.EstudanteId)
                .NotNull().WithMessage("Informe o estudante.")
                .GreaterThan(0);
        });

        When(c => EhTipo(c.TipoCliente, TipoCliente.Externo), () =>
        {
            RuleFor(c => c.EstudanteId).Null().WithMessage("Cliente externo não tem estudante.");
            RuleFor(c => c.FormaPagamento).NotEmpty().WithMessage("Informe a forma de pagamento (dinheiro ou pix).");
        });
    }

    private static bool EhTipo(string? nome, TipoCliente tipo) =>
        Enumeration.TryFromName<TipoCliente>(nome, out var informado) && informado.Id == tipo.Id;
}

public sealed class RegistrarConsumoCommandHandler(
    IRegistroConsumoRepository consumos,
    ICalendarioRefeicaoRepository calendarios,
    IEstudanteRepository estudantes,
    INotificadorTempoReal notificador)
    : IRequestHandler<RegistrarConsumoCommand, Result<int>>
{
    public async Task<Result<int>> Handle(RegistrarConsumoCommand command, CancellationToken cancellationToken)
    {
        var turno = await calendarios.ObterAsync(command.CalendarioRefeicaoId, cancellationToken);
        if (turno is null)
        {
            return Result.Fail<int>(new NaoEncontradoError(
                $"Turno {command.CalendarioRefeicaoId} não encontrado no calendário."));
        }

        var tipo = Enumeration.FromName<TipoCliente>(command.TipoCliente);
        var horario = command.Horario ?? HoraLocal.HorarioAtual;
        FormaPagamento? formaPagamento = string.IsNullOrEmpty(command.FormaPagamento)
            ? null
            : Enumeration.FromName<FormaPagamento>(command.FormaPagamento);

        RegistroConsumo consumo;
        try
        {
            if (tipo.Id == TipoCliente.Estudante.Id)
            {
                var estudante = await estudantes.ObterAsync(command.EstudanteId!.Value, cancellationToken);
                if (estudante is null)
                {
                    return Result.Fail<int>(new NaoEncontradoError($"Estudante {command.EstudanteId} não encontrado."));
                }

                var identificacao = string.IsNullOrEmpty(command.FormaIdentificacao)
                    ? FormaIdentificacao.Matricula
                    : Enumeration.FromName<FormaIdentificacao>(command.FormaIdentificacao);

                consumo = RegistroConsumo.DeEstudante(turno, estudante, horario, formaPagamento, identificacao);
            }
            else
            {
                consumo = RegistroConsumo.DeExterno(turno, horario, formaPagamento);
            }
        }
        catch (DomainException erro)
        {
            // Regras que dependem do estado (sem cardápio, fora do horário, estudante
            // inativo...) ficam no domínio; aqui viram 409 com a mensagem dele.
            return Result.Fail<int>(new ConflitoError(erro.Message));
        }

        consumos.Adicionar(consumo);
        await consumos.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        await notificador.NotificarAsync("dashboard-atualizado", cancellationToken);

        return Result.Ok(consumo.Id);
    }
}

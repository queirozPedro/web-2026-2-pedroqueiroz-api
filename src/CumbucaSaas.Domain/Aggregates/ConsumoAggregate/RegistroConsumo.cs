using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.ConsumoAggregate;

/// <summary>
/// Uma refeição servida no ponto de venda. É a base dos indicadores: contagem de
/// servidos, estudantes x externos, fluxo por hora e valor estimado.
/// O valor pago não vem de fora: sai do cardápio do turno conforme o tipo de cliente.
/// A data é a do turno (CalendarioRefeicao), por isso só o horário fica aqui.
/// </summary>
public class RegistroConsumo : AggregateRoot
{
    // Usado pelo EF Core.
    private RegistroConsumo()
    {
        TipoCliente = null!;
        FormaIdentificacao = null!;
    }

    private RegistroConsumo(
        int calendarioRefeicaoId,
        TipoCliente tipoCliente,
        int? estudanteId,
        TimeOnly horario,
        decimal valorPago,
        FormaPagamento? formaPagamento,
        FormaIdentificacao formaIdentificacao)
    {
        CalendarioRefeicaoId = calendarioRefeicaoId;
        TipoCliente = tipoCliente;
        EstudanteId = estudanteId;
        Horario = horario;
        ValorPago = valorPago;
        FormaPagamento = formaPagamento;
        FormaIdentificacao = formaIdentificacao;
    }

    public int CalendarioRefeicaoId { get; private set; }

    public TipoCliente TipoCliente { get; private set; }

    /// <summary>Nulo para o público externo.</summary>
    public int? EstudanteId { get; private set; }

    public TimeOnly Horario { get; private set; }

    public decimal ValorPago { get; private set; }

    /// <summary>Nula quando não houve pagamento (estudante isento).</summary>
    public FormaPagamento? FormaPagamento { get; private set; }

    public FormaIdentificacao FormaIdentificacao { get; private set; }

    /// <summary>Estudante ativo, identificado por QR Code ou matrícula; paga o subsidiado (ou nada, se isento).</summary>
    public static RegistroConsumo DeEstudante(
        CalendarioRefeicao turno,
        Estudante estudante,
        TimeOnly horario,
        FormaPagamento? formaPagamento,
        FormaIdentificacao formaIdentificacao)
    {
        var cardapio = ExigirCardapio(turno);
        ExigirHorarioDoTurno(turno, horario);

        if (!estudante.EstaAtivo)
        {
            throw new DomainException(
                "Estudante com vínculo inativo não consome com preço subsidiado; registre como externo.");
        }

        if (formaIdentificacao.Id == FormaIdentificacao.Balcao.Id)
        {
            throw new DomainException("Estudante é identificado por QR Code ou matrícula.");
        }

        var valor = estudante.TipoSubsidio.Id == TipoSubsidio.Isento.Id ? 0m : cardapio.PrecoSubsidiado;

        return new RegistroConsumo(
            turno.Id,
            TipoCliente.Estudante,
            estudante.Id,
            horario,
            valor,
            ExigirFormaPagamento(valor, formaPagamento),
            formaIdentificacao);
    }

    /// <summary>Público externo: sem cadastro, identificado no balcão, paga o preço integral.</summary>
    public static RegistroConsumo DeExterno(CalendarioRefeicao turno, TimeOnly horario, FormaPagamento? formaPagamento)
    {
        var cardapio = ExigirCardapio(turno);
        ExigirHorarioDoTurno(turno, horario);

        return new RegistroConsumo(
            turno.Id,
            TipoCliente.Externo,
            null,
            horario,
            cardapio.PrecoIntegral,
            ExigirFormaPagamento(cardapio.PrecoIntegral, formaPagamento),
            FormaIdentificacao.Balcao);
    }

    private static Cardapio ExigirCardapio(CalendarioRefeicao turno) =>
        turno.Cardapio ?? throw new DomainException("O turno ainda não tem cardápio (preços) cadastrado.");

    private static void ExigirHorarioDoTurno(CalendarioRefeicao turno, TimeOnly horario)
    {
        if (horario < turno.Abertura || horario > turno.Fechamento)
        {
            throw new DomainException(
                $"Horário {horario:HH\\:mm} fora do funcionamento do turno ({turno.Abertura:HH\\:mm} às {turno.Fechamento:HH\\:mm}).");
        }
    }

    /// <summary>Com valor a pagar, a forma é obrigatória; sem valor (isento), não se aplica.</summary>
    private static FormaPagamento? ExigirFormaPagamento(decimal valor, FormaPagamento? formaPagamento)
    {
        if (valor == 0)
        {
            return null;
        }

        return formaPagamento ?? throw new DomainException("Informe a forma de pagamento (dinheiro ou pix).");
    }
}

using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.TenantAggregate;

/// <summary>
/// Horário em que o restaurante serve um turno (almoço ou janta) num dia da semana.
/// Dia sem horário = restaurante fechado nesse dia. Faz parte do Tenant (tabela própria).
/// </summary>
public sealed class HorarioFuncionamento
{
    // Usado pelo EF Core.
    private HorarioFuncionamento()
    {
        Turno = null!;
    }

    public HorarioFuncionamento(DayOfWeek diaSemana, Turno turno, TimeOnly abertura, TimeOnly fechamento)
    {
        if (!Enum.IsDefined(diaSemana))
        {
            throw new DomainException("Dia da semana inválido.");
        }

        if (fechamento <= abertura)
        {
            throw new DomainException("O fechamento do turno deve ser depois da abertura.");
        }

        DiaSemana = diaSemana;
        Turno = turno ?? throw new DomainException("O turno é obrigatório.");
        Abertura = abertura;
        Fechamento = fechamento;
    }

    /// <summary>0 = domingo ... 6 = sábado.</summary>
    public DayOfWeek DiaSemana { get; private set; }

    public Turno Turno { get; private set; }

    public TimeOnly Abertura { get; private set; }

    public TimeOnly Fechamento { get; private set; }

    /// <summary>Mesmo dia e turno (a chave do horário dentro do tenant).</summary>
    internal bool MesmoTurno(HorarioFuncionamento outro) =>
        DiaSemana == outro.DiaSemana && Turno.Id == outro.Turno.Id;

    internal void AlterarHorario(TimeOnly abertura, TimeOnly fechamento)
    {
        Abertura = abertura;
        Fechamento = fechamento;
    }
}

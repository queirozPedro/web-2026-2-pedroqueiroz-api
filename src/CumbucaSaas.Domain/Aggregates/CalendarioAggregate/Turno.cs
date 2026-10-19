using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.CalendarioAggregate;

public sealed class Turno : Enumeration
{
    public static readonly Turno Almoco = new(1, "almoco");
    public static readonly Turno Janta = new(2, "janta");

    private Turno(int id, string name) : base(id, name)
    {
    }
}

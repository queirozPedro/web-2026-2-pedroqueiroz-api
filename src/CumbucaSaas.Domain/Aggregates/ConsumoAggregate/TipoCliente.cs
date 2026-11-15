using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.ConsumoAggregate;

/// <summary>Estudante paga o preço subsidiado (ou nada, se isento); externo paga o integral.</summary>
public sealed class TipoCliente : Enumeration
{
    public static readonly TipoCliente Estudante = new(1, "estudante");
    public static readonly TipoCliente Externo = new(2, "externo");

    private TipoCliente(int id, string name) : base(id, name)
    {
    }
}

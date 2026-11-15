using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.ConsumoAggregate;

public sealed class FormaPagamento : Enumeration
{
    public static readonly FormaPagamento Dinheiro = new(1, "dinheiro");
    public static readonly FormaPagamento Pix = new(2, "pix");

    private FormaPagamento(int id, string name) : base(id, name)
    {
    }
}

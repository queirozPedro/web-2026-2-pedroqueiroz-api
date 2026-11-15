using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.EstudanteAggregate;

/// <summary>Situação do estudante na universidade; só o ativo consome com preço subsidiado.</summary>
public sealed class Vinculo : Enumeration
{
    public static readonly Vinculo Ativo = new(1, "ativo");
    public static readonly Vinculo Inativo = new(2, "inativo");

    private Vinculo(int id, string name) : base(id, name)
    {
    }
}

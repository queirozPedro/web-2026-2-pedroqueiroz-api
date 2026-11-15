using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.EstudanteAggregate;

/// <summary>
/// Como o estudante paga a refeição: "padrao" paga o preço subsidiado do cardápio;
/// "isento" tem gratuidade (ex.: auxílio alimentação) e não paga.
/// </summary>
public sealed class TipoSubsidio : Enumeration
{
    public static readonly TipoSubsidio Padrao = new(1, "padrao");
    public static readonly TipoSubsidio Isento = new(2, "isento");

    private TipoSubsidio(int id, string name) : base(id, name)
    {
    }
}

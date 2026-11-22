using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.TenantAggregate;

/// <summary>
/// Restaurante atendido pelo sistema. Por enquanto existe um só (gravado pela
/// migration) e todo usuário que entra faz parte dele.
/// </summary>
public class Tenant : AggregateRoot
{
    public const int NomeTamanhoMaximo = 150;
    public const int NomeResponsavelTamanhoMaximo = 150;

    /// <summary>Tenant em que todo usuário novo entra, até existir a divisão entre tenants.</summary>
    public const int PadraoId = 1;

    // Usado pelo EF Core.
    private Tenant()
    {
        Nome = null!;
        NomeResponsavel = null!;
    }

    public string Nome { get; private set; }

    public string NomeResponsavel { get; private set; }
}

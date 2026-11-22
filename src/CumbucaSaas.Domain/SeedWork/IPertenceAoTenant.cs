namespace CumbucaSaas.Domain.SeedWork;

/// <summary>
/// Agregado separado por tenant (restaurante). O TenantId é preenchido pelo
/// CumbucaDbContext ao gravar um registro novo, a partir do tenant do usuário logado,
/// e todas as leituras filtram por ele.
/// </summary>
public interface IPertenceAoTenant
{
    int TenantId { get; }
}

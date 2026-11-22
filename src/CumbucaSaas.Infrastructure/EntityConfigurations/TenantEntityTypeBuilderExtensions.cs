using CumbucaSaas.Domain.Aggregates.TenantAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CumbucaSaas.Infrastructure.EntityConfigurations;

internal static class TenantEntityTypeBuilderExtensions
{
    /// <summary>
    /// Coluna "TenantId" obrigatória, com chave estrangeira Restrict para "Tenants".
    /// O filtro por tenant fica no CumbucaDbContext (depende do usuário da requisição).
    /// </summary>
    public static void PertenceAoTenant<T>(this EntityTypeBuilder<T> builder)
        where T : class, IPertenceAoTenant
    {
        builder.Property<int>(nameof(IPertenceAoTenant.TenantId)).IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(nameof(IPertenceAoTenant.TenantId))
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using CumbucaSaas.Domain.Aggregates.TenantAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CumbucaSaas.Infrastructure.EntityConfigurations;

/// <summary>Tabela "Tenants", com o primeiro tenant gravado pela migration (HasData).</summary>
internal sealed class TenantEntityTypeConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);
        builder.Ignore(t => t.DomainEvents);

        builder.Property(t => t.Nome).HasMaxLength(Tenant.NomeTamanhoMaximo).IsRequired();
        builder.Property(t => t.NomeResponsavel).HasMaxLength(Tenant.NomeResponsavelTamanhoMaximo).IsRequired();

        // O responsável é provisório (o dado real ainda não está disponível).
        builder.HasData(new
        {
            Id = Tenant.PadraoId,
            Nome = "Restaurante Universitário: Ufersa - Campus Pau dos Ferros",
            NomeResponsavel = "Pedro Andrade"
        });
    }
}

using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CumbucaSaas.Infrastructure.EntityConfigurations;

/// <summary>Tabela "Usuarios", com o id do Cognito (sub) e o e-mail únicos.</summary>
internal sealed class UsuarioEntityTypeConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);
        builder.Ignore(u => u.DomainEvents);

        builder.Property(u => u.IdExterno).HasMaxLength(Usuario.IdExternoTamanhoMaximo).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(Usuario.EmailTamanhoMaximo).IsRequired();
        builder.Property(u => u.Nome).HasMaxLength(Usuario.NomeTamanhoMaximo).IsRequired();
        builder.Property(u => u.CriadoEm).IsRequired();

        // Enumeration é gravada pelo Id.
        builder.Property(u => u.Perfil)
            .HasColumnName("PerfilId")
            .HasConversion(p => p.Id, id => Enumeration.FromValue<Perfil>(id))
            .IsRequired();

        builder.HasIndex(u => u.IdExterno).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

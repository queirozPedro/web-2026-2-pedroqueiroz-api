using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CumbucaSaas.Infrastructure.EntityConfigurations;

/// <summary>Tabela "Estudantes", com matrícula e e-mail únicos no tenant.</summary>
internal sealed class EstudanteEntityTypeConfiguration : IEntityTypeConfiguration<Estudante>
{
    public void Configure(EntityTypeBuilder<Estudante> builder)
    {
        builder.ToTable("Estudantes");
        builder.HasKey(e => e.Id);
        builder.Ignore(e => e.DomainEvents);
        builder.PertenceAoTenant();
        builder.Ignore(e => e.EstaAtivo);

        builder.Property(e => e.Nome).HasMaxLength(Estudante.NomeTamanhoMaximo).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(Estudante.EmailTamanhoMaximo).IsRequired();
        builder.Property(e => e.Matricula).HasMaxLength(Estudante.MatriculaTamanhoMaximo).IsRequired();
        builder.Property(e => e.Curso).HasMaxLength(Estudante.CursoTamanhoMaximo).IsRequired();

        // Enumerations são gravadas pelo Id.
        builder.Property(e => e.Vinculo)
            .HasColumnName("VinculoId")
            .HasConversion(v => v.Id, id => Enumeration.FromValue<Vinculo>(id))
            .IsRequired();

        builder.Property(e => e.TipoSubsidio)
            .HasColumnName("TipoSubsidioId")
            .HasConversion(t => t.Id, id => Enumeration.FromValue<TipoSubsidio>(id))
            .IsRequired();

        // Matrícula e e-mail não se repetem dentro do tenant.
        builder.HasIndex(e => new { e.TenantId, e.Matricula }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();

        // 1 para 1 com o usuário do login (opcional). O índice único aceita vários nulos.
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.UsuarioId).IsUnique();
    }
}

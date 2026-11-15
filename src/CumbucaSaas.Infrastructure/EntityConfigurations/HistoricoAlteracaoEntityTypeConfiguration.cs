using CumbucaSaas.Domain.Aggregates.AuditoriaAggregate;
using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CumbucaSaas.Infrastructure.EntityConfigurations;

/// <summary>Tabela "HistoricoAlteracoes" (auditoria). Os valores ficam em jsonb.</summary>
internal sealed class HistoricoAlteracaoEntityTypeConfiguration : IEntityTypeConfiguration<HistoricoAlteracao>
{
    public void Configure(EntityTypeBuilder<HistoricoAlteracao> builder)
    {
        builder.ToTable("HistoricoAlteracoes");
        builder.HasKey(h => h.Id);
        builder.Ignore(h => h.DomainEvents);

        builder.Property(h => h.Tabela).HasMaxLength(HistoricoAlteracao.TabelaTamanhoMaximo).IsRequired();
        builder.Property(h => h.DataHora).IsRequired();
        builder.Property(h => h.ValorAnterior).HasColumnType("jsonb");
        builder.Property(h => h.ValorNovo).HasColumnType("jsonb");

        // Enumeration é gravada pelo Id.
        builder.Property(h => h.Acao)
            .HasColumnName("AcaoId")
            .HasConversion(a => a.Id, id => Enumeration.FromValue<AcaoAuditoria>(id))
            .IsRequired();

        // Restrict: o histórico não some se alguém tentar apagar o usuário.
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => h.DataHora);
    }
}

using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.Aggregates.ConsumoAggregate;
using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CumbucaSaas.Infrastructure.EntityConfigurations;

/// <summary>
/// Tabela "RegistrosConsumo". Turno e estudante são outros agregados: só a chave
/// estrangeira (sem navegação), com Restrict para não apagar histórico em cascata.
/// </summary>
internal sealed class RegistroConsumoEntityTypeConfiguration : IEntityTypeConfiguration<RegistroConsumo>
{
    public void Configure(EntityTypeBuilder<RegistroConsumo> builder)
    {
        builder.ToTable("RegistrosConsumo");
        builder.HasKey(r => r.Id);
        builder.Ignore(r => r.DomainEvents);
        builder.PertenceAoTenant();

        builder.Property(r => r.Horario).IsRequired();
        builder.Property(r => r.ValorPago).HasPrecision(10, 2).IsRequired();

        // Enumerations são gravadas pelo Id.
        builder.Property(r => r.TipoCliente)
            .HasColumnName("TipoClienteId")
            .HasConversion(t => t.Id, id => Enumeration.FromValue<TipoCliente>(id))
            .IsRequired();

        builder.Property(r => r.FormaPagamento)
            .HasColumnName("FormaPagamentoId")
            .HasConversion(f => f!.Id, id => Enumeration.FromValue<FormaPagamento>(id))
            .IsRequired(false);

        builder.Property(r => r.FormaIdentificacao)
            .HasColumnName("FormaIdentificacaoId")
            .HasConversion(f => f.Id, id => Enumeration.FromValue<FormaIdentificacao>(id))
            .IsRequired();

        builder.HasOne<CalendarioRefeicao>()
            .WithMany()
            .HasForeignKey(r => r.CalendarioRefeicaoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Estudante>()
            .WithMany()
            .HasForeignKey(r => r.EstudanteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CumbucaSaas.Infrastructure.EntityConfigurations;

/// <summary>Tabelas "Feedbacks" e "Avaliacoes" (avaliações fazem parte do agregado Feedback).</summary>
internal sealed class FeedbackEntityTypeConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedbacks");
        builder.HasKey(f => f.Id);
        builder.Ignore(f => f.DomainEvents);

        builder.Property(f => f.EstudanteId).IsRequired();
        builder.Property(f => f.Data).IsRequired();
        builder.Property(f => f.Categoria).HasMaxLength(60).IsRequired();
        builder.Property(f => f.Comentario).HasMaxLength(2000).IsRequired();

        // Enumeration é gravada pelo Id.
        builder.Property(f => f.Status)
            .HasColumnName("StatusId")
            .HasConversion(s => s.Id, id => Enumeration.FromValue<StatusFeedback>(id))
            .IsRequired();

        builder.OwnsMany(f => f.Avaliacoes, avaliacao =>
        {
            avaliacao.ToTable("Avaliacoes");
            avaliacao.WithOwner().HasForeignKey("FeedbackId");
            avaliacao.HasKey(a => a.Id);
            avaliacao.Ignore(a => a.DomainEvents);

            avaliacao.Property(a => a.Categoria)
                .HasColumnName("CategoriaId")
                .HasConversion(c => c.Id, id => Enumeration.FromValue<CategoriaAvaliacao>(id))
                .IsRequired();
            avaliacao.Property(a => a.Nota).IsRequired();
        });

        // A coleção pública é somente leitura; o EF escreve no campo _avaliacoes.
        builder.Navigation(f => f.Avaliacoes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

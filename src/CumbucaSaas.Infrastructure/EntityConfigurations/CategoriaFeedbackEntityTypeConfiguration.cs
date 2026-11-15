using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CumbucaSaas.Infrastructure.EntityConfigurations;

/// <summary>Tabela "CategoriasFeedback", já preenchida pela migration (HasData).</summary>
internal sealed class CategoriaFeedbackEntityTypeConfiguration : IEntityTypeConfiguration<CategoriaFeedback>
{
    public void Configure(EntityTypeBuilder<CategoriaFeedback> builder)
    {
        builder.ToTable("CategoriasFeedback");
        builder.HasKey(c => c.Id);
        builder.Ignore(c => c.DomainEvents);

        builder.Property(c => c.Nome).HasMaxLength(CategoriaFeedback.NomeTamanhoMaximo).IsRequired();
        builder.Property(c => c.Descricao).HasMaxLength(CategoriaFeedback.DescricaoTamanhoMaximo).IsRequired();
        builder.HasIndex(c => c.Nome).IsUnique();

        // Os nomes batem com o tipo CategoriaFeedback do front (dashboard.models.ts).
        builder.HasData(
            new { Id = 1, Nome = "Cardápio", Descricao = "Variedade, sabor e opções do cardápio." },
            new { Id = 2, Nome = "Filas e espera", Descricao = "Tempo de fila e organização da entrada." },
            new { Id = 3, Nome = "Atendimento", Descricao = "Educação e agilidade da equipe e reposição da linha." },
            new { Id = 4, Nome = "Higiene", Descricao = "Limpeza de mesas, talheres e salão." },
            new { Id = 5, Nome = "Estrutura", Descricao = "Mesas, ventilação e espaço do salão." },
            new { Id = 6, Nome = "Preço", Descricao = "Valor cobrado de estudantes e do público externo." });
    }
}

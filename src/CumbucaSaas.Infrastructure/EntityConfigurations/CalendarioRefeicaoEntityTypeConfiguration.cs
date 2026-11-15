using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CumbucaSaas.Infrastructure.EntityConfigurations;

/// <summary>
/// Tabelas "CalendarioRefeicoes", "Cardapios", "Producoes" e "FechamentosCaixa":
/// cardápio, produção e caixa fazem parte do agregado (1 para 1 com o turno).
/// </summary>
internal sealed class CalendarioRefeicaoEntityTypeConfiguration : IEntityTypeConfiguration<CalendarioRefeicao>
{
    public void Configure(EntityTypeBuilder<CalendarioRefeicao> builder)
    {
        builder.ToTable("CalendarioRefeicoes");
        builder.HasKey(c => c.Id);
        builder.Ignore(c => c.DomainEvents);

        builder.Property(c => c.Data).IsRequired();
        builder.Property(c => c.Abertura).IsRequired();
        builder.Property(c => c.Fechamento).IsRequired();
        builder.Property(c => c.Observacoes).HasMaxLength(CalendarioRefeicao.ObservacoesTamanhoMaximo);

        // Enumeration é gravada pelo Id.
        builder.Property(c => c.Turno)
            .HasColumnName("TurnoId")
            .HasConversion(t => t.Id, id => Enumeration.FromValue<Turno>(id))
            .IsRequired();

        // Um único almoço e uma única janta por data.
        builder.HasIndex(c => new { c.Data, c.Turno }).IsUnique();

        // Tabela própria, com a chave primária igual à chave estrangeira (1 para 1).
        builder.OwnsOne(c => c.Cardapio, cardapio =>
        {
            cardapio.ToTable("Cardapios");
            cardapio.WithOwner().HasForeignKey("CalendarioRefeicaoId");

            cardapio.Property(p => p.Itens).HasMaxLength(Cardapio.ItensTamanhoMaximo).IsRequired();
            cardapio.Property(p => p.CategoriaNutricional)
                .HasMaxLength(Cardapio.CategoriaNutricionalTamanhoMaximo)
                .IsRequired();
            cardapio.Property(p => p.PrecoSubsidiado).HasPrecision(10, 2).IsRequired();
            cardapio.Property(p => p.PrecoIntegral).HasPrecision(10, 2).IsRequired();
        });

        builder.OwnsOne(c => c.Producao, producao =>
        {
            producao.ToTable("Producoes");
            producao.WithOwner().HasForeignKey("CalendarioRefeicaoId");

            producao.Property(p => p.QuantidadeProduzida).IsRequired();
            producao.Property(p => p.Observacoes).HasMaxLength(Producao.ObservacoesTamanhoMaximo);
        });

        // "Pagamentos/Caixa" da especificação.
        builder.OwnsOne(c => c.FechamentoCaixa, caixa =>
        {
            caixa.ToTable("FechamentosCaixa");
            caixa.WithOwner().HasForeignKey("CalendarioRefeicaoId");

            caixa.Property(f => f.ValorDinheiro).HasPrecision(10, 2).IsRequired();
            caixa.Property(f => f.ValorPix).HasPrecision(10, 2).IsRequired();
            caixa.Ignore(f => f.ValorTotal);
        });

        // Calculada a partir de Producao e FechamentoCaixa.
        builder.Ignore(c => c.PossuiMovimento);
    }
}

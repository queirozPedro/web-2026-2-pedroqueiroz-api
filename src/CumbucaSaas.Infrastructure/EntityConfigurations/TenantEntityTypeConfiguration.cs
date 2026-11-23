using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.Aggregates.TenantAggregate;
using CumbucaSaas.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CumbucaSaas.Infrastructure.EntityConfigurations;

/// <summary>
/// Tabelas "Tenants" e "HorariosFuncionamento" (parte do agregado), com o primeiro
/// tenant e o seu horário gravados pela migration (HasData).
/// </summary>
internal sealed class TenantEntityTypeConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);
        builder.Ignore(t => t.DomainEvents);

        builder.Property(t => t.Nome).HasMaxLength(Tenant.NomeTamanhoMaximo).IsRequired();
        builder.Property(t => t.NomeResponsavel).HasMaxLength(Tenant.NomeResponsavelTamanhoMaximo).IsRequired();
        builder.Property(t => t.Endereco).HasMaxLength(Tenant.EnderecoTamanhoMaximo);
        builder.Property(t => t.Cidade).HasMaxLength(Tenant.CidadeTamanhoMaximo).IsRequired();
        builder.Property(t => t.Uf).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property(t => t.CorPrincipal).HasMaxLength(7).IsFixedLength().IsRequired();
        builder.Property(t => t.CorSecundaria).HasMaxLength(7).IsFixedLength().IsRequired();
        builder.Property(t => t.FotoChave).HasMaxLength(Tenant.FotoChaveTamanhoMaximo);

        // O responsável é provisório (o dado real ainda não está disponível). As cores são
        // as do tema original do app.
        builder.HasData(new
        {
            Id = Tenant.PadraoId,
            Nome = "Restaurante Universitário: Ufersa - Campus Pau dos Ferros",
            NomeResponsavel = "Pedro Andrade",
            Cidade = "Pau dos Ferros",
            Uf = "RN",
            CorPrincipal = "#1b4535",
            CorSecundaria = "#c2571e"
        });

        // Um almoço e uma janta por dia da semana em cada tenant: a chave é tenant + dia + turno.
        builder.OwnsMany(t => t.Horarios, horario =>
        {
            horario.ToTable("HorariosFuncionamento");
            horario.WithOwner().HasForeignKey("TenantId");
            horario.HasKey("TenantId", nameof(HorarioFuncionamento.DiaSemana), nameof(HorarioFuncionamento.Turno));

            horario.Property(h => h.DiaSemana).IsRequired();
            horario.Property(h => h.Abertura).IsRequired();
            horario.Property(h => h.Fechamento).IsRequired();
            horario.Property(h => h.Turno)
                .HasColumnName("TurnoId")
                .HasConversion(t => t.Id, id => Enumeration.FromValue<Turno>(id))
                .IsRequired();

            // Horário provisório: segunda a sexta, almoço e janta (corrigir pela tela Restaurante).
            horario.HasData(
                Enumerable.Range((int)DayOfWeek.Monday, 5).SelectMany(dia => new object[]
                {
                    new
                    {
                        TenantId = Tenant.PadraoId,
                        DiaSemana = (DayOfWeek)dia,
                        Turno = Turno.Almoco,
                        Abertura = new TimeOnly(11, 0),
                        Fechamento = new TimeOnly(13, 30)
                    },
                    new
                    {
                        TenantId = Tenant.PadraoId,
                        DiaSemana = (DayOfWeek)dia,
                        Turno = Turno.Janta,
                        Abertura = new TimeOnly(17, 0),
                        Fechamento = new TimeOnly(19, 0)
                    }
                }).ToArray());
        });
        builder.Navigation(t => t.Horarios).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

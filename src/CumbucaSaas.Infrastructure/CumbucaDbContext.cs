using System.Text.Json;
using CumbucaSaas.Domain.Aggregates.AuditoriaAggregate;
using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.Aggregates.ConsumoAggregate;
using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using CumbucaSaas.Infrastructure.EntityConfigurations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CumbucaSaas.Infrastructure;

public class CumbucaDbContext(
    DbContextOptions<CumbucaDbContext> options,
    IMediator mediator,
    IUsuarioAtual usuarioAtual)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Feedback> Feedbacks => Set<Feedback>();

    public DbSet<CategoriaFeedback> CategoriasFeedback => Set<CategoriaFeedback>();

    public DbSet<CalendarioRefeicao> CalendarioRefeicoes => Set<CalendarioRefeicao>();

    public DbSet<Estudante> Estudantes => Set<Estudante>();

    public DbSet<RegistroConsumo> RegistrosConsumo => Set<RegistroConsumo>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<HistoricoAlteracao> HistoricoAlteracoes => Set<HistoricoAlteracao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Registro explícito de cada mapeamento (sem ApplyConfigurationsFromAssembly).
        modelBuilder.ApplyConfiguration(new FeedbackEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CategoriaFeedbackEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CalendarioRefeicaoEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new EstudanteEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RegistroConsumoEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new HistoricoAlteracaoEntityTypeConfiguration());
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // Os eventos saem antes do SaveChanges: os handlers podem alterar outros
        // agregados e tudo é gravado na mesma transação.
        await mediator.DispatchDomainEventsAsync(this, cancellationToken);

        if (!ChangeTracker.HasChanges())
        {
            return true;
        }

        // Auditoria: os valores "antes" precisam ser lidos agora; os "depois" só
        // depois de gravar (é quando os Ids novos existem).
        var alteracoes = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is not HistoricoAlteracao)
            .Select(e => new AlteracaoPendente(
                e,
                e.Metadata.GetTableName() ?? e.Metadata.ClrType.Name,
                e.State switch
                {
                    EntityState.Added => AcaoAuditoria.Criacao,
                    EntityState.Modified => AcaoAuditoria.Edicao,
                    _ => AcaoAuditoria.Exclusao
                },
                e.State == EntityState.Added ? null : Serializar(e.OriginalValues)))
            .ToList();

        // Dados e histórico entram juntos ou nenhum entra.
        await using var transacao = Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;

        await base.SaveChangesAsync(cancellationToken);

        var agora = DateTime.UtcNow;
        var usuarioId = usuarioAtual.UsuarioId;
        HistoricoAlteracoes.AddRange(alteracoes.Select(a => new HistoricoAlteracao(
            usuarioId,
            a.Tabela,
            a.Acao,
            agora,
            a.ValorAnterior,
            a.Acao == AcaoAuditoria.Exclusao ? null : Serializar(a.Entrada.CurrentValues))));
        await base.SaveChangesAsync(cancellationToken);

        if (transacao is not null)
        {
            await transacao.CommitAsync(cancellationToken);
        }

        return true;
    }

    /// <summary>Todas as colunas da linha (inclusive chaves estrangeiras) como JSON.</summary>
    private static string Serializar(PropertyValues valores) =>
        JsonSerializer.Serialize(valores.Properties.ToDictionary(p => p.Name, p => valores[p]));

    private sealed record AlteracaoPendente(
        EntityEntry Entrada,
        string Tabela,
        AcaoAuditoria Acao,
        string? ValorAnterior);
}

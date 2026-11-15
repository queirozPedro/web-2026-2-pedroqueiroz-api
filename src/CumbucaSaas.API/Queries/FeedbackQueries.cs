using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using CumbucaSaas.Domain.SeedWork;
using Dapper;
using Npgsql;

namespace CumbucaSaas.API.Queries;

public sealed record CategoriaFeedbackViewModel(int Id, string Nome, string Descricao);

public interface IFeedbackQueries
{
    Task<FeedbackViewModel?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Feedbacks do mais recente para o mais antigo. Filtros opcionais: status
    /// ("novo", "em_analise", "resolvido"), período e estudante (histórico dele).
    /// </summary>
    Task<IReadOnlyList<FeedbackViewModel>> ListarAsync(
        string? status,
        DateOnly? inicio,
        DateOnly? fim,
        int? estudanteId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoriaFeedbackViewModel>> ListarCategoriasAsync(CancellationToken cancellationToken = default);
}

/// <summary>Leitura direta no PostgreSQL com Dapper, sem passar pelo EF nem pelo domínio.</summary>
public sealed class FeedbackQueries(string connectionString) : IFeedbackQueries
{
    /// <summary>
    /// Uma linha por avaliação; FeedbackQueries.Agrupar junta as notas de cada feedback.
    /// Também usado pelo dashboard (DashboardQueries), com o filtro de período.
    /// </summary>
    internal const string SqlBase = """
        SELECT f."Id", f."Data", f."StatusId", cf."Nome" AS "Categoria", f."Comentario",
               a."CategoriaId", a."Nota"
        FROM "Feedbacks" f
        JOIN "CategoriasFeedback" cf ON cf."Id" = f."CategoriaFeedbackId"
        LEFT JOIN "Avaliacoes" a ON a."FeedbackId" = f."Id"
        """;

    public async Task<FeedbackViewModel?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conexao = new NpgsqlConnection(connectionString);
        var linhas = await conexao.QueryAsync<LinhaFeedback>(new CommandDefinition(
            SqlBase + "\nWHERE f.\"Id\" = @id", new { id }, cancellationToken: cancellationToken));

        return Agrupar(linhas).FirstOrDefault();
    }

    public async Task<IReadOnlyList<FeedbackViewModel>> ListarAsync(
        string? status,
        DateOnly? inicio,
        DateOnly? fim,
        int? estudanteId,
        CancellationToken cancellationToken = default)
    {
        int? statusId = Enumeration.TryFromName<StatusFeedback>(status, out var filtro) ? filtro.Id : null;

        // Datas como texto ISO convertidas no PostgreSQL (evita DateOnly -> timestamp nos parâmetros).
        var condicoes = new List<string>();
        if (statusId is not null)
        {
            condicoes.Add("f.\"StatusId\" = @statusId");
        }

        if (inicio is not null)
        {
            condicoes.Add("f.\"Data\" >= CAST(@inicio AS date)");
        }

        if (fim is not null)
        {
            condicoes.Add("f.\"Data\" <= CAST(@fim AS date)");
        }

        if (estudanteId is not null)
        {
            condicoes.Add("f.\"EstudanteId\" = @estudanteId");
        }

        var sql = SqlBase
            + (condicoes.Count > 0 ? "\nWHERE " + string.Join(" AND ", condicoes) : "")
            + "\nORDER BY f.\"Data\" DESC, f.\"Id\" DESC";

        await using var conexao = new NpgsqlConnection(connectionString);
        var linhas = await conexao.QueryAsync<LinhaFeedback>(new CommandDefinition(
            sql,
            new
            {
                statusId,
                inicio = inicio?.ToString("yyyy-MM-dd"),
                fim = fim?.ToString("yyyy-MM-dd"),
                estudanteId
            },
            cancellationToken: cancellationToken));

        return Agrupar(linhas);
    }

    public async Task<IReadOnlyList<CategoriaFeedbackViewModel>> ListarCategoriasAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Nome", "Descricao" FROM "CategoriasFeedback" ORDER BY "Id"
            """;

        await using var conexao = new NpgsqlConnection(connectionString);
        var categorias = await conexao.QueryAsync<CategoriaFeedbackViewModel>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return categorias.ToList();
    }

    /// <summary>O LEFT JOIN traz uma linha por avaliação; junta as notas de cada feedback, mantendo a ordem.</summary>
    internal static List<FeedbackViewModel> Agrupar(IEnumerable<LinhaFeedback> linhas) =>
        linhas
            .GroupBy(l => l.Id)
            .Select(grupo =>
            {
                var primeira = grupo.First();
                var notas = grupo
                    .Where(l => l.CategoriaId is not null)
                    .ToDictionary(
                        l => Enumeration.FromValue<CategoriaAvaliacao>(l.CategoriaId!.Value).Name,
                        l => l.Nota!.Value);

                return new FeedbackViewModel(
                    primeira.Id,
                    DateOnly.FromDateTime(primeira.Data),
                    Enumeration.FromValue<StatusFeedback>(primeira.StatusId).Name,
                    primeira.Categoria,
                    primeira.Comentario,
                    notas);
            })
            .ToList();

    /// <summary>Formato bruto do Npgsql: date chega como DateTime.</summary>
    internal sealed class LinhaFeedback
    {
        public int Id { get; init; }
        public DateTime Data { get; init; }
        public int StatusId { get; init; }
        public string Categoria { get; init; } = "";
        public string Comentario { get; init; } = "";
        public int? CategoriaId { get; init; }
        public int? Nota { get; init; }
    }
}

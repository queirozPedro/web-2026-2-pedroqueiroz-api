using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using CumbucaSaas.Domain.SeedWork;
using Dapper;
using Npgsql;

namespace CumbucaSaas.API.Queries;

public interface IFeedbackQueries
{
    Task<FeedbackViewModel?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>Leitura direta no PostgreSQL com Dapper, sem passar pelo EF nem pelo domínio.</summary>
public sealed class FeedbackQueries(string connectionString) : IFeedbackQueries
{
    public async Task<FeedbackViewModel?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT f."Id", f."Data", f."StatusId", f."Categoria", f."Comentario",
                   a."CategoriaId", a."Nota"
            FROM "Feedbacks" f
            LEFT JOIN "Avaliacoes" a ON a."FeedbackId" = f."Id"
            WHERE f."Id" = @id
            """;

        await using var conexao = new NpgsqlConnection(connectionString);
        var linhas = (await conexao.QueryAsync<LinhaFeedback>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken))).ToList();

        if (linhas.Count == 0)
        {
            return null;
        }

        var primeira = linhas[0];
        var notas = linhas
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
    }

    private sealed class LinhaFeedback
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

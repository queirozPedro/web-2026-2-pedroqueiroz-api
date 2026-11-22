using CumbucaSaas.Domain.Aggregates.AuditoriaAggregate;
using CumbucaSaas.Domain.SeedWork;
using CumbucaSaas.Infrastructure;
using Dapper;
using Npgsql;

namespace CumbucaSaas.API.Queries;

/// <summary>Uma alteração gravada. Valores em JSON (texto); DataHora em UTC; Acao pelo Name.</summary>
public sealed record HistoricoAlteracaoViewModel(
    int Id,
    DateTime DataHora,
    string Tabela,
    string Acao,
    int? UsuarioId,
    string? EmailUsuario,
    string? ValorAnterior,
    string? ValorNovo);

public interface IAuditoriaQueries
{
    /// <summary>
    /// Mais recentes primeiro, até <paramref name="limite"/> linhas. Filtros opcionais:
    /// tabela (ex.: "RegistrosConsumo") e período (datas em UTC).
    /// </summary>
    Task<IReadOnlyList<HistoricoAlteracaoViewModel>> ListarAsync(
        string? tabela,
        DateOnly? inicio,
        DateOnly? fim,
        int limite,
        CancellationToken cancellationToken = default);
}

/// <summary>Leitura direta no PostgreSQL com Dapper, sem passar pelo EF nem pelo domínio.</summary>
public sealed class AuditoriaQueries(string connectionString, ITenantAtual tenantAtual) : IAuditoriaQueries
{
    public async Task<IReadOnlyList<HistoricoAlteracaoViewModel>> ListarAsync(
        string? tabela,
        DateOnly? inicio,
        DateOnly? fim,
        int limite,
        CancellationToken cancellationToken = default)
    {
        var condicoes = new List<string> { "h.\"TenantId\" = @tenantId" };
        if (!string.IsNullOrWhiteSpace(tabela))
        {
            condicoes.Add("h.\"Tabela\" = @tabela");
        }

        // DataHora é timestamptz em UTC; o fim inclui o dia inteiro.
        if (inicio is not null)
        {
            condicoes.Add("h.\"DataHora\" >= CAST(@inicio AS date)");
        }

        if (fim is not null)
        {
            condicoes.Add("h.\"DataHora\" < CAST(@fim AS date) + 1");
        }

        var sql = """
            SELECT h."Id", h."DataHora", h."Tabela", h."AcaoId", h."UsuarioId", u."Email" AS "EmailUsuario",
                   h."ValorAnterior"::text AS "ValorAnterior", h."ValorNovo"::text AS "ValorNovo"
            FROM "HistoricoAlteracoes" h
            LEFT JOIN "Usuarios" u ON u."Id" = h."UsuarioId"
            """
            + (condicoes.Count > 0 ? "\nWHERE " + string.Join(" AND ", condicoes) : "")
            + "\nORDER BY h.\"DataHora\" DESC, h.\"Id\" DESC\nLIMIT @limite";

        await using var conexao = new NpgsqlConnection(connectionString);
        var linhas = await conexao.QueryAsync<Linha>(new CommandDefinition(
            sql,
            new
            {
                tenantId = tenantAtual.Obrigatorio(),
                tabela = tabela?.Trim(),
                inicio = inicio?.ToString("yyyy-MM-dd"),
                fim = fim?.ToString("yyyy-MM-dd"),
                limite = Math.Clamp(limite, 1, 1000)
            },
            cancellationToken: cancellationToken));

        return linhas
            .Select(l => new HistoricoAlteracaoViewModel(
                l.Id,
                DateTime.SpecifyKind(l.DataHora, DateTimeKind.Utc),
                l.Tabela,
                Enumeration.FromValue<AcaoAuditoria>(l.AcaoId).Name,
                l.UsuarioId,
                l.EmailUsuario,
                l.ValorAnterior,
                l.ValorNovo))
            .ToList();
    }

    private sealed class Linha
    {
        public int Id { get; init; }
        public DateTime DataHora { get; init; }
        public string Tabela { get; init; } = "";
        public int AcaoId { get; init; }
        public int? UsuarioId { get; init; }
        public string? EmailUsuario { get; init; }
        public string? ValorAnterior { get; init; }
        public string? ValorNovo { get; init; }
    }
}

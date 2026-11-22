using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using CumbucaSaas.Infrastructure;
using Dapper;
using Npgsql;

namespace CumbucaSaas.API.Queries;

public sealed record CardapioViewModel(
    string Itens,
    string CategoriaNutricional,
    decimal PrecoSubsidiado,
    decimal PrecoIntegral);

public sealed record ProducaoViewModel(int QuantidadeProduzida, string? Observacoes);

/// <summary>Valores informados no fechamento; ValorTotal é o arrecadado real do turno.</summary>
public sealed record FechamentoCaixaViewModel(decimal ValorDinheiro, decimal ValorPix, decimal ValorTotal);

/// <summary>
/// Turno do calendário. Datas saem como "YYYY-MM-DD", horários como "HH:mm:ss" e o turno pelo Name.
/// Cardápio, produção e caixa vêm nulos enquanto não forem registrados.
/// </summary>
public sealed record CalendarioRefeicaoViewModel(
    int Id,
    DateOnly Data,
    string Turno,
    TimeOnly Abertura,
    TimeOnly Fechamento,
    string? Observacoes,
    CardapioViewModel? Cardapio,
    ProducaoViewModel? Producao,
    FechamentoCaixaViewModel? FechamentoCaixa);

public interface ICalendarioQueries
{
    /// <summary>Turnos em ordem cronológica (almoço antes da janta). Sem período, devolve todos.</summary>
    Task<IReadOnlyList<CalendarioRefeicaoViewModel>> ListarAsync(
        DateOnly? inicio,
        DateOnly? fim,
        CancellationToken cancellationToken = default);

    Task<CalendarioRefeicaoViewModel?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>Leitura direta no PostgreSQL com Dapper, sem passar pelo EF nem pelo domínio.</summary>
public sealed class CalendarioQueries(string connectionString, ITenantAtual tenantAtual) : ICalendarioQueries
{
    private const string SqlBase = """
        SELECT c."Id", c."Data", c."TurnoId", c."Abertura", c."Fechamento", c."Observacoes",
               ca."Itens", ca."CategoriaNutricional", ca."PrecoSubsidiado", ca."PrecoIntegral",
               pr."QuantidadeProduzida", pr."Observacoes" AS "ObservacoesProducao",
               fc."ValorDinheiro", fc."ValorPix"
        FROM "CalendarioRefeicoes" c
        LEFT JOIN "Cardapios" ca ON ca."CalendarioRefeicaoId" = c."Id"
        LEFT JOIN "Producoes" pr ON pr."CalendarioRefeicaoId" = c."Id"
        LEFT JOIN "FechamentosCaixa" fc ON fc."CalendarioRefeicaoId" = c."Id"
        """;

    public async Task<IReadOnlyList<CalendarioRefeicaoViewModel>> ListarAsync(
        DateOnly? inicio,
        DateOnly? fim,
        CancellationToken cancellationToken = default)
    {
        // As datas vão como texto ISO e o PostgreSQL converte para date: evita a
        // conversão de DateOnly/DateTime para timestamp nos parâmetros.
        var condicoes = new List<string> { """c."TenantId" = @tenantId""" };
        if (inicio is not null)
        {
            condicoes.Add("""c."Data" >= CAST(@inicio AS date)""");
        }

        if (fim is not null)
        {
            condicoes.Add("""c."Data" <= CAST(@fim AS date)""");
        }

        var sql = SqlBase
            + "\nWHERE " + string.Join(" AND ", condicoes)
            + "\nORDER BY c.\"Data\", c.\"TurnoId\"";

        await using var conexao = new NpgsqlConnection(connectionString);
        var linhas = await conexao.QueryAsync<Linha>(new CommandDefinition(
            sql,
            new
            {
                tenantId = tenantAtual.Obrigatorio(),
                inicio = inicio?.ToString("yyyy-MM-dd"),
                fim = fim?.ToString("yyyy-MM-dd")
            },
            cancellationToken: cancellationToken));

        return linhas.Select(Mapear).ToList();
    }

    public async Task<CalendarioRefeicaoViewModel?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conexao = new NpgsqlConnection(connectionString);
        var linha = await conexao.QuerySingleOrDefaultAsync<Linha>(new CommandDefinition(
            SqlBase + "\nWHERE c.\"Id\" = @id AND c.\"TenantId\" = @tenantId",
            new { id, tenantId = tenantAtual.Obrigatorio() },
            cancellationToken: cancellationToken));

        return linha is null ? null : Mapear(linha);
    }

    private static CalendarioRefeicaoViewModel Mapear(Linha linha) =>
        new(
            linha.Id,
            DateOnly.FromDateTime(linha.Data),
            Enumeration.FromValue<Turno>(linha.TurnoId).Name,
            TimeOnly.FromTimeSpan(linha.Abertura),
            TimeOnly.FromTimeSpan(linha.Fechamento),
            linha.Observacoes,
            // Sem linha na tabela do LEFT JOIN, as colunas dela chegam nulas.
            linha.Itens is null
                ? null
                : new CardapioViewModel(
                    linha.Itens,
                    linha.CategoriaNutricional!,
                    linha.PrecoSubsidiado!.Value,
                    linha.PrecoIntegral!.Value),
            linha.QuantidadeProduzida is null
                ? null
                : new ProducaoViewModel(linha.QuantidadeProduzida.Value, linha.ObservacoesProducao),
            linha.ValorDinheiro is null
                ? null
                : new FechamentoCaixaViewModel(
                    linha.ValorDinheiro.Value,
                    linha.ValorPix!.Value,
                    linha.ValorDinheiro.Value + linha.ValorPix!.Value));

    /// <summary>Formato bruto do Npgsql: date chega como DateTime e time como TimeSpan.</summary>
    private sealed class Linha
    {
        public int Id { get; init; }
        public DateTime Data { get; init; }
        public int TurnoId { get; init; }
        public TimeSpan Abertura { get; init; }
        public TimeSpan Fechamento { get; init; }
        public string? Observacoes { get; init; }
        public string? Itens { get; init; }
        public string? CategoriaNutricional { get; init; }
        public decimal? PrecoSubsidiado { get; init; }
        public decimal? PrecoIntegral { get; init; }
        public int? QuantidadeProduzida { get; init; }
        public string? ObservacoesProducao { get; init; }
        public decimal? ValorDinheiro { get; init; }
        public decimal? ValorPix { get; init; }
    }
}

using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.Aggregates.ConsumoAggregate;
using CumbucaSaas.Domain.SeedWork;
using CumbucaSaas.Infrastructure;
using Dapper;
using Npgsql;

namespace CumbucaSaas.API.Queries;

/*
 * Dados do dashboard a partir das tabelas, no mesmo formato que o front já consome
 * (RegistroTurnoViewModel / FeedbackViewModel). Um registro por turno do calendário
 * que já teve algum movimento (produção, consumo ou caixa):
 *
 *   produzido    Producoes.QuantidadeProduzida (0 enquanto não registrada)
 *   servido      quantidade de RegistrosConsumo do turno
 *   excedente    produzido - servido (nunca negativo)
 *   estudantes   consumos com TipoCliente = estudante; externos idem
 *   estimado     soma do ValorPago dos consumos (preço do cardápio x tipo de cliente)
 *   dinheiro/pix valores contados no FechamentoCaixa; com o caixa ainda aberto,
 *                a soma dos consumos pagos em cada forma
 *   real         dinheiro + pix
 *   fluxoPorHora consumos na 1ª, 2ª e 3ª hora a partir da abertura do turno
 *                (o que passar da 3ª hora entra na última faixa)
 */
public sealed class DashboardQueries(string connectionString, ITenantAtual tenantAtual) : IDashboardQueries
{
    private const string SqlRegistros = """
        SELECT c."Data", c."TurnoId",
               COALESCE(pr."QuantidadeProduzida", 0) AS "Produzido",
               COUNT(r."Id") AS "Servido",
               COUNT(r."Id") FILTER (WHERE r."TipoClienteId" = @estudante) AS "Estudantes",
               COUNT(r."Id") FILTER (WHERE r."TipoClienteId" = @externo) AS "Externos",
               COALESCE(SUM(r."ValorPago"), 0) AS "Estimado",
               COALESCE(SUM(r."ValorPago") FILTER (WHERE r."FormaPagamentoId" = @dinheiro), 0) AS "DinheiroConsumos",
               COALESCE(SUM(r."ValorPago") FILTER (WHERE r."FormaPagamentoId" = @pix), 0) AS "PixConsumos",
               fc."ValorDinheiro" AS "DinheiroCaixa",
               fc."ValorPix" AS "PixCaixa",
               COUNT(r."Id") FILTER (WHERE {0} = 0) AS "Faixa1",
               COUNT(r."Id") FILTER (WHERE {0} = 1) AS "Faixa2",
               COUNT(r."Id") FILTER (WHERE {0} = 2) AS "Faixa3"
        FROM "CalendarioRefeicoes" c
        LEFT JOIN "Producoes" pr ON pr."CalendarioRefeicaoId" = c."Id"
        LEFT JOIN "FechamentosCaixa" fc ON fc."CalendarioRefeicaoId" = c."Id"
        LEFT JOIN "RegistrosConsumo" r ON r."CalendarioRefeicaoId" = c."Id"
        {1}
        GROUP BY c."Id", c."Data", c."TurnoId", c."Abertura",
                 pr."QuantidadeProduzida", fc."ValorDinheiro", fc."ValorPix"
        HAVING pr."QuantidadeProduzida" IS NOT NULL
            OR fc."ValorDinheiro" IS NOT NULL
            OR COUNT(r."Id") > 0
        ORDER BY c."Data", c."TurnoId"
        """;

    /// <summary>Hora do consumo contada a partir da hora de abertura do turno, limitada a 0..2.</summary>
    private const string FaixaHoraria =
        """LEAST(GREATEST(EXTRACT(HOUR FROM r."Horario") - EXTRACT(HOUR FROM c."Abertura"), 0), 2)""";

    public async Task<DashboardViewModel> ObterAsync(
        DateOnly? inicio,
        DateOnly? fim,
        CancellationToken cancellationToken = default)
    {
        // As datas vão como texto ISO e o PostgreSQL converte para date: evita a
        // conversão de DateOnly/DateTime para timestamp nos parâmetros.
        var parametros = new
        {
            tenantId = tenantAtual.Obrigatorio(),
            inicio = inicio?.ToString("yyyy-MM-dd"),
            fim = fim?.ToString("yyyy-MM-dd"),
            estudante = TipoCliente.Estudante.Id,
            externo = TipoCliente.Externo.Id,
            dinheiro = FormaPagamento.Dinheiro.Id,
            pix = FormaPagamento.Pix.Id
        };

        await using var conexao = new NpgsqlConnection(connectionString);

        var registros = await conexao.QueryAsync<LinhaRegistro>(new CommandDefinition(
            string.Format(SqlRegistros, FaixaHoraria, Filtro("c", inicio, fim)),
            parametros,
            cancellationToken: cancellationToken));

        // Mesma leitura do FeedbackQueries (com o nome da categoria), em ordem cronológica.
        var feedbacks = await conexao.QueryAsync<FeedbackQueries.LinhaFeedback>(new CommandDefinition(
            FeedbackQueries.SqlBase
                + "\n" + Filtro("f", inicio, fim)
                + "\nORDER BY f.\"Data\", f.\"Id\"",
            parametros,
            cancellationToken: cancellationToken));

        return new DashboardViewModel(
            registros.Select(MapearRegistro).ToList(),
            FeedbackQueries.Agrupar(feedbacks));
    }

    /// <summary>Tenant da requisição e período opcional sobre a coluna "Data" da tabela <paramref name="alias"/>.</summary>
    private static string Filtro(string alias, DateOnly? inicio, DateOnly? fim)
    {
        var coluna = $"{alias}.\"Data\"";
        var condicoes = new List<string> { $"{alias}.\"TenantId\" = @tenantId" };
        if (inicio is not null)
        {
            condicoes.Add($"{coluna} >= CAST(@inicio AS date)");
        }

        if (fim is not null)
        {
            condicoes.Add($"{coluna} <= CAST(@fim AS date)");
        }

        return "WHERE " + string.Join(" AND ", condicoes);
    }

    private static RegistroTurnoViewModel MapearRegistro(LinhaRegistro linha)
    {
        var produzido = linha.Produzido;
        var servido = (int)linha.Servido;

        // Caixa fechado: vale o que foi contado. Ainda aberto: o que os consumos somam.
        var dinheiro = linha.DinheiroCaixa ?? linha.DinheiroConsumos;
        var pix = linha.PixCaixa ?? linha.PixConsumos;

        return new RegistroTurnoViewModel(
            DateOnly.FromDateTime(linha.Data),
            Enumeration.FromValue<Turno>(linha.TurnoId).Name,
            produzido,
            servido,
            Math.Max(produzido - servido, 0),
            (int)linha.Estudantes,
            (int)linha.Externos,
            (double)dinheiro,
            (double)pix,
            (double)linha.Estimado,
            (double)(dinheiro + pix),
            [(int)linha.Faixa1, (int)linha.Faixa2, (int)linha.Faixa3]);
    }

    /// <summary>Formato bruto do Npgsql: date chega como DateTime, COUNT como bigint e SUM como numeric.</summary>
    private sealed class LinhaRegistro
    {
        public DateTime Data { get; init; }
        public int TurnoId { get; init; }
        public int Produzido { get; init; }
        public long Servido { get; init; }
        public long Estudantes { get; init; }
        public long Externos { get; init; }
        public decimal Estimado { get; init; }
        public decimal DinheiroConsumos { get; init; }
        public decimal PixConsumos { get; init; }
        public decimal? DinheiroCaixa { get; init; }
        public decimal? PixCaixa { get; init; }
        public long Faixa1 { get; init; }
        public long Faixa2 { get; init; }
        public long Faixa3 { get; init; }
    }
}

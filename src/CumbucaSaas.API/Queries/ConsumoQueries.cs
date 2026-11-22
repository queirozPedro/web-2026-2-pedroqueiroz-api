using CumbucaSaas.Domain.Aggregates.ConsumoAggregate;
using CumbucaSaas.Domain.SeedWork;
using CumbucaSaas.Infrastructure;
using Dapper;
using Npgsql;

namespace CumbucaSaas.API.Queries;

/// <summary>Tipo, forma de pagamento e identificação saem pelo Name ("estudante", "pix", "qrcode"...).</summary>
public sealed record ConsumoViewModel(
    int Id,
    int CalendarioRefeicaoId,
    string TipoCliente,
    int? EstudanteId,
    string? NomeEstudante,
    TimeOnly Horario,
    decimal ValorPago,
    string? FormaPagamento,
    string FormaIdentificacao);

public interface IConsumoQueries
{
    /// <summary>Consumos de um turno, do mais antigo para o mais recente.</summary>
    Task<IReadOnlyList<ConsumoViewModel>> ListarPorTurnoAsync(
        int calendarioRefeicaoId,
        CancellationToken cancellationToken = default);
}

/// <summary>Leitura direta no PostgreSQL com Dapper, sem passar pelo EF nem pelo domínio.</summary>
public sealed class ConsumoQueries(string connectionString, ITenantAtual tenantAtual) : IConsumoQueries
{
    public async Task<IReadOnlyList<ConsumoViewModel>> ListarPorTurnoAsync(
        int calendarioRefeicaoId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT r."Id", r."CalendarioRefeicaoId", r."TipoClienteId", r."EstudanteId", e."Nome" AS "NomeEstudante",
                   r."Horario", r."ValorPago", r."FormaPagamentoId", r."FormaIdentificacaoId"
            FROM "RegistrosConsumo" r
            LEFT JOIN "Estudantes" e ON e."Id" = r."EstudanteId"
            WHERE r."CalendarioRefeicaoId" = @calendarioRefeicaoId AND r."TenantId" = @tenantId
            ORDER BY r."Horario", r."Id"
            """;

        await using var conexao = new NpgsqlConnection(connectionString);
        var linhas = await conexao.QueryAsync<Linha>(new CommandDefinition(
            sql,
            new { calendarioRefeicaoId, tenantId = tenantAtual.Obrigatorio() },
            cancellationToken: cancellationToken));

        return linhas
            .Select(l => new ConsumoViewModel(
                l.Id,
                l.CalendarioRefeicaoId,
                Enumeration.FromValue<TipoCliente>(l.TipoClienteId).Name,
                l.EstudanteId,
                l.NomeEstudante,
                TimeOnly.FromTimeSpan(l.Horario),
                l.ValorPago,
                l.FormaPagamentoId is null ? null : Enumeration.FromValue<FormaPagamento>(l.FormaPagamentoId.Value).Name,
                Enumeration.FromValue<FormaIdentificacao>(l.FormaIdentificacaoId).Name))
            .ToList();
    }

    /// <summary>Formato bruto do Npgsql: time chega como TimeSpan.</summary>
    private sealed class Linha
    {
        public int Id { get; init; }
        public int CalendarioRefeicaoId { get; init; }
        public int TipoClienteId { get; init; }
        public int? EstudanteId { get; init; }
        public string? NomeEstudante { get; init; }
        public TimeSpan Horario { get; init; }
        public decimal ValorPago { get; init; }
        public int? FormaPagamentoId { get; init; }
        public int FormaIdentificacaoId { get; init; }
    }
}

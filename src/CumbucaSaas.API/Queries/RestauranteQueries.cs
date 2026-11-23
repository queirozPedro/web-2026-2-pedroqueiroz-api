using CumbucaSaas.API.PerfilUsuario;
using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using CumbucaSaas.Infrastructure;
using Dapper;
using Npgsql;

namespace CumbucaSaas.API.Queries;

/// <summary>Turno servido num dia da semana (0 = domingo ... 6 = sábado). Turno: "almoco" ou "janta".</summary>
public sealed record HorarioFuncionamentoViewModel(int DiaSemana, string Turno, TimeOnly Abertura, TimeOnly Fechamento);

/// <summary>
/// Restaurante (tenant) do usuário logado. FotoUrl é um link temporário (1 hora) para a foto
/// no S3, ou nulo sem foto. Dia da semana sem horário = fechado.
/// </summary>
public sealed record RestauranteViewModel(
    int Id,
    string Nome,
    string NomeResponsavel,
    string? Endereco,
    string Cidade,
    string Uf,
    string CorPrincipal,
    string CorSecundaria,
    string? FotoUrl,
    IReadOnlyList<HorarioFuncionamentoViewModel> Horarios);

public interface IRestauranteQueries
{
    Task<RestauranteViewModel?> ObterAsync(CancellationToken cancellationToken = default);
}

/// <summary>Leitura direta no PostgreSQL com Dapper, do tenant da requisição.</summary>
public sealed class RestauranteQueries(string connectionString, ITenantAtual tenantAtual, IFotosPerfil fotos)
    : IRestauranteQueries
{
    public async Task<RestauranteViewModel?> ObterAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT t."Id", t."Nome", t."NomeResponsavel", t."Endereco", t."Cidade", t."Uf",
                   t."CorPrincipal", t."CorSecundaria", t."FotoChave"
            FROM "Tenants" t
            WHERE t."Id" = @tenantId;

            SELECT h."DiaSemana", h."TurnoId", h."Abertura", h."Fechamento"
            FROM "HorariosFuncionamento" h
            WHERE h."TenantId" = @tenantId
            ORDER BY h."DiaSemana", h."TurnoId";
            """;

        await using var conexao = new NpgsqlConnection(connectionString);
        using var leitura = await conexao.QueryMultipleAsync(new CommandDefinition(
            sql, new { tenantId = tenantAtual.Obrigatorio() }, cancellationToken: cancellationToken));

        var tenant = await leitura.ReadSingleOrDefaultAsync<Linha>();
        if (tenant is null)
        {
            return null;
        }

        var horarios = (await leitura.ReadAsync<LinhaHorario>())
            .Select(h => new HorarioFuncionamentoViewModel(
                h.DiaSemana,
                Enumeration.FromValue<Turno>(h.TurnoId).Name,
                TimeOnly.FromTimeSpan(h.Abertura),
                TimeOnly.FromTimeSpan(h.Fechamento)))
            .ToList();

        return new RestauranteViewModel(
            tenant.Id,
            tenant.Nome,
            tenant.NomeResponsavel,
            tenant.Endereco,
            tenant.Cidade,
            tenant.Uf.Trim(),
            tenant.CorPrincipal.Trim(),
            tenant.CorSecundaria.Trim(),
            tenant.FotoChave is null ? null : await fotos.LinkTemporarioAsync(tenant.FotoChave),
            horarios);
    }

    private sealed class Linha
    {
        public int Id { get; init; }
        public string Nome { get; init; } = "";
        public string NomeResponsavel { get; init; } = "";
        public string? Endereco { get; init; }
        public string Cidade { get; init; } = "";
        public string Uf { get; init; } = "";
        public string CorPrincipal { get; init; } = "";
        public string CorSecundaria { get; init; } = "";
        public string? FotoChave { get; init; }
    }

    /// <summary>Formato bruto do Npgsql: time chega como TimeSpan.</summary>
    private sealed class LinhaHorario
    {
        public int DiaSemana { get; init; }
        public int TurnoId { get; init; }
        public TimeSpan Abertura { get; init; }
        public TimeSpan Fechamento { get; init; }
    }
}

using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.SeedWork;
using Dapper;
using Npgsql;

namespace CumbucaSaas.API.Queries;

/// <summary>Vínculo e tipo de subsídio saem pelo Name ("ativo", "padrao"...).</summary>
public sealed record EstudanteViewModel(
    int Id,
    string Nome,
    string Email,
    string Matricula,
    string Curso,
    string Vinculo,
    string TipoSubsidio);

public interface IEstudanteQueries
{
    /// <summary>Em ordem alfabética. <paramref name="vinculo"/> ("ativo"/"inativo") filtra quando informado.</summary>
    Task<IReadOnlyList<EstudanteViewModel>> ListarAsync(string? vinculo, CancellationToken cancellationToken = default);

    Task<EstudanteViewModel?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Identificação no balcão (matrícula digitada ou lida do QR Code).</summary>
    Task<EstudanteViewModel?> ObterPorMatriculaAsync(string matricula, CancellationToken cancellationToken = default);
}

/// <summary>Leitura direta no PostgreSQL com Dapper, sem passar pelo EF nem pelo domínio.</summary>
public sealed class EstudanteQueries(string connectionString) : IEstudanteQueries
{
    private const string SqlBase = """
        SELECT e."Id", e."Nome", e."Email", e."Matricula", e."Curso", e."VinculoId", e."TipoSubsidioId"
        FROM "Estudantes" e
        """;

    public async Task<IReadOnlyList<EstudanteViewModel>> ListarAsync(
        string? vinculo,
        CancellationToken cancellationToken = default)
    {
        int? vinculoId = Enumeration.TryFromName<Vinculo>(vinculo, out var filtro) ? filtro.Id : null;

        var sql = SqlBase
            + (vinculoId is null ? "" : "\nWHERE e.\"VinculoId\" = @vinculoId")
            + "\nORDER BY e.\"Nome\"";

        await using var conexao = new NpgsqlConnection(connectionString);
        var linhas = await conexao.QueryAsync<Linha>(new CommandDefinition(
            sql, new { vinculoId }, cancellationToken: cancellationToken));

        return linhas.Select(Mapear).ToList();
    }

    public Task<EstudanteViewModel?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        ObterUmAsync("e.\"Id\" = @id", new { id }, cancellationToken);

    public Task<EstudanteViewModel?> ObterPorMatriculaAsync(string matricula, CancellationToken cancellationToken = default) =>
        ObterUmAsync("e.\"Matricula\" = @matricula", new { matricula = matricula.Trim() }, cancellationToken);

    private async Task<EstudanteViewModel?> ObterUmAsync(string condicao, object parametros, CancellationToken cancellationToken)
    {
        await using var conexao = new NpgsqlConnection(connectionString);
        var linha = await conexao.QuerySingleOrDefaultAsync<Linha>(new CommandDefinition(
            SqlBase + "\nWHERE " + condicao, parametros, cancellationToken: cancellationToken));

        return linha is null ? null : Mapear(linha);
    }

    private static EstudanteViewModel Mapear(Linha linha) =>
        new(
            linha.Id,
            linha.Nome,
            linha.Email,
            linha.Matricula,
            linha.Curso,
            Enumeration.FromValue<Vinculo>(linha.VinculoId).Name,
            Enumeration.FromValue<TipoSubsidio>(linha.TipoSubsidioId).Name);

    private sealed class Linha
    {
        public int Id { get; init; }
        public string Nome { get; init; } = "";
        public string Email { get; init; } = "";
        public string Matricula { get; init; } = "";
        public string Curso { get; init; } = "";
        public int VinculoId { get; init; }
        public int TipoSubsidioId { get; init; }
    }
}

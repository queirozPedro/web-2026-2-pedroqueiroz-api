using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using CumbucaSaas.Infrastructure;
using Dapper;
using Npgsql;

namespace CumbucaSaas.API.Queries;

/// <summary>Funcionário do restaurante (usuário com login de e-mail e senha). CriadoEm em UTC.</summary>
public sealed record FuncionarioViewModel(
    int Id,
    string? Nome,
    string Email,
    DateTime CriadoEm,
    bool CadastroCompleto);

public interface IFuncionarioQueries
{
    /// <summary>Funcionários do tenant, em ordem alfabética (sem nome por último).</summary>
    Task<IReadOnlyList<FuncionarioViewModel>> ListarAsync(CancellationToken cancellationToken = default);
}

/// <summary>Leitura direta no PostgreSQL com Dapper, sempre restrita ao tenant da requisição.</summary>
public sealed class FuncionarioQueries(string connectionString, ITenantAtual tenantAtual) : IFuncionarioQueries
{
    public async Task<IReadOnlyList<FuncionarioViewModel>> ListarAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u."Id", u."Nome", u."Email", u."CriadoEm", u."CadastroConcluidoEm"
            FROM "Usuarios" u
            WHERE u."TenantId" = @tenantId AND u."PerfilId" = @perfilId
            ORDER BY u."Nome" NULLS LAST, u."Email"
            """;

        await using var conexao = new NpgsqlConnection(connectionString);
        var linhas = await conexao.QueryAsync<Linha>(new CommandDefinition(
            sql,
            new { tenantId = tenantAtual.Obrigatorio(), perfilId = Perfil.Funcionario.Id },
            cancellationToken: cancellationToken));

        return linhas
            .Select(l => new FuncionarioViewModel(
                l.Id,
                l.Nome,
                l.Email,
                DateTime.SpecifyKind(l.CriadoEm, DateTimeKind.Utc),
                l.CadastroConcluidoEm is not null))
            .ToList();
    }

    private sealed class Linha
    {
        public int Id { get; init; }
        public string? Nome { get; init; }
        public string Email { get; init; } = "";
        public DateTime CriadoEm { get; init; }
        public DateTime? CadastroConcluidoEm { get; init; }
    }
}

using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using CumbucaSaas.Infrastructure;
using Dapper;
using Npgsql;

namespace CumbucaSaas.API.Queries;

/// <summary>
/// O usuário logado, para o front decidir a navegação: sem perfil vai para "sem acesso",
/// com o cadastro incompleto vai para "completar cadastro", e cada perfil tem a sua tela.
/// </summary>
/// <param name="Perfil">"funcionario", "estudante" ou nulo (sem acesso).</param>
/// <param name="MotivoSemAcesso">"dominio-nao-permitido" ou "email-em-uso" quando o perfil é nulo.</param>
public sealed record UsuarioLogadoViewModel(
    int? Id,
    string Email,
    string? Nome,
    DateOnly? DataNascimento,
    string? Perfil,
    bool CadastroCompleto,
    string? Tenant,
    string? MotivoSemAcesso);

public interface IUsuarioQueries
{
    Task<UsuarioLogadoViewModel?> ObterAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>Leitura direta no PostgreSQL com Dapper, sempre restrita ao tenant da requisição.</summary>
public sealed class UsuarioQueries(string connectionString, ITenantAtual tenantAtual) : IUsuarioQueries
{
    public async Task<UsuarioLogadoViewModel?> ObterAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u."Id", u."Email", u."Nome", u."DataNascimento", u."PerfilId", u."CadastroConcluidoEm",
                   t."Nome" AS "Tenant"
            FROM "Usuarios" u
            JOIN "Tenants" t ON t."Id" = u."TenantId"
            WHERE u."Id" = @id AND u."TenantId" = @tenantId
            """;

        await using var conexao = new NpgsqlConnection(connectionString);
        var linha = await conexao.QuerySingleOrDefaultAsync<Linha>(new CommandDefinition(
            sql, new { id, tenantId = tenantAtual.Obrigatorio() }, cancellationToken: cancellationToken));

        return linha is null
            ? null
            : new UsuarioLogadoViewModel(
                linha.Id,
                linha.Email,
                linha.Nome,
                linha.DataNascimento is null ? null : DateOnly.FromDateTime(linha.DataNascimento.Value),
                Enumeration.FromValue<Perfil>(linha.PerfilId).Name,
                linha.CadastroConcluidoEm is not null,
                linha.Tenant,
                null);
    }

    /// <summary>Formato bruto do Npgsql: date chega como DateTime.</summary>
    private sealed class Linha
    {
        public int Id { get; init; }
        public string Email { get; init; } = "";
        public string? Nome { get; init; }
        public DateTime? DataNascimento { get; init; }
        public int PerfilId { get; init; }
        public DateTime? CadastroConcluidoEm { get; init; }
        public string Tenant { get; init; } = "";
    }
}

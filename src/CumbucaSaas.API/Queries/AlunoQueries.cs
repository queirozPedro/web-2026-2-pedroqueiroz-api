using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using CumbucaSaas.Domain.SeedWork;
using CumbucaSaas.Infrastructure;
using Dapper;
using Npgsql;

namespace CumbucaSaas.API.Queries;

/// <summary>
/// Aluno do restaurante na lista da Administração: o cadastro do RU (Estudante, com
/// matrícula) e/ou a conta criada no login com Google (Usuario com perfil estudante).
/// - Com cadastro no RU: EstudanteId, matrícula, curso, vínculo ("ativo"/"inativo") e subsídio.
/// - Só com a conta (ainda sem cadastro no RU): esses campos vêm nulos.
/// PossuiLogin: já entrou no sistema. PrimeiroAcessoEm (UTC): quando a conta foi criada.
/// </summary>
public sealed record AlunoViewModel(
    int? EstudanteId,
    int? UsuarioId,
    string? Nome,
    string Email,
    string? Matricula,
    string? Curso,
    string? Vinculo,
    string? TipoSubsidio,
    bool PossuiLogin,
    DateTime? PrimeiroAcessoEm);

public interface IAlunoQueries
{
    /// <summary>Todos os alunos do tenant, em ordem alfabética (sem nome por último).</summary>
    Task<IReadOnlyList<AlunoViewModel>> ListarAsync(CancellationToken cancellationToken = default);
}

/// <summary>Leitura direta no PostgreSQL com Dapper, sempre restrita ao tenant da requisição.</summary>
public sealed class AlunoQueries(string connectionString, ITenantAtual tenantAtual) : IAlunoQueries
{
    public async Task<IReadOnlyList<AlunoViewModel>> ListarAsync(CancellationToken cancellationToken = default)
    {
        // A conta se liga ao cadastro do RU no login (Estudante.UsuarioId); antes disso, pelo
        // e-mail institucional (o cadastro pode ter sido feito depois do último login).
        const string sql = """
            SELECT e."Id" AS "EstudanteId", u."Id" AS "UsuarioId",
                   COALESCE(e."Nome", u."Nome") AS "Nome", e."Email",
                   e."Matricula", e."Curso", e."VinculoId", e."TipoSubsidioId",
                   u."CriadoEm" AS "PrimeiroAcessoEm"
            FROM "Estudantes" e
            LEFT JOIN "Usuarios" u
                   ON u."TenantId" = @tenantId AND u."PerfilId" = @perfilId
                  AND (u."Id" = e."UsuarioId" OR (e."UsuarioId" IS NULL AND u."Email" = e."Email"))
            WHERE e."TenantId" = @tenantId

            UNION ALL

            SELECT NULL, u."Id", u."Nome", u."Email",
                   NULL, NULL, NULL, NULL,
                   u."CriadoEm"
            FROM "Usuarios" u
            WHERE u."TenantId" = @tenantId AND u."PerfilId" = @perfilId
              AND NOT EXISTS (
                  SELECT 1 FROM "Estudantes" e
                  WHERE e."TenantId" = @tenantId
                    AND (e."UsuarioId" = u."Id" OR (e."UsuarioId" IS NULL AND e."Email" = u."Email")))

            ORDER BY "Nome" NULLS LAST, "Email"
            """;

        await using var conexao = new NpgsqlConnection(connectionString);
        var linhas = await conexao.QueryAsync<Linha>(new CommandDefinition(
            sql,
            new { tenantId = tenantAtual.Obrigatorio(), perfilId = Perfil.Estudante.Id },
            cancellationToken: cancellationToken));

        return linhas
            .Select(l => new AlunoViewModel(
                l.EstudanteId,
                l.UsuarioId,
                l.Nome,
                l.Email,
                l.Matricula,
                l.Curso,
                l.VinculoId is { } vinculo ? Enumeration.FromValue<Vinculo>(vinculo).Name : null,
                l.TipoSubsidioId is { } subsidio ? Enumeration.FromValue<TipoSubsidio>(subsidio).Name : null,
                l.UsuarioId is not null,
                l.PrimeiroAcessoEm is { } criadoEm ? DateTime.SpecifyKind(criadoEm, DateTimeKind.Utc) : null))
            .ToList();
    }

    private sealed class Linha
    {
        public int? EstudanteId { get; init; }
        public int? UsuarioId { get; init; }
        public string? Nome { get; init; }
        public string Email { get; init; } = "";
        public string? Matricula { get; init; }
        public string? Curso { get; init; }
        public int? VinculoId { get; init; }
        public int? TipoSubsidioId { get; init; }
        public DateTime? PrimeiroAcessoEm { get; init; }
    }
}

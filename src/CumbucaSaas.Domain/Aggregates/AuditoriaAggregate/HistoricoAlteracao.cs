using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.AuditoriaAggregate;

/// <summary>
/// Uma linha gravada/alterada/apagada em alguma tabela, com os valores antes e depois
/// (JSON). Gerado automaticamente ao salvar; não há command para criar nem editar.
/// </summary>
public class HistoricoAlteracao : Entity, IPertenceAoTenant
{
    public const int TabelaTamanhoMaximo = 100;

    // Usado pelo EF Core.
    private HistoricoAlteracao()
    {
        Tabela = null!;
        Acao = null!;
    }

    public HistoricoAlteracao(
        int tenantId,
        int? usuarioId,
        string tabela,
        AcaoAuditoria acao,
        DateTime dataHoraUtc,
        string? valorAnterior,
        string? valorNovo)
    {
        TenantId = tenantId;
        UsuarioId = usuarioId;
        Tabela = tabela;
        Acao = acao;
        DataHora = dataHoraUtc;
        ValorAnterior = valorAnterior;
        ValorNovo = valorNovo;
    }

    /// <summary>Tenant do registro alterado.</summary>
    public int TenantId { get; private set; }

    /// <summary>Nulo quando a alteração não veio de um usuário logado (ex.: o próprio cadastro no 1º login).</summary>
    public int? UsuarioId { get; private set; }

    public string Tabela { get; private set; }

    public AcaoAuditoria Acao { get; private set; }

    /// <summary>Em UTC.</summary>
    public DateTime DataHora { get; private set; }

    /// <summary>JSON com os valores antes da edição/exclusão; nulo na criação.</summary>
    public string? ValorAnterior { get; private set; }

    /// <summary>JSON com os valores depois da criação/edição; nulo na exclusão.</summary>
    public string? ValorNovo { get; private set; }
}

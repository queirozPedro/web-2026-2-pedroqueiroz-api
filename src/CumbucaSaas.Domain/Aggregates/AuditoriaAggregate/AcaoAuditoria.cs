using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.AuditoriaAggregate;

public sealed class AcaoAuditoria : Enumeration
{
    public static readonly AcaoAuditoria Criacao = new(1, "criacao");
    public static readonly AcaoAuditoria Edicao = new(2, "edicao");
    public static readonly AcaoAuditoria Exclusao = new(3, "exclusao");

    private AcaoAuditoria(int id, string name) : base(id, name)
    {
    }
}

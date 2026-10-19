using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.FeedbackAggregate;

/// <summary>Categorias que o estudante avalia de 1 a 5 em cada feedback.</summary>
public sealed class CategoriaAvaliacao : Enumeration
{
    public static readonly CategoriaAvaliacao Qualidade = new(1, "qualidade");
    public static readonly CategoriaAvaliacao Sabor = new(2, "sabor");
    public static readonly CategoriaAvaliacao Temperatura = new(3, "temperatura");
    public static readonly CategoriaAvaliacao Variedade = new(4, "variedade");
    public static readonly CategoriaAvaliacao Atendimento = new(5, "atendimento");
    public static readonly CategoriaAvaliacao Limpeza = new(6, "limpeza");
    public static readonly CategoriaAvaliacao Estrutura = new(7, "estrutura");

    private CategoriaAvaliacao(int id, string name) : base(id, name)
    {
    }
}

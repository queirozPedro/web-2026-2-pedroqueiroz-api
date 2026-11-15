using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.CalendarioAggregate;

/// <summary>Cardápio de um turno; só existe dentro de um CalendarioRefeicao.</summary>
public class Cardapio
{
    public const int ItensTamanhoMaximo = 1000;
    public const int CategoriaNutricionalTamanhoMaximo = 60;

    // Usado pelo EF Core.
    private Cardapio()
    {
        Itens = null!;
        CategoriaNutricional = null!;
    }

    internal Cardapio(string itens, string categoriaNutricional, decimal precoSubsidiado, decimal precoIntegral)
    {
        Itens = null!;
        CategoriaNutricional = null!;
        Atualizar(itens, categoriaNutricional, precoSubsidiado, precoIntegral);
    }

    /// <summary>Descrição livre dos itens servidos (ex.: "Arroz, feijão, frango assado, salada").</summary>
    public string Itens { get; private set; }

    /// <summary>Ex.: "Tradicional", "Vegetariano".</summary>
    public string CategoriaNutricional { get; private set; }

    /// <summary>Preço pago pelo estudante (R$).</summary>
    public decimal PrecoSubsidiado { get; private set; }

    /// <summary>Preço pago pelo público externo (R$).</summary>
    public decimal PrecoIntegral { get; private set; }

    internal void Atualizar(string itens, string categoriaNutricional, decimal precoSubsidiado, decimal precoIntegral)
    {
        if (string.IsNullOrWhiteSpace(itens))
        {
            throw new DomainException("Informe os itens do cardápio.");
        }

        if (string.IsNullOrWhiteSpace(categoriaNutricional))
        {
            throw new DomainException("Informe a categoria nutricional do cardápio.");
        }

        if (precoSubsidiado <= 0 || precoIntegral <= 0)
        {
            throw new DomainException("Os preços do cardápio devem ser maiores que zero.");
        }

        if (precoSubsidiado > precoIntegral)
        {
            throw new DomainException("O preço subsidiado não pode ser maior que o integral.");
        }

        Itens = itens.Trim();
        CategoriaNutricional = categoriaNutricional.Trim();
        PrecoSubsidiado = precoSubsidiado;
        PrecoIntegral = precoIntegral;
    }
}

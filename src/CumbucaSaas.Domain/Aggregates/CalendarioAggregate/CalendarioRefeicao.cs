using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.CalendarioAggregate;

/// <summary>
/// Um turno (almoço ou janta) servido em uma data, com horário de funcionamento e cardápio.
/// É a âncora do RU: produção, consumo e caixa de um turno se ligam a ele.
/// </summary>
public class CalendarioRefeicao : AggregateRoot, IPertenceAoTenant
{
    public const int ObservacoesTamanhoMaximo = 500;

    // Usado pelo EF Core.
    private CalendarioRefeicao()
    {
        Turno = null!;
    }

    public CalendarioRefeicao(DateOnly data, Turno turno, TimeOnly abertura, TimeOnly fechamento, string? observacoes)
    {
        Turno = null!;
        Atualizar(data, turno, abertura, fechamento, observacoes);
    }

    /// <summary>Restaurante dono do registro (preenchido pelo DbContext ao gravar).</summary>
    public int TenantId { get; private set; }

    public DateOnly Data { get; private set; }

    public Turno Turno { get; private set; }

    public TimeOnly Abertura { get; private set; }

    public TimeOnly Fechamento { get; private set; }

    public string? Observacoes { get; private set; }

    /// <summary>Nulo até o funcionário cadastrar o cardápio do turno.</summary>
    public Cardapio? Cardapio { get; private set; }

    /// <summary>Nulo até o funcionário registrar a produção do turno.</summary>
    public Producao? Producao { get; private set; }

    /// <summary>Nulo até o funcionário fechar o caixa do turno.</summary>
    public FechamentoCaixa? FechamentoCaixa { get; private set; }

    /// <summary>
    /// Turno com produção ou caixa já registrados não pode ser removido (perderia o histórico).
    /// Os consumos são outro agregado e são checados no command de remoção.
    /// </summary>
    public bool PossuiMovimento => Producao is not null || FechamentoCaixa is not null;

    public void Atualizar(DateOnly data, Turno turno, TimeOnly abertura, TimeOnly fechamento, string? observacoes)
    {
        if (fechamento <= abertura)
        {
            throw new DomainException("O fechamento do turno deve ser depois da abertura.");
        }

        Data = data;
        Turno = turno;
        Abertura = abertura;
        Fechamento = fechamento;
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
    }

    /// <summary>Cria o cardápio do turno ou substitui os dados do atual.</summary>
    public void DefinirCardapio(string itens, string categoriaNutricional, decimal precoSubsidiado, decimal precoIntegral)
    {
        if (Cardapio is null)
        {
            Cardapio = new Cardapio(itens, categoriaNutricional, precoSubsidiado, precoIntegral);
        }
        else
        {
            // Altera a mesma instância: o EF trata como UPDATE na tabela Cardapios.
            Cardapio.Atualizar(itens, categoriaNutricional, precoSubsidiado, precoIntegral);
        }
    }

    /// <summary>Cria o registro de produção do turno ou corrige o atual.</summary>
    public void RegistrarProducao(int quantidadeProduzida, string? observacoes)
    {
        if (Producao is null)
        {
            Producao = new Producao(quantidadeProduzida, observacoes);
        }
        else
        {
            Producao.Atualizar(quantidadeProduzida, observacoes);
        }
    }

    /// <summary>
    /// Registra (ou corrige) os valores contados no fechamento do caixa. Exige o
    /// cardápio, porque sem preço não há valor estimado para comparar.
    /// </summary>
    public void FecharCaixa(decimal valorDinheiro, decimal valorPix)
    {
        if (Cardapio is null)
        {
            throw new DomainException("Defina o cardápio do turno antes de fechar o caixa.");
        }

        if (FechamentoCaixa is null)
        {
            FechamentoCaixa = new FechamentoCaixa(valorDinheiro, valorPix);
        }
        else
        {
            FechamentoCaixa.Atualizar(valorDinheiro, valorPix);
        }
    }
}

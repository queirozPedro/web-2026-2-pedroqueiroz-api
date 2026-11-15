using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.CalendarioAggregate;

/// <summary>
/// Pagamentos/Caixa do turno: o que o funcionário contou em dinheiro e conferiu no
/// extrato do Pix ao fechar. O valor estimado (clientes x preço do cardápio) e a
/// diferença são calculados na leitura, a partir dos registros de consumo.
/// </summary>
public class FechamentoCaixa
{
    // Usado pelo EF Core.
    private FechamentoCaixa()
    {
    }

    internal FechamentoCaixa(decimal valorDinheiro, decimal valorPix)
    {
        Atualizar(valorDinheiro, valorPix);
    }

    public decimal ValorDinheiro { get; private set; }

    public decimal ValorPix { get; private set; }

    /// <summary>Valor real arrecadado no turno.</summary>
    public decimal ValorTotal => ValorDinheiro + ValorPix;

    internal void Atualizar(decimal valorDinheiro, decimal valorPix)
    {
        if (valorDinheiro < 0 || valorPix < 0)
        {
            throw new DomainException("Os valores do caixa não podem ser negativos.");
        }

        ValorDinheiro = valorDinheiro;
        ValorPix = valorPix;
    }
}

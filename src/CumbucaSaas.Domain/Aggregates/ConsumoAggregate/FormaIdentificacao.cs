using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.ConsumoAggregate;

/// <summary>
/// Como o cliente foi identificado no ponto de venda: o estudante pelo QR Code
/// (simulado) ou pela matrícula; o externo só no balcão, sem cadastro.
/// </summary>
public sealed class FormaIdentificacao : Enumeration
{
    public static readonly FormaIdentificacao QrCode = new(1, "qrcode");
    public static readonly FormaIdentificacao Matricula = new(2, "matricula");
    public static readonly FormaIdentificacao Balcao = new(3, "balcao");

    private FormaIdentificacao(int id, string name) : base(id, name)
    {
    }
}

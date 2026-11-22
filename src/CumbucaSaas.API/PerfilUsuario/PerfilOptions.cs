namespace CumbucaSaas.API.PerfilUsuario;

/// <summary>Seção "Perfil" do appsettings (ou variáveis Perfil__Bucket e Perfil__Tabela na Lambda).</summary>
public sealed class PerfilOptions
{
    public const string Secao = "Perfil";

    /// <summary>Bucket do S3 (privado) com as fotos de perfil.</summary>
    public string Bucket { get; set; } = "";

    /// <summary>Tabela do DynamoDB com os dados do perfil (chave de partição "id", String).</summary>
    public string Tabela { get; set; } = "";

    public const long FotoTamanhoMaximo = 2 * 1024 * 1024;

    /// <summary>Tipos aceitos e a extensão usada no nome do arquivo no S3.</summary>
    public static readonly IReadOnlyDictionary<string, string> TiposFoto = new Dictionary<string, string>
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp"
    };
}

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace CumbucaSaas.API.PerfilUsuario;

/// <summary>
/// Fotos num bucket privado (de perfil, em "perfis/{sub}", e do restaurante, em
/// "tenants/{id}"); a tela recebe um link temporário para exibir.
/// </summary>
public interface IFotosPerfil
{
    /// <summary>Grava uma foto nova na <paramref name="pasta"/> e devolve a chave (caminho) no bucket.</summary>
    Task<string> SalvarAsync(string pasta, Stream conteudo, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Apaga sem lançar erro (a foto antiga ou a que sobrou de uma gravação que falhou).</summary>
    Task RemoverAsync(string chave);

    /// <summary>Link de leitura válido por uma hora.</summary>
    Task<string> LinkTemporarioAsync(string chave);
}

internal sealed class S3FotosPerfil(
    IAmazonS3 s3,
    IOptions<PerfilOptions> opcoes,
    ILogger<S3FotosPerfil> logger)
    : IFotosPerfil
{
    private string Bucket => opcoes.Value.Bucket;

    public async Task<string> SalvarAsync(
        string pasta,
        Stream conteudo,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Nome novo a cada troca: o link antigo (em cache no navegador) não mostra a foto nova errada.
        var extensao = PerfilOptions.TiposFoto[contentType];
        var chave = $"{pasta}/foto-{DateTime.UtcNow:yyyyMMddHHmmss}.{extensao}";

        await s3.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = Bucket,
                Key = chave,
                InputStream = conteudo,
                ContentType = contentType
            },
            cancellationToken);

        return chave;
    }

    public async Task RemoverAsync(string chave)
    {
        try
        {
            await s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = Bucket, Key = chave });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Não foi possível apagar a foto {Chave} do bucket.", chave);
        }
    }

    public Task<string> LinkTemporarioAsync(string chave) =>
        s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = chave,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddHours(1)
        });
}

namespace CumbucaSaas.API.Autenticacao;

/// <summary>Seção "Autenticacao" do appsettings (ou variáveis Autenticacao__...).</summary>
public sealed class AutenticacaoOptions
{
    public const string Secao = "Autenticacao";

    /// <summary>User Pool do Cognito: emissor do token (iss).</summary>
    public string Authority { get; set; } = "";

    /// <summary>App client do Cognito: o "aud" do ID token que o front envia.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>Id do User Pool (ex.: us-east-1_RrRiFZVGe): o último trecho do Authority.</summary>
    public string UserPoolId => Authority.TrimEnd('/').Split('/').Last();

    /// <summary>Região do User Pool (ex.: us-east-1): o prefixo do UserPoolId.</summary>
    public string Regiao => UserPoolId.Split('_')[0];
}

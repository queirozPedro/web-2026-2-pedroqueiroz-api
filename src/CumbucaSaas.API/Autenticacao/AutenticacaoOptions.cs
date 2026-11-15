namespace CumbucaSaas.API.Autenticacao;

/// <summary>Seção "Autenticacao" do appsettings (ou variáveis Autenticacao__...).</summary>
public sealed class AutenticacaoOptions
{
    public const string Secao = "Autenticacao";

    /// <summary>User Pool do Cognito: emissor do token (iss).</summary>
    public string Authority { get; set; } = "";

    /// <summary>App client do Cognito: o "aud" do ID token que o front envia.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>
    /// E-mails fora de @ufersa.edu.br que também entram como funcionário (ex.: conta
    /// pessoal para testes). Configure nos User Secrets ou na Lambda
    /// (Autenticacao__Funcionarios__0), não no appsettings do repositório.
    /// </summary>
    public string[] Funcionarios { get; set; } = [];
}

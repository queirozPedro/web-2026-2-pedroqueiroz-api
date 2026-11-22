using System.Text.RegularExpressions;
using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.UsuarioAggregate;

/// <summary>
/// Quem acessa o sistema (funcionário ou estudante), dentro de um tenant. A senha
/// fica no Cognito, nunca aqui. Enquanto o cadastro não for concluído (nome completo
/// e data de nascimento), o usuário não acessa nenhuma tela.
/// </summary>
public class Usuario : AggregateRoot, IPertenceAoTenant
{
    public const int NomeTamanhoMaximo = 150;
    public const int EmailTamanhoMaximo = 150;
    public const int IdExternoTamanhoMaximo = 100;

    /// <summary>Palavras do nome que precisam ter pelo menos <see cref="LetrasMinimasPorPalavra"/> letras.</summary>
    public const int PalavrasMinimasNome = 2;
    public const int LetrasMinimasPorPalavra = 3;

    public static readonly DateOnly DataNascimentoMinima = new(1900, 1, 1);

    // Letras (com acento), espaço, hífen e apóstrofo; começa por letra.
    private static readonly Regex CaracteresNome = new(@"^\p{L}[\p{L}'’\- ]*$", RegexOptions.Compiled);
    private static readonly Regex FormatoEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);
    private static readonly Regex Espacos = new(@"\s+", RegexOptions.Compiled);

    // Usado pelo EF Core.
    private Usuario()
    {
        IdExterno = null!;
        Email = null!;
        Perfil = null!;
    }

    /// <summary>Primeiro acesso: o cadastro começa incompleto (sem nome e data de nascimento).</summary>
    public Usuario(int tenantId, string idExterno, string email, Perfil perfil, DateTime criadoEmUtc)
    {
        if (tenantId <= 0)
        {
            throw new DomainException("O tenant do usuário é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(idExterno))
        {
            throw new DomainException("O id do provedor de login é obrigatório.");
        }

        TenantId = tenantId;
        IdExterno = idExterno;
        Email = null!;
        Perfil = null!;
        CriadoEm = criadoEmUtc;
        RegistrarAcesso(email, perfil);
    }

    /// <summary>Conta de e-mail e senha: o funcionário informa tudo no cadastro, que já nasce completo.</summary>
    public static Usuario CadastrarFuncionario(
        int tenantId,
        string idExterno,
        string email,
        string nome,
        DateOnly dataNascimento,
        DateOnly hoje,
        DateTime agoraUtc)
    {
        var usuario = new Usuario(tenantId, idExterno, email, Perfil.Funcionario, agoraUtc);
        usuario.ConcluirCadastro(nome, dataNascimento, hoje, agoraUtc);
        return usuario;
    }

    public int TenantId { get; private set; }

    /// <summary>"sub" do token do Cognito: identifica o usuário mesmo se o e-mail mudar.</summary>
    public string IdExterno { get; private set; }

    /// <summary>E-mail do login, em minúsculas. Único no sistema.</summary>
    public string Email { get; private set; }

    /// <summary>Nome completo; nulo até o cadastro ser concluído.</summary>
    public string? Nome { get; private set; }

    /// <summary>Nula até o cadastro ser concluído.</summary>
    public DateOnly? DataNascimento { get; private set; }

    public Perfil Perfil { get; private set; }

    /// <summary>Em UTC.</summary>
    public DateTime CriadoEm { get; private set; }

    /// <summary>Em UTC; nulo enquanto o usuário não informar nome e data de nascimento.</summary>
    public DateTime? CadastroConcluidoEm { get; private set; }

    public bool CadastroCompleto => CadastroConcluidoEm is not null;

    /// <summary>Atualiza com o que veio no login (o EF só grava se algo mudou).</summary>
    public void RegistrarAcesso(string email, Perfil perfil)
    {
        if (!EmailValido(email))
        {
            throw new DomainException("Informe um e-mail válido.");
        }

        Email = NormalizarEmail(email);
        Perfil = perfil;
    }

    /// <summary>Nome completo e data de nascimento, obrigatórios para acessar o sistema.</summary>
    public void ConcluirCadastro(string nome, DateOnly dataNascimento, DateOnly hoje, DateTime agoraUtc)
    {
        if (!NomeCompletoValido(nome))
        {
            throw new DomainException(
                $"Informe o nome completo: pelo menos {PalavrasMinimasNome} palavras com {LetrasMinimasPorPalavra} letras ou mais.");
        }

        if (!DataNascimentoValida(dataNascimento, hoje))
        {
            throw new DomainException("Informe uma data de nascimento válida (não pode ser no futuro).");
        }

        Nome = NormalizarNome(nome);
        DataNascimento = dataNascimento;
        CadastroConcluidoEm ??= agoraUtc;
    }

    /// <summary>Só letras, espaço, hífen e apóstrofo, com pelo menos 2 palavras de 3 letras ou mais.</summary>
    public static bool NomeCompletoValido(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return false;
        }

        var normalizado = NormalizarNome(nome);
        if (normalizado.Length > NomeTamanhoMaximo || !CaracteresNome.IsMatch(normalizado))
        {
            return false;
        }

        return normalizado
            .Split(' ')
            .Count(palavra => palavra.Count(char.IsLetter) >= LetrasMinimasPorPalavra) >= PalavrasMinimasNome;
    }

    public static bool DataNascimentoValida(DateOnly dataNascimento, DateOnly hoje) =>
        dataNascimento >= DataNascimentoMinima && dataNascimento <= hoje;

    public static bool EmailValido(string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && email.Trim().Length <= EmailTamanhoMaximo
        && FormatoEmail.IsMatch(email.Trim());

    public static string NormalizarEmail(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>Sem espaços repetidos nem nas pontas.</summary>
    public static string NormalizarNome(string nome) => Espacos.Replace(nome.Trim(), " ");
}

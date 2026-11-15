using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.UsuarioAggregate;

/// <summary>
/// Quem acessa o sistema (funcionário ou estudante). Criado no primeiro login: a
/// autenticação é do Cognito/Google, o sistema não guarda senha.
/// </summary>
public class Usuario : AggregateRoot
{
    public const int NomeTamanhoMaximo = 150;
    public const int EmailTamanhoMaximo = 150;
    public const int IdExternoTamanhoMaximo = 100;

    // Usado pelo EF Core.
    private Usuario()
    {
        IdExterno = null!;
        Email = null!;
        Nome = null!;
        Perfil = null!;
    }

    public Usuario(string idExterno, string email, string nome, Perfil perfil, DateTime criadoEmUtc)
    {
        if (string.IsNullOrWhiteSpace(idExterno))
        {
            throw new DomainException("O id do provedor de login é obrigatório.");
        }

        IdExterno = idExterno;
        Email = null!;
        Nome = null!;
        Perfil = null!;
        CriadoEm = criadoEmUtc;
        Atualizar(email, nome, perfil);
    }

    /// <summary>"sub" do token do Cognito: identifica o usuário mesmo se o e-mail mudar.</summary>
    public string IdExterno { get; private set; }

    public string Email { get; private set; }

    public string Nome { get; private set; }

    public Perfil Perfil { get; private set; }

    /// <summary>Em UTC.</summary>
    public DateTime CriadoEm { get; private set; }

    /// <summary>Atualiza com o que veio no login (o EF só grava se algo mudou).</summary>
    public void Atualizar(string email, string nome, Perfil perfil)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("O e-mail do usuário é obrigatório.");
        }

        Email = email.Trim().ToLowerInvariant();
        Nome = string.IsNullOrWhiteSpace(nome) ? Email.Split('@')[0] : nome.Trim();
        Perfil = perfil;
    }
}

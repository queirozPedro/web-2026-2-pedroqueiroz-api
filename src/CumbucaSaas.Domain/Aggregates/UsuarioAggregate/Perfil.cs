using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.UsuarioAggregate;

/// <summary>
/// Papel do usuário no sistema (RBAC), definido pela forma de login: Google
/// institucional é estudante; e-mail e senha (conta criada no cadastro) é funcionário.
/// </summary>
public sealed class Perfil : Enumeration
{
    /// <summary>Domínio aceito no login com Google (inclui subdomínios, como alunos.ufersa.edu.br).</summary>
    public const string DominioInstitucional = "ufersa.edu.br";

    public static readonly Perfil Funcionario = new(1, "funcionario");
    public static readonly Perfil Estudante = new(2, "estudante");

    private Perfil(int id, string name) : base(id, name)
    {
    }

    /// <summary>
    /// Google: só e-mail da UFERSA, sempre como estudante (outro domínio: sem perfil, nulo).
    /// E-mail e senha: funcionário.
    /// </summary>
    public static Perfil? PorFormaDeLogin(bool loginGoogle, string email)
    {
        if (!loginGoogle)
        {
            return Funcionario;
        }

        return EmailInstitucional(email) ? Estudante : null;
    }

    /// <summary>Domínio igual a ufersa.edu.br ou terminado em .ufersa.edu.br.</summary>
    public static bool EmailInstitucional(string email)
    {
        var normalizado = (email ?? string.Empty).Trim().ToLowerInvariant();
        var arroba = normalizado.LastIndexOf('@');
        if (arroba < 0)
        {
            return false;
        }

        var dominio = normalizado[(arroba + 1)..];
        return dominio == DominioInstitucional
               || dominio.EndsWith("." + DominioInstitucional, StringComparison.Ordinal);
    }
}

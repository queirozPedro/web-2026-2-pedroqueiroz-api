using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.UsuarioAggregate;

/// <summary>Papel do usuário no sistema (RBAC), definido pelo domínio do e-mail institucional.</summary>
public sealed class Perfil : Enumeration
{
    public const string DominioFuncionario = "@ufersa.edu.br";
    public const string DominioEstudante = "@alunos.ufersa.edu.br";

    public static readonly Perfil Funcionario = new(1, "funcionario");
    public static readonly Perfil Estudante = new(2, "estudante");

    private Perfil(int id, string name) : base(id, name)
    {
    }

    /// <summary>
    /// @ufersa.edu.br é funcionário e @alunos.ufersa.edu.br é estudante. Os
    /// <paramref name="funcionariosAutorizados"/> (configuração) também entram como
    /// funcionário, para testes com contas fora do domínio. Fora disso: sem perfil (nulo).
    /// </summary>
    public static Perfil? PorEmail(string email, IEnumerable<string> funcionariosAutorizados)
    {
        var normalizado = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (funcionariosAutorizados.Any(f => string.Equals(f.Trim(), normalizado, StringComparison.OrdinalIgnoreCase))
            || normalizado.EndsWith(DominioFuncionario, StringComparison.Ordinal))
        {
            return Funcionario;
        }

        return normalizado.EndsWith(DominioEstudante, StringComparison.Ordinal) ? Estudante : null;
    }
}

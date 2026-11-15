using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.EstudanteAggregate;

/// <summary>
/// Estudante da UFERSA que consome no RU e envia feedbacks. Nome e e-mail ficam aqui
/// porque o cadastro vem antes do primeiro login; na fase de autenticação, o Usuario
/// criado no login é ligado ao Estudante pelo e-mail institucional.
/// </summary>
public class Estudante : AggregateRoot
{
    public const string DominioInstitucional = "@alunos.ufersa.edu.br";
    public const int NomeTamanhoMaximo = 150;
    public const int EmailTamanhoMaximo = 150;
    public const int MatriculaTamanhoMaximo = 20;
    public const int CursoTamanhoMaximo = 100;

    // Usado pelo EF Core.
    private Estudante()
    {
        Nome = null!;
        Email = null!;
        Matricula = null!;
        Curso = null!;
        Vinculo = null!;
        TipoSubsidio = null!;
    }

    public Estudante(string nome, string email, string matricula, string curso, TipoSubsidio tipoSubsidio)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(matricula) || string.IsNullOrWhiteSpace(curso))
        {
            throw new DomainException("Nome, matrícula e curso são obrigatórios.");
        }

        Email = NormalizarEmail(email);
        if (!Email.EndsWith(DominioInstitucional, StringComparison.Ordinal))
        {
            throw new DomainException($"O e-mail do estudante deve terminar em {DominioInstitucional}.");
        }

        Nome = nome.Trim();
        Matricula = matricula.Trim();
        Curso = curso.Trim();
        TipoSubsidio = tipoSubsidio;
        Vinculo = Vinculo.Ativo;
    }

    public string Nome { get; private set; }

    /// <summary>E-mail institucional, sempre em minúsculas.</summary>
    public string Email { get; private set; }

    public string Matricula { get; private set; }

    public string Curso { get; private set; }

    public Vinculo Vinculo { get; private set; }

    public TipoSubsidio TipoSubsidio { get; private set; }

    /// <summary>Usuário (login) do estudante; nulo até o primeiro acesso dele ao sistema.</summary>
    public int? UsuarioId { get; private set; }

    /// <summary>Só estudante com vínculo ativo consome como estudante e envia feedback.</summary>
    public bool EstaAtivo => Vinculo.Id == Vinculo.Ativo.Id;

    public void AlterarVinculo(Vinculo vinculo) => Vinculo = vinculo;

    /// <summary>Liga o cadastro ao usuário que fez login com o mesmo e-mail institucional.</summary>
    public void VincularUsuario(int usuarioId)
    {
        if (UsuarioId is not null && UsuarioId != usuarioId)
        {
            throw new DomainException("Este estudante já está ligado a outro usuário.");
        }

        UsuarioId = usuarioId;
    }

    /// <summary>Mesma normalização usada para checar e-mail duplicado.</summary>
    public static string NormalizarEmail(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}

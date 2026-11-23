using System.Globalization;
using System.Text.RegularExpressions;
using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.Domain.Aggregates.TenantAggregate;

/// <summary>
/// Restaurante Universitário atendido pelo sistema: nome, local, horário de funcionamento
/// por dia da semana, foto e cores do app. Usuários, estudantes, turnos, consumos e
/// feedbacks pertencem a um tenant pela coluna TenantId (sem navegação, como entre
/// agregados). Por enquanto existe um só (gravado pela migration) e todo usuário entra nele.
/// </summary>
public class Tenant : AggregateRoot
{
    public const int NomeTamanhoMaximo = 150;
    public const int NomeResponsavelTamanhoMaximo = 150;
    public const int EnderecoTamanhoMaximo = 200;
    public const int CidadeTamanhoMaximo = 100;
    public const int FotoChaveTamanhoMaximo = 300;

    /// <summary>
    /// Contraste mínimo das cores com o texto branco (o menu lateral e o item ativo/botões
    /// têm texto branco): 3:1, o mínimo da WCAG para componentes e texto em destaque.
    /// </summary>
    public const double ContrasteMinimoComBranco = 3.0;

    /// <summary>Tenant em que todo usuário novo entra, até existir a divisão entre tenants.</summary>
    public const int PadraoId = 1;

    public static readonly IReadOnlyList<string> Ufs =
    [
        "AC", "AL", "AM", "AP", "BA", "CE", "DF", "ES", "GO", "MA", "MG", "MS", "MT", "PA",
        "PB", "PE", "PI", "PR", "RJ", "RN", "RO", "RR", "RS", "SC", "SE", "SP", "TO"
    ];

    private static readonly Regex FormatoCor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    private readonly List<HorarioFuncionamento> _horarios = [];

    // Usado pelo EF Core.
    private Tenant()
    {
        Nome = null!;
        NomeResponsavel = null!;
        Cidade = null!;
        Uf = null!;
        CorPrincipal = null!;
        CorSecundaria = null!;
    }

    public string Nome { get; private set; }

    public string NomeResponsavel { get; private set; }

    /// <summary>Rua, número, bairro (opcional).</summary>
    public string? Endereco { get; private set; }

    public string Cidade { get; private set; }

    /// <summary>Sigla do estado, em maiúsculas (ex.: "RN").</summary>
    public string Uf { get; private set; }

    /// <summary>"#rrggbb": fundo do menu lateral.</summary>
    public string CorPrincipal { get; private set; }

    /// <summary>"#rrggbb": destaques (item ativo do menu, botões, links).</summary>
    public string CorSecundaria { get; private set; }

    /// <summary>Chave da foto no bucket do S3; nula sem foto.</summary>
    public string? FotoChave { get; private set; }

    public IReadOnlyCollection<HorarioFuncionamento> Horarios => _horarios.AsReadOnly();

    public void AtualizarDados(string nome, string nomeResponsavel, string? endereco, string cidade, string uf)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(nomeResponsavel) || string.IsNullOrWhiteSpace(cidade))
        {
            throw new DomainException("Nome, responsável e cidade são obrigatórios.");
        }

        if (!UfValida(uf))
        {
            throw new DomainException("Informe uma UF válida (ex.: RN).");
        }

        Nome = nome.Trim();
        NomeResponsavel = nomeResponsavel.Trim();
        Endereco = string.IsNullOrWhiteSpace(endereco) ? null : endereco.Trim();
        Cidade = cidade.Trim();
        Uf = uf.Trim().ToUpperInvariant();
    }

    public void DefinirCores(string principal, string secundaria)
    {
        if (!CorValida(principal) || !CorValida(secundaria))
        {
            throw new DomainException("As cores precisam estar no formato #RRGGBB.");
        }

        if (!ContrasteSuficiente(principal) || !ContrasteSuficiente(secundaria))
        {
            throw new DomainException("As cores precisam ser escuras o bastante para o texto branco ficar legível.");
        }

        CorPrincipal = principal.ToLowerInvariant();
        CorSecundaria = secundaria.ToLowerInvariant();
    }

    /// <summary>
    /// Substitui o horário da semana. No máximo um almoço e uma janta por dia, e o almoço
    /// termina antes de a janta abrir. Dia sem horário = fechado.
    /// </summary>
    public void DefinirHorarios(IEnumerable<HorarioFuncionamento> horarios)
    {
        var lista = horarios.ToList();
        if (ErroNosHorarios(lista) is { } erro)
        {
            throw new DomainException(erro);
        }

        // Atualiza no lugar (dia + turno é a chave da linha): o que saiu é removido, o que
        // já existia muda de horário e o que é novo entra.
        _horarios.RemoveAll(atual => !lista.Any(novo => novo.MesmoTurno(atual)));
        foreach (var novo in lista)
        {
            var atual = _horarios.FirstOrDefault(h => h.MesmoTurno(novo));
            if (atual is null)
            {
                _horarios.Add(novo);
            }
            else
            {
                atual.AlterarHorario(novo.Abertura, novo.Fechamento);
            }
        }
    }

    /// <summary>Troca (ou remove, com nulo) a foto. Devolve a chave anterior, para apagar do bucket.</summary>
    public string? DefinirFoto(string? chave)
    {
        var anterior = FotoChave;
        FotoChave = string.IsNullOrWhiteSpace(chave) ? null : chave;
        return anterior;
    }

    public static bool CorValida(string? cor) => cor is not null && FormatoCor.IsMatch(cor);

    public static bool UfValida(string? uf) =>
        uf is not null && Ufs.Contains(uf.Trim().ToUpperInvariant());

    /// <summary>Contraste (WCAG) entre a cor e o branco, de 1 a 21.</summary>
    public static double ContrasteComBranco(string cor)
    {
        static double Canal(string hex)
        {
            var valor = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return valor <= 0.03928 ? valor / 12.92 : Math.Pow((valor + 0.055) / 1.055, 2.4);
        }

        var luminancia = 0.2126 * Canal(cor.Substring(1, 2))
                         + 0.7152 * Canal(cor.Substring(3, 2))
                         + 0.0722 * Canal(cor.Substring(5, 2));
        return 1.05 / (luminancia + 0.05);
    }

    public static bool ContrasteSuficiente(string? cor) =>
        CorValida(cor) && ContrasteComBranco(cor!) >= ContrasteMinimoComBranco;

    /// <summary>Mensagem do primeiro problema no horário da semana, ou nulo se estiver tudo certo.</summary>
    public static string? ErroNosHorarios(IReadOnlyCollection<HorarioFuncionamento> horarios)
    {
        foreach (var dia in horarios.GroupBy(h => h.DiaSemana))
        {
            var nome = NomeDoDia(dia.Key);
            if (dia.GroupBy(h => h.Turno.Id).Any(t => t.Count() > 1))
            {
                return $"{nome}: informe no máximo um almoço e uma janta.";
            }

            var almoco = dia.FirstOrDefault(h => h.Turno.Id == Turno.Almoco.Id);
            var janta = dia.FirstOrDefault(h => h.Turno.Id == Turno.Janta.Id);
            if (almoco is not null && janta is not null && almoco.Fechamento > janta.Abertura)
            {
                return $"{nome}: o almoço precisa terminar antes de a janta abrir.";
            }
        }

        return null;
    }

    public static string NomeDoDia(DayOfWeek dia) => dia switch
    {
        DayOfWeek.Sunday => "Domingo",
        DayOfWeek.Monday => "Segunda-feira",
        DayOfWeek.Tuesday => "Terça-feira",
        DayOfWeek.Wednesday => "Quarta-feira",
        DayOfWeek.Thursday => "Quinta-feira",
        DayOfWeek.Friday => "Sexta-feira",
        DayOfWeek.Saturday => "Sábado",
        _ => dia.ToString()
    };
}

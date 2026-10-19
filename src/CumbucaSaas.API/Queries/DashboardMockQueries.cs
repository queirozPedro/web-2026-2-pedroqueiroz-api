using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using CumbucaSaas.Domain.SeedWork;

namespace CumbucaSaas.API.Queries;

/*
 * Dados fictícios do RU seguindo as entidades da especificação (Produção,
 * Registros de Consumo, Pagamentos/Caixa, Feedbacks e Avaliações).
 * São determinísticos: a semente vem da data, então cada chamada devolve os
 * mesmos números. Quando as tabelas existirem, basta registrar uma
 * implementação Dapper de IDashboardQueries no lugar desta.
 */
public sealed class DashboardMockQueries : IDashboardQueries
{
    /// <summary>Preços de referência (R$) usados no valor estimado.</summary>
    private const double PrecoSubsidiado = 3;
    private const double PrecoIntegral = 15;

    private const int JanelaDias = 180;

    private static readonly DateOnly SegundaDeReferencia = new(2024, 1, 1);

    /// <summary>Multiplicador de demanda por dia da semana (índice 0 = domingo).</summary>
    private static readonly double[] FatorDia = [0, 0.94, 1.05, 1.08, 1.04, 0.82, 0];

    private static readonly string[] FeriadosFixos = ["01-01", "04-21", "05-01", "09-07", "10-12", "11-02", "11-15", "12-25"];

    private sealed record ParametrosTurno(Turno Turno, int Base, double EstudantesMin, double EstudantesMax, double[] Perfil);

    private static readonly ParametrosTurno[] Turnos =
    [
        new(Turno.Almoco, 520, 0.86, 0.92, [0.28, 0.46, 0.26]),
        new(Turno.Janta, 300, 0.9, 0.95, [0.3, 0.45, 0.25])
    ];

    private static readonly Dictionary<CategoriaAvaliacao, double> MediaNotas = new()
    {
        [CategoriaAvaliacao.Qualidade] = 4.0,
        [CategoriaAvaliacao.Sabor] = 3.8,
        [CategoriaAvaliacao.Temperatura] = 3.4,
        [CategoriaAvaliacao.Variedade] = 3.3,
        [CategoriaAvaliacao.Atendimento] = 4.2,
        [CategoriaAvaliacao.Limpeza] = 4.4,
        [CategoriaAvaliacao.Estrutura] = 3.9
    };

    private sealed record CategoriaFeedback(string Nome, double Peso, string[] Comentarios);

    private static readonly CategoriaFeedback[] CategoriasFeedback =
    [
        new("Cardápio", 0.3,
        [
            "Poderia ter mais opções vegetarianas.",
            "A salada estava ótima hoje.",
            "O acompanhamento se repete muito na semana."
        ]),
        new("Filas e espera", 0.24,
        [
            "Fila muito longa perto do meio-dia.",
            "Faltou organização na entrada.",
            "A fila andou rápido hoje, parabéns."
        ]),
        new("Atendimento", 0.14,
        [
            "Equipe muito educada.",
            "Demora para repor a comida na linha.",
            "Atenderam bem quem tem restrição alimentar."
        ]),
        new("Higiene", 0.1, ["Mesas sujas no fim do turno.", "Talheres bem higienizados."]),
        new("Estrutura", 0.12, ["Poucos ventiladores no salão.", "Faltam mesas no horário de pico."]),
        new("Preço", 0.1, ["Valor justo para a qualidade.", "O preço integral está alto para visitantes."])
    ];

    public Task<DashboardViewModel> ObterAsync(DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default)
    {
        var dados = Gerar(DateOnly.FromDateTime(DateTime.Today));

        bool NoPeriodo(DateOnly data) => (inicio is null || data >= inicio) && (fim is null || data <= fim);

        return Task.FromResult(new DashboardViewModel(
            dados.Registros.Where(r => NoPeriodo(r.Data)).ToList(),
            dados.Feedbacks.Where(f => NoPeriodo(f.Data)).ToList()));
    }

    /// <summary>Gera ~180 dias terminando em <paramref name="hoje"/>, só dias úteis, sem feriados fixos nem o recesso de julho.</summary>
    private static DashboardViewModel Gerar(DateOnly hoje)
    {
        var primeiro = hoje.AddDays(-(JanelaDias - 1));
        var registros = new List<RegistroTurnoViewModel>();
        var feedbacks = new List<FeedbackViewModel>();

        for (var i = 0; i < JanelaDias; i++)
        {
            var data = primeiro.AddDays(i);
            if (!TemServico(data))
            {
                continue;
            }

            var progresso = (double)i / (JanelaDias - 1);
            // Segunda-feira de referência para numerar as semanas de forma estável.
            var semana = (data.DayNumber - SegundaDeReferencia.DayNumber) / 7;

            foreach (var parametros in Turnos)
            {
                registros.Add(GerarRegistro(data, parametros, progresso, semana));
            }

            GerarFeedbacks(data, hoje, semana, feedbacks);
        }

        return new DashboardViewModel(registros, feedbacks);
    }

    private static bool TemServico(DateOnly data)
    {
        if (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        var recesso = data.Month == 7 && data.Day is >= 6 and <= 19;
        return !recesso && !FeriadosFixos.Contains(data.ToString("MM-dd"));
    }

    private static RegistroTurnoViewModel GerarRegistro(DateOnly data, ParametrosTurno parametros, double progresso, int semana)
    {
        var rnd = Aleatorio(Semente($"{Iso(data)}|{parametros.Turno.Name}"));

        var demanda = parametros.Base * FatorDia[(int)data.DayOfWeek] * (0.92 + rnd() * 0.16);
        var servido = Arredondar(demanda);

        // Algumas semanas a produção sai bem acima da demanda (dispara os alertas de excesso);
        // de vez em quando a comida acaba (excedente zero).
        var semanaDeExcesso = semana % 5 == 2;
        var margem = rnd() < 0.05 ? 0 : semanaDeExcesso ? 0.12 + rnd() * 0.1 : 0.02 + rnd() * 0.08;
        var produzido = Math.Max(servido, Arredondar(servido * (1 + margem)));

        var estudantes = Arredondar(servido *
            (parametros.EstudantesMin + rnd() * (parametros.EstudantesMax - parametros.EstudantesMin)));
        var externos = servido - estudantes;

        var estimado = Arredondar2(estudantes * PrecoSubsidiado + externos * PrecoIntegral);
        // Arrecadado costuma ficar um pouco abaixo do estimado (entre -3,5% e +0,5%).
        var real = Arredondar2(estimado * (0.965 + rnd() * 0.04));
        // O Pix ganha espaço ao longo do período.
        var fatiaPix = 0.5 + 0.2 * progresso + (rnd() - 0.5) * 0.08;
        var pix = Arredondar2(real * fatiaPix);

        var primeiraHora = Arredondar(servido * (parametros.Perfil[0] + (rnd() - 0.5) * 0.06));
        var ultimaHora = Arredondar(servido * (parametros.Perfil[2] + (rnd() - 0.5) * 0.06));

        return new RegistroTurnoViewModel(
            data,
            parametros.Turno.Name,
            produzido,
            servido,
            produzido - servido,
            estudantes,
            externos,
            Arredondar2(real - pix),
            pix,
            estimado,
            real,
            [primeiraHora, servido - primeiraHora - ultimaHora, ultimaHora]);
    }

    private static void GerarFeedbacks(DateOnly data, DateOnly hoje, int semana, List<FeedbackViewModel> destino)
    {
        var rnd = Aleatorio(Semente($"feedback|{Iso(data)}"));
        var quantidade = 3 + (int)Math.Floor(rnd() * 6);
        var idade = hoje.DayNumber - data.DayNumber;

        for (var i = 0; i < quantidade; i++)
        {
            var categoria = SortearCategoria(rnd());
            var comentario = categoria.Comentarios[(int)Math.Floor(rnd() * categoria.Comentarios.Length)];

            var notas = new Dictionary<string, int>();
            foreach (var avaliacao in Enumeration.GetAll<CategoriaAvaliacao>())
            {
                // Em algumas semanas o sabor cai, para o painel ter o que apontar.
                var ajuste = semana % 7 == 3 && avaliacao == CategoriaAvaliacao.Sabor ? -0.7 : 0;
                var nota = Arredondar(MediaNotas[avaliacao] + ajuste + (rnd() - 0.5) * 2.6);
                notas[avaliacao.Name] = Math.Clamp(nota, Avaliacao.NotaMinima, Avaliacao.NotaMaxima);
            }

            // Feedbacks antigos tendem a estar resolvidos; os recentes ainda estão em aberto.
            var status = rnd() < Math.Min(0.9, idade / 60.0)
                ? StatusFeedback.Resolvido
                : rnd() < 0.5 ? StatusFeedback.EmAnalise : StatusFeedback.Novo;

            destino.Add(new FeedbackViewModel(destino.Count + 1, data, status.Name, categoria.Nome, comentario, notas));
        }
    }

    private static CategoriaFeedback SortearCategoria(double sorteio)
    {
        var acumulado = 0.0;
        foreach (var categoria in CategoriasFeedback)
        {
            acumulado += categoria.Peso;
            if (sorteio < acumulado)
            {
                return categoria;
            }
        }

        return CategoriasFeedback[^1];
    }

    /* ---------- Aleatoriedade determinística ---------- */

    /// <summary>Hash FNV-1a do texto.</summary>
    private static uint Semente(string texto)
    {
        var hash = 2166136261u;
        foreach (var c in texto)
        {
            hash ^= c;
            hash = unchecked(hash * 16777619u);
        }

        return hash;
    }

    /// <summary>mulberry32: gerador pseudoaleatório pequeno, devolve valores em [0, 1).</summary>
    private static Func<double> Aleatorio(uint semente)
    {
        var estado = semente;
        return () =>
        {
            unchecked
            {
                estado += 0x6D2B79F5u;
                var t = estado;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        };
    }

    /* ---------- Utilitários ---------- */

    private static string Iso(DateOnly data) => data.ToString("yyyy-MM-dd");

    /// <summary>Arredonda .5 para cima, como o Math.round do JavaScript.</summary>
    private static int Arredondar(double valor) => (int)Math.Floor(valor + 0.5);

    private static double Arredondar2(double valor) => Math.Floor(valor * 100 + 0.5) / 100;
}

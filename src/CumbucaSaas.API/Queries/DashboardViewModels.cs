namespace CumbucaSaas.API.Queries;

/*
 * Formato de leitura consumido pelo front (dashboard.models.ts). Datas saem como
 * "YYYY-MM-DD" e turno/status/notas usam os Names das Enumerations do domínio.
 */

/// <summary>Um turno de um dia: junta Produção, Registros de Consumo e Pagamentos/Caixa.</summary>
public sealed record RegistroTurnoViewModel(
    DateOnly Data,
    string Turno,
    int Produzido,
    int Servido,
    int Excedente,
    int Estudantes,
    int Externos,
    double Dinheiro,
    double Pix,
    double Estimado,
    double Real,
    IReadOnlyList<int> FluxoPorHora);

public sealed record FeedbackViewModel(
    int Id,
    DateOnly Data,
    string Status,
    string Categoria,
    string Comentario,
    IReadOnlyDictionary<string, int> Notas);

public sealed record DashboardViewModel(
    IReadOnlyList<RegistroTurnoViewModel> Registros,
    IReadOnlyList<FeedbackViewModel> Feedbacks);

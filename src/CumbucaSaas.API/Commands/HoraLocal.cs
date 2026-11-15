namespace CumbucaSaas.API.Commands;

/// <summary>
/// Data e hora no fuso do RU. O Lambda roda em UTC; Pau dos Ferros (RN) fica em
/// UTC-3 o ano todo (sem horário de verão), então basta o deslocamento fixo.
/// </summary>
internal static class HoraLocal
{
    private static readonly TimeSpan Fuso = TimeSpan.FromHours(-3);

    public static DateTime Agora => DateTime.UtcNow + Fuso;

    public static DateOnly Hoje => DateOnly.FromDateTime(Agora);

    public static TimeOnly HorarioAtual => TimeOnly.FromDateTime(Agora);
}

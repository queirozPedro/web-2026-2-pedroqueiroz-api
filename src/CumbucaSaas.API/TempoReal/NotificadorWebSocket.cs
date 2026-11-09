using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CumbucaSaas.API.TempoReal;

public sealed class TempoRealOptions
{
    public const string Secao = "TempoReal";

    /// <summary>URL do stage da WebSocket API (wss://...). Vazia desliga as notificações.</summary>
    public string? WebSocketUrl { get; set; }
}

/// <summary>Avisa todos os fronts conectados de que algo mudou.</summary>
public interface INotificadorTempoReal
{
    Task NotificarAsync(string tipo, CancellationToken cancellationToken = default);
}

/// <summary>
/// Conecta na WebSocket API como cliente e manda {"action":"broadcast"}; a Lambda
/// do WebSocket repassa a mensagem para todas as conexões abertas.
/// Falhas só vão para o log: a notificação não pode desfazer o command.
/// </summary>
public sealed class NotificadorWebSocket(IOptions<TempoRealOptions> options, ILogger<NotificadorWebSocket> logger)
    : INotificadorTempoReal
{
    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(5);

    public async Task NotificarAsync(string tipo, CancellationToken cancellationToken = default)
    {
        var url = options.Value.WebSocketUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(TempoLimite);

        try
        {
            using var socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(url), limite.Token);

            var corpo = JsonSerializer.SerializeToUtf8Bytes(new
            {
                action = "broadcast",
                mensagem = new { tipo, em = DateTimeOffset.UtcNow }
            });
            await socket.SendAsync(new ArraySegment<byte>(corpo), WebSocketMessageType.Text, endOfMessage: true, limite.Token);
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "ok", limite.Token);
        }
        catch (Exception erro) when (erro is WebSocketException or OperationCanceledException or UriFormatException)
        {
            logger.LogWarning(erro, "Não foi possível notificar o WebSocket ({Tipo}).", tipo);
        }
    }
}

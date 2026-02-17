using Microsoft.AspNetCore.SignalR;

namespace ExulofraApi.Infrastructure.SignalR;

public class TranslationHub : Hub
{
    private readonly ILogger<TranslationHub> _logger;

    public TranslationHub(ILogger<TranslationHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        _logger.LogInformation($"Client {Context.ConnectionId} joined session {sessionId}");
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        _logger.LogInformation($"Client {Context.ConnectionId} left session {sessionId}");
    }

    // Client streams audio to server
    public async Task StartStream(string translationId, IAsyncEnumerable<byte[]> audioStream)
    {
        _logger.LogInformation($"Starting audio stream for translation {translationId}");

        // TODO: Integrate with AzureSpeechService here to process the stream
        await foreach (var chunk in audioStream)
        {
            // Placeholder: Process audio chunk
            // await _speechService.ProcessAudioChunkAsync(translationId, chunk);
        }
    }
}

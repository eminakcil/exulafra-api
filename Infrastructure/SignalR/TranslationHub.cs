using ExulofraApi.Common.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace ExulofraApi.Infrastructure.SignalR;

public class TranslationHub : Hub
{
    private readonly ILogger<TranslationHub> _logger;
    private readonly ISpeechService _speechService;

    public TranslationHub(ILogger<TranslationHub> logger, ISpeechService speechService)
    {
        _logger = logger;
        _speechService = speechService;
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

        try
        {
            await _speechService.ProcessAudioStreamAsync(translationId, audioStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing audio stream for translation {TranslationId}",
                translationId
            );
            throw;
        }
    }
}

using ExulofraApi.Common.Abstractions;
using ExulofraApi.Infrastructure.Options;
using ExulofraApi.Infrastructure.Persistence;
using ExulofraApi.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExulofraApi.Infrastructure.Services;

public class AzureSpeechService : ISpeechService
{
    private readonly ILogger<AzureSpeechService> _logger;
    private readonly AzureOptions _options;
    private readonly IHubContext<TranslationHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public AzureSpeechService(
        ILogger<AzureSpeechService> logger,
        IOptions<AzureOptions> options,
        IHubContext<TranslationHub> hubContext,
        IServiceScopeFactory scopeFactory
    )
    {
        _logger = logger;
        _options = options.Value;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    public async Task ProcessAudioStreamAsync(
        string translationId,
        IAsyncEnumerable<byte[]> audioStream
    )
    {
        _logger.LogInformation($"Processing audio stream for translation {translationId}");

        // Usage of options
        var speechKey = _options.SpeechKey;
        var speechRegion = _options.SpeechRegion;

        if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
        {
            throw new InvalidOperationException("Azure Speech configuration is missing.");
        }

        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        // Determine language from translationId (fetch from DB) using scope
        string sourceLang = "en-US"; // Default

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // var translation = await context.Translations.FindAsync(Guid.Parse(translationId));
            // if (translation != null) sourceLang = translation.SourceLang; // ...
            // Simplified for now, assuming args passed or fetched.
        }

        speechConfig.SpeechRecognitionLanguage = sourceLang;

        using var pushStream = AudioInputStream.CreatePushStream(
            AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1)
        );
        using var audioConfig = AudioConfig.FromStreamInput(pushStream);
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        var stopRecognitionToCheck = new TaskCompletionSource<int>();

        recognizer.Recognizing += async (s, e) =>
        {
            await _hubContext
                .Clients.Group(translationId)
                .SendAsync("ReceivePartialResult", e.Result.Text);
        };

        recognizer.Recognized += async (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                await _hubContext
                    .Clients.Group(translationId)
                    .SendAsync("ReceiveFinalResult", e.Result.Text);
                // Trigger translation/TTS here in future steps
            }
        };

        recognizer.Canceled += (s, e) =>
        {
            _logger.LogWarning($"Recognition canceled: {e.Reason}. ErrorDetails: {e.ErrorDetails}");
            stopRecognitionToCheck.TrySetResult(0);
        };

        recognizer.SessionStopped += (s, e) =>
        {
            _logger.LogInformation("Recognition session stopped.");
            stopRecognitionToCheck.TrySetResult(0);
        };

        await recognizer.StartContinuousRecognitionAsync();

        await foreach (var chunk in audioStream)
        {
            pushStream.Write(chunk);
        }

        pushStream.Close();

        // Wait for recognition to finish processing remaining buffered data
        // For simplicity/timeout, assuming end of stream ends session or we wait a bit
        await Task.WhenAny(stopRecognitionToCheck.Task, Task.Delay(2000));
        await recognizer.StopContinuousRecognitionAsync();
    }

    public async Task<byte[]> TextToSpeechAsync(string text, string targetVoice)
    {
        _logger.LogInformation($"Synthesizing speech for: {text}");
        // Implementation with Azure Speech Synthesizer
        await Task.CompletedTask; // fix lint
        return Array.Empty<byte>();
    }
}

using System.Threading.Channels;
using ExulofraApi.Common.Abstractions;
using ExulofraApi.Features.Translations.ProcessAudio.Events;
using ExulofraApi.Infrastructure.Options;
using ExulofraApi.Infrastructure.Persistence;
using MediatR;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExulofraApi.Infrastructure.Services;

public record TranslationPayload(Guid Id, string Text);

public class AzureSpeechService : ISpeechService
{
    private readonly ILogger<AzureSpeechService> _logger;
    private readonly AzureOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPublisher _publisher;

    public AzureSpeechService(
        ILogger<AzureSpeechService> logger,
        IOptions<AzureOptions> options,
        IServiceScopeFactory scopeFactory,
        IPublisher publisher
    )
    {
        _logger = logger;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _publisher = publisher;
    }

    public async Task ProcessAudioStreamAsync(
        string translationId,
        IAsyncEnumerable<byte[]> audioStream
    )
    {
        var config = await GetTranslationConfigAsync(translationId);

        var translationConfig = SpeechTranslationConfig.FromSubscription(
            _options.SpeechKey,
            _options.SpeechRegion
        );
        translationConfig.SpeechRecognitionLanguage = config.SourceLang;
        translationConfig.AddTargetLanguage(config.TargetLang);

        var synthConfig = SpeechConfig.FromSubscription(_options.SpeechKey, _options.SpeechRegion);
        synthConfig.SpeechSynthesisVoiceName = config.TargetVoice;

        using var synthesizer = new SpeechSynthesizer(synthConfig, null);
        using var pushStream = AudioInputStream.CreatePushStream(
            AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1)
        );
        using var audioConfig = AudioConfig.FromStreamInput(pushStream);
        using var recognizer = new TranslationRecognizer(translationConfig, audioConfig);

        var stopTask = new TaskCompletionSource<int>();
        bool isUserConnected = true;

        var payloadChannel = Channel.CreateUnbounded<TranslationPayload>();

        recognizer.Recognizing += (s, e) =>
        {
            var partialText = e.Result.Translations.Values.FirstOrDefault() ?? e.Result.Text;
            if (!string.IsNullOrWhiteSpace(partialText))
            {
                _logger.LogDebug(
                    "Partial recognition. TranslationId: {TranslationId}, Text: {Text}",
                    translationId,
                    partialText
                );
                _publisher.Publish(
                    new PartialTextRecognizedEvent(Guid.Parse(translationId), partialText)
                );
            }
        };

        recognizer.Recognized += (s, e) =>
        {
            if (!isUserConnected)
                return;

            if (e.Result.Reason == ResultReason.TranslatedSpeech)
            {
                var finalTranslation = e.Result.Translations.Values.FirstOrDefault();
                var sourceText = e.Result.Text;

                if (!string.IsNullOrWhiteSpace(finalTranslation))
                {
                    var segmentId = Guid.CreateVersion7();
                    var timestamp = TimeSpan.FromTicks(e.Result.OffsetInTicks);
                    var speakerTag = "Speaker";

                    _logger.LogInformation(
                        "Segment recognized. TranslationId: {TranslationId}, SegmentId: {SegmentId}, Offset: {Offset}",
                        translationId,
                        segmentId,
                        timestamp
                    );

                    _publisher.Publish(
                        new SegmentTranslatedEvent(
                            Guid.Parse(translationId),
                            segmentId,
                            sourceText,
                            finalTranslation,
                            speakerTag,
                            timestamp
                        )
                    );

                    payloadChannel.Writer.TryWrite(
                        new TranslationPayload(segmentId, finalTranslation)
                    );
                }
            }
        };

        recognizer.Canceled += (s, e) => stopTask.TrySetResult(0);
        recognizer.SessionStopped += (s, e) => stopTask.TrySetResult(0);

        var synthesisTask = Task.Run(async () =>
        {
            await foreach (var payload in payloadChannel.Reader.ReadAllAsync())
            {
                var result = await synthesizer.SpeakTextAsync(payload.Text);

                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    string fileName = $"final_{translationId}_{payload.Id}.wav";
                    await File.WriteAllBytesAsync(fileName, result.AudioData);

                    _logger.LogInformation(
                        "Audio synthesized successfully. TranslationId: {TranslationId}, SegmentId: {SegmentId}",
                        translationId,
                        payload.Id
                    );

                    await _publisher.Publish(
                        new AudioSynthesizedEvent(Guid.Parse(translationId), payload.Id, fileName)
                    );
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    _logger.LogError(
                        "Audio synthesis canceled. TranslationId: {TranslationId}, Reason: {Reason}, ErrorDetails: {ErrorDetails}",
                        translationId,
                        cancellation.Reason,
                        cancellation.ErrorDetails
                    );
                }
            }
        });

        try
        {
            await recognizer.StartContinuousRecognitionAsync();

            await foreach (var chunk in audioStream)
            {
                if (chunk != null && chunk.Length > 0)
                {
                    pushStream.Write(chunk);
                }
            }
        }
        catch (OperationCanceledException)
        {
            isUserConnected = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred - Session: {TranslationId}", translationId);
        }
        finally
        {
            isUserConnected = false;
            pushStream.Close();

            await recognizer.StopContinuousRecognitionAsync();

            payloadChannel.Writer.Complete();
            await synthesisTask;
        }
    }

    private async Task<(
        string SourceLang,
        string TargetLang,
        string TargetVoice
    )> GetTranslationConfigAsync(string id)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (Guid.TryParse(id, out var gId))
        {
            var t = await context.Translations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == gId);
            if (t != null)
                return (t.SourceLang, t.TargetLang, t.TargetVoice);
        }

        return ("en-US", "tr", "tr-TR-AhmetNeural");
    }
}

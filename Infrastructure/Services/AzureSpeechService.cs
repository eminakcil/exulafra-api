using System.Threading.Channels;
using ExulofraApi.Common.Abstractions;
using ExulofraApi.Domain.Entities;
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

        using var pushStream = AudioInputStream.CreatePushStream(
            AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1)
        );
        using var audioConfig = AudioConfig.FromStreamInput(pushStream);

        TranslationRecognizer recognizer;

        if (config.SessionType == SessionType.Dialogue)
        {
            translationConfig.SetProperty(
                PropertyId.SpeechServiceConnection_LanguageIdMode,
                "Continuous"
            );

            var autoDetectConfig = AutoDetectSourceLanguageConfig.FromLanguages([
                config.SourceLang,
                config.TargetLang,
            ]);

            translationConfig.AddTargetLanguage(config.SourceLang.Split('-')[0]);
            translationConfig.AddTargetLanguage(config.TargetLang.Split('-')[0]);

            recognizer = new TranslationRecognizer(
                translationConfig,
                autoDetectConfig,
                audioConfig
            );
        }
        else
        {
            translationConfig.SpeechRecognitionLanguage = config.SourceLang;
            translationConfig.AddTargetLanguage(config.TargetLang.Split('-')[0]);

            recognizer = new TranslationRecognizer(translationConfig, audioConfig);
        }

        var stopTask = new TaskCompletionSource<int>();
        bool isUserConnected = true;

        SpeechSynthesizer? synthesizer = null;
        Channel<TranslationPayload>? payloadChannel = null;
        Task? synthesisTask = null;

        if (!config.IsMuted && config.SessionType != SessionType.Reporting)
        {
            var synthConfig = SpeechConfig.FromSubscription(
                _options.SpeechKey,
                _options.SpeechRegion
            );
            synthConfig.SpeechSynthesisVoiceName = config.TargetVoice;

            synthConfig.SetSpeechSynthesisOutputFormat(
                SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm
            );

            synthesizer = new SpeechSynthesizer(synthConfig, null);
            payloadChannel = Channel.CreateUnbounded<TranslationPayload>();

            synthesisTask = Task.Run(async () =>
            {
                await foreach (var payload in payloadChannel.Reader.ReadAllAsync())
                {
                    var result = await synthesizer.SpeakTextAsync(payload.Text);

                    if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                    {
                        string base64Audio = Convert.ToBase64String(result.AudioData);

                        _logger.LogInformation(
                            "Audio synthesized and converted to Base64 successfully. TranslationId: {TranslationId}, SegmentId: {SegmentId}",
                            translationId,
                            payload.Id
                        );

                        await _publisher.Publish(
                            new AudioSynthesizedEvent(
                                Guid.Parse(translationId),
                                payload.Id,
                                base64Audio
                            )
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
        }

        recognizer.Recognizing += (s, e) =>
        {
            if (!isUserConnected)
                return;

            string sourceText = e.Result.Text;
            string partialTranslation = string.Empty;
            string speakerTag = "Speaker";

            if (!string.IsNullOrWhiteSpace(sourceText))
            {
                if (config.SessionType == SessionType.Dialogue)
                {
                    var detectedLang = e.Result.Properties.GetProperty(
                        PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult
                    );

                    var targetLangKey =
                        detectedLang == config.SourceLang
                            ? config.TargetLang.Split('-')[0]
                            : config.SourceLang.Split('-')[0];

                    if (e.Result.Translations.ContainsKey(targetLangKey))
                    {
                        partialTranslation = e.Result.Translations[targetLangKey];
                    }

                    speakerTag = detectedLang ?? "Unknown";
                }
                else
                {
                    partialTranslation =
                        e.Result.Translations.Values.FirstOrDefault() ?? string.Empty;
                    speakerTag = config.SourceLang;
                }

                _publisher.Publish(
                    new PartialTextRecognizedEvent(
                        Guid.Parse(translationId),
                        sourceText,
                        partialTranslation,
                        speakerTag
                    )
                );
            }
        };

        recognizer.Recognized += (s, e) =>
        {
            if (!isUserConnected)
                return;

            if (e.Result.Reason == ResultReason.TranslatedSpeech)
            {
                string sourceText = e.Result.Text;
                string finalTranslation = string.Empty;
                string speakerTag = "Speaker";

                if (config.SessionType == SessionType.Dialogue)
                {
                    var detectedLang = e.Result.Properties.GetProperty(
                        PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult
                    );

                    var targetLangKey =
                        detectedLang == config.SourceLang
                            ? config.TargetLang.Split('-')[0]
                            : config.SourceLang.Split('-')[0];

                    if (e.Result.Translations.ContainsKey(targetLangKey))
                    {
                        finalTranslation = e.Result.Translations[targetLangKey];
                    }

                    speakerTag = detectedLang ?? "Unknown";
                }
                else
                {
                    finalTranslation =
                        e.Result.Translations.Values.FirstOrDefault() ?? string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(finalTranslation))
                {
                    var segmentId = Guid.CreateVersion7();
                    var timestamp = TimeSpan.FromTicks(e.Result.OffsetInTicks);

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

                    if (
                        !config.IsMuted
                        && config.SessionType != SessionType.Reporting
                        && payloadChannel != null
                    )
                    {
                        payloadChannel.Writer.TryWrite(
                            new TranslationPayload(segmentId, finalTranslation)
                        );
                    }
                }
            }
        };

        recognizer.Canceled += (s, e) => stopTask.TrySetResult(0);
        recognizer.SessionStopped += (s, e) => stopTask.TrySetResult(0);

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
            recognizer.Dispose();

            if (
                !config.IsMuted
                && config.SessionType != SessionType.Reporting
                && payloadChannel != null
                && synthesisTask != null
            )
            {
                payloadChannel.Writer.Complete();
                await synthesisTask;
                synthesizer?.Dispose();
            }
        }
    }

    private async Task<(
        string SourceLang,
        string TargetLang,
        string TargetVoice,
        bool IsMuted,
        SessionType SessionType
    )> GetTranslationConfigAsync(string id)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (Guid.TryParse(id, out var gId))
        {
            var t = await context
                .Translations.Include(x => x.Session)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == gId);

            if (t != null)
                return (t.SourceLang, t.TargetLang, t.TargetVoice, t.IsMuted, t.Session.Type);
        }

        return ("en-US", "tr-TR", "tr-TR-AhmetNeural", false, SessionType.Dubbing);
    }
}

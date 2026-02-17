namespace ExulofraApi.Common.Abstractions;

public interface ISpeechService
{
    Task ProcessAudioStreamAsync(string translationId, IAsyncEnumerable<byte[]> audioStream);
    Task<byte[]> TextToSpeechAsync(string text, string targetVoice);
}

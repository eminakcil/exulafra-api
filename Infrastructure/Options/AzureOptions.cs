namespace ExulofraApi.Infrastructure.Options;

public record AzureOptions
{
    public const string SectionName = "Azure";

    public string SpeechKey { get; init; } = string.Empty;
    public string SpeechRegion { get; init; } = string.Empty;
}

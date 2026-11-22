namespace Lexora.Application.Abstractions.Ai;

public sealed class AiProviderUnavailableException : Exception
{
    public AiProviderUnavailableException()
    {
    }

    public AiProviderUnavailableException(string message)
        : base(message)
    {
    }

    public AiProviderUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public AiProviderUnavailableException(string provider, string profile, TimeSpan? retryAfter, Exception innerException)
        : base($"The AI provider '{provider}' (profile '{profile}') is unavailable.", innerException)
    {
        Provider = provider;
        Profile = profile;
        RetryAfter = retryAfter;
    }

    public string? Provider { get; }

    public string? Profile { get; }

    public TimeSpan? RetryAfter { get; }
}

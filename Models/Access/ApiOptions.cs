namespace events_access.Models.Access;

public sealed class ApiOptions
{
    public const string SectionName = "EventsApi";

    public string BaseUrl { get; init; } = "http://localhost:5114/";

    public int TimeoutMilliseconds { get; init; } = 900;
}

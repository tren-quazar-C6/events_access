using events_access.Models.Access;

namespace events_access.ViewModels;

public sealed class AccessIndexViewModel
{
    public IReadOnlyList<TodayEventDto> Eventos { get; init; } = [];

    public string? ApiError { get; init; }
}

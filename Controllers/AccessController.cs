using events_access.Models.Access;
using events_access.Services;
using events_access.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace events_access.Controllers;

public sealed class AccessController : Controller
{
    private readonly AccessApiClient _apiClient;
    private readonly ILogger<AccessController> _logger;

    public AccessController(AccessApiClient apiClient, ILogger<AccessController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet("/")]
    [HttpGet("/access")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var eventsToday = await _apiClient.GetTodayEventsAsync(cancellationToken);
            return View(new AccessIndexViewModel { Eventos = eventsToday });
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Could not load today's events from the API.");
            return View(new AccessIndexViewModel
            {
                ApiError = "No se pudieron cargar los eventos de hoy."
            });
        }
    }

    [HttpPost("/access/scan")]
    public async Task<ActionResult<AccessScanResult>> Scan(
        [FromBody] AccessScanRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.QrPayload))
            return BadRequest(new { error = "QR ilegible." });

        if (request.IdEmpleado <= 0)
            return BadRequest(new { error = "Debes indicar el empleado que controla el acceso." });

        var result = await _apiClient.ScanAsync(request, cancellationToken);
        return Ok(result);
    }

    // ── Métricas (proxy para evitar CORS) ────────────────────────

    [HttpGet("/access/metrics/tickets-sold")]
    public async Task<IActionResult> GetTicketsSold(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _apiClient.GetTicketsSoldAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load tickets-sold metric.");
            return Ok(new { valor = 0 });
        }
    }

    [HttpGet("/access/metrics/attendance/{idEvento:int}")]
    public async Task<IActionResult> GetAttendance(int idEvento, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _apiClient.GetAttendanceAsync(idEvento, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load attendance metric for event {IdEvento}.", idEvento);
            return Ok(0);
        }
    }
}
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using events_access.Models.Access;
using Microsoft.Extensions.Options;

namespace events_access.Services;

public sealed class AccessApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AccessApiClient(HttpClient httpClient, IOptions<ApiOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute);
        _httpClient.Timeout = TimeSpan.FromMilliseconds(options.Value.TimeoutMilliseconds);
    }

    public async Task<IReadOnlyList<TodayEventDto>> GetTodayEventsAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetFromJsonAsync<ServiceResponse<IReadOnlyList<TodayEventDto>>>(
            "api/events/today",
            JsonOptions,
            cancellationToken);

        return response?.Data ?? [];
    }

    /// <summary>
    /// Obtiene el total de tickets vendidos.
    /// Se llama desde el controller como proxy para evitar CORS en el navegador.
    /// </summary>
    public async Task<object> GetTicketsSoldAsync(CancellationToken cancellationToken)
    {
        var result = await _httpClient.GetFromJsonAsync<object>(
            "api/metrics/tickets-sold",
            JsonOptions,
            cancellationToken);

        return result ?? new { valor = 0 };
    }

    /// <summary>
    /// Obtiene el porcentaje de asistencia de un evento específico.
    /// Se llama desde el controller como proxy para evitar CORS en el navegador.
    /// </summary>
    public async Task<object> GetAttendanceAsync(int idEvento, CancellationToken cancellationToken)
    {
        var result = await _httpClient.GetFromJsonAsync<object>(
            $"api/metrics/eventos/{idEvento}/attendance-rate",
            JsonOptions,
            cancellationToken);

        return result ?? 0;
    }

    public async Task<AccessScanResult> ScanAsync(AccessScanRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var scanTime = DateTime.UtcNow;
        string qrToken;

        try
        {
            qrToken = QrPayloadParser.ParseToken(request.QrPayload);
        }
        catch (InvalidOperationException exception)
        {
            return BuildLocalFailure("ERROR_QR", exception.Message, null, null, scanTime, stopwatch.ElapsedMilliseconds, request.QrPayload);
        }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/tickets/scan",
                new ScanTicketRequest(qrToken, request.IdEmpleado, request.IdEvento, request.Dispositivo, scanTime),
                JsonOptions,
                cancellationToken);

            var payload = await response.Content.ReadFromJsonAsync<ServiceResponse<ScanTicketResponse>>(
                JsonOptions,
                cancellationToken);

            stopwatch.Stop();

            if (response.StatusCode == HttpStatusCode.RequestTimeout)
                return BuildLocalFailure("TIMEOUT_API", "Timeout API.", null, null, scanTime, stopwatch.ElapsedMilliseconds, qrToken);

            if (!response.IsSuccessStatusCode || payload is null || payload.Success is false || payload.Data is null)
            {
                var message = payload?.Errors?.FirstOrDefault()
                    ?? payload?.Message
                    ?? "No se pudo validar el ticket.";
                return BuildLocalFailure("ERROR_API", message, null, null, scanTime, stopwatch.ElapsedMilliseconds, qrToken);
            }

            return MapScanResult(payload.Data, stopwatch.ElapsedMilliseconds, scanTime, qrToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return BuildLocalFailure("TIMEOUT_API", "Timeout API.", null, null, scanTime, stopwatch.ElapsedMilliseconds, qrToken);
        }
        catch (HttpRequestException)
        {
            stopwatch.Stop();
            return BuildLocalFailure("SCANNER_DESCONECTADO", "Scanner desconectado o API no disponible.", null, null, scanTime, stopwatch.ElapsedMilliseconds, qrToken);
        }
    }

    private static AccessScanResult MapScanResult(
        ScanTicketResponse scan,
        long latencyMs,
        DateTime scanTime,
        string qrToken)
    {
        var title = scan.Resultado.ToUpperInvariant() switch
        {
            "VALIDO"    => "ACCESO AUTORIZADO",
            "DUPLICADO" => "QR YA ESCANEADO",
            _           => "ACCESO DENEGADO"
        };

        return new AccessScanResult(
            scan.Resultado,
            title,
            scan.Mensaje,
            scan.IdTicket,
            scan.IdScan,
            scan.TipoAlerta,
            scan.Seccion,
            scan.Fila,
            scan.Asiento,
            latencyMs,
            scanTime,
            qrToken);
    }

    private static AccessScanResult BuildLocalFailure(
        string type,
        string message,
        int? ticketId,
        int? scanId,
        DateTime scanTime,
        long latencyMs,
        string qrToken)
    {
        return new AccessScanResult(
            "ERROR",
            "ACCESO DENEGADO",
            message,
            ticketId,
            scanId,
            type,
            null,
            null,
            null,
            latencyMs,
            scanTime,
            qrToken);
    }
}
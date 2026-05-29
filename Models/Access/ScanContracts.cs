using System.Text.Json.Serialization;

namespace events_access.Models.Access;

public sealed record ServiceResponse<T>(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("errors")] IReadOnlyList<string>? Errors);

public sealed record TodayEventDto(
    [property: JsonPropertyName("id_evento")] int IdEvento,
    [property: JsonPropertyName("nombre_evento")] string NombreEvento,
    [property: JsonPropertyName("fecha_evento")] DateTime FechaEvento,
    [property: JsonPropertyName("publicado")] bool Publicado,
    [property: JsonPropertyName("activo")] bool Activo);

public sealed record ScanTicketRequest(
    [property: JsonPropertyName("qr_token")] string QrToken,
    [property: JsonPropertyName("id_empleado")] int IdEmpleado,
    [property: JsonPropertyName("id_evento")] int? IdEvento,
    [property: JsonPropertyName("dispositivo")] string? Dispositivo,
    [property: JsonPropertyName("fecha_scan")] DateTime? FechaScan);

public sealed record ScanTicketResponse(
    [property: JsonPropertyName("resultado")] string Resultado,
    [property: JsonPropertyName("mensaje")] string Mensaje,
    [property: JsonPropertyName("id_ticket")] int? IdTicket,
    [property: JsonPropertyName("id_scan")] int? IdScan,
    [property: JsonPropertyName("tipo_alerta")] string? TipoAlerta,
    [property: JsonPropertyName("seccion")] string? Seccion = null,
    [property: JsonPropertyName("fila")] string? Fila = null,
    [property: JsonPropertyName("asiento")] string? Asiento = null);

public sealed record AccessScanRequest(
    string QrPayload,
    int IdEmpleado,
    int? IdEvento,
    string? Dispositivo);

public sealed record AccessScanResult(
    string Resultado,
    string Titulo,
    string Mensaje,
    int? IdTicket,
    int? IdScan,
    string? TipoAlerta,
    string? Seccion,
    string? Fila,
    string? Asiento,
    long LatenciaMs,
    DateTime FechaScan,
    string QrToken);

using System.Text.Json;

namespace events_access.Services;

public static class QrPayloadParser
{
    private static readonly string[] TokenKeys = ["qr_token", "qrToken", "token", "code", "codigo"];

    public static string ParseToken(string payload)
    {
        var trimmed = payload.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("QR ilegible.");
        }

        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            return ParseJsonToken(trimmed);
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            var token = ParseQueryToken(uri.Query);
            return string.IsNullOrWhiteSpace(token) ? uri.Segments.Last().Trim('/') : token;
        }

        return trimmed;
    }

    private static string ParseJsonToken(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);

            foreach (var key in TokenKeys)
            {
                if (document.RootElement.TryGetProperty(key, out var property))
                {
                    var token = property.GetString();

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        return token;
                    }
                }
            }
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Ticket corrupto.");
        }

        throw new InvalidOperationException("QR ilegible.");
    }

    private static string? ParseQueryToken(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var parameters = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var parameter in parameters)
        {
            var pair = parameter.Split('=', 2);
            var key = Uri.UnescapeDataString(pair[0]);

            if (!TokenKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            return pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : null;
        }

        return null;
    }
}

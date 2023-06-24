using Microsoft.AspNetCore.Mvc;

namespace AzureDevOps.InnerSource.Services;

public class BadgeService
{
    private readonly HttpClient _httpClient;

    public BadgeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FileStreamResult> CreateAsync(string label, string message, string? color = "informational", string? logo = null, string? logoColor = null, CancellationToken ct = default)
    {
        var shieldsIoUrl = $"https://img.shields.io/static/v1?label={label}&message={message}&color={color}";
        if (logo is not null) shieldsIoUrl += $"&logo={logo}";
        if (logoColor is not null) shieldsIoUrl += $"&logoColor={logoColor}";
        var badge = await _httpClient.GetAsync(shieldsIoUrl, ct);
        var stream = await badge.Content.ReadAsStreamAsync(ct);
        return new FileStreamResult(stream, badge.Content.Headers.ContentType?.ToString() ?? "image/svg+xml;charset=utf-8");
    }

    public string GetColorByAge(DateTime? date)
    {
        if (!date.HasValue)
        {
            return "gray";
        }

        int daysElapsed = (DateTime.UtcNow - date.Value).Days;

        return daysElapsed switch
        {
            <= 7 => "brightgreen",
            <= 30 => "green",
            <= 180 => "yellowgreen",
            <= 365 => "yellow",
            <= 730 => "orange",
            _ => "red"
        };
    }
}
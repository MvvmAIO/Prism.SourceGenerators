using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Prism.SourceGenerators.DocsSite.Services;

/// <summary>
/// Loads nested locale JSON from <c>wwwroot/locales/{culture}.json</c> and exposes flat keys (<c>nav.home</c>).
/// </summary>
public sealed class LocalizationService
{
    private readonly HttpClient _http;
    private readonly string _baseUri;
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private string _culture = "en";

    public LocalizationService(HttpClient http, IWebAssemblyHostEnvironment host)
    {
        _http = http;
        _baseUri = host.BaseAddress.ToString().TrimEnd('/') + '/';
    }

    public event Action? CultureChanged;

    public string Culture => _culture;

    public string T(string key)
    {
        if (_cache.TryGetValue(_culture, out var map) && map.TryGetValue(key, out var value))
            return value;
        if (_culture != "en" && _cache.TryGetValue("en", out var en) && en.TryGetValue(key, out var fallback))
            return fallback;
        return key;
    }

    public async Task SetCultureAsync(string culture, CancellationToken cancellationToken = default)
    {
        culture = NormalizeCulture(culture);
        await EnsureCultureLoadedAsync(culture, cancellationToken).ConfigureAwait(false);
        if (culture != "en")
            await EnsureCultureLoadedAsync("en", cancellationToken).ConfigureAwait(false);
        _culture = culture;
        CultureChanged?.Invoke();
    }

    /// <summary>Applies <paramref name="storedCulture"/> from localStorage, otherwise the browser language (normalized).</summary>
    public async Task ApplyBrowserAndStorageAsync(string? storedCulture, string? browserLanguage, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(storedCulture))
        {
            await SetCultureAsync(NormalizeCulture(storedCulture!), cancellationToken).ConfigureAwait(false);
            return;
        }

        await SetCultureAsync(NormalizeCulture(browserLanguage ?? "en"), cancellationToken).ConfigureAwait(false);
    }

    public static string NormalizeCulture(string culture)
    {
        culture = culture.Trim().Replace('_', '-');
        if (culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return culture.Contains('-', StringComparison.Ordinal) ? "zh-CN" : "zh-CN";
        if (culture.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            return "ja";
        return "en";
    }

    private async Task EnsureCultureLoadedAsync(string culture, CancellationToken cancellationToken)
    {
        if (_cache.ContainsKey(culture))
            return;

        var flat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var path = $"{_baseUri}locales/{culture}.json";
            await using var stream = await _http.GetStreamAsync(path, cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            Flatten(doc.RootElement, "", flat);
        }
        catch
        {
            // Missing or invalid locale file — leave map empty; T() may fall back to en.
        }

        _cache[culture] = flat;
    }

    private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> target)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var next = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    Flatten(prop.Value, next, target);
                }
                break;
            case JsonValueKind.String:
                target[prefix] = element.GetString() ?? "";
                break;
            case JsonValueKind.Number:
                target[prefix] = element.GetRawText();
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                target[prefix] = element.GetBoolean().ToString(CultureInfo.InvariantCulture);
                break;
        }
    }
}

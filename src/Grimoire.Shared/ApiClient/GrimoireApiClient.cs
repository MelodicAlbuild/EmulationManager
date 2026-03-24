using System.Net.Http.Json;
using Grimoire.Shared.DTOs;
using Grimoire.Shared.Enums;
using Grimoire.Shared.Interfaces;

namespace Grimoire.Shared.ApiClient;

public class GrimoireApiClient : IGrimoireApi
{
    private readonly HttpClient _http;

    public GrimoireApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<GameListDto>> GetGamesAsync(
        PlatformType? platform = null, string? search = null, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (platform.HasValue) query.Add($"platform={platform.Value}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");

        var url = query.Count > 0 ? $"/api/games?{string.Join('&', query)}" : "/api/games";
        return await _http.GetFromJsonAsync<IReadOnlyList<GameListDto>>(url, ct) ?? [];
    }

    public async Task<GameDetailDto?> GetGameDetailAsync(int gameId, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<GameDetailDto>($"/api/games/{gameId}", ct);
    }

    public async Task<IReadOnlyList<EmulatorDto>> GetEmulatorsAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<EmulatorDto>>("/api/emulators", ct) ?? [];
    }

    public async Task<EmulatorDto?> GetEmulatorAsync(PlatformType platform, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<EmulatorDto>($"/api/emulators/{platform}", ct);
    }

    public async Task<IReadOnlyList<PlatformInfoDto>> GetPlatformsAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<PlatformInfoDto>>("/api/platforms", ct) ?? [];
    }

    public async Task<Stream> GetDownloadStreamAsync(
        DownloadableType type, int id, long? rangeStart = null, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/downloads/{type}/{id}");
        if (rangeStart.HasValue)
        {
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(rangeStart.Value, null);
        }

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task<long> GetDownloadSizeAsync(
        DownloadableType type, int id, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Head, $"/api/downloads/{type}/{id}");
        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return response.Content.Headers.ContentLength ?? 0;
    }

    public async Task<ClientVersionDto?> GetLatestClientVersionAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<ClientVersionDto>("/api/client/latest", ct);
    }
}

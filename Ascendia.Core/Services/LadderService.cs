using Ascendia.Core.Models;
using LCTWorks.Core.Helpers;

namespace Ascendia.Core.Services;

public class LadderService
{
    private const string OpenDotaBaseUrl = "https://api.opendota.com/api";
    private const string OpenDotaPlayerUrl = OpenDotaBaseUrl + "/players/{0}";
    private const string OpenDotaRefreshPlayerUrl = OpenDotaBaseUrl + "/players/{0}/refresh";
    private const string OpenDotaWinLoseUrl = OpenDotaBaseUrl + "/players/{0}/wl";

    public async Task<PlayerOpenDotaModel?> GetPlayerAsync(string? accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return default;
        }
        var url = string.Format(OpenDotaPlayerUrl, accountId);
        return await GetAsync<PlayerOpenDotaModel>(url);
    }

    public async Task<WinLoseOpenDotaModel?> GetPlayerWinLoseAsync(string? accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return default;
        }
        var url = string.Format(OpenDotaWinLoseUrl, accountId);
        return await GetAsync<WinLoseOpenDotaModel>(url);
    }

    public Task<bool> RefreshPlayerAsync(string? accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Task.FromResult(false);
        }
        var url = string.Format(OpenDotaRefreshPlayerUrl, accountId);
        return PostAsync<bool>(url);
    }

    private static async Task<T?> GetAsync<T>(string url)
    {
        using var httpClient = new HttpClient();
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            return Json.ToObject<T>(content);
        }
        catch (Exception)
        {
        }
        return default;
    }

    private static async Task<bool> PostAsync<T>(string url)
    {
        using var httpClient = new HttpClient();
        try
        {
            HttpResponseMessage response = await httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
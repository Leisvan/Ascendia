using Ascendia.Core.Models;
using LCTWorks.Core.Helpers;

namespace Ascendia.Core.Services;

public class LadderService
{
    private const string OpenDotaBaseUrl = "https://api.opendota.com/api";
    private const string OpenDotaPlayerUrl = OpenDotaBaseUrl + "/players/{0}";
    private const string OpenDotaRefreshPlayerUrl = OpenDotaBaseUrl + "/players/{0}/refresh";
    private const string OpenDotaWinLoseUrl = OpenDotaBaseUrl + "/players/{0}/wl";

    public async Task<OpenDotaResponse<PlayerOpenDotaModel?>> GetPlayerAsync(string? accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return OpenDotaResponse<PlayerOpenDotaModel?>.Invalid;
        }
        var url = string.Format(OpenDotaPlayerUrl, accountId);
        return await GetAsync<PlayerOpenDotaModel?>(url);
    }

    public async Task<OpenDotaResponse<WinLoseOpenDotaModel?>> GetPlayerWinLoseAsync(string? accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return OpenDotaResponse<WinLoseOpenDotaModel?>.Invalid;
        }
        var url = string.Format(OpenDotaWinLoseUrl, accountId);
        return await GetAsync<WinLoseOpenDotaModel?>(url);
    }

    public Task<OpenDotaResponse<object>> RefreshPlayerAsync(string? accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Task.FromResult(OpenDotaResponse<object>.Invalid);
        }
        var url = string.Format(OpenDotaRefreshPlayerUrl, accountId);
        return PostAsync(url);
    }

    private static async Task<OpenDotaResponse<T>> GetAsync<T>(string url)
    {
        using var httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(url);
        return await GetParsedResponseAsync<T>(response);
    }

    private static async Task<OpenDotaResponse<T>> GetParsedResponseAsync<T>(HttpResponseMessage response)
    {
        var limitReached = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests;
        var valid = response.StatusCode == System.Net.HttpStatusCode.OK;
        T? value = default;
        if (valid)
        {
            if (response.Content == null)
            {
                valid = false;
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                try
                {
                    value = Json.ToObject<T>(content);
                }
                catch (Exception)
                {
                    valid = false;
                }
            }
        }
        return new OpenDotaResponse<T>(value, valid, limitReached);
    }

    private static async Task<OpenDotaResponse<object>> PostAsync(string url)
    {
        using var httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.PostAsync(url, null);
        return await GetParsedResponseAsync<object>(response);
    }
}
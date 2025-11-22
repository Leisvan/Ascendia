using Ascendia.Core.Models;
using LCTWorks.Core.Helpers;

namespace Ascendia.Core.Services;

public class LadderService
{
    private const string OpenDotaBaseUrl = "https://api.opendota.com/api";
    private const string OpenDotaDistributionsUrl = OpenDotaBaseUrl + "/distributions";
    private const string OpenDotaMatchUrl = OpenDotaBaseUrl + "/matches/{0}";
    private const string OpenDotaPlayerMatchesUrl = OpenDotaBaseUrl + "/players/{0}/matches?limit={1}";
    private const string OpenDotaPlayerUrl = OpenDotaBaseUrl + "/players/{0}";
    private const string OpenDotaRefreshPlayerUrl = OpenDotaBaseUrl + "/players/{0}/refresh";
    private const string OpenDotaWinLoseUrl = OpenDotaBaseUrl + "/players/{0}/wl";

    public static string GetRegionGroup(int regionId)
    {
        switch (regionId)
        {
            // 🌍 Europe
            case 2:  // Europe West (Luxembourg)
            case 3:  // Europe East (Vienna)
            case 8:  // Stockholm
            case 37: // Austria
            case 38: // UK
            case 40: // Italyd
            case 41: // Spain
            case 42: // Poland
            case 43: // Greece
            case 44: // Romania
            case 45: // Turkey
            case 47: // Russia
                return Constants.EURegion;

            // 🌎 America
            case 0:  // US West
            case 1:  // US East
            case 9:  // Brazil
            case 13: // Chile
            case 14: // Peru
            case 25: // Argentina
                return Constants.NARegion;

            // 🌏 Asia
            case 5:  // Singapore
            case 6:  // Dubai
            case 7:  // Australia
            case 10: // South Africa
            case 15: // India
            case 16: // Japan
            case 17: // Taiwan
            case 46: // UAE
            case 48: // Hong Kong
            case 49: // South Korea
                return Constants.ASRegion;

            // 🇨🇳 China (Perfect World clusters)
            case 11: // Perfect World Telecom
            case 12: // Perfect World Unicom
            case 18: // Perfect World Telecom 2
            case 19: // Perfect World Unicom 2
                return Constants.CNRegion;

            // ❓ Unknown / not mapped
            default:
                return string.Empty;
        }
    }

    public async Task<OpenDotaResponse<DistributionsOpenDotaModel?>> GetDistributionsAsync()
        => await GetAsync<DistributionsOpenDotaModel?>(OpenDotaDistributionsUrl);

    public async Task<OpenDotaResponse<PlayerMatchExtendedOpenDotaModel?>> GetMatchDetailsAsync(long matchId)
    {
        if (matchId <= 0)
        {
            return OpenDotaResponse<PlayerMatchExtendedOpenDotaModel?>.Invalid;
        }
        var url = string.Format(OpenDotaMatchUrl, matchId);
        return await GetAsync<PlayerMatchExtendedOpenDotaModel?>(url);
    }

    public async Task<OpenDotaResponse<PlayerOpenDotaModel?>> GetPlayerAsync(string? accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return OpenDotaResponse<PlayerOpenDotaModel?>.Invalid;
        }
        var url = string.Format(OpenDotaPlayerUrl, accountId);
        return await GetAsync<PlayerOpenDotaModel?>(url);
    }

    public async Task<OpenDotaResponse<PlayerMatchOpenDotaModel[]?>> GetPlayerMatchesAsync(string? accountId, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return OpenDotaResponse<PlayerMatchOpenDotaModel[]?>.Invalid;
        }
        var url = string.Format(OpenDotaPlayerMatchesUrl, accountId, limit);
        return await GetAsync<PlayerMatchOpenDotaModel[]?>(url);
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
        int remainingLastMinutes = 0;
        int remainingToday = 0;
        string? ip = null;
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

                    if (response.Headers.Contains("X-Rate-Limit-Remaining-Minute"))
                    {
                        remainingLastMinutes = int.Parse(response.Headers.GetValues("X-Rate-Limit-Remaining-Minute").First());
                    }
                    if (response.Headers.Contains("X-Rate-Limit-Remaining-Day"))
                    {
                        remainingToday = int.Parse(response.Headers.GetValues("X-Rate-Limit-Remaining-Day").First());
                    }
                    if (response.Headers.Contains("X-IP-Address"))
                    {
                        ip = response.Headers.GetValues("X-IP-Address").First();
                    }
                }
                catch (Exception)
                {
                    valid = false;
                }
            }
        }
        return new OpenDotaResponse<T>(value, valid, limitReached)
        {
            RemainingLastMinutes = remainingLastMinutes,
            RemainingToday = remainingToday,
            Ip = ip,
        };
    }

    private static async Task<OpenDotaResponse<object>> PostAsync(string url)
    {
        using var httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.PostAsync(url, null);
        return await GetParsedResponseAsync<object>(response);
    }
}
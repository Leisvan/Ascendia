using Ascendia.Core.Models;
using Ascendia.Core.Services;
using Ascendia.Discord;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LCTWorks.WinUI.Helpers;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AscendiaApp.ViewModels;

public partial class BotViewModel : ObservableObject
{
    private readonly DiscordBotService _botService;
    private readonly CommunityService _communityService;
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private CancellationTokenSource? _regionsCts;

    public BotViewModel(DiscordBotService botService, CommunityService communityService)
    {
        _botService = botService;
        _communityService = communityService;
        _communityService.RequestLimitsChanged += RequestLimitsChanged;
    }

    [ObservableProperty]
    public partial bool? ForceRegionsUpdate { get; set; } = false;

    [ObservableProperty]
    public partial bool? ForceUpdate { get; set; } = false;

    public ObservableCollection<GuildSettingsModel> Guilds { get; } = [];

    public string? Ip => _communityService.Ip;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDisconnected))]
    public partial bool IsConnected { get; set; }

    public bool IsDisconnected => !IsConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRankingIdle))]
    public partial bool IsRankingBusy { get; set; } = false;

    public bool IsRankingIdle => !IsRankingBusy;

    public int MatchesToCheckForRegionUpdate { get; set; } = 50;

    [ObservableProperty]
    public partial bool? NotifyGuild { get; set; } = true;

    public int RemainingRequestDay => _communityService.RemainingRequestDay;

    public int RemainingRequestMinute => _communityService.RemainingRequestMinute;

    [ObservableProperty]
    public partial GuildSettingsModel? SelectedGuild { get; set; }

    [ObservableProperty]
    public partial bool? UpdateWL { get; set; } = true;

    [RelayCommand]
    public void CancelUpdateRank()
    {
        _botService.CancelOperation();
        _regionsCts?.Cancel();
        _regionsCts = null;
    }

    [RelayCommand]
    private async Task ConnectBot()
    {
        if (await _botService.ConnectAsync())
        {
            IsConnected = true;
            await LoadAsync();
            await _communityService.RefreshRequestLimitsAsync();
        }
    }

    [RelayCommand]
    private async Task DisconnectBot()
    {
        await _botService.DisconnectAsync();
        await LoadAsync();
        IsConnected = false;
    }

    [RelayCommand]
    private Task DisplayRankingAsync()
        => ExecuteRankingActionAsync(false, true, false);

    private async Task ExecuteRankingActionAsync(bool updateMembers,
        bool displayRank,
        bool includeBanned = false)
    {
        if (SelectedGuild == null)
        {
            return;
        }
        await UIDispatchAsync(() => IsRankingBusy = true);
        if (updateMembers)
        {
            if (!await _botService.UpdateMembersLadderAsync(ForceUpdate ?? false, UpdateWL ?? true, NotifyGuild ?? true, SelectedGuild.GuildId))
            {
                await UIDispatchAsync(() => IsRankingBusy = false);
                return;
            }
        }
        if (displayRank)
        {
            await _botService.DisplayRankingAsync(includeBanned, SelectedGuild.GuildId);
        }
        await UIDispatchAsync(() => IsRankingBusy = false);
    }

    private async Task LoadAsync(bool forceRefresh = false)
    {
        var allGuilds = await _botService.GetSettingServersAsync(RuntimePackageHelper.IsDebug(), forceRefresh);
        if (allGuilds == null || allGuilds.Count == 0)
        {
            return;
        }
        Guilds.Clear();
        foreach (var item in allGuilds)
        {
            Guilds.Add(item);
        }

        var first = Guilds.Count == 1 ? Guilds.FirstOrDefault() : Guilds.FirstOrDefault(x => !x.Record.IsDebugGuild);
        if (first != null)
        {
            SelectedGuild = first;
        }
    }

    [RelayCommand]
    private Task Refresh()
    {
        if (IsConnected)
        {
            return LoadAsync(true);
        }
        return Task.CompletedTask;
    }

    private void RequestLimitsChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(RemainingRequestDay));
        OnPropertyChanged(nameof(RemainingRequestMinute));
        OnPropertyChanged(nameof(Ip));
    }

    private async Task UIDispatchAsync(Action action)
    {
        _dispatcher.TryEnqueue(() =>
        {
            action();
        });
    }

    [RelayCommand]
    private Task UpdateAllMembersAsync()
        => ExecuteRankingActionAsync(true, false);

    [RelayCommand]
    private async Task UpdateMatchRegionsAsync()
    {
        if (SelectedGuild == null)
        {
            return;
        }
        if (_regionsCts != null)
        {
            return;
        }
        _regionsCts = new CancellationTokenSource();
        await UIDispatchAsync(() => IsRankingBusy = true);
        await _communityService.UpdateAllRegionsAsync(ForceRegionsUpdate ?? false, MatchesToCheckForRegionUpdate, _regionsCts.Token);
        await UIDispatchAsync(() => IsRankingBusy = false);
    }
}
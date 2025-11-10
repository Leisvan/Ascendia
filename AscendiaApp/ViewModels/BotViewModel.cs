using Ascendia.Core.Models;
using Ascendia.Core.Records;
using Ascendia.Core.Services;
using Ascendia.Discord;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LCTWorks.WinUI.Helpers;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace AscendiaApp.ViewModels;

public partial class BotViewModel(DiscordBotService botService, CommunityService communityService) : ObservableObject
{
    private readonly DiscordBotService _botService = botService;
    private readonly CommunityService _communityService = communityService;
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    public ObservableCollection<GuildSettingsModel> Guilds { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDisconnected))]
    public partial bool IsConnected { get; set; }

    public bool IsDisconnected => !IsConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRankingIdle))]
    public partial bool IsRankingBusy { get; set; } = false;

    public bool IsRankingIdle => !IsRankingBusy;

    [ObservableProperty]
    public partial GuildSettingsModel? SelectedGuild { get; set; }

    [RelayCommand]
    public void CancelUpdateRank()
    => _botService.CancelOperation();

    [RelayCommand]
    private async Task ConnectBot()
    {
        if (await _botService.ConnectAsync())
        {
            IsConnected = true;
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task DisconnectBot()
    {
        await _botService.DisconnectAsync();
        await LoadAsync();
        IsConnected = false;
    }

    private async Task ExecuteRankingActionAsync(bool updateMembers,
        bool displayRank,
        bool forceUpdate = false,
        bool includeBanned = false,
        bool includeWL = true)
    {
        if (SelectedGuild == null)
        {
            return;
        }
        await UIDispatchAsync(() => IsRankingBusy = true);
        if (updateMembers)
        {
            if (!await _botService.UpdateMemberRegionsAsync(forceUpdate, includeWL, SelectedGuild.GuildId))
            {
                await UIDispatchAsync(() => IsRankingBusy = false);
                return;
            }
        }
        if (displayRank)
        {
            await _botService.DisplayRankAsync(includeBanned, SelectedGuild.GuildId);
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

    private async Task UIDispatchAsync(Action action)
    {
        _dispatcher.TryEnqueue(() =>
        {
            action();
        });
    }

    [RelayCommand]
    private Task UpdateAllMembersAsync()
        => ExecuteRankingActionAsync(true, false, false, false);
}
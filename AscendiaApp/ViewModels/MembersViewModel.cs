using Ascendia.Core.Records;
using Ascendia.Core.Services;
using AscendiaApp.Models;
using AscendiaApp.Observable;
using AscendiaApp.ViewModels.Dialogs;
using AscendiaApp.Views.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LCTWorks.Telemetry;
using LCTWorks.WinUI.Dialogs;
using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AscendiaApp.ViewModels;

public partial class MembersViewModel : ObservableObject
{
    private readonly CommunityService _communityService;
    private readonly DialogService _dialogService;
    private readonly ILogger<object> _logger;
    private readonly ObservableCollection<MemberObservable> _members = [];
    private readonly ITelemetryService _telemetryService;
    private string? _searchTerm = string.Empty;

    public MembersViewModel(CommunityService communityService, DialogService dialogService, ITelemetryService telemetryService, ILogger<object> logger)
    {
        _communityService = communityService;
        _dialogService = dialogService;
        _telemetryService = telemetryService;
        _logger = logger;
        _telemetryService.Log(message: "MembersViewModel created");
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotLoading))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? LoadingNotification { get; set; }

    public ObservableCollection<MemberObservable> Members => _members;

    public bool NotLoading => !IsLoading;

    public string? SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (SetProperty(ref _searchTerm, value))
            {
                RefreshInternal(false);
            }
        }
    }

    [RelayCommand]
    public async Task Initialize()
    {
        if (IsLoading)
        {
            return;
        }
        IsLoading = true;
        await _communityService.InitializeFromCacheAsync();
        RefreshInternal(false);

        _communityService.Refreshed += CommunityServiceRefreshed;

        IsLoading = false;
    }

    [RelayCommand]
    private async Task AddMember()
    {
        var dialog = new EditMemberContentDialog();
        if (dialog.ViewModel != null)
        {
            dialog.ViewModel.Finished += EditMemberDialogFinished;
            await _dialogService.ShowDialogAsync(dialog, true, App.MainWindow?.Content?.XamlRoot);
        }
    }

    private void CommunityServiceRefreshed(object? sender, EventArgs e)
        => RefreshInternal(false);

    [RelayCommand]
    private void CopyId(MemberObservable member)
    {
        if (string.IsNullOrWhiteSpace(member.Record.AccountId))
        {
            return;
        }
        ClipboardHelper.CopyText(member.Record.AccountId);
    }

    [RelayCommand]
    private async Task EditMemberAsync(MemberObservable member)
    {
        var dialog = new EditMemberContentDialog();
        if (dialog.ViewModel != null)
        {
            dialog.ViewModel.Finished += EditMemberDialogFinished;
            dialog.ViewModel.SetEditProperties(member);
            await _dialogService.ShowDialogAsync(dialog, true, App.MainWindow?.Content?.XamlRoot);
        }
    }

    private void EditMemberDialogFinished(object? sender, EditOperationResult result)
    {
        if (sender is EditMemberViewModel viewModel)
        {
            _dialogService.HideCurrentContentDialog();
            if (result == EditOperationResult.Cancelled)
            {
            }
            else if (result == EditOperationResult.Success)
            {
                RefreshInternal(true);
            }
            viewModel.Finished -= EditMemberDialogFinished;
        }
    }

    [RelayCommand]
    private void OpenDotaBuffProfile(MemberObservable member)
    {
        if (string.IsNullOrWhiteSpace(member.Record.AccountId))
        {
            return;
        }

        var url = $"https://www.dotabuff.com/players/{member.Record.AccountId}";
        OpenUrl(url);
    }

    [RelayCommand]
    private void OpenODotaProfile(MemberObservable member)
    {
        if (string.IsNullOrWhiteSpace(member.Record.AccountId))
        {
            return;
        }
        var url = $"https://www.opendota.com/players/{member.Record.AccountId}";
        OpenUrl(url);
    }

    private async void OpenUrl(string url)
    {
        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open URL: {Url}", url);
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        SearchTerm = null;
        RefreshInternal(true);
    }

    private async void RefreshInternal(bool forceRefresh)
        => await RefreshInternalAsync(forceRefresh);

    private async Task RefreshInternalAsync(bool forceRefresh)
    {
        await Task.Delay(120);

        if (forceRefresh)
        {
            LoadingNotification = "Members-UpdatingMembers".GetTextLocalized();
        }
        IsLoading = true;

        IEnumerable<MemberRecord> members = await _communityService.GetAllMembersAsync(forceRefresh);
        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            members = members.Where(m => m.DisplayName?
            .Contains(_searchTerm, StringComparison.InvariantCultureIgnoreCase) == true ||
            m.AccountName?.Contains(_searchTerm, StringComparison.InvariantCultureIgnoreCase) == true);
        }
        if (members != null)
        {
            _members.Clear();
            foreach (var item in members)
            {
                _members.Add(new MemberObservable(item));
            }
        }

        LoadingNotification = string.Empty;
        IsLoading = false;
    }

    [RelayCommand]
    private async Task RemoveMemberAsync(MemberObservable member)
    {
        var results = await _dialogService.ShowDialogAsync(new ContentDialog
        {
            Title = "Dialog-RemoveMemberTitle".GetTextLocalized(),
            Content = "Dialog-RemoveMemberMessage".GetTextLocalized(),
            PrimaryButtonText = "Dialog-RemoveMemberPrimaryButtonText".GetTextLocalized(),
            SecondaryButtonText = "Dialog-RemoveMemberSecondaryButtonText".GetTextLocalized(),
        });
        if (results != ContentDialogResult.Primary)
        {
            return;
        }
        IsLoading = true;

        var success = await _communityService.RemoveMemberAsync(member.Record.Id!);
        if (success)
        {
            _members.Remove(member);
        }
        IsLoading = false;
    }

    [RelayCommand]
    private async Task UpdateMemberAsync(MemberObservable member)
    {
        IsLoading = true;

        if (string.IsNullOrWhiteSpace(member.Record.AccountId))
        {
            return;
        }

        LoadingNotification = string.Format("Members-UpdatingMemberFormat".GetTextLocalized(), member.DisplayName);
        var results = await _communityService.UpdateLadderAsync(
            member.Record.Id,
            member.Record.AccountId,
            notifications: (s, e) =>
        {
            LoadingNotification = e;
        },
            previousRecord: member.Record);

        await RefreshInternalAsync(true);
        IsLoading = false;
    }
}
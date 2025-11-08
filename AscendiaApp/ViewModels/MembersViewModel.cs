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
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AscendiaApp.ViewModels;

public partial class MembersViewModel : ObservableObject
{
    private readonly CommunityService _communityService;
    private readonly DialogService _dialogService;
    private readonly ILogger<object> _logger;
    private readonly ObservableCollection<MemberObservable> _members = [];
    private readonly ITelemetryService _telemetryService;
    private CancellationTokenSource _cts = new();
    private string? _searchTerm = string.Empty;

    public MembersViewModel(CommunityService communityService, DialogService dialogService, ITelemetryService telemetryService, ILogger<object> logger)
    {
        _communityService = communityService;
        _dialogService = dialogService;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public bool CancelEnabled => _cts != null && !_cts.IsCancellationRequested;

    public bool CancelVisibile => !string.IsNullOrWhiteSpace(LoadingNotification);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotLoading))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CancelVisibile))]
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

    [ObservableProperty]
    public partial MemberObservable? SelectedMember { get; set; }

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

    [RelayCommand]
    private Task Cancel()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            OnPropertyChanged(nameof(CancelEnabled));
        }
        return Task.CompletedTask;
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
    private async Task EditSelectedMemberAsync()
    {
        if (SelectedMember == null)
        {
            return;
        }
        var dialog = new EditMemberContentDialog();
        if (dialog.ViewModel != null)
        {
            dialog.ViewModel.Finished += EditMemberDialogFinished;
            dialog.ViewModel.SetEditProperties(SelectedMember);
            await _dialogService.ShowDialogAsync(dialog, true, App.MainWindow?.Content?.XamlRoot);
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

        LoadingNotification = null;
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

        IsLoading = false;
    }

    [RelayCommand]
    private async Task RemoveSelectedMemberAsync()
    {
        if (SelectedMember == null)
        {
            return;
        }
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

        var success = await _communityService.RemoveMemberAsync(SelectedMember.Record.Id!);
        if (success)
        {
            _members.Remove(SelectedMember);
        }
        IsLoading = false;
    }

    [RelayCommand]
    private async Task UpdateAllMembersAsync()
    {
        await RefreshInternalAsync(true);

        IsLoading = true;
        _cts = new CancellationTokenSource();
        OnPropertyChanged(nameof(CancelEnabled));

        var result = await _communityService.UpdateLadderAsync(true, (s, e) =>
        {
            LoadingNotification = e;
        }, cancellationToken: _cts.Token);

        IsLoading = false;

        if (result > 0)
        {
            await RefreshInternalAsync(true);
        }
    }
}
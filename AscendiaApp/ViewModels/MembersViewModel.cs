using Ascendia.Core.Services;
using AscendiaApp.Models;
using AscendiaApp.Observable;
using AscendiaApp.ViewModels.Dialogs;
using AscendiaApp.Views.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LCTWorks.Telemetry;
using LCTWorks.WinUI.Dialogs;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace AscendiaApp.ViewModels;

public partial class MembersViewModel : ObservableObject
{
    private readonly CommunityService _communityService;
    private readonly DialogService _dialogService;
    private readonly ObservableCollection<MemberObservable> _members = [];
    private readonly ITelemetryService _telemetryService;
    private CancellationTokenSource _cts = new();

    public MembersViewModel(CommunityService communityService, DialogService dialogService, ITelemetryService telemetryService)
    {
        _communityService = communityService;
        _dialogService = dialogService;
        _telemetryService = telemetryService;
        RefreshInternal(false);
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

    [ObservableProperty]
    public partial string? SearchTerm { get; set; }

    [ObservableProperty]
    public partial MemberObservable? SelectedMember { get; set; }

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

    private void LoadFromFile()
    {
        //var picker = new FileOpenPicker
        //{
        //    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        //    ViewMode = PickerViewMode.List
        //};

        //// WinUI 3 requires initializing pickers with the app window handle.
        //var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        //InitializeWithWindow.Initialize(picker, hwnd);

        //picker.FileTypeFilter.Add(".json");

        //var file = await picker.PickSingleFileAsync();
        //if (file is not null)
        //{
        //    IsLoading = true;
        //    var json = await FileIO.ReadTextAsync(file.);
        //    var root = Json.ToObject<Root[]>(json);
        //    int failureCount = 0;
        //    for (int i = 0; i < root?.Length; i++)
        //    {
        //        var item = root[i];
        //        LoadingNotification = $"Adding member {i + 1} of {root.Length}";
        //        if (item == null || item?.id_dota == null)
        //        {
        //            continue;
        //        }
        //        if (!await _communityService.AddNewMemberAsync(item?.nickname, item!.id_dota, item?.team, null, null, null, false, null, false, false, false, null, 0))
        //        {
        //            failureCount++;
        //        }
        //    }

        //    IsLoading = false;
        //}
    }

    [RelayCommand]
    private void Refresh()
        => RefreshInternal(true);

    private async void RefreshInternal(bool forceRefresh)
        => await RefreshInternalAsync(forceRefresh);

    private async Task RefreshInternalAsync(bool forceRefresh)
    {
        LoadingNotification = null;
        IsLoading = true;

        var members = await _communityService.GetAllMembersAsync(forceRefresh);
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
        }, 100, _cts.Token);

        IsLoading = false;

        if (result > 0)
        {
            await RefreshInternalAsync(true);
        }
    }

    private class Root
    {
        public string? id { get; set; }

        public string? id_dota { get; set; }

        public string? leaderboard_rank { get; set; }

        public string? nickname { get; set; }

        public string? team { get; set; }
    }
}
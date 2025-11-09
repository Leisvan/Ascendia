using Ascendia.Core.Services;
using AscendiaApp.Models;
using AscendiaApp.Observable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LCTWorks.WinUI.Extensions;
using System;

using System.Threading.Tasks;

namespace AscendiaApp.ViewModels.Dialogs;

public partial class EditMemberViewModel(CommunityService communityService) : ObservableObject
{
    public EventHandler<EditOperationResult>? Finished;
    private readonly CommunityService _communityService = communityService;
    private string? _errorMessage;

    [ObservableProperty]
    public partial bool CheckLadder { get; set; } = true;

    [ObservableProperty]
    public partial bool? CheckWinLose { get; set; } = true;

    [ObservableProperty]
    public partial string? Country { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(IsNew))]
    public partial MemberObservable? EditMember { get; set; }

    [ObservableProperty]
    public partial string? EMail { get; set; }

    [ObservableProperty]
    public partial string? Notes { get; set; }

    public string? ErrorMessage
    {
        get => _errorMessage ?? "AddMember-FailedDefaultMessage".GetTextLocalized();
        set => SetProperty(ref _errorMessage, value);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    public partial double Id { get; set; } = 0;

    public bool IsBusy => !string.IsNullOrWhiteSpace(ProgressNotificationMessage);

    [ObservableProperty]
    public partial bool IsCaptain { get; set; } = false;

    public bool IsEditing => EditMember != null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotifying))]
    [NotifyPropertyChangedFor(nameof(IsFinished))]
    public partial bool IsFailure { get; set; } = false;

    public bool IsFinished => IsSuccess || IsFailure;

    public bool IsNew => !IsEditing;

    public bool IsNotifying => IsBusy || IsSuccess || IsFailure;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotifying))]
    [NotifyPropertyChangedFor(nameof(IsFinished))]
    public partial bool IsSuccess { get; set; } = false;

    public bool IsValid => Id > 9999999;

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? Phone { get; set; }

    [ObservableProperty]
    public partial string? Position { get; set; } = 1.ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyPropertyChangedFor(nameof(IsNotifying))]
    public partial string? ProgressNotificationMessage { get; set; }

    [ObservableProperty]
    public partial string? Team { get; set; }

    [ObservableProperty]
    public partial bool UpdateBeforeChecking { get; set; } = true;

    public void SetEditProperties(MemberObservable member)
    {
        if (member is null)
        {
            return;
        }

        EditMember = member;

        if (!double.TryParse(member.Record.AccountId, out var accId))
        {
            return;
        }

        CheckLadder = false;
        UpdateBeforeChecking = false;
        CheckWinLose = false;

        Name = member.Record.DisplayName;
        Id = accId;
        Team = member.Record.Team;
        Phone = member.Record.Phone;
        EMail = member.Record.Email;
        Country = member.Record.Country;
        IsCaptain = member.Record.IsCaptain ?? false;
        Position = member.Record.Position;
        Notes = member.Record.Notes;
    }

    [RelayCommand]
    private void AcceptFinished()
    {
        if (IsSuccess)
        {
            InvokeFinished(EditOperationResult.Success);
            return;
        }
        if (IsFailure)
        {
            ProgressNotificationMessage = null;
            IsSuccess = false;
            IsFailure = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        ErrorMessage = null;
        InvokeFinished(EditOperationResult.Cancelled);
    }

    private void InvokeFinished(EditOperationResult result)
            => Finished?.Invoke(this, result);

    [RelayCommand]
    private async Task StartAddingAsync()
    {
        if (IsBusy)
        {
            return;
        }
        if (!IsValid)
        {
            IsFailure = true;
            ErrorMessage = "AddMember-IdRequiredErrorMessage".GetTextLocalized();
            return;
        }
        var idStr = ((int)Id).ToString();

        try
        {
            bool success = false;
            if (IsEditing)
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    Name = EditMember!.Record.DisplayName ?? string.Empty;
                }
                success = await _communityService.EditMemberAsync(EditMember!.Record.Id, Name, idStr, Team, Phone, EMail, Country, IsCaptain, Position, Notes, (s, e) =>
                {
                    ProgressNotificationMessage = e;
                });
            }
            else
            {
                success = await _communityService.AddNewMemberAsync(Name, idStr, Team, Phone, EMail, Country, IsCaptain, Position, Notes, CheckLadder, UpdateBeforeChecking, CheckWinLose ?? false, (s, e) =>
                {
                    ProgressNotificationMessage = e;
                });
            }

            ProgressNotificationMessage = null;
            if (success)
            {
                IsSuccess = true;
                InvokeFinished(EditOperationResult.Success);
            }
            else
            {
                IsFailure = true;
            }
        }
        catch (Exception ex)
        {
            ProgressNotificationMessage = null;
            IsFailure = true;
            ErrorMessage = ex.Message;
        }
    }
}
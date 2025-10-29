using AscendiaApp.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace AscendiaApp.Views.Dialogs;

public sealed partial class EditMemberContentDialog : ContentDialog
{
    public EditMemberContentDialog()
    {
        InitializeComponent();
        ViewModel = App.GetService<EditMemberViewModel>();
    }

    public EditMemberViewModel? ViewModel { get; set; }
}
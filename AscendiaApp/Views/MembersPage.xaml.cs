using AscendiaApp.ViewModels;
using LCTWorks.Telemetry;
using Microsoft.UI.Xaml.Controls;

namespace AscendiaApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MembersPage : Page
    {
        public MembersPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<MembersViewModel>();
        }

        public MembersViewModel? ViewModel
        {
            get;
        }
    }
}
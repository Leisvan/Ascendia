using AscendiaApp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AscendiaApp.Views
{
    public sealed partial class BotPage : Page
    {
        public BotPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<BotViewModel>();
        }

        public BotViewModel? ViewModel { get; }
    }
}
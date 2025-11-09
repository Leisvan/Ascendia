using AscendiaApp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AscendiaApp.Views
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<AboutViewModel>();
        }

        public AboutViewModel? ViewModel { get; }
    }
}
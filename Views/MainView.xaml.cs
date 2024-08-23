using System.Windows.Controls;
using MiddleBooth.ViewModels;
using MiddleBooth.Services.Interfaces;

namespace MiddleBooth.Views
{
    public partial class MainView : UserControl
    {
        public MainView(ISettingsService settingsService, INavigationService navigationService)
        {
            InitializeComponent();
            DataContext = new MainViewModel(settingsService, navigationService);
        }
    }
}
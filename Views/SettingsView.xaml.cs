using System.Windows.Controls;
using MiddleBooth.ViewModels;

namespace MiddleBooth.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.ApplicationPin = passwordBox.Password;
            }
        }
    }
}

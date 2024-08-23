using System.Windows.Controls;
using MiddleBooth.ViewModels;

namespace MiddleBooth.Views
{
    public partial class MessageBoxView : UserControl
    {
        public MessageBoxView()
        {
            InitializeComponent();
        }

        public MessageBoxView(MessageBoxViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}

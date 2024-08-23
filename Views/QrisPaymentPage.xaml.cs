using System.Windows.Controls;
using MiddleBooth.ViewModels;

namespace MiddleBooth.Views
{
    public partial class QrisPaymentPage : UserControl
    {
        public QrisPaymentPage(QrisPaymentPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

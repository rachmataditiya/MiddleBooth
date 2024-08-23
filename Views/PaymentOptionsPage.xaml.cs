using System.Windows.Controls;
using MiddleBooth.ViewModels;

namespace MiddleBooth.Views
{
    public partial class PaymentOptionsPage : UserControl
    {
        public PaymentOptionsPage(PaymentOptionsPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

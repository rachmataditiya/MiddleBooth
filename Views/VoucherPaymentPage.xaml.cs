using System.Windows.Controls;
using MiddleBooth.ViewModels;

namespace MiddleBooth.Views
{
    public partial class VoucherPaymentPage : UserControl
    {
        public VoucherPaymentPage(VoucherPaymentPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

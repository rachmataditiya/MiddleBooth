using System;
using System.Windows;
using System.Windows.Controls;
using MiddleBooth.ViewModels;

namespace MiddleBooth.Views
{
    public partial class VoucherPaymentPage : UserControl
    {
        private readonly VoucherPaymentPageViewModel _viewModel;

        public VoucherPaymentPage(VoucherPaymentPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
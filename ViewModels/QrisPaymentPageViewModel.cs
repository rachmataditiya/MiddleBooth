using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MiddleBooth.ViewModels
{
    public class QrisPaymentPageViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPaymentService _paymentService;

        public ICommand BackCommand { get; }

        private BitmapImage _qrCodeImageSource = new();
        public BitmapImage QrCodeImageSource
        {
            get => _qrCodeImageSource;
            set => SetProperty(ref _qrCodeImageSource, value);
        }

        private string _paymentStatus = string.Empty;
        public string PaymentStatus
        {
            get => _paymentStatus;
            set => SetProperty(ref _paymentStatus, value);
        }

        public QrisPaymentPageViewModel(INavigationService navigationService, IPaymentService paymentService)
        {
            _navigationService = navigationService;
            _paymentService = paymentService;

            BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("MainView"));

            // Mendaftarkan event untuk menerima notifikasi pembayaran
            _paymentService.OnPaymentNotificationReceived += HandlePaymentNotification;

            // Mulai proses pembayaran QRIS
            Task.Run(async () => await StartQrisPayment());
        }

        private async Task StartQrisPayment()
        {
            try
            {
                // Ambil harga dari pengaturan dan buat QR code
                decimal amount = _paymentService.GetServicePrice();
                string qrCodeUrl = await _paymentService.GenerateQRCode(amount);

                // Gunakan Dispatcher untuk memastikan operasi UI dijalankan di thread utama
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Konversi URL QR code menjadi gambar untuk ditampilkan
                    QrCodeImageSource = new BitmapImage(new Uri(qrCodeUrl));
                    PaymentStatus = "Menunggu pembayaran...";
                });
            }
            catch (Exception ex)
            {
                // Gunakan Dispatcher untuk menampilkan pesan error di UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PaymentStatus = $"Error: {ex.Message}";
                });
            }
        }


        private void HandlePaymentNotification(string status)
        {
            // Perbarui status pembayaran berdasarkan notifikasi yang diterima
            if (status == "settlement")
            {
                PaymentStatus = "Pembayaran Berhasil!";
            }
            else if (status == "deny" || status == "cancel" || status == "expire")
            {
                PaymentStatus = "Pembayaran Gagal!";
            }
        }

        ~QrisPaymentPageViewModel()
        {
            // Unsubscribe event ketika ViewModel dihapus untuk menghindari memory leaks
            _paymentService.OnPaymentNotificationReceived -= HandlePaymentNotification;
        }
    }
}

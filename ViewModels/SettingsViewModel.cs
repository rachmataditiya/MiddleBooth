using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using Microsoft.Win32;
using System.Windows.Input;

namespace MiddleBooth.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly INavigationService _navigationService;

        public ICommand SaveSettingsCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand BrowseDSLRBoothPathCommand { get; }

        private string _dslrBoothPath = string.Empty;
        public string DSLRBoothPath
        {
            get => _dslrBoothPath;
            set
            {
                if (SetProperty(ref _dslrBoothPath, value))
                {
                    _settingsService.SetDSLRBoothPath(value);
                }
            }
        }

        private string _paymentGatewayUrl = string.Empty;
        public string PaymentGatewayUrl
        {
            get => _paymentGatewayUrl;
            set
            {
                if (SetProperty(ref _paymentGatewayUrl, value))
                {
                    _settingsService.SetPaymentGatewayUrl(value);
                }
            }
        }

        private decimal _servicePrice;
        public decimal ServicePrice
        {
            get => _servicePrice;
            set
            {
                if (SetProperty(ref _servicePrice, value))
                {
                    _settingsService.SetServicePrice(value);
                }
            }
        }

        private string _applicationPin = string.Empty;
        public string ApplicationPin
        {
            get => _applicationPin;
            set
            {
                if (SetProperty(ref _applicationPin, value))
                {
                    _settingsService.SetApplicationPin(value);
                }
            }
        }

        private bool _isProduction;
        public bool IsProduction
        {
            get => _isProduction;
            set
            {
                if (SetProperty(ref _isProduction, value))
                {
                    _settingsService.SetProduction(value);
                }
            }
        }

        private string _midtransServerKey = string.Empty;
        public string MidtransServerKey
        {
            get => _midtransServerKey;
            set
            {
                if (SetProperty(ref _midtransServerKey, value))
                {
                    _settingsService.SetMidtransServerKey(value);
                }
            }
        }

        public SettingsViewModel(ISettingsService settingsService, INavigationService navigationService)
        {
            _settingsService = settingsService;
            _navigationService = navigationService;

            // Load settings
            DSLRBoothPath = _settingsService.GetDSLRBoothPath();
            PaymentGatewayUrl = _settingsService.GetPaymentGatewayUrl();
            ServicePrice = _settingsService.GetServicePrice();
            ApplicationPin = _settingsService.GetApplicationPin();
            IsProduction = _settingsService.IsProduction();
            MidtransServerKey = _settingsService.GetMidtransServerKey();

            // Commands
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            BackCommand = new RelayCommand(_ => NavigateBack());
            BrowseDSLRBoothPathCommand = new RelayCommand(_ => BrowseDSLRBoothPath());
        }

        private void BrowseDSLRBoothPath()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select DSLRBooth Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                DSLRBoothPath = openFileDialog.FileName;
            }
        }

        private void SaveSettings(object? parameter)
        {
            // Implement logic to save settings if needed
        }

        private void NavigateBack()
        {
            _navigationService.NavigateTo("MainView");
        }
    }
}

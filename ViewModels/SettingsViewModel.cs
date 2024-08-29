using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace MiddleBooth.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly INavigationService _navigationService;

        public ICommand SaveSettingsCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand BrowseDSLRBoothPathCommand { get; }
        public ICommand BrowseProductImageCommand { get; }
        public ICommand BrowseMainBackgroundImageCommand { get; }

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

        private string _odooServer = string.Empty;
        public string OdooServer
        {
            get => _odooServer;
            set
            {
                if (SetProperty(ref _odooServer, value))
                {
                    _settingsService.SetOdooServer(value);
                }
            }
        }

        private string _odooUsername = string.Empty;
        public string OdooUsername
        {
            get => _odooUsername;
            set
            {
                if (SetProperty(ref _odooUsername, value))
                {
                    _settingsService.SetOdooUsername(value);
                }
            }
        }

        private string _odooPassword = string.Empty;
        public string OdooPassword
        {
            get => _odooPassword;
            set
            {
                if (SetProperty(ref _odooPassword, value))
                {
                    _settingsService.SetOdooPassword(value);
                }
            }
        }

        private string _odooDatabase = string.Empty;
        public string OdooDatabase
        {
            get => _odooDatabase;
            set
            {
                if (SetProperty(ref _odooDatabase, value))
                {
                    _settingsService.SetOdooDatabase(value);
                }
            }
        }

        private string _mqttHost = string.Empty;
        public string MqttHost
        {
            get => _mqttHost;
            set
            {
                if (SetProperty(ref _mqttHost, value))
                {
                    _settingsService.SetMqttHost(value);
                }
            }
        }

        private int _mqttPort;
        public int MqttPort
        {
            get => _mqttPort;
            set
            {
                if (SetProperty(ref _mqttPort, value))
                {
                    _settingsService.SetMqttPort(value);
                }
            }
        }

        private string _mqttUsername = string.Empty;
        public string MqttUsername
        {
            get => _mqttUsername;
            set
            {
                if (SetProperty(ref _mqttUsername, value))
                {
                    _settingsService.SetMqttUsername(value);
                }
            }
        }

        private string _mqttPassword = string.Empty;
        public string MqttPassword
        {
            get => _mqttPassword;
            set
            {
                if (SetProperty(ref _mqttPassword, value))
                {
                    _settingsService.SetMqttPassword(value);
                }
            }
        }

        private bool _isNotificationVisible;
        public bool IsNotificationVisible
        {
            get => _isNotificationVisible;
            set => SetProperty(ref _isNotificationVisible, value);
        }

        private string _notificationMessage = string.Empty;
        public string NotificationMessage
        {
            get => _notificationMessage;
            set => SetProperty(ref _notificationMessage, value);
        }

        private string _machineId = string.Empty;
        public string MachineId
        {
            get => _machineId;
            private set => SetProperty(ref _machineId, value);
        }
        private string _ProductImage = string.Empty;
        public string ProductImage
        {
            get => _ProductImage;
            set
            {
                if (SetProperty(ref _ProductImage, value))
                {
                    _settingsService.SetProductImage(value);
                }
            }
        }

        private string _MainBackgroundImage = string.Empty;
        public string MainBackgroundImage
        {
            get => _MainBackgroundImage;
            set
            {
                if (SetProperty(ref _MainBackgroundImage, value))
                {
                    _settingsService.SetMainBackgroundImage(value);
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
            OdooServer = _settingsService.GetOdooServer();
            OdooUsername = _settingsService.GetOdooUsername();
            OdooPassword = _settingsService.GetOdooPassword();
            OdooDatabase = _settingsService.GetOdooDatabase();
            MqttHost = _settingsService.GetMqttHost();
            MqttPort = _settingsService.GetMqttPort();
            MqttUsername = _settingsService.GetMqttUsername();
            MqttPassword = _settingsService.GetMqttPassword();
            ProductImage = _settingsService.GetProductImage();
            MainBackgroundImage = _settingsService.GetMainBackgroundImage();
            MachineId = GetOrCreateMachineId();

            // Commands
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            BackCommand = new RelayCommand(_ => NavigateBack());
            BrowseDSLRBoothPathCommand = new RelayCommand(_ => BrowseDSLRBoothPath());
            BrowseProductImageCommand = new RelayCommand(_ => BrowseProductImage());
            BrowseMainBackgroundImageCommand = new RelayCommand(_ => BrowseMainBackgroundImage());
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
        private void BrowseProductImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                Title = "Select Product Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProductImage = openFileDialog.FileName;
            }
        }

        private void BrowseMainBackgroundImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                Title = "Select Main Background Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                MainBackgroundImage = openFileDialog.FileName;
            }
        }
        private void SaveSettings(object? parameter)
        {
            // Save settings
            _settingsService.SetDSLRBoothPath(DSLRBoothPath);
            _settingsService.SetPaymentGatewayUrl(PaymentGatewayUrl);
            _settingsService.SetServicePrice(ServicePrice);
            _settingsService.SetApplicationPin(ApplicationPin);
            _settingsService.SetProduction(IsProduction);
            _settingsService.SetMidtransServerKey(MidtransServerKey);
            _settingsService.SetOdooServer(OdooServer);
            _settingsService.SetOdooUsername(OdooUsername);
            _settingsService.SetOdooPassword(OdooPassword);
            _settingsService.SetOdooDatabase(OdooDatabase);
            _settingsService.SetMqttHost(MqttHost);
            _settingsService.SetMqttPort(MqttPort);
            _settingsService.SetMqttUsername(MqttUsername);
            _settingsService.SetMqttPassword(MqttPassword);
            _settingsService.SetProductImage(ProductImage);
            _settingsService.SetMainBackgroundImage(MainBackgroundImage);

            // Show notification
            NotificationMessage = "Pengaturan berhasil disimpan!";
            IsNotificationVisible = true;

            // Hide notification after 3 seconds
            Task.Delay(3000).ContinueWith(_ =>
            {
                IsNotificationVisible = false;
                NotificationMessage = string.Empty;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void NavigateBack()
        {
            _navigationService.NavigateTo("MainView");
        }

        private string GetOrCreateMachineId()
        {
            string savedMachineId = _settingsService.GetMachineId();
            if (!string.IsNullOrEmpty(savedMachineId))
            {
                return savedMachineId;
            }

            string newMachineId = GenerateMachineId();
            _settingsService.SetMachineId(newMachineId);
            return newMachineId;
        }

        private static string GenerateMachineId()
        {
            string processorId = GetProcessorId();
            string macAddress = GetMacAddress();
            string combinedInfo = $"{processorId}|{macAddress}";

            if (string.IsNullOrEmpty(combinedInfo))
            {
                // Fallback jika tidak bisa mendapatkan informasi hardware
                combinedInfo = Guid.NewGuid().ToString();
            }

            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(combinedInfo));
            return BitConverter.ToString(hashBytes)[..32].Replace("-", "");
        }

        private static string GetProcessorId()
        {
            try
            {
                using var mc = new ManagementClass("win32_processor");
                using var moc = mc.GetInstances();
                foreach (var mo in moc)
                {
                    if (mo is ManagementObject managementObject)
                    {
                        return managementObject.Properties["processorID"].Value?.ToString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error getting processor ID: {ex.Message}");
            }
            return string.Empty;
        }

        private static string GetMacAddress()
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        return nic.GetPhysicalAddress().ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error getting MAC address: {ex.Message}");
            }
            return string.Empty;
        }
    }
}
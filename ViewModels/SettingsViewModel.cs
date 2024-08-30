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
using System.Windows;
using System.IO;
using Serilog;

namespace MiddleBooth.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly INavigationService _navigationService;
        private readonly IOdooService _odooService;

        public ICommand SaveSettingsCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand BrowseDSLRBoothPathCommand { get; }
        public ICommand GetMachineInfoCommand { get; }
        public ICommand ActivateMachineCommand { get; }

        private bool _isMachineActivated;
        public bool IsMachineActivated
        {
            get => _isMachineActivated;
            set
            {
                SetProperty(ref _isMachineActivated, value);
                OnPropertyChanged(nameof(CanActivateMachine));
            }
        }

        public bool CanActivateMachine => !IsMachineActivated;

        public string DSLRBoothPath
        {
            get => _settingsService.GetDSLRBoothPath();
            set
            {
                _settingsService.SetDSLRBoothPath(value);
                OnPropertyChanged();
                Log.Information($"DSLRBooth path updated: {value}");
            }
        }

        public string PaymentGatewayUrl
        {
            get => _settingsService.GetPaymentGatewayUrl();
            set
            {
                _settingsService.SetPaymentGatewayUrl(value);
                OnPropertyChanged();
                Log.Information($"Payment Gateway URL updated: {value}");
            }
        }

        public decimal ServicePrice
        {
            get => _settingsService.GetServicePrice();
            set
            {
                _settingsService.SetServicePrice(value);
                OnPropertyChanged();
                Log.Information($"Service Price updated: {value}");
            }
        }

        public string ApplicationPin
        {
            get => _settingsService.GetApplicationPin();
            set
            {
                _settingsService.SetApplicationPin(value);
                OnPropertyChanged();
                Log.Information("Application PIN updated");
            }
        }

        public bool IsProduction
        {
            get => _settingsService.IsProduction();
            set
            {
                _settingsService.SetProduction(value);
                OnPropertyChanged();
                Log.Information($"Production mode set to: {value}");
            }
        }

        public string MidtransServerKey
        {
            get => _settingsService.GetMidtransServerKey();
            set
            {
                _settingsService.SetMidtransServerKey(value);
                OnPropertyChanged();
                Log.Information("Midtrans Server Key updated");
            }
        }

        public string OdooServer
        {
            get => _settingsService.GetOdooServer();
            set
            {
                _settingsService.SetOdooServer(value);
                OnPropertyChanged();
                Log.Information($"Odoo Server updated: {value}");
            }
        }

        public string OdooUsername
        {
            get => _settingsService.GetOdooUsername();
            set
            {
                _settingsService.SetOdooUsername(value);
                OnPropertyChanged();
                Log.Information($"Odoo Username updated: {value}");
            }
        }

        public string OdooPassword
        {
            get => _settingsService.GetOdooPassword();
            set
            {
                _settingsService.SetOdooPassword(value);
                OnPropertyChanged();
                Log.Information("Odoo Password updated");
            }
        }

        public string OdooDatabase
        {
            get => _settingsService.GetOdooDatabase();
            set
            {
                _settingsService.SetOdooDatabase(value);
                OnPropertyChanged();
                Log.Information($"Odoo Database updated: {value}");
            }
        }

        public string MqttHost
        {
            get => _settingsService.GetMqttHost();
            set
            {
                _settingsService.SetMqttHost(value);
                OnPropertyChanged();
                Log.Information($"MQTT Host updated: {value}");
            }
        }

        public int MqttPort
        {
            get => _settingsService.GetMqttPort();
            set
            {
                _settingsService.SetMqttPort(value);
                OnPropertyChanged();
                Log.Information($"MQTT Port updated: {value}");
            }
        }

        public string MqttUsername
        {
            get => _settingsService.GetMqttUsername();
            set
            {
                _settingsService.SetMqttUsername(value);
                OnPropertyChanged();
                Log.Information($"MQTT Username updated: {value}");
            }
        }

        public string MqttPassword
        {
            get => _settingsService.GetMqttPassword();
            set
            {
                _settingsService.SetMqttPassword(value);
                OnPropertyChanged();
                Log.Information("MQTT Password updated");
            }
        }

        public string ProductImage
        {
            get => _settingsService.GetProductImage();
            set
            {
                _settingsService.SetProductImage(value);
                OnPropertyChanged();
                Log.Information($"Product Image path updated: {value}");
            }
        }

        public string MainBackgroundImage
        {
            get => _settingsService.GetMainBackgroundImage();
            set
            {
                _settingsService.SetMainBackgroundImage(value);
                OnPropertyChanged();
                Log.Information($"Main Background Image path updated: {value}");
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

        public string MachineId => _settingsService.GetMachineId();

        public SettingsViewModel(ISettingsService settingsService, INavigationService navigationService, IOdooService odooService)
        {
            _settingsService = settingsService;
            _navigationService = navigationService;
            _odooService = odooService;

            // Ensure MachineId is set
            if (string.IsNullOrEmpty(_settingsService.GetMachineId()))
            {
                var machineId = GenerateMachineId();
                _settingsService.SetMachineId(machineId);
                Log.Information($"New Machine ID generated and set: {machineId}");
            }

            IsMachineActivated = _settingsService.MachineActivated();

            // Commands
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            BackCommand = new RelayCommand(_ => NavigateBack());
            BrowseDSLRBoothPathCommand = new RelayCommand(_ => BrowseDSLRBoothPath());
            GetMachineInfoCommand = new RelayCommand(async _ => await GetMachineInfo());
            ActivateMachineCommand = new RelayCommand(async _ => await ActivateMachine(), _ => CanActivateMachine);

            Log.Information("SettingsViewModel initialized");
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
                Log.Information($"DSLRBooth path set to: {DSLRBoothPath}");
            }
        }

        private void SaveSettings(object? parameter)
        {
            _settingsService.SetOdooServer(OdooServer);
            _settingsService.SetOdooUsername(OdooUsername);
            _settingsService.SetOdooPassword(OdooPassword);
            _settingsService.SetOdooDatabase(OdooDatabase);
            ShowNotification("Pengaturan berhasil disimpan!");
            Log.Information("Settings saved successfully");
        }

        private void NavigateBack()
        {
            _navigationService.NavigateTo("MainView");
            Log.Information("Navigated back to MainView");
        }

        private async Task GetMachineInfo()
        {
            Log.Information("Getting machine info from Odoo");
            try
            {
                var machineInfo = await _odooService.GetMachineInfo(MachineId);

                if (machineInfo.Success)
                {
                    ApplicationPin = machineInfo.ApplicationPin ?? string.Empty;
                    MidtransServerKey = machineInfo.MidtransServerKey ?? string.Empty;
                    IsProduction = machineInfo.IsProduction;
                    ServicePrice = (decimal)machineInfo.DefaultBoothPrice;
                    PaymentGatewayUrl = machineInfo.PaymentGatewayUrl ?? string.Empty;
                    MqttHost = machineInfo.MqttHost ?? string.Empty;
                    MqttPort = machineInfo.MqttPort;
                    MqttUsername = machineInfo.MqttUsername ?? string.Empty;
                    MqttPassword = machineInfo.MqttPassword ?? string.Empty;

                    if (!string.IsNullOrEmpty(machineInfo.ProductImage))
                    {
                        ProductImage = SaveBase64Image(machineInfo.ProductImage, "ProductImage.png");
                    }
                    if (!string.IsNullOrEmpty(machineInfo.MainBackgroundImage))
                    {
                        MainBackgroundImage = SaveBase64Image(machineInfo.MainBackgroundImage, "MainBackgroundImage.png");
                    }

                    ShowNotification("Informasi mesin berhasil diperbarui!");
                    Log.Information("Machine info successfully updated from Odoo");
                }
                else
                {
                    ShowNotification($"Gagal mendapatkan informasi mesin: {machineInfo.Message ?? "Unknown error"}", isError: true);
                    Log.Warning($"Failed to get machine info: {machineInfo.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Terjadi kesalahan: {ex.Message}", isError: true);
                Log.Error(ex, "Error occurred while getting machine info");
            }
        }

        private string SaveBase64Image(string base64String, string fileName)
        {
            try
            {
                // Remove MIME type header if present
                string base64Data = base64String;
                if (base64String.Contains(',', StringComparison.Ordinal))
                {
                    base64Data = base64String.Split(',')[1];
                }

                // Ensure the base64 string is valid
                base64Data = base64Data.Trim();
                if (base64Data.Length % 4 > 0)
                {
                    base64Data = base64Data.PadRight(base64Data.Length + (4 - base64Data.Length % 4), '=');
                }

                byte[] imageBytes = Convert.FromBase64String(base64Data);
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                // Ensure the file extension is correct
                string extension = Path.GetExtension(fileName);
                if (string.IsNullOrEmpty(extension))
                {
                    // If no extension, try to determine from the image bytes
                    string mimeType = GetMimeType(imageBytes);
                    extension = GetExtensionFromMimeType(mimeType);
                    imagePath = Path.ChangeExtension(imagePath, extension);
                }

                File.WriteAllBytes(imagePath, imageBytes);
                Log.Information($"Image saved successfully: {imagePath}");
                return imagePath;
            }
            catch (Exception ex)
            {
                ShowNotification($"Gagal menyimpan gambar {fileName}: {ex.Message}", isError: true);
                Log.Error(ex, $"Failed to save image {fileName}. Base64 string: {base64String[..Math.Min(100, base64String.Length)]}...");
                return string.Empty;
            }
        }

        private static string GetMimeType(byte[] imageBytes)
        {
            if (imageBytes.Length >= 2)
            {
                if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8) return "image/jpeg";
                if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50) return "image/png";
                if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49) return "image/gif";
            }
            return "application/octet-stream";
        }

        private static string GetExtensionFromMimeType(string mimeType) => mimeType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            _ => ".bin"
        };

        private void ShowNotification(string message, bool isError = false)
        {
            NotificationMessage = message;
            IsNotificationVisible = true;

            // Hide notification after 3 seconds
            Task.Delay(3000).ContinueWith(_ =>
            {
                IsNotificationVisible = false;
                NotificationMessage = string.Empty;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            if (isError)
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Error notification shown: {message}");
            }
            else
            {
                Log.Information($"Notification shown: {message}");
            }
        }

        private static string GenerateMachineId()
        {
            string processorId = GetProcessorId();
            string macAddress = GetMacAddress();

            // Jika tidak bisa mendapatkan informasi perangkat keras, gunakan fallback ke data yang lebih konsisten
            if (string.IsNullOrEmpty(processorId) || string.IsNullOrEmpty(macAddress))
            {
                processorId = "FallbackProcessorId";
                macAddress = "FallbackMacAddress";
                Log.Warning("Falling back to default IDs due to hardware information retrieval failure");
            }

            string combinedInfo = $"{processorId}|{macAddress}";
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
                Log.Error(ex, "Error getting processor ID");
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
                Log.Error(ex, "Error getting MAC address");
            }
            return string.Empty;
        }

        private async Task ActivateMachine()
        {
            try
            {
                string machineId = _settingsService.GetMachineId();
                var result = await _odooService.ActivateMachine(
                    machineId,
                    $"Machine {machineId}",
                    "MiddleBooth"
                );

                if (result.success)
                {
                    _settingsService.SetMachineActivated(true);
                    IsMachineActivated = true;
                    ShowNotification("Machine activated successfully!");
                    Log.Information("Machine activated successfully");
                }
                else
                {
                    ShowNotification($"Failed to activate machine: {result.message}", isError: true);
                    Log.Warning($"Failed to activate machine: {result.message}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error activating machine");
                ShowNotification($"An error occurred while activating the machine: {ex.Message}", isError: true);
            }
        }
    }
}
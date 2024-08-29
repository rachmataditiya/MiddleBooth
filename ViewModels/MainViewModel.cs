using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System.Windows;
using System.Windows.Input;
using Serilog;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

namespace MiddleBooth.ViewModels
{
    public class MainViewModel : BaseViewModel, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly INavigationService _navigationService;
        private readonly IDSLRBoothService _dslrBoothService;
        private readonly IOdooService _odooService;

        public ICommand OpenSettingsCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand ContinueOrActivateCommand { get; }

        private bool _isKeypadVisible;
        public bool IsKeypadVisible
        {
            get => _isKeypadVisible;
            set => SetProperty(ref _isKeypadVisible, value);
        }

        private string _keypadPurpose = string.Empty;
        public string KeypadPurpose
        {
            get => _keypadPurpose;
            set => SetProperty(ref _keypadPurpose, value);
        }

        private readonly bool _isDSLRBoothSessionActive;

        private string _continueButtonText = "Continue";
        public string ContinueButtonText
        {
            get => _continueButtonText;
            set => SetProperty(ref _continueButtonText, value);
        }
        private ImageSource? _mainBackgroundImageSource;
        public ImageSource? MainBackgroundImageSource
        {
            get => _mainBackgroundImageSource;
            set => SetProperty(ref _mainBackgroundImageSource, value);
        }

        private void LoadMainBackgroundImage()
        {
            string imagePath = _settingsService.GetMainBackgroundImage();
            if (File.Exists(imagePath))
            {
                MainBackgroundImageSource = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                Log.Information($"Main background image loaded from {imagePath}");
            }
            else
            {
                Log.Warning($"Main background image not found at {imagePath}");
                MainBackgroundImageSource = new BitmapImage(new Uri("/Resources/background.png", UriKind.Relative));
            }
        }

        public KeypadViewModel KeypadViewModel { get; }

        public MainViewModel(ISettingsService settingsService, INavigationService navigationService, IDSLRBoothService dslrBoothService, IOdooService odooService)
        {
            _settingsService = settingsService;
            _navigationService = navigationService;
            _dslrBoothService = dslrBoothService;
            _odooService = odooService;
            LoadMainBackgroundImage();

            OpenSettingsCommand = new RelayCommand(_ => ShowKeypad("Settings"));
            ExitCommand = new RelayCommand(_ => ShowKeypad("Exit"));
            ContinueOrActivateCommand = new RelayCommand(async _ => await ContinueOrActivate());

            KeypadViewModel = new KeypadViewModel();
            KeypadViewModel.PinEntered += OnPinEntered;
            Log.Information("MainViewModel initialized. TriggerReceived event handler attached.");

            _ = InitializeView();
        }

        private async Task InitializeView()
        {
            await CheckDSLRBoothStatus();
            UpdateContinueButtonText();
        }

        private async Task CheckDSLRBoothStatus()
        {
            Log.Information("Checking DSLRBooth status");
            if (_dslrBoothService.IsDSLRBoothRunning())
            {
                Log.Information("DSLRBooth is running. Hiding it initially.");
                await _dslrBoothService.SetDSLRBoothVisibility(false);
            }
            else
            {
                bool launched = await _dslrBoothService.LaunchDSLRBooth();
                if (launched)
                {
                    Log.Information("DSLRBooth launched successfully. Hiding it.");
                    await _dslrBoothService.SetDSLRBoothVisibility(false);
                }
                else
                {
                    Log.Warning("Failed to launch DSLRBooth.");
                }
            }
        }

        private void UpdateContinueButtonText()
        {
            ContinueButtonText = _settingsService.MachineActivated() ? "Continue" : "Activate Machine";
        }

        private async Task ContinueOrActivate()
        {
            if (_settingsService.MachineActivated())
            {
                NavigateToPaymentOptions();
            }
            else
            {
                await ActivateMachine();
            }
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
                    UpdateContinueButtonText();
                    MessageBox.Show("Machine activated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to activate machine: {result.message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error activating machine");
                MessageBox.Show($"An error occurred while activating the machine: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task SetInitialVisibility()
        {
            Log.Information($"Setting initial visibility. DSLRBooth session active: {_isDSLRBoothSessionActive}");
            await CheckDSLRBoothStatus();
            if (!_isDSLRBoothSessionActive)
            {
                await _dslrBoothService.SetDSLRBoothVisibility(false);
            }
        }

        private void ShowKeypad(string purpose)
        {
            Log.Information($"Showing keypad for purpose: {purpose}");
            KeypadPurpose = purpose;
            IsKeypadVisible = true;
        }

        private void OnPinEntered(string pin)
        {
            Log.Information("PIN entered");
            IsKeypadVisible = false;
            if (pin == _settingsService.GetApplicationPin())
            {
                if (KeypadPurpose == "Settings")
                {
                    Log.Information("Correct PIN entered for Settings. Navigating to SettingsView.");
                    _navigationService.NavigateTo("SettingsView");
                }
                else if (KeypadPurpose == "Exit")
                {
                    Log.Information("Correct PIN entered for Exit. Exiting application.");
                    Exit();
                }
            }
            else
            {
                Log.Warning("Incorrect PIN entered.");
            }
        }

        private void NavigateToPaymentOptions()
        {
            Log.Information("Navigating to PaymentOptionsPage");
            _navigationService.NavigateTo("PaymentOptionsPage");
        }

        private static void Exit()
        {
            Log.Information("Application shutting down");
            Application.Current.Shutdown();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                KeypadViewModel.PinEntered -= OnPinEntered;
            }
        }
    }
}
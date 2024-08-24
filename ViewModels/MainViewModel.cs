// File: ViewModels/MainViewModel.cs

using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using Serilog;

namespace MiddleBooth.ViewModels
{
    public class MainViewModel : BaseViewModel, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly INavigationService _navigationService;
        private readonly IDSLRBoothService _dslrBoothService;

        public ICommand OpenSettingsCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand ContinueCommand { get; }

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

        private bool _isDSLRBoothSessionActive = false;

        public KeypadViewModel KeypadViewModel { get; }

        public MainViewModel(ISettingsService settingsService, INavigationService navigationService, IDSLRBoothService dslrBoothService)
        {
            _settingsService = settingsService;
            _navigationService = navigationService;
            _dslrBoothService = dslrBoothService;

            OpenSettingsCommand = new RelayCommand(_ => ShowKeypad("Settings"));
            ExitCommand = new RelayCommand(_ => ShowKeypad("Exit"));
            ContinueCommand = new RelayCommand(_ => NavigateToPaymentOptions());

            KeypadViewModel = new KeypadViewModel();
            KeypadViewModel.PinEntered += OnPinEntered;
            Log.Information("MainViewModel initialized. TriggerReceived event handler attached.");

            _ = CheckDSLRBoothStatus();
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
            Log.Information("Disposing MainViewModel");
            KeypadViewModel.PinEntered -= OnPinEntered;
        }
    }
}
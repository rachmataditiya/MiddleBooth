// File: ViewModels/MainViewModel.cs

using MiddleBooth.Services.Interfaces;
using MiddleBooth.Utilities;
using System;
using System.Windows.Input;
using System.Threading.Tasks;

namespace MiddleBooth.ViewModels
{
    public class MainViewModel : BaseViewModel
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

            CheckDSLRBoothStatus();
        }

        private async void CheckDSLRBoothStatus()
        {
            if (_dslrBoothService.CheckDSLRBoothPath())
            {
                if (!_dslrBoothService.IsDSLRBoothRunning())
                {
                    bool launched = await _dslrBoothService.LaunchDSLRBooth();
                    if (launched)
                    {
                        await _dslrBoothService.SetDSLRBoothTopmost(false);
                    }
                    else
                    {
                        // Handle launch failure (e.g., show a message to the user)
                    }
                }
                else
                {
                    await _dslrBoothService.SetDSLRBoothTopmost(false);
                }
            }
            else
            {
                // Handle invalid DSLRBooth path (e.g., show a message to the user)
            }
        }

        private void ShowKeypad(string purpose)
        {
            KeypadPurpose = purpose;
            IsKeypadVisible = true;
        }

        private void OnPinEntered(string pin)
        {
            IsKeypadVisible = false;
            if (pin == _settingsService.GetApplicationPin())
            {
                if (KeypadPurpose == "Settings")
                {
                    _navigationService.NavigateTo("SettingsView");
                }
                else if (KeypadPurpose == "Exit")
                {
                    Exit();
                }
            }
            else
            {
                // Show error message for incorrect PIN
            }
        }

        private void NavigateToPaymentOptions()
        {
            _navigationService.NavigateTo("PaymentOptionsPage");
        }

        private static void Exit()
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
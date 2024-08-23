// ViewModels/KeypadViewModel.cs
using MiddleBooth.Utilities;
using System;
using System.Windows.Input;

namespace MiddleBooth.ViewModels
{
    public class KeypadViewModel : BaseViewModel
    {
        private string _enteredPin = string.Empty;
        public string EnteredPin
        {
            get => _enteredPin;
            set => SetProperty(ref _enteredPin, value);
        }

        public ICommand NumberCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SubmitCommand { get; }

        public event Action<string>? PinEntered;

        public KeypadViewModel()
        {
            NumberCommand = new RelayCommand<string>(AddNumber);
            ClearCommand = new RelayCommand(_ => ClearPin());
            SubmitCommand = new RelayCommand(_ => SubmitPin());
        }

        private void AddNumber(string? number)
        {
            if (number != null && EnteredPin.Length < 4)
            {
                EnteredPin += number;
            }
        }

        private void ClearPin()
        {
            EnteredPin = string.Empty;
        }

        private void SubmitPin()
        {
            if (EnteredPin.Length == 4)
            {
                PinEntered?.Invoke(EnteredPin);
                EnteredPin = string.Empty;
            }
        }
    }
}
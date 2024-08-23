using System;
using System.Windows.Input;
using MiddleBooth.Utilities;
using MaterialDesignThemes.Wpf;

namespace MiddleBooth.ViewModels
{
    public class MessageBoxViewModel : BaseViewModel
    {
        public string Message { get; }
        public PackIconKind IconKind { get; }
        public string IconColor { get; }
        public ICommand OkCommand { get; }

        public event Action OnClose = delegate { };

        public MessageBoxViewModel(string message, PackIconKind iconKind, string iconColor)
        {
            Message = message;
            IconKind = iconKind;
            IconColor = iconColor;
            OkCommand = new RelayCommand(_ => Close());
        }

        private void Close()
        {
            OnClose?.Invoke();
        }
    }
}

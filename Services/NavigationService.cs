using System;
using System.Windows;
using MiddleBooth.Services.Interfaces;

namespace MiddleBooth.Services
{
    public class NavigationService : INavigationService
    {
        public event EventHandler<string>? NavigationRequested;
        public event EventHandler<object>? OverlayRequested;

        public void NavigateTo(string viewName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                NavigationRequested?.Invoke(this, viewName);
            });
        }

        public void NavigateToOverlay(object overlayView)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OverlayRequested?.Invoke(this, overlayView);
            });
        }
    }
}
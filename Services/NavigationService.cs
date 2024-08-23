using System;
using MiddleBooth.Services.Interfaces;

namespace MiddleBooth.Services
{
    public class NavigationService : INavigationService
    {
        public event EventHandler<string>? NavigationRequested;
        public event EventHandler<object>? OverlayRequested;

        public void NavigateTo(string viewName)
        {
            NavigationRequested?.Invoke(this, viewName);
        }

        public void NavigateToOverlay(object overlayView)
        {
            OverlayRequested?.Invoke(this, overlayView);
        }
    }
}

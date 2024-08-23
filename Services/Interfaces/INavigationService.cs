using System;

namespace MiddleBooth.Services.Interfaces
{
    public interface INavigationService
    {
        event EventHandler<string> NavigationRequested;
        event EventHandler<object> OverlayRequested;

        void NavigateTo(string viewName);
        void NavigateToOverlay(object overlayView);
    }
}

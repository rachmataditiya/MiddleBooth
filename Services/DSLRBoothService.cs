using MiddleBooth.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Serilog;
using System.Windows.Interop;
using System.Windows;

namespace MiddleBooth.Services
{
    public class DSLRBoothService : IDSLRBoothService
    {
        private readonly ISettingsService _settingsService;
        private Process? _dslrBoothProcess;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        public DSLRBoothService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public bool CheckDSLRBoothPath()
        {
            string path = _settingsService.GetDSLRBoothPath();
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        public async Task<bool> LaunchDSLRBooth()
        {
            if (!CheckDSLRBoothPath())
            {
                Log.Warning("DSLRBooth path is invalid or not set.");
                return false;
            }

            string path = _settingsService.GetDSLRBoothPath();
            try
            {
                if (_dslrBoothProcess != null && !_dslrBoothProcess.HasExited)
                {
                    Log.Information("Stopping existing DSLRBooth process.");
                    _dslrBoothProcess.CloseMainWindow();
                    await Task.Delay(1000);
                    if (!_dslrBoothProcess.HasExited)
                    {
                        _dslrBoothProcess.Kill();
                    }
                }

                Log.Information("Launching DSLRBooth from path: {Path}", path);
                _dslrBoothProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    }
                };
                _dslrBoothProcess.Start();
                await Task.Delay(3000);
                Log.Information("DSLRBooth launched successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error launching DSLRBooth");
                return false;
            }
        }

        public Task SetDSLRBoothVisibility(bool isVisible)
        {
            return Task.Run(() =>
            {
                var dslrBoothProcess = Process.GetProcessesByName("dslrBooth").FirstOrDefault();
                if (dslrBoothProcess != null && dslrBoothProcess.MainWindowHandle != IntPtr.Zero)
                {
                    if (isVisible)
                    {
                        BringToFront(dslrBoothProcess.MainWindowHandle, restoreIfMinimized: true);
                    }
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.SetVisibility(!isVisible);
                    }
                });
            });
        }
        public bool IsDSLRBoothRunning()
        {
            if (_dslrBoothProcess != null && !_dslrBoothProcess.HasExited)
            {
                return true;
            }
            Process[] processes = Process.GetProcessesByName("dslrBooth");
            if (processes.Length > 0)
            {
                _dslrBoothProcess = processes[0];
                return true;
            }
            return false;
        }
        public static void BringToFront(IntPtr handle, bool restoreIfMinimized = false)
        {
            if (restoreIfMinimized)
            {
                ShowWindow(handle, SW_RESTORE);
            }
            SetForegroundWindow(handle);
        }
    }
}
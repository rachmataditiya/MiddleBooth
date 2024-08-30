using MiddleBooth.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Serilog;
using System.Windows.Interop;
using System.Windows;
using System.Net.Http;

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
        private const int SW_MAXIMIZE = 3;

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
                    Log.Information("DSLRBooth is already running. Bringing it to front and maximizing.");
                    BringToFront(_dslrBoothProcess.MainWindowHandle);
                    return true;
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
                await Task.Delay(3000); // Wait for the process to initialize
                BringToFront(_dslrBoothProcess.MainWindowHandle);
                Log.Information("DSLRBooth launched successfully and maximized.");
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
                        BringToFront(dslrBoothProcess.MainWindowHandle);
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

        public static void BringToFront(IntPtr handle)
        {
            ShowWindow(handle, SW_RESTORE);
            ShowWindow(handle, SW_MAXIMIZE);
            SetForegroundWindow(handle);
        }

        public async Task CallStartApi()
        {
            string _dslrBoothPassword = _settingsService.GetDSLRBoothPassword();
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"http://localhost:1500/api/start?mode=print&password={_dslrBoothPassword}");
                response.EnsureSuccessStatusCode();
                Log.Information("Successfully called the start API");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error calling the start API: {ex.Message}");
            }
        }
    }
}
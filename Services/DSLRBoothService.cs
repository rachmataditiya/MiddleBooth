using MiddleBooth.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace MiddleBooth.Services
{
    public class DSLRBoothService : IDSLRBoothService
    {
        private readonly ISettingsService _settingsService;
        private Process? _dslrBoothProcess;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

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
                return false;
            }

            string path = _settingsService.GetDSLRBoothPath();

            try
            {
                _dslrBoothProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    }
                };

                _dslrBoothProcess.Start();
                await Task.Delay(2000); // Berikan waktu untuk DSLRBooth untuk memulai
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        public async Task SetDSLRBoothTopmost(bool isTopmost)
        {
            await Task.Run(() =>
            {
                IntPtr hWnd = FindWindow(null, "DSLRBooth");
                if (hWnd != IntPtr.Zero)
                {
                    SetWindowPos(hWnd,
                                 isTopmost ? HWND_TOPMOST : HWND_NOTOPMOST,
                                 0, 0, 0, 0,
                                 SWP_NOMOVE | SWP_NOSIZE);
                }
            });
        }

        public bool IsDSLRBoothRunning()
        {
            if (_dslrBoothProcess != null && !_dslrBoothProcess.HasExited)
            {
                return true;
            }

            Process[] processes = Process.GetProcessesByName("DSLRBooth");
            if (processes.Length > 0)
            {
                _dslrBoothProcess = processes[0];
                return true;
            }

            return false;
        }
    }
}
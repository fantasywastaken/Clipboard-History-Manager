using System;
using System.Threading;
using System.Windows;

namespace Fantasy.ClipboardHistory
{
    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;
        private const string MutexName = "Fantasy.ClipboardHistory.SingleInstance";

        private MainWindow? _mainWindow;
        private TrayIconManager? _trayIcon;

        private void OnStartup(object sender, StartupEventArgs e)
        {
            _singleInstanceMutex = new Mutex(true, MutexName, out bool created);
            if (!created)
            {
                Shutdown();
                return;
            }

            _mainWindow = new MainWindow();
            MainWindow = _mainWindow;

            _trayIcon = new TrayIconManager(_mainWindow);
            _trayIcon.ShowRequested += (_, _) => _mainWindow.ShowAndActivate();
            _trayIcon.ExitRequested += (_, _) => ExitApplication();

            _mainWindow.InitializeAfterStartup();
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
        }

        public void ExitApplication()
        {
            _mainWindow?.PrepareForShutdown();
            Shutdown();
        }
    }
}

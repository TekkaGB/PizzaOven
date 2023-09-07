using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System;

namespace PizzaOven
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected static bool AlreadyRunning()
        {
            bool running = false;
            try
            {
                // Getting collection of process  
                Process currentProcess = Process.GetCurrentProcess();

                // Check with other process already running   
                foreach (var p in Process.GetProcesses())
                {
                    if (p.Id != currentProcess.Id) // Check running process   
                    {
                        if (p.ProcessName.Equals(currentProcess.ProcessName) && p.MainModule.FileName.Equals(currentProcess.MainModule.FileName))
                        {
                            running = true;
                            break;
                        }
                    }
                }
            }
            catch { }
            return running;
        }
        protected async override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            RegistryConfig.InstallGBHandler();
            bool running = AlreadyRunning();
            if (!running)
            {
                MainWindow mw = new MainWindow();
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mw.Show();
                // Only check for updates if PizzaOven wasn't launched by 1-click install button
                if (e.Args.Length == 0)
                    if (await AutoUpdater.CheckForPizzaOvenUpdate(new CancellationTokenSource()))
                        mw.Close();
            }

            // Allow 1-click installs even if another instance is running
            if (e.Args.Length > 1 && e.Args[0] == "-download") {
                // For some reason the downloader doesn't work if we don't create a main window...
                // (the code above already creates one when no instance is running)
                if (running) {
                    MainWindow mw = new MainWindow();
                    ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
                new ModDownloader().Download(e.Args[1], running);
            }
            else if (running)
            {
                MessageBox.Show("Pizza Oven is already running", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Application.Current.Shutdown();
            }
        }
        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception occured:\n{e.Exception.Message}\n\nInner Exception:\n{e.Exception.InnerException}" +
                $"\n\nStack Trace:\n{e.Exception.StackTrace}", "Error", MessageBoxButton.OK,
                             MessageBoxImage.Error);

            e.Handled = true;
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                if (Current.MainWindow != null)
                {
                    ((MainWindow)Current.MainWindow).ModGrid.IsEnabled = true;
                    ((MainWindow)Current.MainWindow).ConfigButton.IsEnabled = true;
                    ((MainWindow)Current.MainWindow).LaunchButton.IsEnabled = true;
                    ((MainWindow)Current.MainWindow).ClearButton.IsEnabled = true;
                    ((MainWindow)Current.MainWindow).UpdateButton.IsEnabled = true;
                }
            });
        }
    }
}

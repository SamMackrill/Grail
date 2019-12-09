using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Aveva.Tools.SquirrelNutkin;

using Grail.Properties;
using Grail.ViewModel;

namespace Grail
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private MainWindowViewModel context;
        private Task update = Task.FromResult(true);

        private async void Application_Start(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            var args = e.Args.Where(a => !a.StartsWith(SquirrelHelper.SquirrelArg)).ToList();
            var options = args
                .Where(a => a.StartsWith("/"))
                .Select(o => o.TrimStart('/').ToUpperInvariant())
                .ToList();


            context = new MainWindowViewModel(options);

            var window = new MainWindow
            {
                DataContext = context
            };

            window.Show();

            try
            {
                update = context.CheckForUpdates(e.Args);
                await update;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if (!e.IsTerminating) MessageBox.Show((e.ExceptionObject as Exception)?.Message ?? "Unknown", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
            await update.ContinueWith(ex => { });
            context?.Dispose();
        }
    }
}

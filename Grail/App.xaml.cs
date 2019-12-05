using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using Aveva.Tools.SquirrelNutkin;

using Grail.Properties;

namespace Grail
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string applicationName;

        private void Application_Start(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            applicationName = typeof(App).Assembly.GetName().Name;

            var args = e.Args.Where(a => !a.StartsWith(SquirrelHelper.SquirrelArg)).ToList();
            var options = args
                .Where(a => a.StartsWith("/"))
                .Select(o => o.TrimStart('/').ToUpperInvariant())
                .ToList();

            var squirrel = new SquirrelHelper(applicationName, Settings.Default.ReleasePath, e.Args, true);
            squirrel.Message += m =>
            {
                MessageBox.Show(m, "Install", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            squirrel.ResponseMessage += m =>
            {
                var result = MessageBox.Show(m, "Install", MessageBoxButton.YesNo, MessageBoxImage.Information);
                return result == MessageBoxResult.Yes ? UserResponse.Exit : UserResponse.OK;
            };
            if (squirrel.SpecialCall || !options.Contains("U"))
            {
                var update = squirrel.UpdateApp();
                var success = update.Wait(new TimeSpan(0, 0, Settings.Default.UpdateTimeout));
                if (!success) Console.WriteLine("Update failed, carry on as if it didn't");

                if (update.Result == UpdateState.UpdatedWithExit || squirrel.SpecialCall)
                {
                    if (Debugger.IsAttached) Console.ReadKey();
                    return;
                }
            }

            var window = new MainWindow();

            window.Show();

        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception)?.Message ?? "Unknown", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Grail.Properties;

using Squirrel;

namespace Grail.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly List<string> options;
        public static string Title => $"The One True Grail {Extensions.GetShortVersion()}";

        private string version;
        public string Version
        {
            get => version;
            private set
            {
                if (version == value) return;
                version = value;
                OnPropertyChanged();
            }
        }

        private string updateInformation;
        public string UpdateInformation
        {
            get => updateInformation;
            private set
            {
                if (updateInformation == value) return;
                updateInformation = value;
                OnPropertyChanged();
            }
        }

        private readonly string applicationName;
        private readonly string releasePath;

        public MainWindowViewModel(List<string> options)
        {
            applicationName = typeof(App).Assembly.GetName().Name;
            this.options = options;

            releasePath = Directory.Exists(Settings.Default.LocalReleasePath)
                ? Settings.Default.LocalReleasePath 
                : Settings.Default.ReleasePath;
        }

        private bool updateAvailable;
        public bool UpdateAvailable
        {
            get => updateAvailable;
            private set
            {
                if (updateAvailable == value) return;
                updateAvailable = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public override string Key => "Main";

        public async Task CheckForUpdates(string[] args)
        {

            UpdateAvailable = false;

            if (options.Contains("U"))
            {
                UpdateInformation = "UpdatesDisabled!";
                return;
            }

            try
            {

                UpdateInformation = "Checking...";

                string latestVersion;
                using (var updateManager = new UpdateManager(releasePath, applicationName))
                {
                    void OnDo(string caller, Action<Version> doAction, Version v = null)
                    {
                        try
                        {
                            doAction(v);
                        }
                        catch (Exception e)
                        {
                            UpdateInformation = $"Error in {caller}: {e.Message}";
                        }
                    }

                    void OnAppUninstall(Version v)
                    {
                        OnDo(GetCaller(), v0 =>
                        {
                            updateManager.RemoveShortcutForThisExe();
                        }, v);
                    }

                    void OnInitialInstall(Version v)
                    {
                        OnDo(GetCaller(), v0 =>
                        {
                            updateManager.CreateShortcutForThisExe();
                        }, v);
                    }

                    void OnAppUpdate(Version v)
                    {
                        OnDo(GetCaller(), v0 =>
                        {
                            updateManager.CreateShortcutForThisExe();
                        }, v);
                    }

                    void OnAppObsoleted(Version v) => OnDo(GetCaller(), v0 =>
                    {
                    }, v);

                    void OnFirstRun() => OnDo(GetCaller(), v0 =>
                    {
                    });

                    SquirrelAwareApp.HandleEvents(
                        onAppUninstall: OnAppUninstall,
                        onInitialInstall: OnInitialInstall,
                        onAppUpdate: OnAppUpdate,
                        onAppObsoleted: OnAppObsoleted,
                        onFirstRun: OnFirstRun
                    );

                    updates = await updateManager.CheckForUpdate();

                    Version = updates.CurrentlyInstalledVersion == null ? "development" : updates.CurrentlyInstalledVersion.Version.ToString();

                    if (!updates.ReleasesToApply.Any())
                    {
                        UpdateInformation = "You are running the latest version.";
                        return;
                    }

                    latestVersion = updates.ReleasesToApply.OrderBy(x => x.Version).LastOrDefault()?.Version.ToString() ?? "Unknown";
                    UpdateInformation = $"Version: {latestVersion} available. Downloading...";

                    await updateManager.DownloadReleases(updates.ReleasesToApply);
                }

                UpdateAvailable = true;
                UpdateInformation = $"Version: {latestVersion} ready";
            }
            catch (Exception e)
            {
                UpdateInformation = $"Error while updating: {e.Message}";
            }
        }

        private UpdateInfo updates;


        private static string GetCaller([CallerMemberName] string caller = null)
        {
            return caller;
        }

        public RelayCommand ApplyUpdateCommand => new RelayCommand(async () =>
        {
            if (!UpdateAvailable || updates == null) return;

            UpdateAvailable = false;
            UpdateInformation = "Applying Update...";
            try
            {
                using (var updateManager = new UpdateManager(releasePath, applicationName))
                {
                    await updateManager.ApplyReleases(updates);
                    await updateManager.UpdateApp();
                }

                var latestVersion = updates.ReleasesToApply.OrderBy(x => x.Version).LastOrDefault();
                var currentVersion = latestVersion?.Version.ToString() ?? "unknown";

                var installedVersion = updates.CurrentlyInstalledVersion == null ? "development" : updates.CurrentlyInstalledVersion.Version.ToString();

                UpdateInformation = $"{applicationName} has been updated from {installedVersion} to {currentVersion}, rerun to finish update";
                UpdateManager.RestartApp();
            }
            finally
            {
                UpdateAvailable = false;
                updates = null;
            }

        }, () => UpdateAvailable);

        public void Dispose()
        {
            UpdateAvailable = false;
            updates = null;
        }
    }
}

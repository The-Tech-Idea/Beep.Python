using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Beep.Python.Model;

namespace Beep.Python.Winform.PackageManagement
{
    public partial class uc_Packages
    {
        private async void btnInstallSelected_Click(object sender, EventArgs e)
        {
            if (_isInstalling)
            {
                AppendLog("An installation is already in progress.");
                return;
            }

            if (_packageManager == null)
            {
                AppendLog("Package manager service is not available.");
                return;
            }

            if (checkedListPackages.CheckedItems.Count == 0)
            {
                AppendLog("Select at least one package to install.");
                return;
            }

            if (comboEnvironment.SelectedItem is not PythonVirtualEnvironment environment)
            {
                AppendLog("Select a virtual environment before installing packages.");
                return;
            }

            var packages = checkedListPackages.CheckedItems.Cast<object>()
                .Select(item => item?.ToString())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .ToList();

            if (packages.Count == 0)
            {
                AppendLog("No valid package names found in the selection.");
                return;
            }

            _isInstalling = true;
            _installCts = new CancellationTokenSource();
            progressInstall.Maximum = packages.Count;
            progressInstall.Value = 0;
            progressInstall.Style = ProgressBarStyle.Blocks;
            lblInstallStatus.Text = "Installing packages...";
            btnCancelInstall.Enabled = true;

            SetWorkingState(true);

            try
            {
                var sessionReady = await EnsureSessionAsync(environment, _installCts.Token);
                if (!sessionReady)
                {
                    AppendLog("Unable to configure a Python session for the selected environment.");
                    return;
                }

                var index = 0;
                foreach (var packageName in packages)
                {
                    _installCts.Token.ThrowIfCancellationRequested();

                    index++;
                    AppendLog($"Installing {packageName} ({index}/{packages.Count})...");
                    bool success;
                    try
                    {
                        success = await _packageManager.InstallNewPackageWithSessionAsync(packageName, _installCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        AppendLog("Installation cancelled.");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Error installing {packageName}: {ex.Message}");
                        continue;
                    }

                    progressInstall.Value = index;
                    lblInstallStatus.Text = $"Installed {index} of {packages.Count}";
                    AppendLog(success
                        ? "[OK] Installed " + packageName
                        : "[FAIL] Unable to install " + packageName);
                }

                AppendLog("Package installation completed.");
                lblInstallStatus.Text = "Installation complete.";
            }
            catch (OperationCanceledException)
            {
                AppendLog("Package installation cancelled.");
                lblInstallStatus.Text = "Installation cancelled.";
            }
            catch (Exception ex)
            {
                AppendLog($"Package installation failed: {ex.Message}");
                lblInstallStatus.Text = "Installation failed.";
            }
            finally
            {
                SetWorkingState(false);
                ResetInstallState();
            }
        }

        private async Task<bool> EnsureSessionAsync(PythonVirtualEnvironment environment, CancellationToken cancellationToken)
        {
            if (_packageManager == null)
            {
                return false;
            }

            var configuredEnv = _packageManager.GetConfiguredVirtualEnvironment();
            if (configuredEnv != null && string.Equals(configuredEnv.ID, environment.ID, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            AppendLog($"Configuring session for environment \"{environment.Name}\"...");

            return await Task.Run(() =>
            {
                lock (_sessionSync)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return _packageManager.ConfigureSessionForUser(System.Environment.UserName, environment.ID);
                }
            }, cancellationToken);
        }

        private void btnCancelInstall_Click(object sender, EventArgs e)
        {
            if (!_isInstalling || _installCts is null)
            {
                return;
            }

            AppendLog("Cancelling installation...");
            _installCts.Cancel();
        }

        private void ResetInstallState()
        {
            _installCts?.Dispose();
            _installCts = null;
            _isInstalling = false;
            btnCancelInstall.Enabled = false;
            progressInstall.Value = 0;
            lblInstallStatus.Text = string.Empty;
        }

        private void SetWorkingState(bool isWorking)
        {
            Cursor = isWorking ? Cursors.WaitCursor : Cursors.Default;
            comboPackageSet.Enabled = !isWorking;
            comboEnvironment.Enabled = !isWorking;
            btnInstallSelected.Enabled = !isWorking;
            btnSelectAll.Enabled = !isWorking;
            btnClearSelection.Enabled = !isWorking;
            btnRefreshEnvironments.Enabled = !isWorking;
            checkedListPackages.Enabled = !isWorking;



            btnCancelInstall.Enabled = _isInstalling;
        }

        private void AppendLog(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), message);
                return;
            }

            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            lstLog.Items.Add(line);
            lstLog.TopIndex = Math.Max(0, lstLog.Items.Count - 1);
        }
    }
}

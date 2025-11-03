using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Beep.Python.Model;
using Beep.Python.Services;

namespace Beep.Python.Winform.PackageManagement
{
    public partial class uc_Packages
    {
        private void InitializeServices()
        {
            try
            {
                _packageManager = PythonServices.GetPythonPackageManager();
            }
            catch (Exception ex)
            {
                AppendLog($"Unable to resolve package manager: {ex.Message}");
            }

            try
            {
                _virtualEnvManager = PythonServices.GetPythonVirtualEnv();
            }
            catch (Exception ex)
            {
                AppendLog($"Unable to resolve virtual environment manager: {ex.Message}");
            }

        }

        private void LoadPackageSets()
        {
            _packageSetViewModels.Clear();
            comboPackageSet.DataSource = null;
            comboPackageSet.Items.Clear();
            txtSetDescription.Clear();
            checkedListPackages.Items.Clear();

            Dictionary<string, List<PackageDefinition>>? availableSets = null;

            if (_packageManager != null)
            {
                try
                {
                    availableSets = _packageManager.GetAvailablePackageSets();
                }
                catch (Exception ex)
                {
                    AppendLog($"Failed to load package sets from manager: {ex.Message}");
                }
            }

            if (availableSets == null || availableSets.Count == 0)
            {
                availableSets = BuildDefaultPackageSets();
            }

            foreach (var kvp in availableSets.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var metadata = ResolveMetadata(kvp.Key);
                _packageSetViewModels.Add(new PackageSetViewModel(kvp.Key, metadata.Name, metadata.Description, kvp.Value));
            }

            comboPackageSet.DisplayMember = nameof(PackageSetViewModel.DisplayName);
            comboPackageSet.ValueMember = nameof(PackageSetViewModel.Key);
            comboPackageSet.DataSource = _packageSetViewModels;

            lblSelectedCount.Visible = _packageSetViewModels.Count > 0;

            if (_packageSetViewModels.Count > 0)
            {
                comboPackageSet.SelectedIndex = 0;
            }
            else
            {
                AppendLog("No package sets available. Verify package configuration.");
            }


        }

        private Dictionary<string, List<PackageDefinition>> BuildDefaultPackageSets()
        {
            var result = new Dictionary<string, List<PackageDefinition>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in DefaultPackageSeeds)
            {
                var packages = kvp.Value
                    .Select(name => new PackageDefinition
                    {
                        PackageName = name,
                        Status = PackageStatus.Available
                    })
                    .ToList();
                result[kvp.Key] = packages;
            }

            return result;
        }

        private (string Name, string Description) ResolveMetadata(string key)
        {
            if (PackageSetMetadata.TryGetValue(key, out var metadata))
            {
                return metadata;
            }

            var title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(key.Replace('_', ' '));
            return (title, $"Package set \"{title}\".");
        }

        private void PopulatePackageList(PackageSetViewModel selectedSet)
        {
            checkedListPackages.BeginUpdate();
            try
            {
                checkedListPackages.Items.Clear();

                foreach (var package in selectedSet.Packages)
                {
                    if (string.IsNullOrWhiteSpace(package?.PackageName))
                    {
                        continue;
                    }

                    var index = checkedListPackages.Items.Add(package.PackageName);
                    checkedListPackages.SetItemChecked(index, true);
                }
            }
            finally
            {
                checkedListPackages.EndUpdate();
            }

            UpdateSelectionCount();
        }

        private void LoadEnvironments()
        {
            comboEnvironment.DataSource = null;
            comboEnvironment.Items.Clear();

            if (_virtualEnvManager?.ManagedVirtualEnvironments is not { } managed || managed.Count == 0)
            {
                AppendLog("No managed virtual environments detected. Configure environments in the runtime manager.");
                return;
            }

            var environments = managed.OrderBy(env => env.Name, StringComparer.OrdinalIgnoreCase).ToList();
            comboEnvironment.DisplayMember = nameof(PythonVirtualEnvironment.Name);
            comboEnvironment.ValueMember = nameof(PythonVirtualEnvironment.ID);
            comboEnvironment.DataSource = environments;

            if (environments.Count > 0)
            {
                comboEnvironment.SelectedIndex = 0;
            }
        }

        private void UpdateSelectionCount()
        {
            var total = checkedListPackages.Items.Count;
            var selected = checkedListPackages.CheckedItems.Count;
            lblSelectedCount.Text = $"Selected: {selected}/{total}";
        }

        private void comboPackageSet_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (comboPackageSet.SelectedItem is not PackageSetViewModel selected)
            {
                return;
            }

            txtSetDescription.Text = selected.Description;
            PopulatePackageList(selected);
        }

        private void checkedListPackages_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            BeginInvoke(new Action(UpdateSelectionCount));
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            checkedListPackages.BeginUpdate();
            for (int i = 0; i < checkedListPackages.Items.Count; i++)
            {
                checkedListPackages.SetItemChecked(i, true);
            }
            checkedListPackages.EndUpdate();
            UpdateSelectionCount();
        }

        private void btnClearSelection_Click(object sender, EventArgs e)
        {
            checkedListPackages.BeginUpdate();
            for (int i = 0; i < checkedListPackages.Items.Count; i++)
            {
                checkedListPackages.SetItemChecked(i, false);
            }
            checkedListPackages.EndUpdate();
            UpdateSelectionCount();
        }

        private void btnRefreshEnvironments_Click(object sender, EventArgs e)
        {
            LoadEnvironments();
        }
    }
}

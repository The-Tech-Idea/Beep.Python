using Beep.Python.Model;
using Beep.Python.RuntimeEngine;
using DataManagementModels.Editor;
using Newtonsoft.Json.Linq;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.Logic.ViewModels
{
    public class PackageManagerViewModel: PythonBaseViewModel
    {
        public ObservableBindingList<PackageDefinition> Packages => unitofWork.Units;
        public UnitofWork<PackageDefinition> unitofWork {  get; set; }
        IDMEEditor Editor;

        public PackageManagerViewModel() : base()
        {

        }
        public PackageManagerViewModel(PythonNetRunTimeManager pythonRuntimeManager, PyModule persistentScope) : base(pythonRuntimeManager, persistentScope)
        {
            _pythonRuntimeManager = pythonRuntimeManager;
            _persistentScope = persistentScope;
            Init();

        }
        public PackageManagerViewModel(PythonNetRunTimeManager pythonRuntimeManager) : base(pythonRuntimeManager)
        {
            _pythonRuntimeManager = pythonRuntimeManager;
            Init();
            InitializePythonEnvironment();
        }
        public void Init()
        {
            Editor = _pythonRuntimeManager.DMEditor;
            unitofWork = new UnitofWork<PackageDefinition>(Editor, true, new ObservableBindingList<PackageDefinition>(_pythonRuntimeManager.CurrentRuntimeConfig.Packagelist), "ID");
        }
        public async Task<bool> InstallPipToolAsync()
        {
            bool retval = false;
            if (!_pythonRuntimeManager.IsInitialized)

                return retval;
            if (_pythonRuntimeManager.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }

            await _pythonRuntimeManager.InstallPIP(Progress, Token).ConfigureAwait(true);


            _pythonRuntimeManager.IsBusy = false;
            return retval;
        }
        public async Task<bool> InstallNewPackageAsync(string packagename)
        {
            bool retval=false;
            if (!_pythonRuntimeManager.IsInitialized)

                return retval;
            if (_pythonRuntimeManager.IsBusy)
            {
              //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            if (!string.IsNullOrEmpty(packagename))
            {
               retval = await _pythonRuntimeManager.InstallPackage(packagename, Progress, Token).ConfigureAwait(true);
            }
           
            return retval;
        }
        public async Task<bool> UnInstallPackageAsync(string packagename)
        {
            bool retval = false;
            if (!_pythonRuntimeManager.IsInitialized)

                return retval;
            if (_pythonRuntimeManager.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            if (!string.IsNullOrEmpty(packagename))
            {
                retval = await _pythonRuntimeManager.RemovePackage(packagename, Progress, Token).ConfigureAwait(true);
            }

            return retval;
        }
        public async Task<bool> UpgradePackageAsync(string packagename)
        {
            bool retval = false;
            if (!_pythonRuntimeManager.IsInitialized)

                return retval;
            if (_pythonRuntimeManager.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            if (!string.IsNullOrEmpty(packagename))
            {
                retval = await _pythonRuntimeManager.UpdatePackage(packagename, Progress, Token).ConfigureAwait(true);
            }

            return retval;
        }
        public async Task<bool> UpgradeAllPackagesAsync()
        {
            bool retval = false;
            if (!_pythonRuntimeManager.IsInitialized)

                return retval;
            if (_pythonRuntimeManager.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            retval = await _pythonRuntimeManager.RefreshInstalledPackagesList(Progress, Token).ConfigureAwait(true);
            return retval;
        }
        public async Task<bool> RefreshPackageAsync(string packagename)
        {
            bool retval = false;
            if (!_pythonRuntimeManager.IsInitialized)

                return retval;
            if (_pythonRuntimeManager.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            if (!string.IsNullOrEmpty(packagename))
            {
                retval = await _pythonRuntimeManager.RefreshInstalledPackage(packagename, Progress, Token).ConfigureAwait(true);
            }

            return retval;
        }
        public async Task<bool> RefreshAllPackagesAsync()
        {
            bool retval = false;
            if (!_pythonRuntimeManager.IsInitialized)

                return retval;
            if (_pythonRuntimeManager.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            if (_pythonRuntimeManager != null)
            {
               await _pythonRuntimeManager.RefreshInstalledPackagesList(Progress, Token).ConfigureAwait(true);
               _pythonRuntimeManager.SaveConfig();
            }
            return retval;
        }
    }
}

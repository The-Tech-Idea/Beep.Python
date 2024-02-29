using Beep.Python.Model;
using DataManagementModels.Editor;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine.ViewModels
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

        public void InstallPipTool()
        {
            if (!_pythonRuntimeManager.IsInitialized)

                return;
            if (_pythonRuntimeManager.IsBusy)
            {
               // MessageBox.Show("Please wait until the current operation is finished");
                return;
            }

            _pythonRuntimeManager.InstallPIP(Progress, Token).ConfigureAwait(true);


            _pythonRuntimeManager.IsBusy = false;
        }

        public void RefreshPackages()
        {
            if (!_pythonRuntimeManager.IsInitialized)

                return;
            if (_pythonRuntimeManager.IsBusy)
            {
               // MessageBox.Show("Please wait until the current operation is finished");
                return;
            }
          //  refersh();
        }

        public void InstallNewPackage(string packagename)
        {
            if (!_pythonRuntimeManager.IsInitialized)

                return;
            if (_pythonRuntimeManager.IsBusy)
            {
              //  MessageBox.Show("Please wait until the current operation is finished");
                return;
            }
            if (!string.IsNullOrEmpty(packagename))
            {
                var retval = _pythonRuntimeManager.InstallPackage(packagename, Progress, Token).ConfigureAwait(true);
            }

        }
    }
}

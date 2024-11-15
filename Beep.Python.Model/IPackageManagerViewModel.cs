using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.Editor;

namespace Beep.Python.Model
{
    public interface IPackageManagerViewModel:IDisposable
    {
        ObservableBindingList<PackageDefinition> Packages { get; }
        UnitofWork<PackageDefinition> unitofWork { get; set; }
        IDMEEditor Editor { get; set; }

        void Init();
        bool InstallNewPackageAsync(string packagename);
        bool InstallPipToolAsync();
        bool RefreshAllPackagesAsync();
        bool RefreshPackageAsync(string packagename);
        bool UnInstallPackageAsync(string packagename);
        bool UpgradeAllPackagesAsync();
        bool UpgradePackageAsync(string packagename);
       
    }
}
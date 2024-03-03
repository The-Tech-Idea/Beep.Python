using Beep.Python.Model;
using DataManagementModels.Editor;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.Model
{
    public interface IPackageManagerViewModel:IDisposable
    {
        ObservableBindingList<PackageDefinition> Packages { get; }
        UnitofWork<PackageDefinition> unitofWork { get; set; }
        IDMEEditor Editor { get; set; }

        void Init();
        Task<bool> InstallNewPackageAsync(string packagename);
        Task<bool> InstallPipToolAsync();
        Task<bool> RefreshAllPackagesAsync();
        Task<bool> RefreshPackageAsync(string packagename);
        Task<bool> UnInstallPackageAsync(string packagename);
        Task<bool> UpgradeAllPackagesAsync();
        Task<bool> UpgradePackageAsync(string packagename);
       
    }
}
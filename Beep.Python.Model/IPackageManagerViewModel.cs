using TheTechIdea.Beep.Editor;


namespace Beep.Python.Model
{
    public interface IPackageManagerViewModel:IDisposable
    {
        ObservableBindingList<PackageDefinition> Packages { get; }
        UnitofWork<PackageDefinition> UnitofWork { get; set; }
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
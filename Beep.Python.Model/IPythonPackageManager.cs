using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.Model
{
    public interface IPythonPackageManager : IDisposable
    {
   
        UnitofWork<PackageDefinition> UnitofWork { get; set; }

        // Existing methods
        bool InstallNewPackageAsync(string packagename);
        bool InstallPipToolAsync();
        bool RefreshAllPackagesAsync();
        bool RefreshPackageAsync(string packagename);
        bool UnInstallPackageAsync(string packagename);
        bool UpgradeAllPackagesAsync();
        bool UpgradePackageAsync(string packagename);

        // New methods for requirements file support
        bool InstallFromRequirementsFile(string filePath);
        bool GenerateRequirementsFile(string filePath, bool includeVersions = true);
        bool UpdateVirtualEnvironmentWithRequirementsFile(PythonVirtualEnvironment environment);

        // New methods for categorized package management
        ObservableBindingList<PackageDefinition> GetPackagesByCategory(PackageCategory category);
        void SetPackageCategory(string packageName, PackageCategory category);
        void UpdatePackageCategories(Dictionary<string, PackageCategory> packageCategories);

        // Method to populate common package categories
        bool PopulateCommonPackageCategories();

        // Method to suggest categories for uncategorized packages
        Task<Dictionary<string, PackageCategory>> SuggestCategoriesForPackages(IEnumerable<string> packageNames);

        // Methods to work with predefined package sets
        bool InstallPackageSet(string setName);
        Dictionary<string, List<string>> GetAvailablePackageSets();
        bool SavePackageSetFromCurrentEnvironment(string setName, string description = "");
    }
}

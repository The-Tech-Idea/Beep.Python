using System.Text;
 

namespace Beep.Python.Model
{
    public class PackageDefinition : Entity
    {
      
        private string _IDValue = Guid.NewGuid().ToString();

        public string ID
        {
            get
            {
                return this._IDValue;
            }

            set
            {
                SetProperty(ref _IDValue, value);
            }
        }
        private string _packagetitleValue;

        public string PackageTitle
        {
            get
            {
                return this._packagetitleValue;
            }

            set
            {
                SetProperty(ref _packagetitleValue, value);
            }
        }

        private string _packagenameValue;

        public string PackageName
        {
            get
            {
                return this._packagenameValue;
            }

            set
            {
                SetProperty(ref _packagenameValue, value);
            }
        }

        private string _installpathValue;

        public string Installpath
        {
            get
            {
                return this._installpathValue;
            }

            set
            {
                SetProperty(ref _installpathValue, value);
            }
        }

        private string _sourcepathValue;

        public string Sourcepath
        {
            get
            {
                return this._sourcepathValue;
            }

            set
            {
                SetProperty(ref _sourcepathValue, value);
            }
        }
        private PackageStatus _installedValue;

        public PackageStatus Status
        {
            get
            {
                return this._installedValue;
            }

            set
            {
                SetProperty(ref _installedValue, value);
            }
        }
        private PackageCategory _categoryValue;

        public PackageCategory Category
        {
            get
            {
                return this._categoryValue;
            }

            set
            {
                SetProperty(ref _categoryValue, value);
            }
        }
        private string _versionValue;

        public string Version
        {
            get
            {
                return this._versionValue;
            }

            set
            {
                SetProperty(ref _versionValue, value);
            }
        }
        private string _versiondisplayValue;

        public string Versiondisplay
        {
            get
            {
                return this._versiondisplayValue;
            }

            set
            {
                SetProperty(ref _versiondisplayValue, value);
            }
        }
        private string _updateversionValue;

        public string Updateversion
        {
            get
            {
                return this._updateversionValue;
            }

            set
            {
                SetProperty(ref _updateversionValue, value);
            }
        }
        private string _image;
        public string Image
        {
            get { return _image; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Image path cannot be null or empty.");
                }
                _image = value;
            }
        }

        private string _descriptionValue;

        public string Description
        {
            get
            {
                return this._descriptionValue;
            }

            set
            {
                SetProperty(ref _descriptionValue, value);
            }
        }
        private string _dbuttondisplayValue;

        public string Buttondisplay
        {
            get
            {
                return this._dbuttondisplayValue;
            }

            set
            {
                SetProperty(ref _dbuttondisplayValue, value);
            }
        }
        private bool _isupdatableValue => !string.IsNullOrEmpty(Updateversion) && Updateversion != Version;
        public bool IsUpdatable
        {
            get
            {
                return _isupdatableValue;
            }
          
        }

    }
    /// <summary>
    /// Result class for package operations to provide detailed feedback
    /// </summary>
    public class PackageOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string PackageName { get; set; }
        public string CommandExecuted { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public PackageOperationResult(bool success, string message, string packageName = null)
        {
            Success = success;
            Message = message;
            PackageName = packageName;
        }
    }
    public class packageCategoryImages
    {
        public packageCategoryImages()
        {

        }
       
        public string image { get; set; }
     
        public string category { get; set; }
    }
    /// <summary>
    /// Represents a collection of packages designed for a specific purpose.
    /// </summary>
    public class PackageSet : Entity
    {
        private string _id = Guid.NewGuid().ToString();
        /// <summary>
        /// Gets or sets the unique identifier for this package set.
        /// </summary>
        public string ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _name;
        /// <summary>
        /// Gets or sets the display name of the package set.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private string _description;
        /// <summary>
        /// Gets or sets the description of what this package set is for.
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }

        private PackageCategory _category = PackageCategory.Uncategorized;
        /// <summary>
        /// Gets or sets the primary category for this package set.
        /// </summary>
        public PackageCategory Category
        {
            get { return _category; }
            set { SetProperty(ref _category, value); }
        }
        public string Image { get; set; }
        private List<PackageDefinition> _packages = new();
        /// <summary>
        /// Gets or sets the list of package names included in this set.
        /// </summary>
        public List<PackageDefinition> Packages
        {
            get { return _packages; }
            set { SetProperty(ref _packages, value); }
        }

        private Dictionary<string, string> _versions = new();
        /// <summary>
        /// Gets or sets the version constraints for packages in this set.
        /// </summary>
        public Dictionary<string, string> Versions
        {
            get { return _versions; }
            set { SetProperty(ref _versions, value); }
        }

        /// <summary>
        /// Creates a requirements file content string from this package set.
        /// </summary>
        /// <param name="includeVersions">Whether to include version constraints.</param>
        /// <returns>Content for a requirements.txt file.</returns>
        public string ToRequirementsText(bool includeVersions = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Package set: {Name}");
            sb.AppendLine($"# Description: {Description}");
            sb.AppendLine($"# Generated: {DateTime.Now}");
            sb.AppendLine();

            foreach (var pkg in Packages)
            {
                if (includeVersions && !string.IsNullOrWhiteSpace(pkg.Version))
                {
                    sb.AppendLine($"{pkg.PackageName}{pkg.Version}");
                }
                else
                {
                    sb.AppendLine(pkg.PackageName);
                }
            }

            return sb.ToString();
        }

    }



}

using TheTechIdea.Beep.Editor;

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
    public enum PackageCategory
    {
        UI,
        Utilities,
        Development,
        Graphics,
        DataScience,
        Compute,
        Other
    }

   

}

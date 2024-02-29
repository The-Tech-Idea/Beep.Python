using TheTechIdea.Beep.Editor;

namespace Beep.Python.Model
{
    public class PackageDefinition : Entity
    {
        public PackageDefinition()
        {
            _IDValue=Guid.NewGuid().ToString();
        }
        private string _IDValue;

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

        public string packagetitle
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

        public string packagename
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

        public string installpath
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

        public string sourcepath
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
        private bool _installedValue;

        public bool installed
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
        private string _categoryValue;

        public string category
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

        public string version
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

        public string versiondisplay
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

        public string updateversion
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
        private string _descriptionValue;

        public string description
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

        public string buttondisplay
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
      
    }
    public class packageCategoryImages
    {
        public packageCategoryImages()
        {

        }
       
        public string image { get; set; }
     
        public string category { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.Model
{
    public partial class PythonProject: Entity
    {
        public PythonProject()
        {
            _ProjectGuidValue= Guid.NewGuid().ToString();
            _PythonAlgorithmParamsValue=new List<PythonalgorithmParams>();
            _PythonDataPipeLineValue=new List<PythonDataPipeLine>();
            _ListofFeaturesValue=new List<LOVData>();
        }
        // create Splitratio property
        private float _SplitratioValue;
        public float Splitratio
        {
            get
            {
                return this._SplitratioValue;
            }

            set
            {
                SetProperty(ref _SplitratioValue, value);
            }
        }
        // create DataSourceName property
        private string _DataSourceNameValue;
        public string DataSourceName
        {
            get
            {
                return this._DataSourceNameValue;
            }

            set
            {
                SetProperty(ref _DataSourceNameValue, value);
            }
        }
        //create EntityName property
        private string _EntityNameValue;
        public string EntityName
        {
            get
            {
                return this._EntityNameValue;
            }

            set
            {
                SetProperty(ref _EntityNameValue, value);
            }
        }
        // create DataFile property
        private string _DataFileValue;
        public string DataFile
        {
            get
            {
                return this._DataFileValue;
            }

            set
            {
                SetProperty(ref _DataFileValue, value);
            }
        }
        // create TestDataFile property
        private string _TestDataFileValue;
        public string TestDataFile
        {
            get
            {
                return this._TestDataFileValue;
            }

            set
            {
                SetProperty(ref _TestDataFileValue, value);
            }
        }
        // create TrainDataFile property
        private string _TrainDataFileValue;
        public string TrainDataFile
        {
            get
            {
                return this._TrainDataFileValue;
            }

            set
            {
                SetProperty(ref _TrainDataFileValue, value);
            }
        }
        //create  key property
        private string _Key;
        public string Key
        {
            get
            {
                return this._Key;
            }

            set
            {
                SetProperty(ref _Key, value);
            }
        }
        //create label property
        private string _LabelValue;
        public string Label
        {
            get
            {
                return this._LabelValue;
            }

            set
            {
                SetProperty(ref _LabelValue, value);
            }
        }
        // create ListofFeatures property
        private List<LOVData> _ListofFeaturesValue;
        public List<LOVData> Features
        {
            get
            {
                return this._ListofFeaturesValue;
            }

            set
            {
                SetProperty(ref _ListofFeaturesValue, value);
            }
        }
        //create FeaturesArray property
        private string[] _FeaturesArrayValue;
        public string[] FeaturesArray
        {
            get
            {
                return this._FeaturesArrayValue;
            }

            set
            {
                SetProperty(ref _FeaturesArrayValue, value);
            }
        }
        
            
        //create Title property
        private string _TitleValue;
        public string Title
        {
            get
            {
                return this._TitleValue;
            }

            set
            {
                SetProperty(ref _TitleValue, value);
            }
        }
        //create Description property
        private string _DescriptionValue;
        public string Description
        {
            get
            {
                return this._DescriptionValue;
            }

            set
            {
                SetProperty(ref _DescriptionValue, value);
            }
        }
        // create algorithm property
        private string _AlgorithmValue;
        public string Algorithm
        {
            get
            {
                return this._AlgorithmValue;
            }

            set
            {
                SetProperty(ref _AlgorithmValue, value);
            }
        }
        // create list<pythonalgorithmparams> property
        private List<PythonalgorithmParams> _PythonAlgorithmParamsValue;
        public List<PythonalgorithmParams> PythonAlgorithmParams
        {
            get
            {
                return this._PythonAlgorithmParamsValue;
            }

            set
            {
                SetProperty(ref _PythonAlgorithmParamsValue, value);
            }
        }
        // create List<PythonDataPipeLine> property
        private List<PythonDataPipeLine> _PythonDataPipeLineValue;
        public List<PythonDataPipeLine> PythonDataPipeLine
        {
            get
            {
                return this._PythonDataPipeLineValue;
            }

            set
            {
                SetProperty(ref _PythonDataPipeLineValue, value);
            }
        }
        // create pythondataclasses property
        private PythonDataClasses _PythonDataClassesValue;
        public PythonDataClasses PythonDataClasses
        {
            get
            {
                return this._PythonDataClassesValue;
            }

            set
            {
                SetProperty(ref _PythonDataClassesValue, value);
            }
        }
        // create guid property
        private string _ProjectGuidValue;
        public string ProjectGuidValue
        {
            get
            {
                return this._ProjectGuidValue;
            }

            set
            {
                SetProperty(ref _ProjectGuidValue, value);
            }
        }
        private double _IDValue;

        public double ID
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

        private System.String _ProjectNameValue;

        public System.String ProjectName
        {
            get
            {
                return this._ProjectNameValue;
            }

            set
            {
                SetProperty(ref _ProjectNameValue, value);
            }
        }

        private System.String _ProjectPathValue;

        public System.String ProjectPath
        {
            get
            {
                return this._ProjectPathValue;
            }

            set
            {
                SetProperty(ref _ProjectPathValue, value);
            }
        }

        private System.String _ProjectTypeValue;

        public System.String ProjectType
        {
            get
            {
                return this._ProjectTypeValue;
            }

            set
            {
                SetProperty(ref _ProjectTypeValue, value);
            }
        }

        private System.String _ProjectDescriptionValue;

        public System.String ProjectDescription
        {
            get
            {
                return this._ProjectDescriptionValue;
            }

            set
            {
                SetProperty(ref _ProjectDescriptionValue, value);
            }
        }

        private System.String _ProjectStatusValue;

        public System.String ProjectStatus
        {
            get
            {
                return this._ProjectStatusValue;
            }

            set
            {
                SetProperty(ref _ProjectStatusValue, value);
            }
        }

        private System.String _ProjectOwnerValue;

        public System.String ProjectOwner
        {
            get
            {
                return this._ProjectOwnerValue;
            }

            set
            {
                SetProperty(ref _ProjectOwnerValue, value);
            }
        }

        private System.String _ProjectOwnerEmailValue;

        public System.String ProjectOwnerEmail
        {
            get
            {
                return this._ProjectOwnerEmailValue;
            }

            set
            {
                SetProperty(ref _ProjectOwnerEmailValue, value);
            }
        }

        private System.String _ProjectOwnerPhoneValue;

        public System.String ProjectOwnerPhone
        {
            get
            {
                return this._ProjectOwnerPhoneValue;
            }

            set
            {
                SetProperty(ref _ProjectOwnerPhoneValue, value);
            }
        }

    }
}

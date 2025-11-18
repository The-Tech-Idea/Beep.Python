 
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace  Beep.Python.Model 
{ 
public class PythonDataClasses :  Entity 
{ 
public  PythonDataClasses (){}

 private  double   _IDValue ;

 public  double  ID
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

 private System.String  _NAMEValue ;

 public System.String NAME
    {
        get
        {
            return this._NAMEValue;
        }

        set
        {
       SetProperty(ref _NAMEValue, value);
    }
    }

 private System.String  _URLPATHValue ;

 public System.String URLPATH
    {
        get
        {
            return this._URLPATHValue;
        }

        set
        {
       SetProperty(ref _URLPATHValue, value);
    }
    }

 private System.String  _FILENAMEValue ;

 public System.String FILENAME
    {
        get
        {
            return this._FILENAMEValue;
        }

        set
        {
       SetProperty(ref _FILENAMEValue, value);
    }
    }

 private System.String  _TESTDATAFILENAMEValue ;

 public System.String TESTDATAFILENAME
    {
        get
        {
            return this._TESTDATAFILENAMEValue;
        }

        set
        {
       SetProperty(ref _TESTDATAFILENAMEValue, value);
    }
    }

 private System.String  _TRAININGFILENAMEValue ;

 public System.String TRAININGFILENAME
    {
        get
        {
            return this._TRAININGFILENAMEValue;
        }

        set
        {
       SetProperty(ref _TRAININGFILENAMEValue, value);
    }
    }

 private System.String  _COMPITIONNAMEValue ;

 public System.String COMPITIONNAME
    {
        get
        {
            return this._COMPITIONNAMEValue;
        }

        set
        {
       SetProperty(ref _COMPITIONNAMEValue, value);
    }
    }

 private System.String  _DESCRIPTIONValue ;

 public System.String DESCRIPTION
    {
        get
        {
            return this._DESCRIPTIONValue;
        }

        set
        {
       SetProperty(ref _DESCRIPTIONValue, value);
    }
    }

 private System.String  _PRIMARYFIELDValue ;

 public System.String PRIMARYFIELD
    {
        get
        {
            return this._PRIMARYFIELDValue;
        }

        set
        {
       SetProperty(ref _PRIMARYFIELDValue, value);
    }
    }

 private System.String  _LABELFIELDValue ;

 public System.String LABELFIELD
    {
        get
        {
            return this._LABELFIELDValue;
        }

        set
        {
       SetProperty(ref _LABELFIELDValue, value);
    }
    }

 private System.String  _ICONNAMEValue ;

 public System.String ICONNAME
    {
        get
        {
            return this._ICONNAMEValue;
        }

        set
        {
       SetProperty(ref _ICONNAMEValue, value);
    }
    }

 private System.String  _VALIDATIONDATAFILENAMEValue ;

 public System.String VALIDATIONDATAFILENAME
    {
        get
        {
            return this._VALIDATIONDATAFILENAMEValue;
        }

        set
        {
       SetProperty(ref _VALIDATIONDATAFILENAMEValue, value);
    }
    }

        //FEATURES

        private System.String _FEATURESValue;

        public System.String FEATURES
        {
            get
            {
                return this._FEATURESValue;
            }

            set
            {
                SetProperty(ref _FEATURESValue, value);
            }
        }
    }


} 


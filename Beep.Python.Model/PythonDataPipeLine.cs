using TheTechIdea.Beep.Editor;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace  Beep.Python.Model 
{ 
public class PythonDataPipeLine :  Entity 
{ 
public  PythonDataPipeLine (){}

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

 private System.String  _STEPNAMEValue ;

 public System.String STEPNAME
    {
        get
        {
            return this._STEPNAMEValue;
        }

        set
        {
       SetProperty(ref _STEPNAMEValue, value);
    }
    }

 private System.String  _OUTPUTFILENAMEValue ;

 public System.String OUTPUTFILENAME
    {
        get
        {
            return this._OUTPUTFILENAMEValue;
        }

        set
        {
       SetProperty(ref _OUTPUTFILENAMEValue, value);
    }
    }

 private System.String  _GUIDIDValue ;

 public System.String GUIDID
    {
        get
        {
            return this._GUIDIDValue;
        }

        set
        {
       SetProperty(ref _GUIDIDValue, value);
    }
    }

 private  double   _DATACLASS_IDValue ;

 public  double  DATACLASS_ID
    {
        get
        {
            return this._DATACLASS_IDValue;
        }

        set
        {
       SetProperty(ref _DATACLASS_IDValue, value);
    }
    }

 private System.String  _MODEL_IDValue ;

 public System.String MODEL_ID
    {
        get
        {
            return this._MODEL_IDValue;
        }

        set
        {
       SetProperty(ref _MODEL_IDValue, value);
    }
    }

 private System.String  _OUTPUTDIRValue ;

 public System.String OUTPUTDIR
    {
        get
        {
            return this._OUTPUTDIRValue;
        }

        set
        {
       SetProperty(ref _OUTPUTDIRValue, value);
    }
    }

 private System.DateTime  _ROW_CREATE_DATEValue ;

 public System.DateTime ROW_CREATE_DATE
    {
        get
        {
            return this._ROW_CREATE_DATEValue;
        }

        set
        {
       SetProperty(ref _ROW_CREATE_DATEValue, value);
    }
    }

 private System.String  _ROW_CREATE_BYValue ;

 public System.String ROW_CREATE_BY
    {
        get
        {
            return this._ROW_CREATE_BYValue;
        }

        set
        {
       SetProperty(ref _ROW_CREATE_BYValue, value);
    }
    }
} 


} 


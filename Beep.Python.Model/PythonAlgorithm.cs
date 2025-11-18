 
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace  Beep.Python.Model 
{ 
public class PythonAlgorithm :  Entity 
{ 
public  PythonAlgorithm (){}

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
        ////Score

        //private double _SCOREValue;

        //public double SCORE
        //{
        //    get
        //    {
        //        return this._SCOREValue;
        //    }

        //    set
        //    {
        //        SetProperty(ref _SCOREValue, value);
        //    }
        //}

        private System.String  _ALGORITHIMValue ;

 public System.String ALGORITHM
    {
        get
        {
            return this._ALGORITHIMValue;
        }

        set
        {
       SetProperty(ref _ALGORITHIMValue, value);
    }
    }

 private System.String  _TRAINFILENAMEValue ;

 public System.String TRAINFILENAME
    {
        get
        {
            return this._TRAINFILENAMEValue;
        }

        set
        {
       SetProperty(ref _TRAINFILENAMEValue, value);
    }
    }

 private System.String  _TRAINFILEPATHValue ;

 public System.String TRAINFILEPATH
    {
        get
        {
            return this._TRAINFILEPATHValue;
        }

        set
        {
       SetProperty(ref _TRAINFILEPATHValue, value);
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
} 


} 


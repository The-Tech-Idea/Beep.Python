using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Beep.Python.Model
{
    public class CodeFileList
    {
        public CodeFileList()
        {

        }
        public List<CodeFile> CodeFiles { get; set; } = new List<CodeFile>();
        public int CurrentIDX { get; set; }
        public CodeFile CurrentFile
        {
            get
            {
                if (CurrentIDX >= 0)
                {
                    return CodeFiles[CurrentIDX];
                }
                else
                    return null;

            }
        }
    }
    public class CodeFile
    {
        public string Filename { get; set; }
        public string Path { get; set; }
        public string Extension { get; set; }
        public string Code { get; set; }
        public string CodeType { get; set; }
        public string CodeLanguage { get; set; }
        public string CodeDescription { get; set; }
        public string CodeCategory { get; set; }
        public string CodeSubCategory { get; set; }
     
    }
}

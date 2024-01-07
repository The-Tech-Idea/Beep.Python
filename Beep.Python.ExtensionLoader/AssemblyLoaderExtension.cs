using System.Reflection;
using TheTechIdea.Tools;
using TheTechIdea.Util;


namespace AssemblyLoaderExtension
{
    public class AssemblyLoaderExtension : ILoaderExtention
    {
        public AppDomain CurrentDomain { get; set; }
   
        public IAssemblyHandler Loader { get; set; }
        public AssemblyLoaderExtension(IAssemblyHandler ploader)
        {
            Loader = ploader;
          
          //  DMEEditor = 
            CurrentDomain = AppDomain.CurrentDomain;
         
            CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public IErrorsInfo LoadAllAssembly()
        {
            ErrorsInfo er = new ErrorsInfo();

            List<assemblies_rep> ls = Loader.Assemblies.Where(p => p.FileTypes == FolderFileTypes.ProjectClass).ToList();
            foreach (var item in ls)
            {
                    try
                    {
                        ScanAssembly(item.DllLib);
                    }
                    catch (Exception ex)
                    {

                        
                    }
                  
                }
               
          
            
            return er;
        }
        #region "Class Extractors"
        private bool ScanAssembly(Assembly asm)
        {
            Type[] t;

            try
            {
                try
                {
                    t = asm.GetTypes();
                }
                catch (Exception ex2)
                {
                    //DMEEditor.AddLogMessage("Failed", $"Could not get types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
                    try
                    {
                        //DMEEditor.AddLogMessage("Try", $"Trying to get exported types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Ok);
                        t = asm.GetExportedTypes();
                    }
                    catch (Exception ex3)
                    {
                        t = null;
                        //DMEEditor.AddLogMessage("Failed", $"Could not get types for {asm.GetName().ToString()}", DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
                    }

                }

                if (t != null)
                {
                    foreach (var mytype in t) //asm.DefinedTypes
                    {

                        TypeInfo type = mytype.GetTypeInfo();
                        string[] p = asm.FullName.Split(new char[] { ',' });
                        p[1] = p[1].Substring(p[1].IndexOf("=") + 1);
                        // Console.WriteLine(p[1]);

                    }
                }
            }
            catch (Exception ex)
            {
                //DMEEditor.AddLogMessage("Failed", $"Could not get Any types for {asm.GetName().ToString()}" , DateTime.Now, -1, asm.GetName().ToString(), Errors.Failed);
            };

            return true;


        }
        #endregion "Class Extractors"
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Ignore missing resources
            if (args.Name.Contains(".resources"))
                return null;
            string filename = args.Name.Split(',')[0] + ".dll".ToLower();
            string filenamewo = args.Name.Split(',')[0];
            // check for assemblies already loaded
            //   var s = AppDomain.CurrentDomain.GetAssemblies();
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.StartsWith(filenamewo));
            if (assembly == null)
            {
                assemblies_rep s = Loader.Assemblies.FirstOrDefault(a => a.DllLib.FullName.StartsWith(filenamewo));
                if (s != null)
                {
                    assembly = s.DllLib;
                }

            }
            if (assembly != null)
                return assembly;
            foreach (var moduleDir in Loader.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.OtherDLL))
            {
                var di = new DirectoryInfo(moduleDir.FolderPath);
                var module = di.GetFiles().FirstOrDefault(i => i.Name == filename);
                if (module != null)
                {
                    return Assembly.LoadFrom(module.FullName);
                }
            }
            if (assembly != null)
                return assembly;
            foreach (var moduleDir in Loader.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ConnectionDriver))
            {
                var di = new DirectoryInfo(moduleDir.FolderPath);
                var module = di.GetFiles().FirstOrDefault(i => i.Name == filename);
                if (module != null)
                {
                    return Assembly.LoadFrom(module.FullName);
                }
            }
            if (assembly != null)
                return assembly;
            foreach (var moduleDir in Loader.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass))
            {
                var di = new DirectoryInfo(moduleDir.FolderPath);
                var module = di.GetFiles().FirstOrDefault(i => i.Name == filename);
                if (module != null)
                {
                    return Assembly.LoadFrom(module.FullName);
                }
            }


            return null;

        }
        public IErrorsInfo Scan(assemblies_rep assembly)
        {
            ErrorsInfo er = new ErrorsInfo();
            try
            {

                ScanAssembly(assembly.DllLib);
                er.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;

            }
            return er;
        }
        public IErrorsInfo Scan(Assembly assembly)
        {
            ErrorsInfo er = new ErrorsInfo();
            try
            {

                ScanAssembly(assembly);
                er.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;

            }
            return er;
        }
         public IErrorsInfo Scan()
        {
            ErrorsInfo er = new ErrorsInfo();
            try
            {

                LoadAllAssembly();
                er.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;

            }
            return er;
        }
    }
}

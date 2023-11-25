namespace Beep.Python.Model
{
    public class PackageDefinition
    {
        public PackageDefinition()
        {

        }
        public string packagetitle { get; set; }
        public string packagename { get; set; }
        public string installpath { get; set; }
        public string sourcepath { get; set; }
        public bool installed { get; set; }
        public string category { get; set; }
        public string version { get; set; }
        public string versiondisplay { get; set; }
        public string updateversion { get; set; }
        public string description { get; set; }
        public string buttondisplay { get; set; }
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

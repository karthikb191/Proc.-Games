using System.Collections;
using System.Collections.Generic;

public class PackagesInfo {
    public List<string> defaultPackageList = new List<string>();
    public List<string> litePackageList = new List<string>();
    public List<string> exceedPackageList = new List<string>();

    

    public PackagesInfo()
    {
        //string[] defaultPackage = { "dc motor", "protein test", "microscope-leaf venations", "buildscene" };
        string[] defaultPackage = { "dc motor", "microscope-leaf venations", "protein test"};
        string[] litePackage = { "buildscene", "microscope-leaf venations"};
        string[] exceedPackage = { "buildscene", "microscope-leaf venations", "microscope-onion cells" };
        defaultPackageList.AddRange(defaultPackage);
        litePackageList.AddRange(litePackage);
        exceedPackageList.AddRange(exceedPackage);
    }
}

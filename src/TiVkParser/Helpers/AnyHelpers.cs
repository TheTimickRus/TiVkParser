using System.Reflection;

namespace TiVkParser.Helpers;

public static class AnyHelpers
{
    public static void ExtractResourceToFile(
        string nameSpace, 
        string internalFilePath, 
        string resourceName,
        string outDirectory 
    )
    {
        if (!Directory.Exists(outDirectory))
            Directory.CreateDirectory(outDirectory);
        
        var assembly = Assembly.GetCallingAssembly();
        using var s = assembly.GetManifestResourceStream(nameSpace + "." + (internalFilePath == "" ? "" : internalFilePath + ".") + resourceName);
        if (s == null) 
            return;
        
        using var r = new BinaryReader(s);
        using var fs = new FileStream(outDirectory + "\\" + resourceName, FileMode.OpenOrCreate);
        using var w = new BinaryWriter(fs);
        w.Write(r.ReadBytes((int)s.Length));
    }
}
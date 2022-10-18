using System.Reflection;

namespace TiVkParser.Helpers;

public static class AnyHelpers
{
    /// <summary>
    /// Метод, для извлечения вшитых ресурсов
    /// </summary>
    /// <param name="nameSpace">Пространство имен</param>
    /// <param name="resPath">Путь до ресурса (Разделитель - .)</param>
    /// <param name="resName">Название ресурса (с расширением)</param>
    /// <param name="outDirectory">Папка, в которую необходимо извдечь ресурсы (Без \ в конце)</param>
    /// <example>
    /// AnyHelpers.ExtractResourceToFile("TiVkParser", "Assets", "TiVkParser.toml", Environment.CurrentDirectory);
    /// </example>
    public static void ExtractResourceToFile(
        string nameSpace, 
        string resPath, 
        string resName,
        string outDirectory 
    )
    {
        // Если нет папки, в которую необходимо извлекать ресурсы - создаем
        if (Directory.Exists(outDirectory) is false)
            Directory.CreateDirectory(outDirectory);
        
        using var stream = Assembly
            .GetCallingAssembly()
            .GetManifestResourceStream(nameSpace + "." + (resPath is "" ? "" : resPath + ".") + resName);
        
        if (stream is null)
            throw new Exception("Ресурс не найден!");
        
        using var binaryReader = new BinaryReader(stream);
        using var fileStream = new FileStream(outDirectory + "\\" + resName, FileMode.OpenOrCreate);
        using var binaryWriter = new BinaryWriter(fileStream);
        binaryWriter.Write(binaryReader.ReadBytes((int)stream.Length));
    }
}
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using OfficeOpenXml;
using TiVkParser.Models.Exports;

namespace TiVkParser.Exports;

public static class ExportData
{
    public static string FileName { get; set; } = "TiVkParser";
    
    public static void ToExcel((IEnumerable<OutLike>?, IEnumerable<OutComment>?, IEnumerable<long>?) data)
    {
        var filename = $"{Path.GetFileNameWithoutExtension(FileName)}.xlsx";

        if (File.Exists(filename))
        {
            File.Copy(filename, $"{Path.GetFileNameWithoutExtension(filename)}_backup{new Random(Environment.TickCount).Next(1000, 9999)}.xlsx");
            File.Delete(filename);
        }
        
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var package = new ExcelPackage(new FileInfo(filename));

        if (data.Item1 != null)
        {
            var sheetLikes = package.Workbook.Worksheets.Add("Likes");
            sheetLikes.Cells["A1"].LoadFromCollection(data.Item1, true);
        }

        if (data.Item2 != null)
        {
            var sheetComments = package.Workbook.Worksheets.Add("Comments");
            sheetComments.Cells["A1"].LoadFromCollection(data.Item2, true);
        }

        if (data.Item3 != null)
        {
            var sheet = package.Workbook.Worksheets.Add("Friends");
            sheet.Cells["A1"].LoadFromCollection(data.Item3, true);
        }
        
        package.Save();
    }
}
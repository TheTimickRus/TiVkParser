// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using OfficeOpenXml;
using TiVkParser.Models.Exports;

namespace TiVkParser.Exports;

public static class ExportData
{
    public static string FileName { get; set; } = "TiVkParser";
    
    public static void ToExcel(ExportsDataModel data)
    {
        var filename = $"{Path.GetFileNameWithoutExtension(FileName)}.xlsx";

        if (File.Exists(filename))
        {
            File.Copy(filename, $"{Path.GetFileNameWithoutExtension(filename)}_backup{new Random(Environment.TickCount).Next(100000, 999999)}.xlsx");
            File.Delete(filename);
        }
        
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var package = new ExcelPackage(new FileInfo(filename));

        if (data.Likes != null)
        {
            var sheetLikes = package.Workbook.Worksheets.Add("Likes");
            sheetLikes.Cells["A1"].LoadFromCollection(data.Likes, true);
        }

        if (data.Comments != null)
        {
            var sheetComments = package.Workbook.Worksheets.Add("Comments");
            sheetComments.Cells["A1"].LoadFromCollection(data.Comments, true);
        }

        if (data.Friends != null)
        {
            var sheet = package.Workbook.Worksheets.Add("Friends");
            sheet.Cells["A1"].LoadFromCollection(data.Friends, true);
        }
        
        if (data.SearchByKeywords != null)
        {
            var sheet = package.Workbook.Worksheets.Add("SearchByKeywords");
            sheet.Cells["A1"].LoadFromCollection(data.SearchByKeywords, true);
        }
        
        package.Save();
    }
}
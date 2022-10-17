using OfficeOpenXml;
using TiVkParser.Models.SaveDataModels;

namespace TiVkParser.Services;

public static class SaveDataService
{
    public static void LikesToExcel(string fileName, IEnumerable<OutLike> data)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var package = new ExcelPackage(new FileInfo(fileName));
        var sheet = package.Workbook.Worksheets
            .Add("Likes");
        sheet.Cells["A1"].LoadFromCollection(data, true);
        package.Save();
    }
    
    public static void CommentsToExcel(string fileName, List<OutComment> data)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var package = new ExcelPackage(new FileInfo(fileName));
        var sheet = package.Workbook.Worksheets
            .Add("Comments");
        sheet.Cells["A1"].LoadFromCollection(data, true);
        package.Save();
    }
    
    public static void FriendsToExcel(string fileName, IEnumerable<long> data)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var package = new ExcelPackage(new FileInfo(fileName));
        var sheet = package.Workbook.Worksheets
            .Add("Friends");
        sheet.Cells["A1"].LoadFromCollection(data, true);
        package.Save();
    }
}
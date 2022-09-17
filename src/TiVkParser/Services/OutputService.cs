using OfficeOpenXml;
using TiVkParser.Models;

namespace TiVkParser.Services;

public static class OutputService
{
    public static void LikesToExcel(string fileName, List<LikeModel> data)
    {
        var package = new ExcelPackage(new FileInfo(fileName));
        
        var sheet = package.Workbook.Worksheets
            .Add("Likes");
        
        sheet.Cells[1, 1, 1, 4].LoadFromArrays(new[]
        {
            new[]
            {
                "ID Пользователя", 
                "ID Сообщества", 
                "ID Поста"
            }
        });

        sheet.Cells[2, 1, data.Count, 1].LoadFromArrays((IEnumerable<object[]>) new []{ data.Select(model => model.UserId) });
        sheet.Cells[2, 2, data.Count, 2].LoadFromArrays((IEnumerable<object[]>) new []{ data.Select(model => model.GroupId) });
        sheet.Cells[2, 3, data.Count, 3].LoadFromArrays((IEnumerable<object[]>) new []{ data.Select(model => model.PostId) });
        
        package.Save();
    }
    
    public static void CommentsToExcel(string fileName, List<CommentModel> data)
    {
        var package = new ExcelPackage(new FileInfo(fileName));
        
        var sheet = package.Workbook.Worksheets
            .Add("Comments");
        
        sheet.Cells[1, 1, 1, 4].LoadFromArrays(new[]
        {
            new[]
            {
                "ID Пользователя", 
                "ID Сообщества", 
                "ID Поста", 
                "ID Комментария", 
                "Текст комментария"
            }
        });

        sheet.Cells[2, 1, data.Count, 1].LoadFromArrays((IEnumerable<object[]>) new []{ data.Select(model => model.UserId) });
        sheet.Cells[2, 2, data.Count, 2].LoadFromArrays((IEnumerable<object[]>) new []{ data.Select(model => model.GroupId) });
        sheet.Cells[2, 3, data.Count, 3].LoadFromArrays((IEnumerable<object[]>) new []{ data.Select(model => model.PostId) });
        sheet.Cells[2, 4, data.Count, 4].LoadFromArrays((IEnumerable<object[]>) new []{ data.Select(model => model.CommentId) });
        sheet.Cells[2, 5, data.Count, 5].LoadFromArrays((IEnumerable<object[]>) new []{ data.Select(model => model.CommentText) });
        
        package.Save();
    }
}
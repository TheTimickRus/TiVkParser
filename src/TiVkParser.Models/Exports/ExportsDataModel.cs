// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace TiVkParser.Models.Exports;

public record ExportsDataModel
{
    public IEnumerable<OutLikeModel>? Likes { get; set; }
    public IEnumerable<OutCommentModel>? Comments { get; set; }
    public IEnumerable<long>? Friends { get; set; }
    public IEnumerable<OutCommentModel>? SearchByKeywords { get; set; }
}
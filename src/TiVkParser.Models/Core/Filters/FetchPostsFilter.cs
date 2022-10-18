namespace TiVkParser.Models.Core.Filters;

public record FetchPostsFilter(
    long? TotalLimit, 
    DateTime? DateFilter
);
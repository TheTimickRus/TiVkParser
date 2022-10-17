namespace TiVkParser.Models.Filters;

public record FetchPostsFilter(
    long? TotalLimit, 
    DateTime? DateFilter
);
namespace TiVkParser.Models;

public record FetchPostsFilterParams(
    long? TotalLimit, 
    DateTime? DateFilter
);
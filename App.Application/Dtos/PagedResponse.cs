public record PagedResponse<T>
(
    List<T> Data,
    int CurrentPage,
    int PageSize,
    int TotalItems,
    int TotalPages
);
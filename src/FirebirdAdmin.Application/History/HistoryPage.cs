namespace FirebirdAdmin.Application.History;

public sealed record HistoryPage<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);

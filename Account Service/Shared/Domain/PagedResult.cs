namespace AccountService.Shared.Domain;

/// <summary>
/// Представляет результат запроса с пагинацией.
/// </summary>
/// <typeparam name="T">Тип элементов в результате.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Элементы на текущей странице.
    /// </summary>
    public IEnumerable<T> Items { get; private set; }

    /// <summary>
    /// Общее количество элементов во всем списке (после применения фильтров).
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// Номер текущей страницы (начиная с 1).
    /// </summary>
    public int PageNumber { get; private set; }

    /// <summary>
    /// Размер страницы (количество запрошенных элементов).
    /// </summary>
    public int PageSize { get; private set; }

    /// <summary>
    /// Общее количество страниц.
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// Указывает, существует ли предыдущая страница.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Указывает, существует ли следующая страница.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
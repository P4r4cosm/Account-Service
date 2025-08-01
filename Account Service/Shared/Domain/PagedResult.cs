using System.Diagnostics.CodeAnalysis;

namespace AccountService.Shared.Domain;

/// <summary>
/// Представляет результат запроса с пагинацией.
/// </summary>
/// <typeparam name="T">Тип элементов в результате.</typeparam>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
//Resharper решил, что поля не используются, хотя на самом деле используются в ответах для эндпоинтов, использующих пагинацию 
public class PagedResult<T>(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
{
    /// <summary>
    /// Элементы на текущей странице.
    /// </summary>
    public IEnumerable<T> Items { get; private set; } = items ?? throw new ArgumentNullException(nameof(items));

    /// <summary>
    /// Общее количество элементов во всем списке (после применения фильтров).
    /// </summary>
    public int TotalCount { get; private set; } = totalCount;

    /// <summary>
    /// Номер текущей страницы (начиная с 1).
    /// </summary>
    public int PageNumber { get; } = pageNumber;

    /// <summary>
    /// Размер страницы (количество запрошенных элементов).
    /// </summary>
    public int PageSize { get; private set; } = pageSize;

    /// <summary>
    /// Общее количество страниц.
    /// </summary>
    public int TotalPages { get; } = (int)Math.Ceiling(totalCount / (double)pageSize);

    /// <summary>
    /// Указывает, существует ли предыдущая страница.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Указывает, существует ли следующая страница.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
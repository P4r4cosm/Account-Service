using System.ComponentModel.DataAnnotations;
using MediatR;

namespace AccountService.Features.Transfers.CreateTransfer;

/// <summary>
/// Команда для выполнения перевода средств между двумя банковскими счетами.
/// </summary>
public class CreateTransferCommand : IRequest<Unit>
{
    /// <summary>
    /// ID счёта, с которого будут списаны средства.
    /// </summary>
    [Required]
    public Guid FromAccountId { get; init; }

    /// <summary>
    /// ID счёта, на который будут зачислены средства.
    /// </summary>
    [Required]
    public Guid ToAccountId { get; init; }

    /// <summary>
    /// Сумма перевода. Должна быть положительным числом.
    /// </summary>
    [Required]
    public decimal Amount { get; init; }

    /// <summary>
    /// Описание или назначение перевода (необязательно).
    /// </summary>
    /// <example>Перевод на накопительный счёт</example>
    public string? Description { get; init; }
}
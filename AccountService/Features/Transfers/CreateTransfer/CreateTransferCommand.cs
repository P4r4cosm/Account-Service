using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Transfers.CreateTransfer;

/// <summary>
/// Команда для выполнения перевода средств между двумя банковскими счетами.
/// </summary>

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] //Resharper решает, что set-еры не нужны, а они нужны для корректного создания команд в эндпоинтах
public class CreateTransferCommand : IRequest<MbResult>
{
    /// <summary>
    /// ID счёта, с которого будут списаны средства.
    /// </summary>
    [Required]
    public Guid FromAccountId { get; set; }

    /// <summary>
    /// ID счёта, на который будут зачислены средства.
    /// </summary>
    [Required]
    public Guid ToAccountId { get; set; }

    /// <summary>
    /// Сумма перевода. Должна быть положительным числом.
    /// </summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Описание или назначение перевода (необязательно).
    /// </summary>
    /// <example>Перевод на накопительный счёт</example>
    public string? Description { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AccountService.Shared.Domain;

/// <summary>
/// Представляет запись об обработанном входящем сообщении для обеспечения идемпотентности (паттерн Inbox).
/// </summary>

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")] // Resharper жалуется на set, они нужны для EF core
public class InboxConsumedMessage
{
    /// <summary>
    /// Первичный ключ. Это уникальный ID входящего события (eventId из сообщения).
    /// Соответствует полю "message_id" из задания.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Время, когда сообщение было успешно обработано.
    /// Соответствует полю "processed_at" из задания.
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Имя обработчика (consumer'а), который обработал это сообщение.
    /// Это полезно, если на одно событие подписано несколько обработчиков внутри сервиса.
    /// Соответствует полю "handler" из задания.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public required string Handler { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AccountService.Shared.Domain;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")] // Resharper жалуется на set, они нужны для EF core
public class InboxDeadLetterMessage
{
   
    /// <summary>
    /// Первичный ключ. Это уникальный ID входящего события (eventId из сообщения).
    /// Соответствует полю "message_id" из задания.
    /// </summary>
    [Key]
    public Guid MessageId { get; set; }

    /// <summary>
    /// Время получения сообщения сервисом.
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Имя обработчика (consumer'а), который должен был обработать сообщение.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public required string Handler { get; set; }

    /// <summary>
    /// Полное тело (payload) полученного сообщения в виде строки.
    /// </summary>
    [Required]
    public required string Payload { get; set; }

    /// <summary>
    /// Причина, по которой сообщение попало в карантин (например, "Unsupported version").
    /// </summary>
    [Required]
    public required string Error { get; set; }
    
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace AccountService.Shared.Domain;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")] // Resharper жалуется на set, они нужны для EF core
public class OutboxMessage
{
    /// <summary>
    /// Первичный ключ. Используем ID события (eventId) для обеспечения уникальности.
    /// Соответствует требованию idempotency на стороне потребителя (message_id).
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Имя типа события, например "AccountOpened". 
    /// Необходимо для структурированного логирования и определения routing key.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public required string Type { get; set; }

    /// <summary>
    /// Полное тело сообщения в формате JSON, готовое к отправке.
    /// Соответствует "Приложение 4.1 Рекомендуемая оболочка для событий".
    /// </summary>
    [Required]
    [Column(TypeName = "jsonb")] // Оптимальный тип данных для JSON в Postgres
    
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength Resharper жалуется на длинну, т.к. здесь будет указываться Json, то считаю, что не нужно ставить ограничение по длине
    public required string Payload { get; set; }

    /// <summary>
    /// Время возникновения события. Используется для упорядочивания отправки.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Время, когда сообщение было успешно опубликовано в RabbitMQ.
    /// Если NULL, сообщение ожидает отправки. Основной флаг для фонового процесса.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Количество неудачных попыток отправки.
    /// Необходимо для реализации логики Retry и Dead-Letter (Приложение 2).
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Текст последней ошибки при отправке.
    /// Полезно для диагностики и при перемещении в "dead-letter" статус.
    /// </summary>
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength Resharper жалуется на длинну, т.к. здесь будет указываться ошибка, то считаю, что не нужно ставить ограничение по длине
    public string? Error { get; set; }
    
    /// <summary>
    /// ID для сквозной трассировки. Вынесен на верхний уровень для удобства
    /// индексации и поиска, что важно для логирования (п.17).
    /// </summary>
    public Guid CorrelationId { get; set; }
}
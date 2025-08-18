using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using Hangfire;

namespace AccountService.Infrastructure.Persistence.HangfireServices;

public class OutboxProcessorJob(
    IOutboxMessageRepository outboxRepository,
    IMessagePublisher publisher,
    IUnitOfWork unitOfWork,
    ILogger<OutboxProcessorJob> logger)
{
    // Внедряем зависимости напрямую через конструктор

    private const int BatchSize = 20;
    private const int MaxRetries = 5; // Максимальное количество попыток для одного сообщения

    // Этот публичный метод будет вызываться Hangfire
    public async Task ProcessOutboxMessagesAsync(IJobCancellationToken cancellationToken)
    {
        logger.LogInformation("Запуск задачи обработки сообщений из Outbox...");
        var token = cancellationToken.ShutdownToken;
        var messages = await outboxRepository.GetUnprocessedMessagesAsync(BatchSize, token);

        if (messages.Count == 0)
        {
            logger.LogInformation("Необработанных сообщений в Outbox не найдено.");
            return;
        }

        logger.LogInformation("Найдено {MessageCount} сообщений для обработки.", messages.Count);

        foreach (var message in messages)
        {
            var eventId = "N/A";
            try
            {
                var envelope = JsonSerializer.Deserialize<EventEnvelope<object>>(message.Payload);
                eventId = envelope?.EventId.ToString() ?? "N/A";
            }
            catch (JsonException)
            {
                /* Игнорируем, если payload невалидный */
            }

            // Создаем контекст логирования для ОДНОГО сообщения
            using (logger.BeginScope(new Dictionary<string, object>
                   {
                       ["EventId"] = eventId,
                       ["CorrelationId"] = message.CorrelationId,
                       ["MessageType"] = message.Type,
                       ["OutboxMessageId"] = message.Id
                   }))
            {
                try
                {
                    logger.LogInformation("Начинается публикация сообщения из Outbox.");


                    await publisher.PublishAsync(message, token);


                    message.ProcessedAt = DateTime.UtcNow;
                    message.Error = null; 
                }
                catch (Exception ex)
                {
                    // Ошибка! Управляем состоянием сообщения самостоятельно.
                    message.RetryCount++;
                    message.Error = ex.ToString(); // Записываем полную информацию об ошибке

                    if (message.RetryCount > MaxRetries)
                    {
                        // Это "ядовитое" сообщение. Перемещаем в "мертвые".
                        message.ProcessedAt = DateTime.UtcNow; // Убираем из очереди на обработку
                        message.Error = $"FATAL: Moved to dead-letter after {message.RetryCount} retries. Last error: {ex.Message}";
                        
                        logger.LogCritical(ex, 
                            "Достигнут лимит ({MaxRetries}) попыток для сообщения. Сообщение перемещено в карантин (dead-letter).", 
                            MaxRetries);
                    }
                    else
                    {
                        // Обычная ошибка, просто логируем. Попробуем в следующий раз.
                        logger.LogError(ex, "Ошибка публикации сообщения. Попытка #{RetryCount} из {MaxRetries} не удалась.", 
                            message.RetryCount, MaxRetries);
                    }
                }
                outboxRepository.Update(message);
            }
        }

        try
        {
            await unitOfWork.SaveChangesAsync(token);
            logger.LogInformation("Обработка {MessageCount} сообщений из Outbox успешно завершена.", messages.Count);
        }
        catch (Exception ex)
        {
            logger.LogInformation("Возникла ошибка при попытке сохранить сообщения {ex.Message}", ex.Message);
        }
    }
}
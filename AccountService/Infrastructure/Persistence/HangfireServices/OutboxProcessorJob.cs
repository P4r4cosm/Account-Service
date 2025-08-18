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
                    outboxRepository.Update(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка публикации сообщения. Hangfire выполнит повтор.");
                    throw;
                }
            }
        }

        try
        {
            await unitOfWork.SaveChangesAsync(token);
            logger.LogInformation("Обработка {MessageCount} сообщений из Outbox успешно завершена.", messages.Count);
        }
        catch (Exception ex)
        {
            logger.LogInformation("Возникла ошибка при попытке сохранить сообщения {ex.Message}",ex.Message);
        }
    }
}
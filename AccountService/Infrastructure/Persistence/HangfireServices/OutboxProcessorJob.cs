using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using Hangfire;

namespace AccountService.Infrastructure.Persistence.HangfireServices;

public class OutboxProcessorJob(
    IOutboxMessageRepository outboxRepository,
    IMessagePublisher publisher,
    IUnitOfWork unitOfWork,
    ILogger<OutboxProcessorJob> logger)
{
    // Внедряем зависимости напрямую через конструктор

    // Этот публичный метод будет вызываться Hangfire
    public async Task ProcessOutboxMessagesAsync(IJobCancellationToken cancellationToken)
    {
        logger.LogInformation("Запуск задачи обработки сообщений из Outbox...");
        var token = cancellationToken.ShutdownToken;
        var messages = await outboxRepository.GetUnprocessedMessagesAsync(token);

        if (messages.Count == 0)
        {
            logger.LogInformation("Необработанных сообщений в Outbox не найдено.");
            return;
        }

        logger.LogInformation("Найдено {MessageCount} сообщений для обработки.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(message, token);
                message.ProcessedAt = DateTime.UtcNow;
                outboxRepository.Update(message);
            }
            catch (Exception ex)
            {
                // Hangfire имеет свою собственную логику повторов. 
                // Здесь мы просто логируем ошибку. Если задача завершится с исключением,
                // Hangfire попробует выполнить ее снова позже.
                logger.LogError(ex,
                    "Не удалось обработать сообщение {MessageId} из Outbox. Задача будет повторена Hangfire.",
                    message.Id);

                // Перебрасываем исключение, чтобы Hangfire зафиксировал сбой и запланировал повтор
                throw;
            }
        }

        await unitOfWork.SaveChangesAsync(token);
        logger.LogInformation("Обработка {MessageCount} сообщений из Outbox успешно завершена.", messages.Count);
    }
}
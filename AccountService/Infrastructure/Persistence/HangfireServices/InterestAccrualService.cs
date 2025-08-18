using System.Data;
using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using Hangfire;
using Hangfire.Server;

namespace AccountService.Infrastructure.Persistence.HangfireServices;

public class InterestAccrualService(
    IAccountRepository accountRepository,
    IOutboxMessageRepository outboxMessageRepository,
    ILogger<InterestAccrualService> logger,
    IUnitOfWork unitOfWork)
    : IInterestAccrualService
{
    public async Task AccrueInterestForBatchAsync(int pageNumber, int pageSize, PerformContext? performContext,
        IJobCancellationToken cancellationToken)
    {
        var token = cancellationToken.ShutdownToken;
        var correlationId = Guid.NewGuid();
        var causationId = Guid.TryParse(performContext?.BackgroundJob.Id, out var jobId) ? jobId : Guid.NewGuid();
        logger.LogInformation(
            "Начало обработки батча #{PageNumber}. CorrelationId: {CorrelationId}, CausationId (JobId): {CausationId}",
            pageNumber, correlationId, causationId);

        await unitOfWork.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        try
        {
            var accountIds =
                (List<Guid>)await accountRepository.GetPagedAccountIdsForAccrueInterestAsync(pageNumber, pageSize,
                    token);

            foreach (var id in accountIds)
            {
                token.ThrowIfCancellationRequested();

                // 1. Вызываем наш новый, мощный метод репозитория
                var accrualResult = await accountRepository.AccrueInterest(id, token);

                // 2. Проверяем, что результат есть и что-то действительно было начислено.
                if (accrualResult is null || !accrualResult.WasAccrued) continue;
                // 3. Создаем доменное событие, используя точные данные из БД.
                var interestAccruedEvent = new InterestAccruedEvent
                {
                    AccountId = id,
                    Amount = accrualResult.AccruedAmount,
                    PeriodFrom = accrualResult.PeriodFrom!.Value,
                    PeriodTo = accrualResult.PeriodTo!.Value
                };
                var eventEnvelope = new EventEnvelope<InterestAccruedEvent>(interestAccruedEvent, correlationId, causationId);

                // 4. Создаем и добавляем сообщение в Outbox.
                var outboxMessage = new OutboxMessage
                {
                    Id = eventEnvelope.EventId,
                    Type = nameof(InterestAccruedEvent),
                    Payload = JsonSerializer.Serialize(eventEnvelope),
                    OccurredAt = eventEnvelope.OccurredAt,
                    CorrelationId = correlationId
                };
                outboxMessageRepository.Add(outboxMessage);
            }

            await unitOfWork.SaveChangesAsync(token);
            await unitOfWork.CommitTransactionAsync(token);
            logger.LogInformation("Батч #{PageNumber} успешно обработан. Счетов: {Count}", pageNumber,
                accountIds.Count);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Обработка батча #{PageNumber} была отменена. Откат транзакции.", pageNumber);
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Критическая ошибка при обработке батча #{PageNumber}. Откат транзакции.", pageNumber);
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }
}
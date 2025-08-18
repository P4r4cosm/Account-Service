using System.Data;
using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using AccountService.Shared.Providers;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountHandler(
    IAccountRepository accountRepository,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IOutboxMessageRepository outboxMessageRepository,
    ICorrelationIdProvider correlationIdProvider,
    ILogger<CreateAccountHandler> logger,
    IClientVerificationService clientVerificationService)
    : IRequestHandler<CreateAccountCommand, MbResult<AccountDto>>
{
    public async Task<MbResult<AccountDto>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var clientExists = await clientVerificationService.ClientExistsAsync(request.OwnerId, cancellationToken);
            if (!clientExists)
            {
                return MbResult<AccountDto>.Failure(MbError.Custom("Client.NotFound",
                    $"Клиент с ID '{request.OwnerId}' не найден."));
            }

            var account = mapper.Map<Account>(request);
            account.Id = Guid.NewGuid();
            account.Balance = 0;
            account.OpenedDate = DateTime.UtcNow;


            // Добавляем запись
            await accountRepository.AddAsync(account, cancellationToken);

            var correlationId = correlationIdProvider.GetCorrelationId();
            var causationId = request.CommandId;
            
            var accountOpenedEvent = new AccountOpenedEvent
            {
                AccountId = account.Id,
                OwnerId = account.OwnerId,
                Currency = account.Currency,
                Type = account.AccountType.ToString()
            };
            var eventEnvelope = new EventEnvelope<AccountOpenedEvent>(accountOpenedEvent, correlationId, causationId);

            // 4. Создаем и добавляем сообщение в Outbox
            var outboxMessage = new OutboxMessage
            {
                Id = eventEnvelope.EventId,
                Type = nameof(AccountOpenedEvent), // Безопасное получение имени типа
                Payload = JsonSerializer.Serialize(eventEnvelope),
                OccurredAt = eventEnvelope.OccurredAt,
                CorrelationId = correlationId
            };
            outboxMessageRepository.Add(outboxMessage);
            
            // Пытаемся зафиксировать изменения
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Сразу мапим созданную сущность в DTO
            var accountDto = mapper.Map<AccountDto>(account);

            await unitOfWork.CommitTransactionAsync(cancellationToken);
            // Возвращаем DTO в успешном результате
            return MbResult<AccountDto>.Success(accountDto);
        }
        catch (DbUpdateException ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            // Проверяем внутреннее исключение на конкретные ошибки БД
            if (ex.InnerException is Npgsql.PostgresException
                {
                    SqlState: "23505"
                } postgresEx) // 23505 - unique_violation
            {
                logger.LogWarning(postgresEx, "Попытка создать счет с дублирующимся ключом.");
                return MbResult<AccountDto>.Failure(MbError.Custom("Account.Conflict",
                    "Счет с такими параметрами уже существует."));
            }

            logger.LogError(ex, "Ошибка базы данных при создании счета.");
            return MbResult<AccountDto>.Failure(MbError.Custom("Database.DbError",
                "Произошла ошибка при сохранении данных в базу."));
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);

            logger.LogError(ex, "Непредвиденная ошибка при создании счета.");
            return MbResult<AccountDto>.Failure(MbError.Custom("Server.Error", "Произошла внутренняя ошибка сервера."));
        }
    }
}
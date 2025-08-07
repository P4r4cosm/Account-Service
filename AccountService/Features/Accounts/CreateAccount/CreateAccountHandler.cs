using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Domain;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountHandler(
    IAccountRepository accountRepository,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    ILogger<CreateAccountHandler> logger,
    IClientVerificationService clientVerificationService)
    : IRequestHandler<CreateAccountCommand, MbResult<AccountDto>>
{
    public async Task<MbResult<AccountDto>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var clientExists = await clientVerificationService.ClientExistsAsync(request.OwnerId, cancellationToken);
        if (!clientExists)
        {
            return MbResult<AccountDto>.Failure(MbError.Custom("Client.NotFound", $"Клиент с ID '{request.OwnerId}' не найден."));
        }

        var account = mapper.Map<Account>(request);
        account.Id = Guid.NewGuid();
        account.Balance = 0;
        account.OpenedDate = DateTime.UtcNow;
        
        // Добавляем запись
        await accountRepository.AddAsync(account, cancellationToken);
        // Фиксируем изменения
        try
        {
            // Пытаемся зафиксировать изменения
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
             logger.LogWarning(ex, "Конфликт параллельного доступа при создании счета {OwnerId}", request.OwnerId);
        
            // Возвращаем клиенту понятную ошибку и правильный HTTP статус (409 Conflict)
            return MbResult<AccountDto>.Failure(MbError.Custom("Account.Conflict", "Произошел конфликт при создании записи, попробуйте снова."));
        }
        catch (DbUpdateException ex)
        {
            // Проверяем внутреннее исключение на конкретные ошибки БД
            if (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" } postgresEx) // 23505 - unique_violation
            {
                logger.LogWarning(postgresEx, "Попытка создать счет с дублирующимся ключом.");
                return MbResult<AccountDto>.Failure(MbError.Custom("Account.Duplicate", "Счет с такими параметрами уже существует."));
            }

            logger.LogError(ex, "Ошибка базы данных при создании счета.");
            // Возвращаем общую ошибку, которая превратится в HTTP 500
            return MbResult<AccountDto>.Failure(MbError.Custom("Database.Error", "Произошла ошибка при сохранении данных в базу."));
        }
        
        // Сразу мапим созданную сущность в DTO
        var accountDto = mapper.Map<AccountDto>(account);
        
        // Возвращаем DTO в успешном результате
        return MbResult<AccountDto>.Success(accountDto);
    }
}
using System.Data;
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
            // Фиксируем изменения

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
            // Проверяем внутреннее исключение на конкретные ошибки БД
            if (ex.InnerException is Npgsql.PostgresException
                {
                    SqlState: "23505"
                } postgresEx) // 23505 - unique_violation
            {
                logger.LogWarning(postgresEx, "Попытка создать счет с дублирующимся ключом.");
                return MbResult<AccountDto>.Failure(MbError.Custom("Account.Duplicate",
                    "Счет с такими параметрами уже существует."));
            }

            logger.LogError(ex, "Ошибка базы данных при создании счета.");
            // Возвращаем общую ошибку, которая превратится в HTTP 500
            return MbResult<AccountDto>.Failure(MbError.Custom("Database.Error",
                "Произошла ошибка при сохранении данных в базу."));
        }
    }
}
using AccountService.Infrastructure.Persistence;
using AccountService.Shared.Domain;
using AccountService.Shared.Exceptions;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Transactions.GetAccountStatement;

public class GetAccountStatementHandler(IAccountRepository accountRepository, IMapper mapper)
    : IRequestHandler<GetAccountStatementQuery, AccountStatementDto>
{
    public async Task<AccountStatementDto> Handle(GetAccountStatementQuery request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null) 
            throw new NotFoundException($"Счёт {request.AccountId} не найден.");
            
        // --- Логика фильтрации и пагинации транзакций ---
        var queryableTransactions = account.Transactions.AsQueryable();

        if (request.StartDate.HasValue)
        {
            queryableTransactions = queryableTransactions.Where(t => t.Timestamp >= request.StartDate.Value);
        }
        if (request.EndDate.HasValue)
        {
            queryableTransactions = queryableTransactions.Where(t => t.Timestamp <= request.EndDate.Value);
        }

        var sortedTransactions = queryableTransactions.OrderByDescending(t => t.Timestamp);
        var totalCount = sortedTransactions.Count();
        var pagedTransactions = sortedTransactions
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        
        // --- КОНЕЦ БЛОКА ПАГИНАЦИИ ---

        // 1. Создаем DTO для пагинированного списка транзакций
        var transactionsDto = mapper.Map<List<TransactionDto>>(pagedTransactions);
        var pagedTransactionResult = new PagedResult<TransactionDto>(
            transactionsDto, 
            totalCount, 
            request.PageNumber, 
            request.PageSize
        );
        
        // 2. Маппим основную информацию о счёте из доменной модели
        var statementDto = mapper.Map<AccountStatementDto>(account);
        
        // 3. Устанавливаем оставшееся поле, которое мы проигнорировали в маппинге
        statementDto.Transactions = pagedTransactionResult;

        return statementDto;
    }
}
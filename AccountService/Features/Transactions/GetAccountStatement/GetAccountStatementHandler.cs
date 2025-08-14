using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Transactions.GetAccountStatement;

public class GetAccountStatementHandler(IAccountRepository accountRepository,
    ITransactionRepository transactionRepository,
    IMapper mapper)
    : IRequestHandler<GetAccountStatementQuery, MbResult<AccountStatementDto>>
{
    public async Task<MbResult<AccountStatementDto>> Handle(GetAccountStatementQuery request,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return MbResult<AccountStatementDto>.Failure(
                MbError.Custom("Account.NotFound", $"Счёт {request.AccountId} не найден."));
        }

        var pagedTransactions = await transactionRepository.GetPagedStatementAsync(
            request.AccountId, request.StartDate, request.EndDate, 
            request.PageNumber, request.PageSize, cancellationToken);


        // Маппим результат, который уже содержит пагинированные данные
        var transactionsDto = mapper.Map<List<TransactionDto>>(pagedTransactions.Items);
        var pagedTransactionResult = new PagedResult<TransactionDto>(
            transactionsDto,
            pagedTransactions.TotalCount,
            pagedTransactions.PageNumber,
            pagedTransactions.PageSize
        );
        
        var statementDto = mapper.Map<AccountStatementDto>(account);
        statementDto.Transactions = pagedTransactionResult;

        return MbResult<AccountStatementDto>.Success(statementDto);
    }
}
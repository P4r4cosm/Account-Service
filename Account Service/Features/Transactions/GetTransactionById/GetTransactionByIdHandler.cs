using AccountService.Infrastructure.Persistence;
using AccountService.Shared.Domain;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Transactions.GetTransactionById;

public class GetTransactionByIdHandler(IAccountRepository accountRepository, IMapper mapper)
    : IRequestHandler<GetTransactionByIdQuery, MbResult<TransactionDto>>
{
    public async Task<MbResult<TransactionDto>> Handle(GetTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            //Даже если транзакция с таким ID существует у другого счёта,
            // мы не должны её возвращать, так как запрос был в контексте конкретного счёта.
            // Возвращаем ошибку вместо исключения
            return MbResult<TransactionDto>.Failure(
                MbError.Custom("Account.NotFound", $"Счёт {request.AccountId} не найден."));
        }

        // Ищем транзакцию в коллекции счёта
        var transaction = account.Transactions.FirstOrDefault(t => t.Id == request.TransactionId);

        if (transaction is null)
        {
            return MbResult<TransactionDto>.Failure(
                MbError.Custom("Transaction.NotFound", $"Транзакция {request.TransactionId} на счёте {request.AccountId} не найдена."));
        }

        var res = mapper.Map<TransactionDto>(transaction);
        return MbResult<TransactionDto>.Success(res);
    }
}
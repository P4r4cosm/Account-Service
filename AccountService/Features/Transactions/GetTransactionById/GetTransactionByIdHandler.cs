using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Transactions.GetTransactionById;

public class GetTransactionByIdHandler(
    ITransactionRepository transactionRepository,
    IMapper mapper)
    : IRequestHandler<GetTransactionByIdQuery, MbResult<TransactionDto>>
{
    public async Task<MbResult<TransactionDto>> Handle(GetTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Один вызов, который делает всю работу в базе данных
        var transaction = await transactionRepository.GetByIdAndAccountIdAsync(
            request.TransactionId, 
            request.AccountId, 
            cancellationToken);

        if (transaction is null)
        {
            // Эта ошибка теперь означает, что транзакции нет, либо она не принадлежит этому счету.
            // Для клиента это одно и то же - "не найдено".
            return MbResult<TransactionDto>.Failure(
                MbError.Custom("Transaction.NotFound", $"Транзакция {request.TransactionId} на счёте {request.AccountId} не найдена."));
        }

        var res = mapper.Map<TransactionDto>(transaction);
        return MbResult<TransactionDto>.Success(res);
    }
}
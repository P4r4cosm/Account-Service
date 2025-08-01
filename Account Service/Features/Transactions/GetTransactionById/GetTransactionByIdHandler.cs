using AccountService.Domain.Exceptions;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Transactions.GetTransactionById;

public class GetTransactionByIdHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;

    public GetTransactionByIdHandler(IAccountRepository accountRepository, IMapper mapper)
    {
        _accountRepository = accountRepository;
        _mapper = mapper;
    }

    public async Task<TransactionDto> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            //Даже если транзакция с таким ID существует у другого счёта,
            // мы не должны её возвращать, так как запрос был в контексте конкретного счёта.
            throw new NotFoundException($"Счёт {request.AccountId} не найден.");
        }

        // Ищем транзакцию в коллекции счёта
        var transaction = account.Transactions.FirstOrDefault(t => t.Id == request.TransactionId);

        if (transaction is null)
        {
            throw new NotFoundException($"Транзакция {request.TransactionId} на счёте {request.AccountId} не найдена.");
        }

        return _mapper.Map<TransactionDto>(transaction);
    }
}
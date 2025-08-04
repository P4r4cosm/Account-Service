using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Transactions.GetTransactionById;

/// <summary>
/// Запрос на получение одной транзакции по её ID.
/// </summary>
public class GetTransactionByIdQuery(Guid accountId, Guid transactionId) : IRequest<MbResult<TransactionDto>>
{
    /// <summary>
    /// ID счёта, к которому относится транзакция.
    /// </summary>
    public Guid AccountId { get; } = accountId;

    /// <summary>
    /// ID искомой транзакции.
    /// </summary>
    public Guid TransactionId { get; } = transactionId;
}
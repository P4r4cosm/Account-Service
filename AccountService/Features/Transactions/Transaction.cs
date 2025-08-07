namespace AccountService.Features.Transactions;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? CounterpartyAccountId { get; init; }
    public decimal Amount { get; init; }
    public required string Currency { get; set; }
    public TransactionType Type { get; init; } 
    public required string Description { get; init; }
    public DateTime Timestamp { get; set; }
}
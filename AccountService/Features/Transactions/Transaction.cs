using System.ComponentModel.DataAnnotations;

namespace AccountService.Features.Transactions;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    /// <summary>
    /// ID счёта, на который будут зачислены средства.
    /// </summary>
    public Guid? CounterpartyAccountId { get; init; }
    public decimal Amount { get; init; }
    [StringLength(3)]
    public required string Currency { get; set; }
    public TransactionType Type { get; init; } 
    [StringLength(1000)]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global Resharper требует убрать get, он нужен для EF
    public required string Description { get; init; }
    public DateTime Timestamp { get; set; }
}
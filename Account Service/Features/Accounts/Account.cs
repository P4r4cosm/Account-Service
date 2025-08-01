using AccountService.Features.Transactions;

namespace AccountService.Features.Accounts;

public class Account
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public AccountType AccountType { get; set; }
    public required string Currency{get;set;}
    public decimal Balance{get;set;}
    public decimal? InterestRate { get; set; }
    public DateTime OpenedDate {get;set;}
    public DateTime? CloseDate {get;set;}
    
    /// <summary>
    /// Коллекция всех транзакций, проведённых по этому счёту.
    /// </summary>
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
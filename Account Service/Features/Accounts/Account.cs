namespace AccountService.Features.Accounts;

public class Account
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public AccountType AccountType { get; set; }
    public string Currency{get;set;}
    public decimal Balance{get;set;}
    public decimal? InterestRage { get; set; }
    public DateTime OpenedDate {get;set;}
    public DateTime? CloseDate {get;set;}
}
namespace AccountService.Features.Accounts;

/// <summary>
/// Объект для передачи данных о счёте клиенту.
/// </summary>
public class AccountDto
{
    public Guid Id { get; set; }
    public Guid OwnerId{ get; set; }
    public string AccountType{ get; set; }
    public string Currency{ get; set; }
    public decimal Balance{ get; set; }
    public decimal? InterestRate{ get; set; }
    public DateTime OpenedDate{ get; set; }
};
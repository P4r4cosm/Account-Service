using System.ComponentModel.DataAnnotations;
using AccountService.Features.Transactions;

namespace AccountService.Features.Accounts;

public class Account
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public AccountType AccountType { get; init; }
    
    [StringLength(3)]
    public required string Currency{get; init; }
    public decimal Balance{get;set;}
    public decimal? InterestRate { get; set; }
    public DateTime OpenedDate {get;set;}
    public DateTime? CloseDate {get;set;}
    
    // Последний день начисления процентов
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global  Resharper считает, что set не нужен, он необходим для EF core.
    public DateTime? LastInterestAccrualDate { get; set; } 
    
    /// <summary>
    /// Коллекция всех транзакций, проведённых по этому счёту.
    /// </summary>
    public ICollection<Transaction> Transactions { get; init; } = new List<Transaction>();
    
    /// <summary>
    /// Флаг, указывающий, что счет заморожен.
    /// Расходные операции по такому счету запрещены.
    /// </summary>
    //ReSharper disable once PropertyCanBeMadeInitOnly.Global  Resharper считает, что set не нужен, он необходим для EF core.
    public bool IsFrozen { get; set; } = false; // По умолчанию счет не заморожен
}
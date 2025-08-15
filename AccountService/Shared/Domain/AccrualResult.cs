using System.ComponentModel.DataAnnotations.Schema;

namespace AccountService.Shared.Domain;

public record AccrualResult
{
   
    [Column("accrued_amount")]
    public decimal AccruedAmount { get; init; }

    [Column("period_from")]
    public DateTime? PeriodFrom { get; init; }

    [Column("period_to")]
    public DateTime? PeriodTo { get; init; }
    
    
    public bool WasAccrued => AccruedAmount > 0;
}
using AccountService.Features.Accounts.CreateAccount;
using AccountService.Features.Transactions;
using AccountService.Features.Transactions.RegisterTransaction;
using AutoMapper;

namespace AccountService.Features.Accounts;

public class MappingProfile: Profile
{
    public MappingProfile()
    {
        CreateMap<Account, AccountDto>();
        
        CreateMap<CreateAccountCommand, Account>()
            // Говорим AutoMapper, как сопоставить поле "Type" (тип enum)
            // с полем "AccountType" (тип string) из источника.
            // Enum.Parse преобразует строку в enum, `true` игнорирует регистр.
            .ForMember(dest => dest.AccountType, 
                opt =>
                    opt.MapFrom(src => Enum.Parse<AccountType>(src.AccountType, true)));


        CreateMap<Transaction, TransactionDto>();
        
        
        CreateMap<RegisterTransactionCommand, Transaction>()
            // Маппим строковый Type из команды в enum Type в доменной модели
            .ForMember(dest => dest.Type, 
                opt => opt.MapFrom(src => Enum.Parse<TransactionType>(src.Type, true)))
            // Игнорируем поля, которые должны быть установлены вручную в обработчике
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AccountId, opt => opt.Ignore()) // Мы его из URL берем
            .ForMember(dest => dest.Currency, opt => opt.Ignore()) // Мы его со счёта берем
            .ForMember(dest => dest.Timestamp, opt => opt.Ignore())
            .ForMember(dest => dest.CounterpartyAccountId, opt => opt.Ignore()); // В этой команде его нет
        
        CreateMap<Account, AccountStatementDto>()
            // Маппим AccountType (enum) в строку
            .ForMember(dest => dest.AccountType, 
                opt => opt.MapFrom(src => src.AccountType.ToString()))
            .ForMember(dest => dest.CurrentBalance,
                opt => opt.MapFrom(src => src.Balance))
            .ForMember(dest => dest.Transactions, opt => opt.Ignore());
        
    }
}
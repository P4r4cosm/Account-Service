using AccountService.Features.Accounts.CreateAccount;
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
    }
}
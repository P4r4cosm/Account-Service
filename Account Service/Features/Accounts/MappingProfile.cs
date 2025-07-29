using AutoMapper;
using BankAccounts.Features.Accounts.CreateAccount;

namespace BankAccounts.Features.Accounts;

public class MappingProfile: Profile
{
    public MappingProfile()
    {
        CreateMap<Account, AccountDto>();
        
        CreateMap<CreateAccountCommand, Account>();
    }
}
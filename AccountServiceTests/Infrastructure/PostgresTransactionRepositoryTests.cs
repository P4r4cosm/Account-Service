using AccountService.Features.Accounts;
using AccountService.Features.Accounts.GetAccounts;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AccountServiceTests.Infrastructure;

public  class PostgresAccountRepositoryTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly PostgresAccountRepository _repository;

   
    public PostgresAccountRepositoryTests()
    {
       
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new PostgresAccountRepository(_dbContext);

        
        SeedData();
    }

    private void SeedData()
    {
        var accounts = new List<Account>
        {
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000001"), AccountType = AccountType.Checking, Currency = "RUB", Balance = 1000, OpenedDate = DateTime.UtcNow.AddDays(-10) },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000001"), AccountType = AccountType.Deposit, Currency = "USD", Balance = 5000, OpenedDate = DateTime.UtcNow.AddDays(-5) },
            new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000002"), AccountType = AccountType.Credit, Currency = "EUR", Balance = -200, OpenedDate = DateTime.UtcNow.AddDays(-1) }
        };

        _dbContext.Accounts.AddRange(accounts);
        _dbContext.SaveChanges();
    }

    

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAccount_WhenAccountExists()
    {
        // Arrange
        var existingAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var result = await _repository.GetByIdAsync(existingAccountId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingAccountId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenAccountDoesNotExist()
    {
        // Arrange
        var nonExistentAccountId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentAccountId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    // --- Тесты для GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAccounts()
    {
        // Act
        var result = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        var enumerable = result as Account[] ?? result.ToArray();
        enumerable.Should().NotBeNull();
        enumerable.Length.Should().Be(3);
    }

  

    [Fact]
    public async Task AddAsync_ShouldAddAccountToDatabase()
    {
        // Arrange
        var newAccount = new Account
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            AccountType = AccountType.Checking,
            Currency = "GBP",
            Balance = 0,
            OpenedDate = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(newAccount, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        // Assert
        var addedAccount = await _dbContext.Accounts.FindAsync(newAccount.Id);
        addedAccount.Should().NotBeNull();
        addedAccount.Should().BeEquivalentTo(newAccount);
    }
    
    [Fact]
    public async Task DeleteAsync_ShouldSetCloseDate()
    {
        // Arrange
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var account = await _dbContext.Accounts.FindAsync(accountId);
        if (account != null)
        {
            var originalCloseDate = account.CloseDate;

            // Act
            await _repository.DeleteAsync(account, CancellationToken.None);
        
            // Assert
            originalCloseDate.Should().BeNull();
        }

        account?.CloseDate.Should().NotBeNull();
        account?.CloseDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    // --- Тесты для OwnerHasAccountsAsync ---
    
    [Fact]
    public async Task OwnerHasAccountsAsync_ShouldReturnTrue_WhenOwnerHasAccounts()
    {
        // Arrange
        var ownerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Act
        var result = await _repository.OwnerHasAccountsAsync(ownerId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task OwnerHasAccountsAsync_ShouldReturnFalse_WhenOwnerDoesNotHaveAccounts()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var result = await _repository.OwnerHasAccountsAsync(ownerId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
    

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAccountInDatabase()
    {
        // Arrange
        var accountId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var account = await _dbContext.Accounts.FindAsync(accountId);
        
        // Отсоединяем объект, чтобы имитировать сценарий обновления в реальном приложении
        if (account != null)
        {
            _dbContext.Entry(account).State = EntityState.Detached;
        }

        if (account != null)
        {
            account.Balance = 9999;

            // Act
            await _repository.UpdateAsync(account, CancellationToken.None);
        }

        await _dbContext.SaveChangesAsync();

        // Assert
        var accountFromDb = await _dbContext.Accounts.FindAsync(accountId);
        accountFromDb.Should().NotBeNull();
        accountFromDb.Balance.Should().Be(9999);
    }
    
    
    [Fact]
    public async Task GetPagedAccountsAsync_ShouldFilterByOwnerId()
    {
        // Arrange
        var filters = new GetAccountsQuery
        {
            OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            PageNumber = 1,
            PageSize = 10
        };
        
        // Act
        var result = await _repository.GetPagedAccountsAsync(filters, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(a => a.OwnerId == filters.OwnerId);
    }
    
    [Fact]
    public async Task GetPagedAccountsAsync_ShouldFilterByAccountType()
    {
        // Arrange
        var filters = new GetAccountsQuery
        {
            AccountType = "Deposit",
            PageNumber = 1,
            PageSize = 10
        };
        
        // Act
        var result = await _repository.GetPagedAccountsAsync(filters, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.First().AccountType.Should().Be(AccountType.Deposit);
    }
    
    [Fact]
    public async Task GetPagedAccountsAsync_ShouldCorrectlyPaginateResults()
    {
        // Arrange
        var filters = new GetAccountsQuery
        {
            PageNumber = 2,
            PageSize = 1
        };
        
        await _dbContext.Accounts.OrderBy(a => a.OpenedDate).ToListAsync();

        var result = await _repository.GetPagedAccountsAsync(filters, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(1);
        result.Items.Count().Should().Be(1);
      
    }
    
     [Fact]
    public async Task GetPagedAccountsAsync_ShouldFilterByBalanceRange()
    {
        // Arrange
        var filters = new GetAccountsQuery
        {
            BalanceGte = 0,
            BalanceLte = 2000,
            PageNumber = 1,
            PageSize = 10
        };
        
        // Act
        var result = await _repository.GetPagedAccountsAsync(filters, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.First().Balance.Should().Be(1000);
    }
    
    [Fact]
    public async Task GetPagedAccountsAsync_ShouldReturnEmpty_WhenNoMatch()
    {
        // Arrange
        var filters = new GetAccountsQuery
        {
            Currency = "XYZ", // Несуществующая валюта
            PageNumber = 1,
            PageSize = 10
        };
        
        // Act
        var result = await _repository.GetPagedAccountsAsync(filters, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
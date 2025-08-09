using AccountService.Features.Accounts;
using AccountService.Features.Accounts.GetAccounts;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AutoMapper;
using FluentAssertions;
using Moq;

namespace AccountServiceTests.Features.Accounts;

public class GetAccountsHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetAccountsHandler _handler;

    public GetAccountsHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _mapperMock = new Mock<IMapper>();
        
        _handler = new GetAccountsHandler(_accountRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CallRepository_And_MapResults_To_Return_PagedDto()
    {
        // Arrange (Настройка)

        // 1. Входящий запрос
        var query = new GetAccountsQuery { PageNumber = 1, PageSize = 10 };

        // 2. Данные, которые "вернет" репозиторий (сущности Account)
        var accountsFromRepo = new List<Account>
        {
            new() { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), AccountType = AccountType.Checking, Currency = "RUB", Balance = 100 },
            new() { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), AccountType = AccountType.Deposit, Currency = "USD", Balance = 5000 }
        };
        var pagedAccountsFromRepo = new PagedResult<Account>(accountsFromRepo, 25, query.PageNumber, query.PageSize);
        
        // 3. Данные, которые "вернет" маппер (DTO)
        var mappedAccountDtos = new List<AccountDto>
        {
            new() { Id = accountsFromRepo[0].Id, AccountType = "Checking", Currency = "RUB" },
            new() { Id = accountsFromRepo[1].Id, AccountType = "Deposit", Currency = "USD" }
        };
        
        // 4. Настройка моков
        _accountRepositoryMock
            .Setup(r => r.GetPagedAccountsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedAccountsFromRepo);

        _mapperMock
            .Setup(m => m.Map<IEnumerable<AccountDto>>(accountsFromRepo))
            .Returns(mappedAccountDtos);

        // Act (Действие)
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert (Проверка)
        
        // Проверяем, что результат успешный
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        
        // Проверяем сам PagedResult
        var pagedDto = result.Value;
        pagedDto.Should().NotBeNull();
        pagedDto.Items.Should().BeEquivalentTo(mappedAccountDtos); // Содержимое страницы верное
        pagedDto.TotalCount.Should().Be(25); // Общее количество верное
        pagedDto.PageNumber.Should().Be(query.PageNumber);
        pagedDto.PageSize.Should().Be(query.PageSize);
        
        // Убедимся, что зависимости были вызваны
        _accountRepositoryMock.Verify(r => r.GetPagedAccountsAsync(query, It.IsAny<CancellationToken>()), Times.Once);
        _mapperMock.Verify(m => m.Map<IEnumerable<AccountDto>>(accountsFromRepo), Times.Once);
    }
}
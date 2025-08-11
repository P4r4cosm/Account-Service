using AccountService.Features.Accounts;
using AccountService.Features.Accounts.GetAccountById.GetAccountField;
using AccountService.Infrastructure.Persistence.Interfaces;
using FluentAssertions;
using Moq;

namespace AccountServiceTests.Features.Accounts;

public class GetAccountFieldHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly GetAccountFieldHandler _handler;

    public GetAccountFieldHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _handler = new GetAccountFieldHandler(_accountRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenAccountNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var query = new GetAccountFieldQuery(accountId, "anyField");

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account)null!);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be("Account.NotFound");
        result.Error?.Description.Should().Be($"Счёт {accountId} не найден.");
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessWithNull_WhenFieldNameIsInvalid()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Currency = "RUB"
        };
        var query = new GetAccountFieldQuery(account.Id, "InvalidFieldName");

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Theory]
    [InlineData("ownerid")]
    [InlineData("OwnerId")] // Проверка независимости от регистра
    [InlineData("ACCOUNTType")]
    [InlineData("currency")]
    [InlineData("balance")]
    [InlineData("interestrate")]
    [InlineData("openeddate")]
    [InlineData("closeddate")]
    public async Task Handle_Should_ReturnCorrectFieldValue_WhenFieldIsValid(string fieldName)
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var closedDate = DateTime.UtcNow;
        var account = new Account
        {
            Id = accountId,
            OwnerId = Guid.NewGuid(),
            AccountType = AccountType.Deposit,
            Currency = "USD",
            Balance = 1250.75m,
            InterestRate = 0.05m,
            OpenedDate = new DateTime(2023, 1, 15),
            CloseDate = closedDate
        };

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var query = new GetAccountFieldQuery(accountId, fieldName);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        object expectedValue = (fieldName.ToLowerInvariant() switch
        {
            "ownerid" => account.OwnerId,
            "accounttype" => account.AccountType.ToString(),
            "currency" => account.Currency,
            "balance" => account.Balance,
            "interestrate" => account.InterestRate,
            "openeddate" => account.OpenedDate,
            "closeddate" => account.CloseDate,
            _ => null
        })!;
        
        result.Value.Should().Be(expectedValue);
    }
}
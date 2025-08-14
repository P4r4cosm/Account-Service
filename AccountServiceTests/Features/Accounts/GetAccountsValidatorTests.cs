using AccountService.Features.Accounts.GetAccounts;
using AccountService.Infrastructure.Verification;
using FluentValidation.TestHelper;
using Moq;

namespace AccountServiceTests.Features.Accounts;

public class GetAccountsValidatorTests
{
    private readonly Mock<ICurrencyService> _currencyServiceMock;
    private readonly GetAccountsValidator _validator;

    public GetAccountsValidatorTests()
    {
        _currencyServiceMock = new Mock<ICurrencyService>();
        _validator = new GetAccountsValidator(_currencyServiceMock.Object);
    }

    [Fact]
    public async Task Should_Not_Have_Error_When_Query_Is_Valid()
    {
        // Arrange
        var query = new GetAccountsQuery { PageNumber = 1, PageSize = 20, Currency = "RUB", AccountType = "Checking" };
        _currencyServiceMock
            .Setup(s => s.IsSupportedAsync(query.Currency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        // Act
        var result = await _validator.TestValidateAsync(query);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_Have_Error_When_PageNumber_Is_Less_Than_1()
    {
        // Arrange
        var query = new GetAccountsQuery { PageNumber = 0 };
        
        // Act
        var result = await _validator.TestValidateAsync(query);
        
        // Assert
        result.ShouldHaveValidationErrorFor(q => q.PageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Should_Have_Error_When_PageSize_Is_Out_Of_Range(int pageSize)
    {
        // Arrange
        var query = new GetAccountsQuery { PageSize = pageSize };
        
        // Act
        var result = await _validator.TestValidateAsync(query);
        
        // Assert
        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    [Fact]
    public async Task Should_Have_Error_When_Currency_Is_Not_Supported()
    {
        // Arrange
        var query = new GetAccountsQuery { Currency = "XYZ" };
        _currencyServiceMock
            .Setup(s => s.IsSupportedAsync(query.Currency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        // Act
        var result = await _validator.TestValidateAsync(query);
        
        // Assert
        result.ShouldHaveValidationErrorFor(q => q.Currency);
    }

    [Fact]
    public async Task Should_Have_Error_When_AccountType_Is_Invalid()
    {
        // Arrange
        var query = new GetAccountsQuery { AccountType = "NonExistentType" };
        
        // Act
        var result = await _validator.TestValidateAsync(query);
        
        // Assert
        result.ShouldHaveValidationErrorFor(q => q.AccountType);
    }
    
    [Fact]
    public async Task Should_Not_Have_Error_When_Optional_Fields_Are_Null()
    {
        // Arrange
        // Проверяем, что валидация не падает, если необязательные поля не заданы
        var query = new GetAccountsQuery { PageNumber = 1, PageSize = 10, AccountType = null, Currency = null };
        
        // Act
        var result = await _validator.TestValidateAsync(query);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(q => q.AccountType);
        result.ShouldNotHaveValidationErrorFor(q => q.Currency);
    }
}
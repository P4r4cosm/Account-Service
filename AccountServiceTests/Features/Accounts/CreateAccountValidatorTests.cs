using AccountService.Features.Accounts.CreateAccount;
using AccountService.Infrastructure.Verification;
using FluentValidation.TestHelper;
using Moq;

namespace AccountServiceTests.Features.Accounts;

public class CreateAccountValidatorTests
{
    private readonly Mock<ICurrencyService> _currencyServiceMock;
    private readonly CreateAccountValidator _validator;

    public CreateAccountValidatorTests()
    {
        _currencyServiceMock = new Mock<ICurrencyService>();
        _validator = new CreateAccountValidator(_currencyServiceMock.Object);
    }

    // --- Тесты на валидные команды ---

    [Fact]
    public async Task Should_Not_Have_Error_When_DepositCommand_Is_Valid()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            OwnerId = Guid.NewGuid(),
            AccountType = "Deposit",
            Currency = "RUB",
            InterestRate = 5.5m
        };
        _currencyServiceMock
            .Setup(s => s.IsSupportedAsync(command.Currency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public async Task Should_Not_Have_Error_When_CheckingCommand_Is_Valid()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            OwnerId = Guid.NewGuid(),
            AccountType = "Checking",
            Currency = "USD",
            InterestRate = null // Для текущего счета ставка не нужна
        };
        _currencyServiceMock
            .Setup(s => s.IsSupportedAsync(command.Currency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // --- Тесты на невалидные команды ---
    
    [Fact]
    public async Task Should_Have_Error_When_OwnerId_Is_Empty()
    {
        // Arrange
        var command = new CreateAccountCommand { OwnerId = Guid.Empty, AccountType="Checking", Currency="EUR" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.OwnerId);
    }
    
    [Fact]
    public async Task Should_Have_Error_When_Currency_Is_Not_Supported()
    {
        // Arrange
        var command = new CreateAccountCommand { OwnerId = Guid.NewGuid(), AccountType="Checking", Currency="XYZ" };
        _currencyServiceMock
            .Setup(s => s.IsSupportedAsync(command.Currency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Настраиваем мок на невалидную валюту
        
        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Currency);
    }

    [Fact]
    public async Task Should_Have_Error_When_AccountType_Is_Invalid()
    {
        // Arrange
        var command = new CreateAccountCommand { OwnerId = Guid.NewGuid(), AccountType="SuperAccount", Currency="RUB", InterestRate = 1 };
        
        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.AccountType);
    }
    
    [Theory]
    [InlineData("Deposit")]
    [InlineData("Credit")]
    public async Task Should_Have_Error_When_InterestRate_Is_Null_For_Deposit_Or_Credit(string accountType)
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            OwnerId = Guid.NewGuid(),
            AccountType = accountType,
            Currency = "RUB",
            InterestRate = null
        };
        _currencyServiceMock.Setup(s => s.IsSupportedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        
        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.InterestRate)
              .WithErrorMessage("Процентная ставка обязательна для вкладов и кредитов.");
    }
    
    [Fact]
    public async Task Should_Have_Error_When_InterestRate_Is_Not_Null_For_Checking()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            OwnerId = Guid.NewGuid(),
            AccountType = "Checking",
            Currency = "USD",
            InterestRate = 1.0m
        };
        _currencyServiceMock.Setup(s => s.IsSupportedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        
        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.InterestRate)
              .WithErrorMessage("Процентная ставка не может быть указана для текущего счёта.");
    }
}
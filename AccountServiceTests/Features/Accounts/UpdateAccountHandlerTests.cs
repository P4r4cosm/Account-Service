using AccountService.Features.Accounts;
using AccountService.Features.Accounts.UpdateAccount;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountServiceTests.Features.Accounts;

public class UpdateAccountHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClientVerificationService> _clientVerificationServiceMock;
    private readonly UpdateAccountHandler _handler;

    public UpdateAccountHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<UpdateAccountHandler>>();
        _clientVerificationServiceMock = new Mock<IClientVerificationService>();

        _handler = new UpdateAccountHandler(
            _accountRepositoryMock.Object,
            _unitOfWorkMock.Object,
            loggerMock.Object,
            _clientVerificationServiceMock.Object);
    }
    
    private static Account GetTestAccount(AccountType type = AccountType.Deposit) => new()
    {
        Currency = "RUB",
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        AccountType = type,
        OpenedDate = new DateTime(2024, 1, 1)
    };

    [Fact]
    public async Task Handle_ShouldReturnSuccess_AndUpdateAllFields_WhenRequestIsValid()
    {
        // Arrange
        var account = GetTestAccount();
        var newOwnerId = Guid.NewGuid();
        var command = new UpdateAccountCommand
        {
            AccountId = account.Id,
            OwnerId = newOwnerId,
            InterestRate = 10.5m,
            CloseDate = new DateTime(2025, 1, 1)
        };
        
        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(command.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        
        _clientVerificationServiceMock
            .Setup(s => s.ClientExistsAsync(command.OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.OwnerId.Should().Be(command.OwnerId);
        account.InterestRate.Should().Be(command.InterestRate);
        account.CloseDate.Should().Be(command.CloseDate);
        
        _accountRepositoryMock.Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Handle_ShouldReturnAccountNotFound_WhenAccountDoesNotExist()
    {
        // Arrange
        var command = new UpdateAccountCommand { AccountId = Guid.NewGuid() };
        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(command.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Account.NotFound");
    }
    
    [Fact]
    public async Task Handle_ShouldReturnClientNotFound_WhenNewOwnerDoesNotExist()
    {
        // Arrange
        var account = GetTestAccount();
        var command = new UpdateAccountCommand { AccountId = account.Id, OwnerId = Guid.NewGuid() };
        
        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(command.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        
        _clientVerificationServiceMock
            .Setup(s => s.ClientExistsAsync(command.OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Клиент не найден

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Client.NotFound");
    }
    
    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenUpdatingInterestRateForCheckingAccount()
    {
        // Arrange
        var account = GetTestAccount(AccountType.Checking);
        var command = new UpdateAccountCommand { AccountId = account.Id, OwnerId = Guid.NewGuid(), InterestRate = 5.0m };
        
        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(command.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        
        _clientVerificationServiceMock
            .Setup(s => s.ClientExistsAsync(command.OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Account.Update.Forbidden");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenCloseDateIsBeforeOpenedDate()
    {
        // Arrange
        var account = GetTestAccount(); 
        var command = new UpdateAccountCommand { AccountId = account.Id, OwnerId = Guid.NewGuid(), CloseDate = new DateTime(2023, 12, 31) };

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(command.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        
        _clientVerificationServiceMock
            .Setup(s => s.ClientExistsAsync(command.OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Account.Validation");
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenConcurrencyExceptionOccurs()
    {
        // Arrange
        var account = GetTestAccount();
        var command = new UpdateAccountCommand { AccountId = account.Id, OwnerId = Guid.NewGuid() };

        _accountRepositoryMock
            .Setup(r => r.GetByIdAsync(command.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _clientVerificationServiceMock
            .Setup(s => s.ClientExistsAsync(command.OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Account.Conflict");
    }
}
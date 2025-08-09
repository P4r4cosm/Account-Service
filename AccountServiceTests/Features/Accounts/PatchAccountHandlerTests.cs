using AccountService.Features.Accounts;
using AccountService.Features.Accounts.PatchAccount;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountServiceTests.Features.Accounts;

public class PatchAccountHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IClientVerificationService> _clientVerificationServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly PatchAccountHandler _handler;

    public PatchAccountHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _clientVerificationServiceMock = new Mock<IClientVerificationService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<PatchAccountHandler>>();

        _handler = new PatchAccountHandler(
            _accountRepositoryMock.Object,
            _clientVerificationServiceMock.Object,
            _unitOfWorkMock.Object,
            loggerMock.Object);
    }
    
    // Вспомогательный метод для создания тестового счёта
    private static Account GetTestAccount(AccountType type = AccountType.Deposit) => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        AccountType = type,
        Balance = 100,
        Currency = "RUB",
        InterestRate = 5.0m,
        OpenedDate = new DateTime(2024, 1, 1),
        CloseDate = null
    };

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenAccountDoesNotExist()
    {
        // Arrange
        var command = new PatchAccountCommand { AccountId = Guid.NewGuid() };
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(command.AccountId, It.IsAny<CancellationToken>()))
                              .ReturnsAsync((Account)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Account.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenNoChangesAreProvided()
    {
        // Arrange
        var account = GetTestAccount();
        var command = new PatchAccountCommand { AccountId = account.Id }; // Пустая команда
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(account);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Самое главное: убедиться, что не было вызовов на сохранение
        _accountRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task Handle_ShouldUpdateOwnerId_WhenNewValidOwnerIdIsProvided()
    {
        // Arrange
        var account = GetTestAccount();
        var newOwnerId = Guid.NewGuid();
        var command = new PatchAccountCommand { AccountId = account.Id, OwnerId = newOwnerId };
        
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(account);
        _clientVerificationServiceMock.Setup(s => s.ClientExistsAsync(newOwnerId, It.IsAny<CancellationToken>()))
                                      .ReturnsAsync(true);
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.OwnerId.Should().Be(newOwnerId);
        _accountRepositoryMock.Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnOwnerNotFound_WhenNewOwnerDoesNotExist()
    {
        // Arrange
        var account = GetTestAccount();
        var newOwnerId = Guid.NewGuid();
        var command = new PatchAccountCommand { AccountId = account.Id, OwnerId = newOwnerId };
        
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(account);
        _clientVerificationServiceMock.Setup(s => s.ClientExistsAsync(newOwnerId, It.IsAny<CancellationToken>()))
                                      .ReturnsAsync(false);
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Owner.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenUpdatingInterestRateForCheckingAccount()
    {
        // Arrange
        var account = GetTestAccount(AccountType.Checking);
        var command = new PatchAccountCommand { AccountId = account.Id, InterestRate = 10m };

        _accountRepositoryMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(account);
        
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
        var account = GetTestAccount(); // OpenedDate = 2024-01-01
        var command = new PatchAccountCommand { AccountId = account.Id, CloseDate = new DateTime(2023, 12, 31) };

        _accountRepositoryMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(account);
        
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
        var command = new PatchAccountCommand { AccountId = account.Id, InterestRate = 10m };
        
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(account);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Account.Conflict");
    }
    
    [Fact]
    public async Task Handle_ShouldReturnDbError_WhenGenericDbExceptionOccurs()
    {
        // Arrange
        var account = GetTestAccount();
        var command = new PatchAccountCommand { AccountId = account.Id, InterestRate = 10m };
        
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(account);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateException());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Database.DbError");
    }
}
using System.Data;
using AccountService.Features.Accounts;
using AccountService.Features.Transactions;
using AccountService.Features.Transfers.CreateTransfer;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AccountService.Shared.Providers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountServiceTests.Features.Transfers;

public class CreateTransferHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateTransferHandler _handler;

    public CreateTransferHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<CreateTransferHandler>>();
        var outboxMessageRepository = new Mock<IOutboxMessageRepository>();
        var correlationIdProviderMock = new Mock<ICorrelationIdProvider>();
        _handler = new CreateTransferHandler(
            _accountRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _unitOfWorkMock.Object,
            outboxMessageRepository.Object,
            correlationIdProviderMock.Object,
            loggerMock.Object);
    }
    
    // Вспомогательный метод для создания тестовых счетов
    private static Account CreateTestAccount(Guid id, string currency, decimal balance, DateTime? closeDate = null)
    {
        return new Account
        {
            Id = id,
            Currency = currency,
            Balance = balance,
            CloseDate = closeDate,
            Transactions = new List<Transaction>()
        };
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenTransferIsValid()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var fromAccount = CreateTestAccount(fromAccountId, "RUB", 1000m);
        var toAccount = CreateTestAccount(toAccountId, "RUB", 500m);
        var command = new CreateTransferCommand { FromAccountId = fromAccountId, ToAccountId = toAccountId, Amount = 200m };

        _accountRepositoryMock.Setup(r => r.GetByIdAsync(fromAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(toAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(toAccount);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fromAccount.Balance.Should().Be(800m);
        toAccount.Balance.Should().Be(700m);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(IsolationLevel.Serializable, It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepositoryMock.Verify(t => t.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenFromAccountIsNotFound()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var toAccount = CreateTestAccount(toAccountId, "RUB", 500m);
        var command = new CreateTransferCommand { FromAccountId = fromAccountId, ToAccountId = toAccountId, Amount = 200m };

        _accountRepositoryMock.Setup(r => r.GetByIdAsync(fromAccountId, It.IsAny<CancellationToken>())).ReturnsAsync((Account)null!);
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(toAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(toAccount);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Transfer.NotFound");
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task Handle_ShouldFail_WhenCurrenciesMismatch()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var fromAccount = CreateTestAccount(fromAccountId, "RUB", 1000m);
        var toAccount = CreateTestAccount(toAccountId, "USD", 500m); // Разные валюты
        var command = new CreateTransferCommand { FromAccountId = fromAccountId, ToAccountId = toAccountId, Amount = 200m };

        _accountRepositoryMock.Setup(r => r.GetByIdAsync(fromAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(toAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(toAccount);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Transfer.Validation");
        result.Error?.Description.Should().Contain("одной валюте");
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenInsufficientFunds()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var fromAccount = CreateTestAccount(fromAccountId, "RUB", 100m); // Недостаточно средств
        var toAccount = CreateTestAccount(toAccountId, "RUB", 500m);
        var command = new CreateTransferCommand { FromAccountId = fromAccountId, ToAccountId = toAccountId, Amount = 200m };

        _accountRepositoryMock.Setup(r => r.GetByIdAsync(fromAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(toAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(toAccount);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Transfer.Validation");
        result.Error?.Description.Should().Contain("Недостаточно средств");
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Handle_ShouldRollback_WhenConcurrencyExceptionOccurs()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var fromAccount = CreateTestAccount(fromAccountId, "RUB", 1000m);
        var toAccount = CreateTestAccount(toAccountId, "RUB", 500m);
        var command = new CreateTransferCommand { FromAccountId = fromAccountId, ToAccountId = toAccountId, Amount = 200m };
        
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(fromAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(fromAccount);
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(toAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(toAccount);
        
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Transfer.Conflict");
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
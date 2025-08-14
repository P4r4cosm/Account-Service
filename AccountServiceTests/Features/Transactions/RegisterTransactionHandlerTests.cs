using System.Data;
using AccountService.Features.Accounts;
using AccountService.Features.Transactions;
using AccountService.Features.Transactions.RegisterTransaction;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountServiceTests.Features.Transactions;

public class RegisterTransactionHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly RegisterTransactionHandler _handler;

    public RegisterTransactionHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<RegisterTransactionHandler>>();

        _handler = new RegisterTransactionHandler(
            _accountRepositoryMock.Object,
            loggerMock.Object,
            _transactionRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }
    
    // Вспомогательный метод для создания тестового счёта
    private static Account GetTestAccount(decimal initialBalance = 1000m) => new()
    {
        Id = Guid.NewGuid(),
        Balance = initialBalance,
        Currency = "RUB",
        CloseDate = null
    };

    [Fact]
    public async Task Handle_ShouldSucceed_WhenRegisteringCreditTransaction()
    {
        // Arrange
        const decimal initialBalance = 1000m;
        const decimal transactionAmount = 500m;
        var account = GetTestAccount();
        var command = new RegisterTransactionCommand { AccountId = account.Id, Amount = transactionAmount, Type = "Credit", Description = "Test" };
        var newTransaction = new Transaction
        {
            Amount = command.Amount,
            Type = TransactionType.Credit,
            Currency = null!,
            Description = null!
        };
        var transactionDto = new TransactionDto
        {
            Amount = command.Amount,
            Type = "Credit",
            Currency = null!,
            Description = null!
        };

        _accountRepositoryMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _mapperMock.Setup(m => m.Map<Transaction>(command)).Returns(newTransaction);
        _mapperMock.Setup(m => m.Map<TransactionDto>(newTransaction)).Returns(transactionDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(transactionDto);
        account.Balance.Should().Be(initialBalance + transactionAmount);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(IsolationLevel.ReadCommitted, It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepositoryMock.Verify(t => t.AddAsync(newTransaction, It.IsAny<CancellationToken>()), Times.Once);
        _accountRepositoryMock.Verify(a => a.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnInsufficientFunds_WhenDebitIsLargerThanBalance()
    {
        // Arrange
        var account = GetTestAccount(100m); // Баланс 100
        var command = new RegisterTransactionCommand { AccountId = account.Id, Amount = 200m, Type = "Debit", Description = "Test" };
        var newTransaction = new Transaction
        {
            Amount = command.Amount,
            Type = TransactionType.Debit,
            Currency = null!,
            Description = null!
        };

        _accountRepositoryMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _mapperMock.Setup(m => m.Map<Transaction>(command)).Returns(newTransaction);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Transaction.Validation");
        
        // Проверяем, что не было попыток сохранить изменения
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task Handle_ShouldReturnAccountNotFound_WhenAccountDoesNotExist()
    {
        // Arrange
        var command = new RegisterTransactionCommand { AccountId = Guid.NewGuid(), Amount = 100, Type = "Credit", Description = "Test" };
        _accountRepositoryMock.Setup(r => r.GetByIdAsync(command.AccountId, It.IsAny<CancellationToken>())).ReturnsAsync((Account)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Account.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenAccountIsClosed()
    {
        // Arrange
        var account = GetTestAccount();
        account.CloseDate = DateTime.UtcNow; // Закрываем счет
        var command = new RegisterTransactionCommand { AccountId = account.Id, Amount = 100, Type = "Credit", Description = "Test" };

        _accountRepositoryMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Account.Validation");
    }
    
    [Fact]
    public async Task Handle_ShouldRollbackTransaction_WhenConcurrencyExceptionOccurs()
    {
        // Arrange
        var account = GetTestAccount();
        var command = new RegisterTransactionCommand { AccountId = account.Id, Amount = 100, Type = "Credit", Description = "Test" };
        var newTransaction = new Transaction
        {
            Amount = command.Amount,
            Type = TransactionType.Credit,
            Currency = null!,
            Description = null!
        };

        _accountRepositoryMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _mapperMock.Setup(m => m.Map<Transaction>(command)).Returns(newTransaction);
        
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("Account.Conflict");
        
        // Самая важная проверка - был ли откат
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
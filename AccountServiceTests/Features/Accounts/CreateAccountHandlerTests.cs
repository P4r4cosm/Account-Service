using System.Data;
using AccountService.Features.Accounts;
using AccountService.Features.Accounts.CreateAccount;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Domain;
using AccountService.Shared.Providers;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace AccountServiceTests.Features.Accounts;

public class CreateAccountHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClientVerificationService> _clientVerificationServiceMock;
    private readonly CreateAccountHandler _handler;

    public CreateAccountHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _mapperMock = new Mock<IMapper>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<CreateAccountHandler>>();
        _clientVerificationServiceMock = new Mock<IClientVerificationService>();
        var outboxMessageRepository = new Mock<IOutboxMessageRepository>();
        var correlationIdProviderMock = new Mock<ICorrelationIdProvider>();

        _handler = new CreateAccountHandler(
            _accountRepositoryMock.Object,
            _mapperMock.Object,
            _unitOfWorkMock.Object,
            outboxMessageRepository.Object,
            correlationIdProviderMock.Object,
            loggerMock.Object,
            _clientVerificationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessResult_WhenClientExistsAndDataIsValid()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            OwnerId = Guid.NewGuid(),
            AccountType = "Deposit",
            Currency = "RUB",
            InterestRate = (decimal)5.0
        };

        _clientVerificationServiceMock
            .Setup(s => s.ClientExistsAsync(command.OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var newAccount = new Account { OwnerId = command.OwnerId, Currency = command.Currency };
        _mapperMock
            .Setup(m => m.Map<Account>(command))
            .Returns(newAccount);

        var accountDto = new AccountDto { Id = Guid.NewGuid(), OwnerId = command.OwnerId, AccountType = command.AccountType, Currency = command.Currency };
        _mapperMock
            .Setup(m => m.Map<AccountDto>(It.IsAny<Account>()))
            .Returns(accountDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(accountDto);
        result.Error.Should().BeNull(); // В успешном результате нет ошибки

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(IsolationLevel.Serializable, It.IsAny<CancellationToken>()), Times.Once);
        _accountRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureResult_WhenClientDoesNotExist()
    {
        // Arrange
        var command = new CreateAccountCommand { OwnerId = Guid.NewGuid(), AccountType="Checking", Currency = "USD" };

        _clientVerificationServiceMock
            .Setup(s => s.ClientExistsAsync(command.OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().NotBeNull();
        result.Error.Code.Should().Be("Client.NotFound");

        _accountRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictError_WhenDbUpdateExceptionIsUniqueViolation()
    {
        // Arrange
        var command = new CreateAccountCommand { OwnerId = Guid.NewGuid(), AccountType="Credit", Currency = "EUR", InterestRate = 10 };

        _clientVerificationServiceMock
            .Setup(s => s.ClientExistsAsync(command.OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _mapperMock.Setup(m => m.Map<Account>(command)).Returns(new Account
        {
            Currency = "EUR"
        });

        var postgresException = new PostgresException("Сообщение", "Критично", "Критично", "23505");
        var dbUpdateException = new DbUpdateException("Ошибка", postgresException);
        
        _unitOfWorkMock
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbUpdateException);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Code.Should().Be("Account.Conflict");

        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

     [Fact]
    public async Task Handle_ShouldReturnDatabaseError_WhenDbUpdateExceptionIsNotUniqueViolation()
    {
        // Arrange
        var command = new CreateAccountCommand { OwnerId = Guid.NewGuid(), AccountType="Checking", Currency = "GBP" };

        _clientVerificationServiceMock.Setup(s => s.ClientExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mapperMock.Setup(m => m.Map<Account>(command)).Returns(new Account
        {
            Currency = "GBP"
        });
        
        var genericDbException = new DbUpdateException("Другая ошибка БД");
        
        _unitOfWorkMock
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(genericDbException);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Code.Should().Be("Database.DbError");

        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
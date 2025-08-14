using AccountService.Infrastructure.Persistence.HangfireServices;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountServiceTests.Hangfire;

public class InterestAccrualServiceTests
{
    [Fact]
    public async Task AccrueInterestForBatchAsync_Commits_WhenNoErrors()
    {
        // Arrange
        var accountRepo = new Mock<IAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<InterestAccrualService>>();

        var accountIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        accountRepo.Setup(r => r.GetPagedAccountIdsForAccrueInterestAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountIds);

        var service = new InterestAccrualService(
            accountRepo.Object,
            logger.Object,
            unitOfWork.Object);

        var cancellationToken = new Mock<IJobCancellationToken>();
        cancellationToken.Setup(c => c.ShutdownToken).Returns(CancellationToken.None);

        // Act
        await service.AccrueInterestForBatchAsync(1, 2, cancellationToken.Object);

        // Assert
        unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>(), It.IsAny<CancellationToken>()), Times.Once);
        accountRepo.Verify(r => r.AccrueInterest(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AccrueInterestForBatchAsync_RollsBack_WhenCanceled()
    {
        // Arrange
        var accountRepo = new Mock<IAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<InterestAccrualService>>();

        accountRepo.Setup(r => r.GetPagedAccountIdsForAccrueInterestAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { Guid.NewGuid() });

        var service = new InterestAccrualService(accountRepo.Object, logger.Object, unitOfWork.Object);

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cancellationToken = new Mock<IJobCancellationToken>();
        cancellationToken.Setup(c => c.ShutdownToken).Returns(cts.Token);

        // Act
        await service.AccrueInterestForBatchAsync(1, 1, cancellationToken.Object);

        // Assert
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AccrueInterestForBatchAsync_RollsBack_WhenExceptionThrown()
    {
        // Arrange
        var accountRepo = new Mock<IAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<InterestAccrualService>>();

        accountRepo.Setup(r => r.GetPagedAccountIdsForAccrueInterestAsync(1, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var service = new InterestAccrualService(accountRepo.Object, logger.Object, unitOfWork.Object);

        var cancellationToken = new Mock<IJobCancellationToken>();
        cancellationToken.Setup(c => c.ShutdownToken).Returns(CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AccrueInterestForBatchAsync(1, 1, cancellationToken.Object));

        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
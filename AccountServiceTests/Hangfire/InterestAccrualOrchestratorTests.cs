using AccountService.Infrastructure.Persistence.HangfireServices;
using AccountService.Infrastructure.Persistence.Interfaces;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountServiceTests.Hangfire;

public class InterestAccrualOrchestratorTests
{
    [Fact]
    public async Task StartAccrualProcess_NoAccounts_NoJobsCreated()
    {
        // Arrange
        var accountRepo = new Mock<IAccountRepository>();
        var backgroundJobClient = new Mock<IBackgroundJobClient>();
        var logger = new Mock<ILogger<InterestAccrualOrchestrator>>();

        accountRepo
            .Setup(r => r.GetAccountCountForAccrueInterestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var orchestrator = new InterestAccrualOrchestrator(
            accountRepo.Object,
            backgroundJobClient.Object,
            logger.Object);

        // Act
        await orchestrator.StartAccrualProcess();

        // Assert — проверяем Create, а не Enqueue
        backgroundJobClient.Verify(
            c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAccrualProcess_CreatesCorrectNumberOfJobs()
    {
        // Arrange
        var accountRepo = new Mock<IAccountRepository>();
        var backgroundJobClient = new Mock<IBackgroundJobClient>();
        var logger = new Mock<ILogger<InterestAccrualOrchestrator>>();

        accountRepo.Setup(r => r.GetAccountCountForAccrueInterestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1200);

        var orchestrator = new InterestAccrualOrchestrator(
            accountRepo.Object, 
            backgroundJobClient.Object, 
            logger.Object);

        // Act
        await orchestrator.StartAccrualProcess();

        // Assert
        backgroundJobClient.Verify(
            c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Exactly(3));
    }
}


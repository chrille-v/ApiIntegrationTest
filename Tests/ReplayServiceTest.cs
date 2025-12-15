using APIIntegration.Core;
using APIIntegration.Core.Models;
using APIIntegration.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace ApiIntegrationTest.Tests
{
    public class OutboxMessageData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new List<OutboxMessage>
                {
                    new OutboxMessage { Id = Guid.NewGuid(), Type = "", Payload = "", CreatedAt = DateTime.Now, LastAttemptAt = DateTime.UtcNow, RetryCount = 0, Status = "Pending"},
                    new OutboxMessage { Id = Guid.NewGuid(), Type = "", Payload = "", CreatedAt = DateTime.Now, LastAttemptAt = DateTime.UtcNow, RetryCount = 0, Status = "Pending"},
                    new OutboxMessage { Id = Guid.NewGuid(), Type = "", Payload = "", CreatedAt = DateTime.Now, LastAttemptAt = DateTime.UtcNow, RetryCount = 0, Status = null!},
                }
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ReplayServiceTest
    {
        [Theory]
        [ClassData(typeof(OutboxMessageData))]
        public async Task ReplayPendingMessageAsync_ShouldSentMessage(List<OutboxMessage> messages)
        {
            // Arrange
            var onlyPending = messages
                .Where(x => x.Status == "Pending")
                .OrderBy(x => x.CreatedAt)
                .Take(20)
                .ToList();

            var repoMock = new Mock<IOutboxRepository>();

            repoMock.Setup(r => r.GetPendingAsync(maxBatch: 20))
                .ReturnsAsync(onlyPending);

            var logger = new Mock<ILogger<ReplayService>>();

            var api = new Mock<ICustomerApiClient>();
            api.Setup(a => a.SendAsync(It.IsAny<OutboxMessage>())).ReturnsAsync(ApiResult.Ok);

            var cancellationToken = CancellationToken.None;

            var service = new ReplayService(repoMock.Object, api.Object, logger.Object);

            // Act
            var result = service.ReplayPendingMessageAsync(cancellationToken);

            // Assert
            // No messages with pending = no messages sent
            //api.VerifyNoOtherCalls();
            // Messages pending
            api.Verify(x => x.SendAsync(It.IsAny<OutboxMessage>()), Times.Exactly(2));
            repoMock.Verify(r => r.MarkAsSentAsync(It.IsAny<Guid>()), Times.Exactly(2));
            repoMock.Verify(x => x.MarkAsFailedAsync(It.IsAny<Guid>(), ""), Times.Never());
            repoMock.Verify(x => x.MarkAsSentAsync(It.IsAny<Guid>()), Times.Exactly(2));
        }

        [Theory]
        [ClassData(typeof(OutboxMessageData))]
        public async Task ReplayPendingMessageAsync_Should_Fail(List<OutboxMessage> messages)
        {
            // Arrange
            var onlyPending = messages
                .Where(x => x.Status == "Pending")
                .OrderBy(x => x.CreatedAt)
                .Take(20)
                .ToList();

            var repoMock = new Mock<IOutboxRepository>();

            repoMock.Setup(r => r.GetPendingAsync(maxBatch: 20))
                .ReturnsAsync(onlyPending);

            var logger = new Mock<ILogger<ReplayService>>();

            var api = new Mock<ICustomerApiClient>();
            api.Setup(a => a.SendAsync(It.IsAny<OutboxMessage>())).ReturnsAsync(ApiResult.Fail("Fail"));
            //api.Setup(a => a.SendAsync(It.IsAny<OutboxMessage>())).ReturnsAsync(ApiResult.Ok);

            var cancellationToken = CancellationToken.None;

            var service = new ReplayService(repoMock.Object, api.Object, logger.Object);

            // Act
            var result = service.ReplayPendingMessageAsync(cancellationToken);

            // Assert
            repoMock.Verify(x => x.MarkAsFailedAsync(It.IsAny<Guid>(), "Fail"), Times.Exactly(2));
            repoMock.Verify(r => r.MarkAsSentAsync(It.IsAny<Guid>()), Times.Never());
        }

        [Theory]
        [ClassData(typeof(OutboxMessageData))]
        public async Task ReplayPendingMessageAsync_Cast_Simulated_Exception(List<OutboxMessage> messages)
        {
            // Arrange
            var onlyPending = messages
                .Where(x => x.Status == "Pending")
                .OrderBy(x => x.CreatedAt)
                .Take(20)
                .ToList();

            var repoMock = new Mock<IOutboxRepository>();

            repoMock.Setup(r => r.GetPendingAsync(maxBatch: 20))
                .ReturnsAsync(onlyPending);

            var logger = new Mock<ILogger<ReplayService>>();

            var api = new Mock<ICustomerApiClient>();
            api.Setup(a => a.SendAsync(It.IsAny<OutboxMessage>())).ThrowsAsync(new InvalidOperationException("Simulated failure"));

            var cancellationToken = CancellationToken.None;

            var service = new ReplayService(repoMock.Object, api.Object, logger.Object);
           
            // Act
            var result = service.ReplayPendingMessageAsync(cancellationToken);

            // Assert
            repoMock.Verify(x => x.MarkAsFailedAsync(It.IsAny<Guid>(), "Fail"), Times.Never());
            repoMock.Verify(r => r.MarkAsSentAsync(It.IsAny<Guid>()), Times.Never());
            repoMock.Verify(c => c.IncrementRetryAsync(It.IsAny<Guid>()), Times.Exactly(onlyPending.Count));
        }
    }
}
using APIIntegration.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApiIntegrationTest.Tests
{
    public class LocalCacheTest
    {
        [Fact]
        public void Save_Message_Async_Test()
        {
            // Arrange
            var fakeMessage = new Message
            {
                MessageId = "1",
                Payload = "Payload",
                LastUpdate = DateTime.UtcNow,
                Status = MessageStatus.Pending,
                Type = MessageType.JobUpdate,
                RetryCount = 0,
            };

            var cancellationToken = CancellationToken.None;

            // Act
            // Assert
            Assert.True(true);
        }
    }
}

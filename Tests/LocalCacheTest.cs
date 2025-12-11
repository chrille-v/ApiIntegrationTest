using APIIntegration.Config;
using APIIntegration.Core.Models;
using APIIntegration.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.NetworkInformation;
using System.Runtime;
using System.Text;
using System.Threading;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ApiIntegrationTest.Tests
{
    public class LocalCacheTest
    {
        [Fact]
        public async Task Save_Message_Async_Test()
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

            // Skapa temporär fil för testdatabas
            string tempDbPath = Path.Combine(Path.GetTempPath(), $"testMessages_{Guid.NewGuid()}.db");
            File.Copy("localMessages.db", tempDbPath);

            var dbSettings = new DatabaseSettings
            {
                Path = tempDbPath
            };

            var options = Options.Create(dbSettings);
            var logger = new Mock<ILogger<LocalCache>>();

            var service = new LocalCache(options, logger.Object);

            // Act
            await service.SaveMessageAsync(fakeMessage, cancellationToken);

            // Assert
            await using var verifyConnection = new SqliteConnection($"Data Source={tempDbPath}");

            await verifyConnection.OpenAsync();

            using var verifyCmd = verifyConnection.CreateCommand();

            verifyCmd.CommandText = "SELECT Payload, Status, Type, RetryCount FROM Messages WHERE MessageId = @id";
            verifyCmd.Parameters.AddWithValue("@id", "1");

            await using var reader = await verifyCmd.ExecuteReaderAsync();

            Assert.True(await reader.ReadAsync());
            Assert.Equal("Payload", reader.GetString(0));
            Assert.Equal((int)MessageStatus.Pending, reader.GetInt32(1));
            Assert.Equal((int)MessageType.JobUpdate, reader.GetInt32(2));
            Assert.Equal(0, reader.GetInt32(3));
        }

        [Fact]
        public async Task SaveAndLoad_CanRunInParallel()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Skapa temporär fil för testdatabas
            string tempDbPath = Path.Combine(Path.GetTempPath(), $"testMessages_{Guid.NewGuid()}.db");
            File.Copy("localMessages.db", tempDbPath);

            var dbSettings = new DatabaseSettings
            {
                Path = tempDbPath
            };

            var options = Options.Create(dbSettings);
            var logger = new Mock<ILogger<LocalCache>>();

            var service = new LocalCache(options, logger.Object);

            int numberOfTasks = 250;
            var tasks = new Task[numberOfTasks];

            for (int i = 0; i < numberOfTasks; i++)
            {
                var messageId = i.ToString();

                var fakeMessage = new Message
                {
                    MessageId = messageId,
                    Payload = "Payload",
                    LastUpdate = DateTime.UtcNow,
                    Status = MessageStatus.Pending,
                    Type = MessageType.JobUpdate,
                    RetryCount = 0,
                };

                tasks[i] = Task.Run(async () =>
                {
                    await service.SaveMessageAsync(fakeMessage, cancellationToken);
                    var data = await service.GetMessageAsync(messageId, cancellationToken);
                    Assert.NotNull(data);
                });
            }

            await Task.WhenAll(tasks);
        }
    }
}

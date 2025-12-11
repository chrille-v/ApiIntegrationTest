using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using APIIntegration.Infrastructure;
using APIIntegration.Config;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using APIIntegration.Core.Models;
using System.Net;
using Moq;

namespace ApiIntegrationTest.Tests
{
    public class LanForwarderTest
    {
        [Fact]
        public async Task ForwardJobAsyncReturnsTrueOnSuccess()
        {
            // Arrange
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("OK")
            };

            var handler = new FakeHandler(fakeResponse);
            var httpClient = new HttpClient(handler);
            var cancellationToken = CancellationToken.None;

            var lanOptions = Options.Create(new LanSettings
            {
                BaseUrl = "http://localhost:5001/",
                JobUpdateEndpoint = "jobUpdate",
                JobStatusEndpoint = "jobStatus",
                SharedKey = "1234"
            });

            var fakeMessage = new Message
            {
                MessageId = "1",
                Payload = "Payload",
                LastUpdate = DateTime.UtcNow,
                Status = MessageStatus.Pending,
                Type = MessageType.JobUpdate,
                RetryCount = 0,
            };

            var logger = new Mock<ILogger<LanForwarder>>();

            var forwarder = new LanForwarder(httpClient, lanOptions, logger.Object);

            // Act
            var result = forwarder.ForwardJobAsync(fakeMessage, cancellationToken);

            // Assert
            Assert.True(await result);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, true)]
        [InlineData(HttpStatusCode.NotFound, false)]
        [InlineData(HttpStatusCode.Conflict, false)]
        [InlineData(HttpStatusCode.Unauthorized, false)]
        [InlineData(HttpStatusCode.Forbidden, false)]
        [InlineData(HttpStatusCode.BadRequest, false)]
        [InlineData(HttpStatusCode.BadGateway, false)]
        public async Task ForwardJobAsync(HttpStatusCode statusCode, bool expected)
        {
            // Arrange
            var fakeResponse = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("TEST")
            };

            var handler = new FakeHandler(fakeResponse);
            var httpClient = new HttpClient(handler);
            var cancellationToken = CancellationToken.None;

            var lanOptions = Options.Create(new LanSettings
            {
                BaseUrl = "http://localhost:5001/",
                JobUpdateEndpoint = "jobUpdate",
                JobStatusEndpoint = "jobStatus",
                SharedKey = "1234"
            });

            var fakeMessage = new Message
            {
                MessageId = "1",
                Payload = "Payload",
                LastUpdate = DateTime.UtcNow,
                Status = MessageStatus.Pending,
                Type = MessageType.JobUpdate,
                RetryCount = 0,
            };

            var logger = new Mock<ILogger<LanForwarder>>();

            var forwarder = new LanForwarder(httpClient, lanOptions, logger.Object);

            // Act
            var result = forwarder.ForwardJobAsync(fakeMessage, cancellationToken);

            // Assert
            Assert.Equal(expected, await result);
        }
    }
}
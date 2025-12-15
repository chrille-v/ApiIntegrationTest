using APIIntegration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApiIntegrationTest.Tests
{
    public class ConnectivityServiceTest
    {
        [Fact]
        public async Task ConnectivityService_IsOnline_Parallel()
        {
            var service = new ConnectivityService();
            int numTasks = 100;

            var tasks = new Task<bool>[numTasks];

            for (int i = 0; i < numTasks; i++)
            {
                tasks[i] = Task.Run(() => service.IsOnline);
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                Assert.True(result);
            }
        }

        [Fact]
        public void IsOnline_ShouldSetIsOnline_ToTrue()
        {
            var service = new ConnectivityService();
            var result = service.IsOnline;

            Assert.True(result);
        }

        [Fact]
        public void LastChecked_ShouldBeRecent()
        {
            var service = new ConnectivityService();
            var lastCheck = service.LastCheck;

            Assert.True((DateTime.UtcNow - lastCheck).TotalSeconds < 1);
        }

        [Fact]
        public void SetOffline_ShouldSetIsOnline_ToFalse()
        {
            var service = new ConnectivityService();
            service.SetOffline();
            Assert.False(service.IsOnline);
        }

        [Fact]
        public void SetOnline_ShouldSetIsOnline_ToTrue()
        {
            var service = new ConnectivityService();
            service.SetOnline();
            Assert.True(service.IsOnline);
        }

        [Fact]
        public void SetOnline_ShouldUpdate_LastCheck()
        {
            var service = new ConnectivityService();
            var before = service.LastCheck;

            service.SetOnline();
            var after = service.LastCheck;

            Assert.True(after > before);
        }

        [Fact]
        public void SetOffline_ShouldUpdate_LastCheck()
        {
            // Arrange
            var service = new ConnectivityService();
            var before = service.LastCheck;

            // Act
            service.SetOffline();
            var after = service.LastCheck;

            // Assert
            Assert.True(after > before);
        }
    }
}

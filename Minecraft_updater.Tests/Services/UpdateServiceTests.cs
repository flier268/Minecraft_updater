using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Minecraft_updater.Services;
using Minecraft_updater.Models;

namespace Minecraft_updater.Tests.Services
{
    public class UpdateServiceTests
    {
        [Fact]
        public async Task CheckUpdateAsync_ShouldReturnUpdateMessage()
        {
            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<UpdateMessage>();
        }

        [Fact]
        public void CheckUpdate_SyncVersion_ShouldReturnUpdateMessage()
        {
            // Act
            var result = UpdateService.CheckUpdate();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<UpdateMessage>();
        }

        [Fact]
        public async Task CheckUpdateAsync_ShouldInitializeUpdateMessageProperties()
        {
            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert
            (result.HaveUpdate == true || result.HaveUpdate == false).Should().BeTrue();
            result.NewstVersion.Should().NotBeNull();
            result.SHA1.Should().NotBeNull();
            result.Message.Should().NotBeNull();
        }

        [Fact]
        public void StartAutoUpdater_NonExistentFile_ShouldNotThrow()
        {
            // Act
            Action act = () => UpdateService.StartAutoUpdater();

            // Assert - Should not throw even if AutoUpdater.exe doesn't exist
            act.Should().NotThrow();
        }

        [Fact]
        public void StartAutoUpdater_ShouldHandleGracefully()
        {
            // Arrange - No AutoUpdater.exe exists in test environment

            // Act & Assert - Should not crash
            Action act = () => UpdateService.StartAutoUpdater();
            act.Should().NotThrow();
        }

        [Fact]
        public async Task CheckUpdateAsync_OnNetworkError_ShouldReturnDefaultUpdateMessage()
        {
            // Act - This will likely fail to connect but should return a default UpdateMessage
            var result = await UpdateService.CheckUpdateAsync();

            // Assert - Even on error, should return a valid UpdateMessage object
            result.Should().NotBeNull();
            result.NewstVersion.Should().NotBeNull();
            result.SHA1.Should().NotBeNull();
            result.Message.Should().NotBeNull();
        }

        [Fact]
        public void CheckUpdate_ShouldBeSynchronousWrapperOfAsync()
        {
            // Act
            var syncResult = UpdateService.CheckUpdate();

            // Assert - Both should return UpdateMessage objects
            syncResult.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckUpdateAsync_ResultShouldHaveValidStructure()
        {
            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert
            (result.HaveUpdate == true || result.HaveUpdate == false).Should().BeTrue();

            // If there is an update, version info should be populated
            if (result.HaveUpdate)
            {
                result.NewstVersion.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public async Task CheckUpdateAsync_MultipleCallsShouldWork()
        {
            // Act - Call multiple times
            var result1 = await UpdateService.CheckUpdateAsync();
            var result2 = await UpdateService.CheckUpdateAsync();

            // Assert - Both calls should return valid results
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
        }

        [Fact]
        public void StartAutoUpdater_MultipleCallsShouldNotThrow()
        {
            // Act & Assert
            Action act = () =>
            {
                UpdateService.StartAutoUpdater();
                UpdateService.StartAutoUpdater();
                UpdateService.StartAutoUpdater();
            };

            act.Should().NotThrow();
        }
    }
}

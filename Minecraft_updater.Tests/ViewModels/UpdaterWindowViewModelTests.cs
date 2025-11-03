using System;
using System.IO;
using Xunit;
using FluentAssertions;
using Minecraft_updater.ViewModels;

namespace Minecraft_updater.Tests.ViewModels
{
    public class UpdaterWindowViewModelTests : IDisposable
    {
        private readonly string _testDirectory;

        public UpdaterWindowViewModelTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            // Set base directory for testing
            AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", _testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Act
            var viewModel = new UpdaterWindowViewModel();

            // Assert
            viewModel.Should().NotBeNull();
            viewModel.ProgressMax.Should().Be(100);
            viewModel.ProgressValue.Should().Be(0);
            viewModel.ProgressText.Should().Be("目前進度：0/0");
            viewModel.UpdateInfoText.Should().NotBeNullOrEmpty();
            viewModel.LogMessages.Should().NotBeNull();
            viewModel.LogMessages.Should().BeEmpty();
        }

        [Fact]
        public void ProgressMax_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new UpdaterWindowViewModel();

            // Act
            viewModel.ProgressMax = 200;

            // Assert
            viewModel.ProgressMax.Should().Be(200);
        }

        [Fact]
        public void ProgressValue_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new UpdaterWindowViewModel();

            // Act
            viewModel.ProgressValue = 50;

            // Assert
            viewModel.ProgressValue.Should().Be(50);
        }

        [Fact]
        public void ProgressText_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new UpdaterWindowViewModel();

            // Act
            viewModel.ProgressText = "目前進度：10/20";

            // Assert
            viewModel.ProgressText.Should().Be("目前進度：10/20");
        }

        [Fact]
        public void UpdateInfoText_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new UpdaterWindowViewModel();

            // Act
            viewModel.UpdateInfoText = "Test info";

            // Assert
            viewModel.UpdateInfoText.Should().Be("Test info");
        }

        [Fact]
        public void LogMessages_InitialState_ShouldBeEmpty()
        {
            // Arrange
            var viewModel = new UpdaterWindowViewModel();

            // Assert
            viewModel.LogMessages.Should().BeEmpty();
        }

        [Fact]
        public void LogMessages_AddMessage_ShouldContainMessage()
        {
            // Arrange
            var viewModel = new UpdaterWindowViewModel();

            // Act
            viewModel.LogMessages.Add("Test message");

            // Assert
            viewModel.LogMessages.Should().Contain("Test message");
            viewModel.LogMessages.Count.Should().Be(1);
        }

        [Fact]
        public void ProgressMax_DefaultValue_ShouldBe100()
        {
            // Arrange
            var viewModel = new UpdaterWindowViewModel();

            // Assert
            viewModel.ProgressMax.Should().Be(100);
        }

        [Fact]
        public void ProgressValue_DefaultValue_ShouldBe0()
        {
            // Arrange
            var viewModel = new UpdaterWindowViewModel();

            // Assert
            viewModel.ProgressValue.Should().Be(0);
        }

        [Fact]
        public void UpdateInfoText_ShouldContainVersion()
        {
            // Arrange
            var viewModel = new UpdaterWindowViewModel();

            // Assert
            viewModel.UpdateInfoText.Should().Contain("Minecraft updater");
        }
    }
}

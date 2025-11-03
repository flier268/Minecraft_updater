using System;
using System.IO;
using Xunit;
using FluentAssertions;
using Minecraft_updater.Models;
using Minecraft_updater.ViewModels;

namespace Minecraft_updater.Tests.ViewModels
{
    public class UpdateSelfWindowViewModelTests : IDisposable
    {
        private readonly string _testDirectory;

        public UpdateSelfWindowViewModelTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
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
            // Arrange
            var updateMessage = new UpdateMessage
            {
                HaveUpdate = true,
                NewstVersion = "2.0.0",
                SHA1 = "ABC123",
                Message = "New update available"
            };

            // Act
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Assert
            viewModel.Should().NotBeNull();
            viewModel.NewVersion.Should().Be("2.0.0");
            viewModel.Message.Should().Be("New update available");
            viewModel.CurrentVersion.Should().NotBeNullOrEmpty();
            viewModel.UpdateButtonText.Should().Be("更新");
            viewModel.IsUpdateEnabled.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithEmptyMessage_ShouldHandleGracefully()
        {
            // Arrange
            var updateMessage = new UpdateMessage
            {
                HaveUpdate = false,
                NewstVersion = "",
                SHA1 = "",
                Message = ""
            };

            // Act
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Assert
            viewModel.NewVersion.Should().Be("");
            viewModel.Message.Should().Be("");
        }

        [Fact]
        public void CurrentVersion_ShouldNotBeEmpty()
        {
            // Arrange
            var updateMessage = new UpdateMessage
            {
                NewstVersion = "1.0.0"
            };

            // Act
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Assert
            viewModel.CurrentVersion.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void NewVersion_ShouldMatchUpdateMessage()
        {
            // Arrange
            var updateMessage = new UpdateMessage
            {
                NewstVersion = "3.5.7"
            };

            // Act
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Assert
            viewModel.NewVersion.Should().Be("3.5.7");
        }

        [Fact]
        public void Message_ShouldMatchUpdateMessage()
        {
            // Arrange
            var updateMessage = new UpdateMessage
            {
                Message = "This is a test update message"
            };

            // Act
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Assert
            viewModel.Message.Should().Be("This is a test update message");
        }

        [Fact]
        public void UpdateButtonText_DefaultValue_ShouldBe更新()
        {
            // Arrange
            var updateMessage = new UpdateMessage();

            // Act
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Assert
            viewModel.UpdateButtonText.Should().Be("更新");
        }

        [Fact]
        public void IsUpdateEnabled_DefaultValue_ShouldBeTrue()
        {
            // Arrange
            var updateMessage = new UpdateMessage();

            // Act
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Assert
            viewModel.IsUpdateEnabled.Should().BeTrue();
        }

        [Fact]
        public void CancelCommand_ShouldRaiseUpdateCancelledEvent()
        {
            // Arrange
            var updateMessage = new UpdateMessage();
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);
            var eventRaised = false;

            viewModel.UpdateCancelled += (sender, args) => eventRaised = true;

            // Act
            viewModel.CancelCommand.Execute(null);

            // Assert
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void UpdateButtonText_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var updateMessage = new UpdateMessage();
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Act
            viewModel.UpdateButtonText = "下載中...";

            // Assert
            viewModel.UpdateButtonText.Should().Be("下載中...");
        }

        [Fact]
        public void IsUpdateEnabled_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var updateMessage = new UpdateMessage();
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Act
            viewModel.IsUpdateEnabled = false;

            // Assert
            viewModel.IsUpdateEnabled.Should().BeFalse();
        }

        [Fact]
        public void Message_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var updateMessage = new UpdateMessage();
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Act
            viewModel.Message = "Updated message";

            // Assert
            viewModel.Message.Should().Be("Updated message");
        }

        [Fact]
        public void Constructor_WithChineseMessage_ShouldHandleCorrectly()
        {
            // Arrange
            var updateMessage = new UpdateMessage
            {
                NewstVersion = "1.2.3",
                Message = "新版本已發布，包含錯誤修正和新功能"
            };

            // Act
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Assert
            viewModel.Message.Should().Be("新版本已發布，包含錯誤修正和新功能");
        }

        [Fact]
        public void Constructor_WithLongVersionString_ShouldHandleCorrectly()
        {
            // Arrange
            var updateMessage = new UpdateMessage
            {
                NewstVersion = "10.20.30.40"
            };

            // Act
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Assert
            viewModel.NewVersion.Should().Be("10.20.30.40");
        }

        [Fact]
        public void UpdateCancelledEvent_MultipleSubscribers_ShouldNotifyAll()
        {
            // Arrange
            var updateMessage = new UpdateMessage();
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);
            var count = 0;

            viewModel.UpdateCancelled += (sender, args) => count++;
            viewModel.UpdateCancelled += (sender, args) => count++;

            // Act
            viewModel.CancelCommand.Execute(null);

            // Assert
            count.Should().Be(2);
        }

        [Fact]
        public void Constructor_WithSpecialCharactersInMessage_ShouldHandleCorrectly()
        {
            // Arrange
            var updateMessage = new UpdateMessage
            {
                Message = "Update <v2.0> available! @#$%"
            };

            // Act
            var viewModel = new UpdateSelfWindowViewModel(updateMessage);

            // Assert
            viewModel.Message.Should().Be("Update <v2.0> available! @#$%");
        }
    }
}

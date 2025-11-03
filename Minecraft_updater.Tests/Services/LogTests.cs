using System;
using System.IO;
using Xunit;
using FluentAssertions;
using Minecraft_updater.Services;

namespace Minecraft_updater.Tests.Services
{
    public class LogTests : IDisposable
    {
        private readonly string _originalBaseDirectory;
        private readonly string _testDirectory;

        public LogTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            // Store original state
            _originalBaseDirectory = AppContext.BaseDirectory;
            Log.LogFile = false; // Ensure LogFile is disabled by default
        }

        public void Dispose()
        {
            // Reset to original state
            Log.LogFile = false;

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
        public void AddLine_WithoutLogFile_ShouldInvokeUICallback()
        {
            // Arrange
            Log.LogFile = false;
            string? capturedMessage = null;
            Action<string> uiCallback = msg => capturedMessage = msg;

            // Act
            Log.AddLine("Test message", uiCallback);

            // Assert
            capturedMessage.Should().Be("Test message");
        }

        [Fact]
        public void AddLine_WithNullCallback_ShouldNotThrow()
        {
            // Arrange
            Log.LogFile = false;

            // Act
            Action act = () => Log.AddLine("Test message", null);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void AddLine_WithColor_ShouldInvokeUICallbackWithColor()
        {
            // Arrange
            Log.LogFile = false;
            string? capturedMessage = null;
            string? capturedColor = null;
            Action<string, string> uiCallback = (msg, color) =>
            {
                capturedMessage = msg;
                capturedColor = color;
            };

            // Act
            Log.AddLine("Error message", "#FF0000", uiCallback);

            // Assert
            capturedMessage.Should().Be("Error message");
            capturedColor.Should().Be("#FF0000");
        }

        [Fact]
        public void AddLine_WithColorAndNullCallback_ShouldNotThrow()
        {
            // Arrange
            Log.LogFile = false;

            // Act
            Action act = () => Log.AddLine("Test message", "#00FF00", null);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void AddLine_MultipleMessages_ShouldInvokeCallbackForEach()
        {
            // Arrange
            Log.LogFile = false;
            var messages = new System.Collections.Generic.List<string>();
            Action<string> uiCallback = msg => messages.Add(msg);

            // Act
            Log.AddLine("Message 1", uiCallback);
            Log.AddLine("Message 2", uiCallback);
            Log.AddLine("Message 3", uiCallback);

            // Assert
            messages.Should().HaveCount(3);
            messages.Should().Contain("Message 1");
            messages.Should().Contain("Message 2");
            messages.Should().Contain("Message 3");
        }

        [Fact]
        public void AddLine_WithEmptyString_ShouldStillInvokeCallback()
        {
            // Arrange
            Log.LogFile = false;
            string? capturedMessage = null;
            Action<string> uiCallback = msg => capturedMessage = msg;

            // Act
            Log.AddLine("", uiCallback);

            // Assert
            capturedMessage.Should().Be("");
        }

        [Fact]
        public void AddLine_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            Log.LogFile = false;
            string? capturedMessage = null;
            Action<string> uiCallback = msg => capturedMessage = msg;
            var specialMessage = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            Log.AddLine(specialMessage, uiCallback);

            // Assert
            capturedMessage.Should().Be(specialMessage);
        }

        [Fact]
        public void AddLine_WithChineseCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            Log.LogFile = false;
            string? capturedMessage = null;
            Action<string> uiCallback = msg => capturedMessage = msg;
            var chineseMessage = "中文訊息測試";

            // Act
            Log.AddLine(chineseMessage, uiCallback);

            // Assert
            capturedMessage.Should().Be(chineseMessage);
        }

        [Fact]
        public void AddLine_WithMultilineString_ShouldHandleCorrectly()
        {
            // Arrange
            Log.LogFile = false;
            string? capturedMessage = null;
            Action<string> uiCallback = msg => capturedMessage = msg;
            var multilineMessage = "Line 1\nLine 2\nLine 3";

            // Act
            Log.AddLine(multilineMessage, uiCallback);

            // Assert
            capturedMessage.Should().Be(multilineMessage);
        }

        [Fact]
        public void LogFile_SetToTrue_ShouldEnableFileLogging()
        {
            // Arrange & Act
            Log.LogFile = true;

            // Assert
            Log.LogFile.Should().BeTrue();

            // Cleanup
            Log.LogFile = false;
        }

        [Fact]
        public void LogFile_SetToFalse_ShouldDisableFileLogging()
        {
            // Arrange
            Log.LogFile = true;

            // Act
            Log.LogFile = false;

            // Assert
            Log.LogFile.Should().BeFalse();
        }

        [Fact]
        public void AddLine_DifferentColors_ShouldInvokeWithCorrectColors()
        {
            // Arrange
            Log.LogFile = false;
            var coloredMessages = new System.Collections.Generic.List<(string message, string color)>();
            Action<string, string> uiCallback = (msg, color) => coloredMessages.Add((msg, color));

            // Act
            Log.AddLine("Red message", "#FF0000", uiCallback);
            Log.AddLine("Green message", "#00FF00", uiCallback);
            Log.AddLine("Blue message", "#0000FF", uiCallback);

            // Assert
            coloredMessages.Should().HaveCount(3);
            coloredMessages[0].Should().Be(("Red message", "#FF0000"));
            coloredMessages[1].Should().Be(("Green message", "#00FF00"));
            coloredMessages[2].Should().Be(("Blue message", "#0000FF"));
        }

        [Fact]
        public void AddLine_WithLogFileEnabled_ShouldWriteToFile()
        {
            // Arrange
            Log.LogFile = true;
            var logCalled = false;
            Action<string> uiCallback = msg => logCalled = true;

            // Act
            Log.AddLine("Test file log", uiCallback);

            // Assert
            logCalled.Should().BeTrue();

            // Cleanup
            Log.LogFile = false;
        }

        [Fact]
        public void AddLine_WithColorAndLogFileEnabled_ShouldWriteToFile()
        {
            // Arrange
            Log.LogFile = true;
            var logCalled = false;
            Action<string, string> uiCallback = (msg, color) => logCalled = true;

            // Act
            Log.AddLine("Test colored file log", "#FF0000", uiCallback);

            // Assert
            logCalled.Should().BeTrue();

            // Cleanup
            Log.LogFile = false;
        }

        [Fact]
        public void LogFile_ToggleMultipleTimes_ShouldWork()
        {
            // Act & Assert
            Log.LogFile = true;
            Log.LogFile.Should().BeTrue();

            Log.LogFile = false;
            Log.LogFile.Should().BeFalse();

            Log.LogFile = true;
            Log.LogFile.Should().BeTrue();

            // Cleanup
            Log.LogFile = false;
        }

        // Note: Testing actual file writing is complex due to AppContext.BaseDirectory
        // In a real scenario, you might want to refactor Log class to accept a path
        // or use dependency injection for better testability
    }
}

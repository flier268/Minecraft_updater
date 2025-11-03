using Xunit;
using FluentAssertions;
using Minecraft_updater.Models;

namespace Minecraft_updater.Tests.Models
{
    public class UpdateMessageTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var message = new UpdateMessage();

            // Assert
            message.HaveUpdate.Should().BeFalse();
            message.NewstVersion.Should().Be(string.Empty);
            message.SHA1.Should().Be(string.Empty);
            message.Message.Should().Be(string.Empty);
        }

        [Fact]
        public void HaveUpdate_SetValue_ShouldReturnCorrectValue()
        {
            // Arrange
            var message = new UpdateMessage();

            // Act
            message.HaveUpdate = true;

            // Assert
            message.HaveUpdate.Should().BeTrue();
        }

        [Fact]
        public void NewstVersion_SetValue_ShouldReturnCorrectValue()
        {
            // Arrange
            var message = new UpdateMessage();

            // Act
            message.NewstVersion = "1.2.3";

            // Assert
            message.NewstVersion.Should().Be("1.2.3");
        }

        [Fact]
        public void SHA1_SetValue_ShouldReturnCorrectValue()
        {
            // Arrange
            var message = new UpdateMessage();

            // Act
            message.SHA1 = "ABC123DEF456";

            // Assert
            message.SHA1.Should().Be("ABC123DEF456");
        }

        [Fact]
        public void Message_SetValue_ShouldReturnCorrectValue()
        {
            // Arrange
            var message = new UpdateMessage();

            // Act
            message.Message = "Update available";

            // Assert
            message.Message.Should().Be("Update available");
        }

        [Fact]
        public void UpdateMessage_SetAllProperties_ShouldRetainValues()
        {
            // Arrange
            var message = new UpdateMessage();

            // Act
            message.HaveUpdate = true;
            message.NewstVersion = "2.0.0";
            message.SHA1 = "SHA1HASH123456";
            message.Message = "New version available with bug fixes";

            // Assert
            message.HaveUpdate.Should().BeTrue();
            message.NewstVersion.Should().Be("2.0.0");
            message.SHA1.Should().Be("SHA1HASH123456");
            message.Message.Should().Be("New version available with bug fixes");
        }

        [Fact]
        public void UpdateMessage_MultipleInstances_ShouldBeIndependent()
        {
            // Arrange
            var message1 = new UpdateMessage { HaveUpdate = true, NewstVersion = "1.0" };
            var message2 = new UpdateMessage { HaveUpdate = false, NewstVersion = "2.0" };

            // Assert
            message1.HaveUpdate.Should().BeTrue();
            message1.NewstVersion.Should().Be("1.0");
            message2.HaveUpdate.Should().BeFalse();
            message2.NewstVersion.Should().Be("2.0");
        }
    }
}

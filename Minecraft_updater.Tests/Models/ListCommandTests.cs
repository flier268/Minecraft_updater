using Xunit;
using FluentAssertions;
using Minecraft_updater.Models;

namespace Minecraft_updater.Tests.Models
{
    public class ListCommandTests
    {
        [Fact]
        public void CheckUpdate_ShouldHaveCorrectValue()
        {
            // Assert
            ListCommand.CheckUpdate.Should().Be("/Check_Update");
        }

        [Fact]
        public void UpdatepackMaker_ShouldHaveCorrectValue()
        {
            // Assert
            ListCommand.UpdatepackMaker.Should().Be("/updatepackMaker");
        }

        [Fact]
        public void CheckUpdaterVersion_ShouldHaveCorrectValue()
        {
            // Assert
            ListCommand.CheckUpdaterVersion.Should().Be("/Check_updaterVersion");
        }

        [Fact]
        public void AllCommands_ShouldStartWithSlash()
        {
            // Assert
            ListCommand.CheckUpdate.Should().StartWith("/");
            ListCommand.UpdatepackMaker.Should().StartWith("/");
            ListCommand.CheckUpdaterVersion.Should().StartWith("/");
        }

        [Fact]
        public void AllCommands_ShouldNotBeEmpty()
        {
            // Assert
            ListCommand.CheckUpdate.Should().NotBeNullOrEmpty();
            ListCommand.UpdatepackMaker.Should().NotBeNullOrEmpty();
            ListCommand.CheckUpdaterVersion.Should().NotBeNullOrEmpty();
        }
    }
}

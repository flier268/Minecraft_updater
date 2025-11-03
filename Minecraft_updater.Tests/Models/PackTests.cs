using Xunit;
using FluentAssertions;
using Minecraft_updater.Models;

namespace Minecraft_updater.Tests.Models
{
    public class PackTests
    {
        [Fact]
        public void Resolve_NormalFormat_ShouldParseCorrectly()
        {
            // Arrange
            var input = "mods/example.jar||ABC123||https://example.com/file.jar";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.Path.Should().Be("mods/example.jar");
            result.MD5.Should().Be("ABC123");
            result.URL.Should().Be("https://example.com/file.jar");
            result.Delete.Should().BeFalse();
            result.DownloadWhenNotExist.Should().BeFalse();
            result.IsChecked.Should().BeFalse();
        }

        [Fact]
        public void Resolve_DeleteFormat_ShouldSetDeleteFlag()
        {
            // Arrange
            var input = "#mods/old.jar||DEF456||";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.Path.Should().Be("mods/old.jar");
            result.MD5.Should().Be("DEF456");
            result.URL.Should().Be("");
            result.Delete.Should().BeTrue();
            result.DownloadWhenNotExist.Should().BeFalse();
        }

        [Fact]
        public void Resolve_DownloadWhenNotExistFormat_ShouldSetFlag()
        {
            // Arrange
            var input = ":config/settings.cfg||GHI789||https://example.com/config.cfg";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.Path.Should().Be("config/settings.cfg");
            result.MD5.Should().Be("GHI789");
            result.URL.Should().Be("https://example.com/config.cfg");
            result.Delete.Should().BeFalse();
            result.DownloadWhenNotExist.Should().BeTrue();
        }

        [Fact]
        public void Resolve_EmptyMD5AndURL_ShouldReturnEmptyStrings()
        {
            // Arrange
            var input = "test/path.txt||||";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.Path.Should().Be("test/path.txt");
            result.MD5.Should().Be("");
            result.URL.Should().Be("");
        }

        [Fact]
        public void Resolve_PathWithSlashes_ShouldPreserveSlashes()
        {
            // Arrange
            var input = "folder/subfolder/file.dat||MD5HASH||http://url.com/file.dat";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.Path.Should().Be("folder/subfolder/file.dat");
        }

        [Fact]
        public void Resolve_InvalidFormat_ShouldReturnEmptyPack()
        {
            // Arrange
            var input = "invalid format without separators";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.Path.Should().BeNull();
            result.MD5.Should().BeNull();
            result.URL.Should().BeNull();
        }

        [Fact]
        public void Resolve_EmptyString_ShouldReturnEmptyPack()
        {
            // Arrange
            var input = "";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.Path.Should().BeNull();
        }

        [Fact]
        public void Resolve_OnlyOneSeparator_ShouldReturnEmptyPack()
        {
            // Arrange
            var input = "path||md5";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.Path.Should().BeNull();
        }

        [Fact]
        public void Resolve_ComplexURL_ShouldParseCorrectly()
        {
            // Arrange
            var input = "mods/mod.jar||ABCDEF||https://example.com/path/to/file.jar?version=1.0&dl=true";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.URL.Should().Be("https://example.com/path/to/file.jar?version=1.0&dl=true");
        }

        [Fact]
        public void Resolve_ChineseCharactersInPath_ShouldParseCorrectly()
        {
            // Arrange
            var input = "模組/測試.jar||MD5||https://example.com/file.jar";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.Path.Should().Be("模組/測試.jar");
        }

        [Fact]
        public void Resolve_DeleteWithComplexPath_ShouldWork()
        {
            // Arrange
            var input = "#folder/sub/file.txt||HASH||";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.Delete.Should().BeTrue();
            result.Path.Should().Be("folder/sub/file.txt");
        }

        [Fact]
        public void Resolve_DownloadWhenNotExistWithComplexURL_ShouldWork()
        {
            // Arrange
            var input = ":resources/data.json||12345||https://cdn.example.com/v2/data.json?key=abc";

            // Act
            var result = Packs.Resolve(input);

            // Assert
            result.DownloadWhenNotExist.Should().BeTrue();
            result.URL.Should().Contain("?key=abc");
        }
    }
}

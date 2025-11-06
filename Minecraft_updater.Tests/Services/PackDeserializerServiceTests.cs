using FluentAssertions;
using Minecraft_updater.Models;
using Minecraft_updater.Services;

namespace Minecraft_updater.Tests.Services
{
    public class PackDeserializerServiceTests
    {
        private readonly PackDeserializerService _deserializer;

        public PackDeserializerServiceTests()
        {
            _deserializer = new PackDeserializerService();
        }

        [Fact]
        public void DeserializeLine_NormalLine_ReturnsPack()
        {
            // Arrange
            var line = "mods/Botania-1.20.jar||abc123def456||http://example.com/Botania-1.20.jar";

            // Act
            var result = _deserializer.DeserializeLine(line);

            // Assert
            result.Should().NotBeNull();
            result!.Value.Path.Should().Be("mods/Botania-1.20.jar");
            result.Value.SHA256.Should().Be("abc123def456");
            result.Value.URL.Should().Be("http://example.com/Botania-1.20.jar");
            result.Value.Delete.Should().BeFalse();
            result.Value.DownloadWhenNotExist.Should().BeFalse();
        }

        [Fact]
        public void DeserializeLine_DeletePrefix_SetsDeleteFlag()
        {
            // Arrange
            var line = "#mods/OldMod||xyz789||";

            // Act
            var result = _deserializer.DeserializeLine(line);

            // Assert
            result.Should().NotBeNull();
            result!.Value.Path.Should().Be("mods/OldMod");
            result.Value.SHA256.Should().Be("xyz789");
            result.Value.Delete.Should().BeTrue();
            result.Value.DownloadWhenNotExist.Should().BeFalse();
        }

        [Fact]
        public void DeserializeLine_ColonPrefix_SetsDownloadWhenNotExistFlag()
        {
            // Arrange
            var line = ":config/optional.cfg||789ghi||http://example.com/optional.cfg";

            // Act
            var result = _deserializer.DeserializeLine(line);

            // Assert
            result.Should().NotBeNull();
            result!.Value.Path.Should().Be("config/optional.cfg");
            result.Value.SHA256.Should().Be("789ghi");
            result.Value.URL.Should().Be("http://example.com/optional.cfg");
            result.Value.Delete.Should().BeFalse();
            result.Value.DownloadWhenNotExist.Should().BeTrue();
        }

        [Fact]
        public void DeserializeLine_WithMinimumVersion_SetsMinimumVersion()
        {
            // Arrange
            var line = "mods/TestMod.jar||hash||http://url";
            var minVersion = "1.2.3";

            // Act
            var result = _deserializer.DeserializeLine(line, minVersion);

            // Assert
            result.Should().NotBeNull();
            result!.Value.MinimumVersion.Should().Be("1.2.3");
        }

        [Fact]
        public void DeserializeLine_EmptyString_ReturnsNull()
        {
            // Act
            var result = _deserializer.DeserializeLine("");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void DeserializeLine_WhitespaceOnly_ReturnsNull()
        {
            // Act
            var result = _deserializer.DeserializeLine("   ");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void DeserializeLine_MalformedLine_ReturnsNull()
        {
            // Act
            var result = _deserializer.DeserializeLine("invalid line without delimiters");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void DeserializeLine_OnlyOneDelimiter_ReturnsNull()
        {
            // Act
            var result = _deserializer.DeserializeLine("path||md5");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void DeserializeFile_NewMinVersionFormat_ExtractsVersion()
        {
            // Arrange
            var content = @"MinVersion=1.2.3
mods/TestMod.jar||abc123||http://example.com/mod.jar";

            // Act
            var (packs, minVersion) = _deserializer.DeserializeFile(content);

            // Assert
            minVersion.Should().Be("1.2.3");
            packs.Should().HaveCount(1);
            packs[0].MinimumVersion.Should().Be("1.2.3");
        }

        [Fact]
        public void DeserializeFile_OldMinVersionFormat_ExtractsVersion()
        {
            // Arrange
            var content = @"MinVersion||1.2.3||
mods/TestMod.jar||abc123||http://example.com/mod.jar";

            // Act
            var (packs, minVersion) = _deserializer.DeserializeFile(content);

            // Assert
            minVersion.Should().Be("1.2.3");
            packs.Should().HaveCount(1);
            packs[0].MinimumVersion.Should().Be("1.2.3");
        }

        [Fact]
        public void DeserializeFile_OldMinVersionFormatTwoParts_ExtractsVersion()
        {
            // Arrange
            var content = @"MinVersion||1.2.3
mods/TestMod.jar||abc123||http://example.com/mod.jar";

            // Act
            var (packs, minVersion) = _deserializer.DeserializeFile(content);

            // Assert
            minVersion.Should().Be("1.2.3");
        }

        [Fact]
        public void DeserializeFile_MinimumVersionAlias_ExtractsVersion()
        {
            // Arrange
            var content = @"MinimumVersion=2.0.0
mods/TestMod.jar||abc123||http://example.com/mod.jar";

            // Act
            var (packs, minVersion) = _deserializer.DeserializeFile(content);

            // Assert
            minVersion.Should().Be("2.0.0");
        }

        [Fact]
        public void DeserializeFile_NoVersion_VersionIsNull()
        {
            // Arrange
            var content = @"mods/Mod1.jar||hash1||http://url1
mods/Mod2.jar||hash2||http://url2";

            // Act
            var (packs, minVersion) = _deserializer.DeserializeFile(content);

            // Assert
            minVersion.Should().BeNull();
            packs.Should().HaveCount(2);
        }

        [Fact]
        public void DeserializeFile_MultiplePacks_AllParsed()
        {
            // Arrange
            var content = @"MinVersion=1.0.0
mods/Mod1.jar||md51||http://url1
#mods/OldMod||md52||
:config/opt.cfg||md53||http://url3";

            // Act
            var (packs, minVersion) = _deserializer.DeserializeFile(content);

            // Assert
            packs.Should().HaveCount(3);

            packs[0].Path.Should().Be("mods/Mod1.jar");
            packs[0].Delete.Should().BeFalse();
            packs[0].DownloadWhenNotExist.Should().BeFalse();

            packs[1].Path.Should().Be("mods/OldMod");
            packs[1].Delete.Should().BeTrue();

            packs[2].Path.Should().Be("config/opt.cfg");
            packs[2].DownloadWhenNotExist.Should().BeTrue();

            // All should have the minimum version
            packs.Should().AllSatisfy(p => p.MinimumVersion.Should().Be("1.0.0"));
        }

        [Fact]
        public void DeserializeFile_EmptyContent_ReturnsEmptyList()
        {
            // Act
            var (packs, minVersion) = _deserializer.DeserializeFile("");

            // Assert
            packs.Should().BeEmpty();
            minVersion.Should().BeNull();
        }

        [Fact]
        public void DeserializeFile_OnlyWhitespace_ReturnsEmptyList()
        {
            // Act
            var (packs, minVersion) = _deserializer.DeserializeFile("   \n  \r\n  ");

            // Assert
            packs.Should().BeEmpty();
            minVersion.Should().BeNull();
        }

        [Fact]
        public void DeserializeFile_SkipsEmptyLines()
        {
            // Arrange
            var content = @"mods/Mod1.jar||hash1||url1

mods/Mod2.jar||hash2||url2

";

            // Act
            var (packs, minVersion) = _deserializer.DeserializeFile(content);

            // Assert
            packs.Should().HaveCount(2);
        }

        [Fact]
        public void DeserializeFile_SkipsMalformedLines()
        {
            // Arrange
            var content = @"mods/ValidMod.jar||hash||url
this is invalid line
mods/AnotherMod.jar||hash2||url2";

            // Act
            var (packs, minVersion) = _deserializer.DeserializeFile(content);

            // Assert
            packs.Should().HaveCount(2);
            packs[0].Path.Should().Be("mods/ValidMod.jar");
            packs[1].Path.Should().Be("mods/AnotherMod.jar");
        }

        [Fact]
        public void DeserializeFile_CaseInsensitiveMinVersion_Works()
        {
            // Arrange
            var content = @"minversion=1.5.0
mods/Mod.jar||hash||url";

            // Act
            var (packs, minVersion) = _deserializer.DeserializeFile(content);

            // Assert
            minVersion.Should().Be("1.5.0");
        }

        [Fact]
        public void DeserializeFile_DifferentLineEndings_ParsesCorrectly()
        {
            // Arrange - Windows line endings
            var contentWindows = "MinVersion=1.0.0\r\nmods/Mod.jar||hash||url\r\n";
            var contentUnix = "MinVersion=1.0.0\nmods/Mod.jar||hash||url\n";
            var contentMac = "MinVersion=1.0.0\rmods/Mod.jar||hash||url\r";

            // Act
            var resultWindows = _deserializer.DeserializeFile(contentWindows);
            var resultUnix = _deserializer.DeserializeFile(contentUnix);
            var resultMac = _deserializer.DeserializeFile(contentMac);

            // Assert
            resultWindows.Packs.Should().HaveCount(1);
            resultUnix.Packs.Should().HaveCount(1);
            resultMac.Packs.Should().HaveCount(1);

            resultWindows.MinimumVersion.Should().Be("1.0.0");
            resultUnix.MinimumVersion.Should().Be("1.0.0");
            resultMac.MinimumVersion.Should().Be("1.0.0");
        }

        [Fact]
        public void DeserializeLine_PathWithSpecialChars_PreservesPath()
        {
            // Arrange
            var line = "mods/Special-Mod_v1.0+build.jar||hash||url";

            // Act
            var result = _deserializer.DeserializeLine(line);

            // Assert
            result!.Value.Path.Should().Be("mods/Special-Mod_v1.0+build.jar");
        }

        [Fact]
        public void DeserializeLine_URLWithQueryParams_PreservesURL()
        {
            // Arrange
            var line = "mods/Mod.jar||hash||http://example.com/file.jar?version=1.2&build=latest";

            // Act
            var result = _deserializer.DeserializeLine(line);

            // Assert
            result!.Value.URL.Should().Be("http://example.com/file.jar?version=1.2&build=latest");
        }
    }
}

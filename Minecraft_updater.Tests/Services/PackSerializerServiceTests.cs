using FluentAssertions;
using Minecraft_updater.Models;
using Minecraft_updater.Services;

namespace Minecraft_updater.Tests.Services
{
    public class PackSerializerServiceTests
    {
        private readonly PackSerializerService _serializer;

        public PackSerializerServiceTests()
        {
            _serializer = new PackSerializerService();
        }

        [Fact]
        public void SerializeLine_NormalPack_ReturnsCorrectFormat()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "mods/Botania-1.20.jar",
                MD5 = "abc123def456",
                URL = "http://example.com/Botania-1.20.jar",
                Delete = false,
                DownloadWhenNotExist = false
            };

            // Act
            var result = _serializer.SerializeLine(pack);

            // Assert
            result.Should().Be("mods/Botania-1.20.jar||abc123def456||http://example.com/Botania-1.20.jar");
        }

        [Fact]
        public void SerializeLine_DeletePack_HasHashPrefix()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "mods/OldMod",
                MD5 = "xyz789",
                URL = "",
                Delete = true,
                DownloadWhenNotExist = false
            };

            // Act
            var result = _serializer.SerializeLine(pack);

            // Assert
            result.Should().Be("#mods/OldMod||xyz789||");
        }

        [Fact]
        public void SerializeLine_DownloadWhenNotExistPack_HasColonPrefix()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "config/optional.cfg",
                MD5 = "789ghi",
                URL = "http://example.com/optional.cfg",
                Delete = false,
                DownloadWhenNotExist = true
            };

            // Act
            var result = _serializer.SerializeLine(pack);

            // Assert
            result.Should().Be(":config/optional.cfg||789ghi||http://example.com/optional.cfg");
        }

        [Fact]
        public void SerializeLine_EmptyPath_ReturnsDelimiterOnly()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "",
                MD5 = "",
                URL = ""
            };

            // Act
            var result = _serializer.SerializeLine(pack);

            // Assert
            result.Should().Be("||||");
        }

        [Fact]
        public void SerializeFile_WithMinVersion_IncludesVersionHeader()
        {
            // Arrange
            var packs = new[]
            {
                new Pack
                {
                    Path = "mods/TestMod.jar",
                    MD5 = "abc123",
                    URL = "http://example.com/TestMod.jar"
                }
            };

            // Act
            var result = _serializer.SerializeFile(packs, "1.2.3");

            // Assert
            result.Should().StartWith("MinVersion=1.2.3");
            result.Should().Contain("mods/TestMod.jar||abc123||http://example.com/TestMod.jar");
        }

        [Fact]
        public void SerializeFile_WithoutMinVersion_NoVersionHeader()
        {
            // Arrange
            var packs = new[]
            {
                new Pack
                {
                    Path = "mods/TestMod.jar",
                    MD5 = "abc123",
                    URL = "http://example.com/TestMod.jar"
                }
            };

            // Act
            var result = _serializer.SerializeFile(packs, null);

            // Assert
            result.Should().NotContain("MinVersion");
            result.Should().StartWith("mods/TestMod.jar");
        }

        [Fact]
        public void SerializeFile_MultiplePacks_AllIncluded()
        {
            // Arrange
            var packs = new[]
            {
                new Pack { Path = "mods/Mod1.jar", MD5 = "md51", URL = "http://url1" },
                new Pack
                {
                    Path = "mods/OldMod",
                    MD5 = "md52",
                    URL = "",
                    Delete = true
                },
                new Pack
                {
                    Path = "config/opt.cfg",
                    MD5 = "md53",
                    URL = "http://url3",
                    DownloadWhenNotExist = true
                }
            };

            // Act
            var result = _serializer.SerializeFile(packs, "1.0.0");

            // Assert
            var lines = result.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.RemoveEmptyEntries
            );
            lines.Should().HaveCount(4); // Version + 3 packs
            lines[0].Should().Be("MinVersion=1.0.0");
            lines[1].Should().Be("mods/Mod1.jar||md51||http://url1");
            lines[2].Should().Be("#mods/OldMod||md52||");
            lines[3].Should().Be(":config/opt.cfg||md53||http://url3");
        }

        [Fact]
        public void SerializeFile_EmptyCollection_ReturnsEmptyOrVersionOnly()
        {
            // Arrange
            var packs = Array.Empty<Pack>();

            // Act
            var result = _serializer.SerializeFile(packs, "1.0.0");

            // Assert
            result.Trim().Should().Be("MinVersion=1.0.0");
        }

        [Fact]
        public void SerializeFile_EmptyCollectionNoVersion_ReturnsEmpty()
        {
            // Arrange
            var packs = Array.Empty<Pack>();

            // Act
            var result = _serializer.SerializeFile(packs, null);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SerializeLine_PathWithSpecialCharacters_PreservesCharacters()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "mods/Special-Mod_v1.0+build.jar",
                MD5 = "hash123",
                URL = "http://example.com/file.jar"
            };

            // Act
            var result = _serializer.SerializeLine(pack);

            // Assert
            result.Should().Be("mods/Special-Mod_v1.0+build.jar||hash123||http://example.com/file.jar");
        }

        [Fact]
        public void SerializeLine_URLWithQueryParams_PreservesParams()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "mods/Mod.jar",
                MD5 = "hash",
                URL = "http://example.com/file.jar?version=1.2&build=latest"
            };

            // Act
            var result = _serializer.SerializeLine(pack);

            // Assert
            result.Should()
                .Be("mods/Mod.jar||hash||http://example.com/file.jar?version=1.2&build=latest");
        }
    }
}

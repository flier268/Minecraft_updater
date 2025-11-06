using FluentAssertions;
using Minecraft_updater.Models;
using Minecraft_updater.Services;

namespace Minecraft_updater.Tests.Integration
{
    /// <summary>
    /// 整合測試：確保序列化和反序列化是對稱操作
    /// 序列化 → 反序列化 → 再序列化 應該產生相同的輸出
    /// </summary>
    public class SerializeDeserializeTests
    {
        private readonly PackSerializerService _serializer;
        private readonly PackDeserializerService _deserializer;

        public SerializeDeserializeTests()
        {
            _serializer = new PackSerializerService();
            _deserializer = new PackDeserializerService();
        }

        [Fact]
        public void RoundTrip_SingleNormalPack_PreservesData()
        {
            // Arrange
            var originalPack = new Pack
            {
                Path = "mods/Botania-1.20.jar",
                SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                URL = "http://example.com/Botania-1.20.jar",
                Delete = false,
                DownloadWhenNotExist = false
            };

            // Act - Serialize
            var serialized = _serializer.SerializeLine(originalPack);

            // Act - Deserialize
            var deserialized = _deserializer.DeserializeLine(serialized);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Value.Path.Should().Be(originalPack.Path);
            deserialized.Value.SHA256.Should().Be(originalPack.SHA256);
            deserialized.Value.URL.Should().Be(originalPack.URL);
            deserialized.Value.Delete.Should().Be(originalPack.Delete);
            deserialized.Value.DownloadWhenNotExist.Should().Be(originalPack.DownloadWhenNotExist);
        }

        [Fact]
        public void RoundTrip_DeletePack_PreservesData()
        {
            // Arrange
            var originalPack = new Pack
            {
                Path = "mods/OldMod",
                SHA256 = "6ca13d52ca70c883e0f0bb101e425a89e8624de51db2d2392593af6a84118090",
                URL = "",
                Delete = true,
                DownloadWhenNotExist = false
            };

            // Act
            var serialized = _serializer.SerializeLine(originalPack);
            var deserialized = _deserializer.DeserializeLine(serialized);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Value.Path.Should().Be(originalPack.Path);
            deserialized.Value.Delete.Should().BeTrue();
        }

        [Fact]
        public void RoundTrip_DownloadWhenNotExistPack_PreservesData()
        {
            // Arrange
            var originalPack = new Pack
            {
                Path = "config/optional.cfg",
                SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                URL = "http://example.com/optional.cfg",
                Delete = false,
                DownloadWhenNotExist = true
            };

            // Act
            var serialized = _serializer.SerializeLine(originalPack);
            var deserialized = _deserializer.DeserializeLine(serialized);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Value.DownloadWhenNotExist.Should().BeTrue();
            deserialized.Value.URL.Should().Be(originalPack.URL);
        }

        [Fact]
        public void RoundTrip_CompleteFile_PreservesAllData()
        {
            // Arrange
            var originalPacks = new[]
            {
                new Pack
                {
                    Path = "mods/Mod1.jar",
                    SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    URL = "http://example.com/Mod1.jar"
                },
                new Pack
                {
                    Path = "mods/OldMod",
                    SHA256 = "6ca13d52ca70c883e0f0bb101e425a89e8624de51db2d2392593af6a84118090",
                    URL = "",
                    Delete = true
                },
                new Pack
                {
                    Path = "config/optional.cfg",
                    SHA256 = "d2a84f4b8b650937ec8f73cd8be2c74add5a911ba64df27458ed8229da804a26",
                    URL = "http://example.com/optional.cfg",
                    DownloadWhenNotExist = true
                }
            };
            var minVersion = "1.2.3";

            // Act - First serialization
            var serialized1 = _serializer.SerializeFile(originalPacks, minVersion);

            // Act - Deserialize
            var (deserializedPacks, deserializedVersion) = _deserializer.DeserializeFile(
                serialized1
            );

            // Act - Second serialization
            var serialized2 = _serializer.SerializeFile(deserializedPacks, deserializedVersion);

            // Assert - Compare two serializations
            serialized1.Should().Be(serialized2);

            // Assert - Verify deserialized data
            deserializedVersion.Should().Be(minVersion);
            deserializedPacks.Should().HaveCount(3);

            deserializedPacks[0].Path.Should().Be("mods/Mod1.jar");
            deserializedPacks[0].Delete.Should().BeFalse();

            deserializedPacks[1].Path.Should().Be("mods/OldMod");
            deserializedPacks[1].Delete.Should().BeTrue();

            deserializedPacks[2].Path.Should().Be("config/optional.cfg");
            deserializedPacks[2].DownloadWhenNotExist.Should().BeTrue();
        }

        [Fact]
        public void RoundTrip_FileWithoutVersion_PreservesData()
        {
            // Arrange
            var originalPacks = new[]
            {
                new Pack
                {
                    Path = "mods/Mod.jar",
                    SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    URL = "http://example.com/Mod.jar"
                }
            };

            // Act
            var serialized1 = _serializer.SerializeFile(originalPacks, null);
            var (deserializedPacks, deserializedVersion) = _deserializer.DeserializeFile(
                serialized1
            );
            var serialized2 = _serializer.SerializeFile(deserializedPacks, deserializedVersion);

            // Assert
            serialized1.Should().Be(serialized2);
            deserializedVersion.Should().BeNull();
        }

        [Fact]
        public void RoundTrip_PathWithSpecialCharacters_PreservesPath()
        {
            // Arrange
            var originalPack = new Pack
            {
                Path = "mods/Special-Mod_v1.0+build.jar",
                SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                URL = "http://example.com/Special-Mod_v1.0+build.jar"
            };

            // Act
            var serialized = _serializer.SerializeLine(originalPack);
            var deserialized = _deserializer.DeserializeLine(serialized);

            // Assert
            deserialized!.Value.Path.Should().Be(originalPack.Path);
        }

        [Fact]
        public void RoundTrip_URLWithQueryParams_PreservesURL()
        {
            // Arrange
            var originalPack = new Pack
            {
                Path = "mods/Mod.jar",
                SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                URL = "http://example.com/file.jar?version=1.2&build=latest"
            };

            // Act
            var serialized = _serializer.SerializeLine(originalPack);
            var deserialized = _deserializer.DeserializeLine(serialized);

            // Assert
            deserialized!.Value.URL.Should().Be(originalPack.URL);
        }

        [Fact]
        public void RoundTrip_MultipleIterations_RemainsStable()
        {
            // Arrange
            var originalPacks = new[]
            {
                new Pack
                {
                    Path = "mods/TestMod.jar",
                    SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    URL = "http://example.com/TestMod.jar"
                }
            };

            // Act - Multiple round trips
            var iteration1 = _serializer.SerializeFile(originalPacks, "1.0.0");
            var (packs1, version1) = _deserializer.DeserializeFile(iteration1);

            var iteration2 = _serializer.SerializeFile(packs1, version1);
            var (packs2, version2) = _deserializer.DeserializeFile(iteration2);

            var iteration3 = _serializer.SerializeFile(packs2, version2);

            // Assert - All iterations should be identical
            iteration1.Should().Be(iteration2);
            iteration2.Should().Be(iteration3);
        }

        [Fact]
        public void RoundTrip_BackwardCompatibility_OldMinVersionFormat()
        {
            // Arrange - Manually create old format file
            var oldFormatContent = @"MinVersion||1.5.0||
mods/Mod.jar||5d41402abc4b2a76b9719d911017c592||http://example.com/Mod.jar";

            // Act - Deserialize old format
            var (packs, version) = _deserializer.DeserializeFile(oldFormatContent);

            // Act - Serialize to new format
            var newFormatContent = _serializer.SerializeFile(packs, version);

            // Assert - Version should be preserved
            version.Should().Be("1.5.0");

            // Assert - New format should use = instead of ||
            newFormatContent.Should().Contain("MinVersion=1.5.0");
            newFormatContent.Should().NotContain("MinVersion||");

            // Act - Deserialize new format again
            var (packsAgain, versionAgain) = _deserializer.DeserializeFile(newFormatContent);

            // Assert - Data should be preserved
            versionAgain.Should().Be("1.5.0");
            packsAgain.Should().HaveCount(1);
            packsAgain[0].Path.Should().Be("mods/Mod.jar");
        }

        [Fact]
        public void RoundTrip_EmptyCollections_HandlesGracefully()
        {
            // Arrange
            var emptyPacks = Array.Empty<Pack>();

            // Act
            var serialized = _serializer.SerializeFile(emptyPacks, null);
            var (deserialized, version) = _deserializer.DeserializeFile(serialized);

            // Assert
            deserialized.Should().BeEmpty();
            version.Should().BeNull();
        }

        [Fact]
        public void RoundTrip_LargeCollection_PreservesOrder()
        {
            // Arrange - Create 100 packs
            var largePacks = Enumerable
                .Range(1, 100)
                .Select(i => new Pack
                {
                    Path = $"mods/Mod{i}.jar",
                    SHA256 = $"{i:D32}", // Pad to 32 characters
                    URL = $"http://example.com/Mod{i}.jar"
                })
                .ToArray();

            // Act
            var serialized = _serializer.SerializeFile(largePacks, "2.0.0");
            var (deserialized, version) = _deserializer.DeserializeFile(serialized);

            // Assert
            deserialized.Should().HaveCount(100);
            for (int i = 0; i < 100; i++)
            {
                deserialized[i].Path.Should().Be($"mods/Mod{i + 1}.jar");
            }
        }
    }
}

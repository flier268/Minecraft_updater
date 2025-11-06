using System;
using System.IO;
using FluentAssertions;
using Minecraft_updater.Models;
using Minecraft_updater.Services;
using Xunit;

namespace Minecraft_updater.Tests.Services
{
    public class ConfigurationPathResolverTests : IDisposable
    {
        private readonly string _tempDirectory;

        public ConfigurationPathResolverTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Fact]
        public void DetermineConfigPath_NoArgument_ShouldReturnDefaultIniPath()
        {
            // Act
            var path = ConfigurationPathResolver.DetermineConfigPath(
                Array.Empty<string>(),
                _tempDirectory
            );

            // Assert
            path.Should().Be(Path.Combine(_tempDirectory, ConfigurationPathResolver.DefaultFileName));
        }

        [Fact]
        public void DetermineConfigPath_WithConfigEqualsArgument_ShouldUseCustomValue()
        {
            // Arrange
            var customPath = Path.Combine(_tempDirectory, "custom.ini");

            // Act
            var path = ConfigurationPathResolver.DetermineConfigPath(
                new[] { "--config=" + customPath },
                _tempDirectory
            );

            // Assert
            path.Should().Be(customPath);
        }

        [Fact]
        public void DetermineConfigPath_WithSeparateArguments_ShouldUseNextValue()
        {
            // Arrange
            var custom = "custom.ini";

            // Act
            var path = ConfigurationPathResolver.DetermineConfigPath(
                new[] { "-c", custom },
                _tempDirectory
            );

            // Assert
            path.Should().Be(Path.Combine(_tempDirectory, custom));
        }

        [Fact]
        public void EnsureConfigurationFile_WithLegacyConfig_ShouldCopyWhenScUrlPresent()
        {
            // Arrange
            var legacyPath = Path.Combine(_tempDirectory, ConfigurationPathResolver.LegacyFileName);
            File.WriteAllLines(
                legacyPath,
                new[]
                {
                    "[Minecraft_updater]",
                    "scUrl=https://example.com/updatePackList.sc",
                }
            );

            var newPath = Path.Combine(
                _tempDirectory,
                ConfigurationPathResolver.DefaultFileName
            );

            // Act
            var result = ConfigurationPathResolver.EnsureConfigurationFile(newPath, _tempDirectory);

            // Assert
            result.Should().Be(newPath);
            File.Exists(newPath).Should().BeTrue();
        }

        [Fact]
        public void EnsureConfigurationFile_LegacyWithoutScUrl_ShouldNotCopy()
        {
            // Arrange
            var legacyPath = Path.Combine(_tempDirectory, ConfigurationPathResolver.LegacyFileName);
            File.WriteAllLines(
                legacyPath,
                new[]
                {
                    "[Minecraft_updater]",
                    "AutoClose_AfterFinishd=false",
                }
            );

            var newPath = Path.Combine(
                _tempDirectory,
                ConfigurationPathResolver.DefaultFileName
            );

            // Act
            var result = ConfigurationPathResolver.EnsureConfigurationFile(newPath, _tempDirectory);

            // Assert
            result.Should().Be(newPath);
            File.Exists(newPath).Should().BeFalse();
        }
    }
}

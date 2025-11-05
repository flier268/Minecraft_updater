using System;
using System.IO;
using FluentAssertions;
using Minecraft_updater.Models;
using Xunit;

namespace Minecraft_updater.Tests.Models
{
    public sealed class DownloadAuthenticationOptionsTests : IDisposable
    {
        private readonly string _tempDirectory;

        public DownloadAuthenticationOptionsTests()
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
        public void FromIni_WithBasicCredentials_ShouldPopulateProperties()
        {
            // Arrange
            var iniPath = CreateIniFile(
                "[Minecraft_updater]",
                "DownloadAuthType=Basic",
                "DownloadAuthUsername=tester",
                "DownloadAuthPassword=secret"
            );

            var ini = new IniFile(iniPath);

            // Act
            var options = DownloadAuthenticationOptions.FromIni(ini);

            // Assert
            options.Mode.Should().Be(DownloadAuthenticationMode.Basic);
            options.Username.Should().Be("tester");
            options.Password.Should().Be("secret");
            options.IsConfigured.Should().BeTrue();
        }

        [Fact]
        public void FromIni_MissingUsername_ShouldFallbackToNone()
        {
            // Arrange
            var iniPath = CreateIniFile(
                "[Minecraft_updater]",
                "DownloadAuthType=Basic",
                "DownloadAuthPassword=secret"
            );

            var ini = new IniFile(iniPath);

            // Act
            var options = DownloadAuthenticationOptions.FromIni(ini);

            // Assert
            options.Mode.Should().Be(DownloadAuthenticationMode.None);
            options.IsConfigured.Should().BeFalse();
        }

        [Fact]
        public void FromIni_WithApiKeyQuery_ShouldCaptureParameter()
        {
            // Arrange
            var iniPath = CreateIniFile(
                "[Minecraft_updater]",
                "DownloadAuthType=ApiKeyQuery",
                "DownloadAuthQueryName=token",
                "DownloadAuthQueryValue=abc123"
            );

            var ini = new IniFile(iniPath);

            // Act
            var options = DownloadAuthenticationOptions.FromIni(ini);

            // Assert
            options.Mode.Should().Be(DownloadAuthenticationMode.ApiKeyQuery);
            options.QueryParameterName.Should().Be("token");
            options.QueryParameterValue.Should().Be("abc123");
            options.IsConfigured.Should().BeTrue();
        }

        private string CreateIniFile(params string[] lines)
        {
            var path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.ini");
            File.WriteAllLines(path, lines);
            return path;
        }
    }
}

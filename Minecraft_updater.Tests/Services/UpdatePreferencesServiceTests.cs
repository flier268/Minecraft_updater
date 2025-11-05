using System;
using System.IO;
using FluentAssertions;
using Minecraft_updater.Models;
using Minecraft_updater.Services;
using Xunit;

namespace Minecraft_updater.Tests.Services
{
    public class UpdatePreferencesServiceTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly string _iniPath;
        private readonly IniFile _ini;
        private readonly UpdatePreferencesService _preferences;

        public UpdatePreferencesServiceTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            _iniPath = Path.Combine(_tempDirectory, "test.ini");
            File.WriteAllText(_iniPath, "[Minecraft_updater]\n");
            _ini = new IniFile(_iniPath);
            _preferences = new UpdatePreferencesService(_ini);
        }

        [Fact]
        public void SetSelfUpdateDisabled_ShouldPersistValue()
        {
            _preferences.SetSelfUpdateDisabled(true);
            _preferences.IsSelfUpdateDisabled.Should().BeTrue();

            _preferences.SetSelfUpdateDisabled(false);
            _preferences.IsSelfUpdateDisabled.Should().BeFalse();
        }

        [Fact]
        public void SetSkippedVersion_ShouldPersistVersion()
        {
            _preferences.SetSkippedVersion("1.2.3");
            _preferences.SkippedVersion.Should().Be("1.2.3");

            _preferences.SetSkippedVersion(null);
            _preferences.SkippedVersion.Should().BeNull();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
    }
}

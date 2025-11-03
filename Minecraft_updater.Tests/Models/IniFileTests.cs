using System;
using System.IO;
using Xunit;
using FluentAssertions;
using Minecraft_updater.Models;

namespace Minecraft_updater.Tests.Models
{
    public class IniFileTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _testFilePath;

        public IniFileTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _testFilePath = Path.Combine(_testDirectory, "test.ini");
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public void Constructor_FileDoesNotExist_ShouldNotThrow()
        {
            // Arrange & Act
            var iniFile = new IniFile(_testFilePath);

            // Assert
            iniFile.Should().NotBeNull();
            iniFile.Path.Should().Be(_testFilePath);
        }

        [Fact]
        public void IniWriteValue_NewSectionAndKey_ShouldCreateFile()
        {
            // Arrange
            var iniFile = new IniFile(_testFilePath);

            // Act
            iniFile.IniWriteValue("Section1", "Key1", "Value1");

            // Assert
            File.Exists(_testFilePath).Should().BeTrue();
        }

        [Fact]
        public void IniReadValue_ExistingKey_ShouldReturnValue()
        {
            // Arrange
            var iniFile = new IniFile(_testFilePath);
            iniFile.IniWriteValue("Settings", "Username", "TestUser");

            // Act
            var result = iniFile.IniReadValue("Settings", "Username");

            // Assert
            result.Should().Be("TestUser");
        }

        [Fact]
        public void IniReadValue_NonExistingKey_ShouldReturnEmptyString()
        {
            // Arrange
            var iniFile = new IniFile(_testFilePath);

            // Act
            var result = iniFile.IniReadValue("Settings", "NonExisting");

            // Assert
            result.Should().Be(string.Empty);
        }

        [Fact]
        public void IniReadValue_NonExistingSection_ShouldReturnEmptyString()
        {
            // Arrange
            var iniFile = new IniFile(_testFilePath);
            iniFile.IniWriteValue("Section1", "Key1", "Value1");

            // Act
            var result = iniFile.IniReadValue("NonExistingSection", "Key1");

            // Assert
            result.Should().Be(string.Empty);
        }

        [Fact]
        public void IniWriteValue_UpdateExistingKey_ShouldUpdateValue()
        {
            // Arrange
            var iniFile = new IniFile(_testFilePath);
            iniFile.IniWriteValue("Settings", "Version", "1.0");

            // Act
            iniFile.IniWriteValue("Settings", "Version", "2.0");
            var result = iniFile.IniReadValue("Settings", "Version");

            // Assert
            result.Should().Be("2.0");
        }

        [Fact]
        public void IniFile_MultipleSections_ShouldHandleCorrectly()
        {
            // Arrange
            var iniFile = new IniFile(_testFilePath);

            // Act
            iniFile.IniWriteValue("Section1", "Key1", "Value1");
            iniFile.IniWriteValue("Section2", "Key2", "Value2");
            iniFile.IniWriteValue("Section3", "Key3", "Value3");

            // Assert
            iniFile.IniReadValue("Section1", "Key1").Should().Be("Value1");
            iniFile.IniReadValue("Section2", "Key2").Should().Be("Value2");
            iniFile.IniReadValue("Section3", "Key3").Should().Be("Value3");
        }

        [Fact]
        public void IniFile_LoadExistingFile_ShouldReadCorrectly()
        {
            // Arrange
            var iniContent = @"[Database]
Server=localhost
Port=5432

[Application]
Name=TestApp
Version=1.0
";
            File.WriteAllText(_testFilePath, iniContent);

            // Act
            var iniFile = new IniFile(_testFilePath);

            // Assert
            iniFile.IniReadValue("Database", "Server").Should().Be("localhost");
            iniFile.IniReadValue("Database", "Port").Should().Be("5432");
            iniFile.IniReadValue("Application", "Name").Should().Be("TestApp");
            iniFile.IniReadValue("Application", "Version").Should().Be("1.0");
        }

        [Fact]
        public void IniFile_LoadFileWithComments_ShouldIgnoreComments()
        {
            // Arrange
            var iniContent = @"; This is a comment
[Settings]
# Another comment
Key1=Value1
; Comment in middle
Key2=Value2
";
            File.WriteAllText(_testFilePath, iniContent);

            // Act
            var iniFile = new IniFile(_testFilePath);

            // Assert
            iniFile.IniReadValue("Settings", "Key1").Should().Be("Value1");
            iniFile.IniReadValue("Settings", "Key2").Should().Be("Value2");
        }

        [Fact]
        public void IniFile_LoadFileWithEmptyLines_ShouldIgnoreEmptyLines()
        {
            // Arrange
            var iniContent = @"
[Settings]

Key1=Value1


Key2=Value2

";
            File.WriteAllText(_testFilePath, iniContent);

            // Act
            var iniFile = new IniFile(_testFilePath);

            // Assert
            iniFile.IniReadValue("Settings", "Key1").Should().Be("Value1");
            iniFile.IniReadValue("Settings", "Key2").Should().Be("Value2");
        }

        [Fact]
        public void IniWriteValue_ChineseCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var iniFile = new IniFile(_testFilePath);

            // Act
            iniFile.IniWriteValue("設定", "使用者名稱", "測試使用者");

            // Assert
            iniFile.IniReadValue("設定", "使用者名稱").Should().Be("測試使用者");
        }

        [Fact]
        public void IniFile_ValueWithSpaces_ShouldPreserveSpaces()
        {
            // Arrange
            var iniFile = new IniFile(_testFilePath);

            // Act
            iniFile.IniWriteValue("Section", "Key", "Value with spaces");

            // Assert
            iniFile.IniReadValue("Section", "Key").Should().Be("Value with spaces");
        }

        [Fact]
        public void IniFile_ValueWithEquals_ShouldHandleCorrectly()
        {
            // Arrange
            var iniContent = @"[Connection]
ConnectionString=Server=localhost;Port=5432
";
            File.WriteAllText(_testFilePath, iniContent);

            // Act
            var iniFile = new IniFile(_testFilePath);

            // Assert
            iniFile.IniReadValue("Connection", "ConnectionString").Should().Be("Server=localhost;Port=5432");
        }

        [Fact]
        public void IniFile_Persistence_ShouldSaveAndReload()
        {
            // Arrange
            var iniFile1 = new IniFile(_testFilePath);
            iniFile1.IniWriteValue("Test", "Key1", "Value1");
            iniFile1.IniWriteValue("Test", "Key2", "Value2");

            // Act - Create new instance to reload from file
            var iniFile2 = new IniFile(_testFilePath);

            // Assert
            iniFile2.IniReadValue("Test", "Key1").Should().Be("Value1");
            iniFile2.IniReadValue("Test", "Key2").Should().Be("Value2");
        }

        [Fact]
        public void IniFile_MultipleKeysInSection_ShouldMaintainAll()
        {
            // Arrange
            var iniFile = new IniFile(_testFilePath);

            // Act
            iniFile.IniWriteValue("Config", "Host", "localhost");
            iniFile.IniWriteValue("Config", "Port", "8080");
            iniFile.IniWriteValue("Config", "Timeout", "30");

            // Assert
            iniFile.IniReadValue("Config", "Host").Should().Be("localhost");
            iniFile.IniReadValue("Config", "Port").Should().Be("8080");
            iniFile.IniReadValue("Config", "Timeout").Should().Be("30");
        }

        [Fact]
        public void IniFile_EmptyValue_ShouldHandleCorrectly()
        {
            // Arrange
            var iniFile = new IniFile(_testFilePath);

            // Act
            iniFile.IniWriteValue("Section", "EmptyKey", "");

            // Assert
            iniFile.IniReadValue("Section", "EmptyKey").Should().Be("");
        }
    }
}

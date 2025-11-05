using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Minecraft_updater.Services;

namespace Minecraft_updater.Tests.Services
{
    public class PrivateFunctionTests : IDisposable
    {
        private readonly string _testDirectory;

        public PrivateFunctionTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        #region SHA256 Tests

        [Fact]
        public void GetSHA256_ValidFile_ShouldReturnCorrectSHA256()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "test.txt");
            File.WriteAllText(testFile, "Hello World");

            // Act
            var sha256 = PrivateFunction.GetSHA256(testFile);

            // Assert
            sha256.Should().NotBeNullOrEmpty();
            sha256.Should().HaveLength(64); // SHA256 hash is 64 characters in hex
            sha256.Should().MatchRegex("^[a-f0-9]+$"); // Should be lowercase hex
        }

        [Fact]
        public void GetSHA256_EmptyFile_ShouldReturnSHA256()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "empty.txt");
            File.WriteAllText(testFile, "");

            // Act
            var sha256 = PrivateFunction.GetSHA256(testFile);

            // Assert
            sha256.Should().NotBeNullOrEmpty();
            sha256.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"); // SHA256 of empty string
        }

        [Fact]
        public void GetSHA256_SameContent_ShouldReturnSameSHA256()
        {
            // Arrange
            var file1 = Path.Combine(_testDirectory, "file1.txt");
            var file2 = Path.Combine(_testDirectory, "file2.txt");
            var content = "Test content for SHA256";
            File.WriteAllText(file1, content);
            File.WriteAllText(file2, content);

            // Act
            var sha256_1 = PrivateFunction.GetSHA256(file1);
            var sha256_2 = PrivateFunction.GetSHA256(file2);

            // Assert
            sha256_1.Should().Be(sha256_2);
        }

        [Fact]
        public void GetSHA256_DifferentContent_ShouldReturnDifferentSHA256()
        {
            // Arrange
            var file1 = Path.Combine(_testDirectory, "file1.txt");
            var file2 = Path.Combine(_testDirectory, "file2.txt");
            File.WriteAllText(file1, "Content A");
            File.WriteAllText(file2, "Content B");

            // Act
            var sha256_1 = PrivateFunction.GetSHA256(file1);
            var sha256_2 = PrivateFunction.GetSHA256(file2);

            // Assert
            sha256_1.Should().NotBe(sha256_2);
        }

        [Fact]
        public void GetSHA256_NonExistentFile_ShouldThrow()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

            // Act
            Action act = () => PrivateFunction.GetSHA256(nonExistentFile);

            // Assert
            act.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void GetSHA256_BinaryFile_ShouldReturnSHA256()
        {
            // Arrange
            var binaryFile = Path.Combine(_testDirectory, "binary.dat");
            var binaryData = new byte[] { 0x00, 0xFF, 0xAA, 0x55, 0x12, 0x34 };
            File.WriteAllBytes(binaryFile, binaryData);

            // Act
            var sha256 = PrivateFunction.GetSHA256(binaryFile);

            // Assert
            sha256.Should().NotBeNullOrEmpty();
            sha256.Should().HaveLength(64);
        }

        #endregion

        #region Temporary File Tests

        [Fact]
        public void CreateTmpFile_ShouldCreateFile()
        {
            // Act
            var tmpFile = PrivateFunction.CreateTmpFile();

            // Assert
            tmpFile.Should().NotBeNullOrEmpty();
            File.Exists(tmpFile).Should().BeTrue();

            // Cleanup
            File.Delete(tmpFile);
        }

        [Fact]
        public void CreateTmpFile_FileShouldExist()
        {
            // Act
            var tmpFile = PrivateFunction.CreateTmpFile();

            // Assert
            var fileInfo = new FileInfo(tmpFile);
            fileInfo.Exists.Should().BeTrue();
            // Note: Temporary attribute behavior differs across OS platforms

            // Cleanup
            File.Delete(tmpFile);
        }

        [Fact]
        public void CreateTmpFile_MultipleCalls_ShouldCreateDifferentFiles()
        {
            // Act
            var tmpFile1 = PrivateFunction.CreateTmpFile();
            var tmpFile2 = PrivateFunction.CreateTmpFile();

            // Assert
            tmpFile1.Should().NotBe(tmpFile2);
            File.Exists(tmpFile1).Should().BeTrue();
            File.Exists(tmpFile2).Should().BeTrue();

            // Cleanup
            File.Delete(tmpFile1);
            File.Delete(tmpFile2);
        }

        [Fact]
        public void DeleteTmpFile_ExistingFile_ShouldDeleteFile()
        {
            // Arrange
            var tmpFile = PrivateFunction.CreateTmpFile();
            File.Exists(tmpFile).Should().BeTrue();

            // Act
            PrivateFunction.DeleteTmpFile(tmpFile);

            // Assert
            File.Exists(tmpFile).Should().BeFalse();
        }

        [Fact]
        public void DeleteTmpFile_NonExistentFile_ShouldNotThrow()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.tmp");

            // Act
            Action act = () => PrivateFunction.DeleteTmpFile(nonExistentFile);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region Download File Tests (需要測試環境支援網路，這裡提供基本測試)

        [Fact]
        public async Task DownloadFileAsync_InvalidURL_ShouldReturnFalse()
        {
            // Arrange
            var invalidUrl = "invalid-url";
            var targetPath = Path.Combine(_testDirectory, "download.txt");
            var logMessages = new System.Collections.Generic.List<string>();

            // Act
            var result = await PrivateFunction.DownloadFileAsync(
                invalidUrl,
                targetPath,
                msg => logMessages.Add(msg)
            );

            // Assert
            result.Should().BeFalse();
            logMessages.Should().Contain(msg => msg.Contains("錯誤"));
        }

        [Fact]
        public void DownloadFile_SyncVersion_InvalidURL_ShouldReturnFalse()
        {
            // Arrange
            var invalidUrl = "invalid-url";
            var targetPath = Path.Combine(_testDirectory, "download.txt");

            // Act
            var result = PrivateFunction.DownloadFile(invalidUrl, targetPath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DownloadFileAsync_ShouldCreateDirectory()
        {
            // Arrange
            var subDir = Path.Combine(_testDirectory, "subfolder", "nested");
            var targetPath = Path.Combine(subDir, "file.txt");
            var invalidUrl = "http://invalid.url.example.com/file.txt";

            // Act
            await PrivateFunction.DownloadFileAsync(invalidUrl, targetPath);

            // Assert - Directory should be created even if download fails
            Directory.Exists(subDir).Should().BeTrue();
        }

        [Fact]
        public async Task DownloadFileAsync_ExistingFile_ShouldNotRemoveFileOnFailure()
        {
            // Arrange
            var targetPath = Path.Combine(_testDirectory, "existing.txt");
            File.WriteAllText(targetPath, "Old content");
            var originalContent = File.ReadAllText(targetPath);

            // Act - Try to download (will fail but should keep old file)
            var result = await PrivateFunction.DownloadFileAsync(
                "http://invalid.url/file.txt",
                targetPath
            );

            // Assert - Download should fail but existing file must remain untouched
            result.Should().BeFalse();
            File.Exists(targetPath).Should().BeTrue();
            File.ReadAllText(targetPath).Should().Be(originalContent);
        }

        [Fact]
        public async Task DownloadFileAsync_WithLogAction_ShouldInvokeCallback()
        {
            // Arrange
            var targetPath = Path.Combine(_testDirectory, "test.txt");
            var logMessages = new System.Collections.Generic.List<string>();
            var invalidUrl = "http://invalid.example.com/file.txt";

            // Act
            await PrivateFunction.DownloadFileAsync(
                invalidUrl,
                targetPath,
                msg => logMessages.Add(msg)
            );

            // Assert
            logMessages.Should().NotBeEmpty();
            logMessages.Should().Contain(msg => msg.Contains("正在下載") || msg.Contains("錯誤"));
        }

        [Fact]
        public async Task DownloadFileAsync_NullLogAction_ShouldNotThrow()
        {
            // Arrange
            var targetPath = Path.Combine(_testDirectory, "test.txt");
            var invalidUrl = "http://invalid.example.com/file.txt";

            // Act
            Func<Task> act = async () => await PrivateFunction.DownloadFileAsync(
                invalidUrl,
                targetPath,
                null
            );

            // Assert
            await act.Should().NotThrowAsync();
        }

        #endregion
    }
}

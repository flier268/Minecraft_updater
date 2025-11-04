using FluentAssertions;
using Minecraft_updater.Models;
using Minecraft_updater.Services;

namespace Minecraft_updater.Tests.Services
{
    public class PackValidationServiceTests
    {
        private readonly PackValidationService _validator;

        public PackValidationServiceTests()
        {
            _validator = new PackValidationService();
        }

        #region MD5 Validation Tests

        [Theory]
        [InlineData("5d41402abc4b2a76b9719d911017c592")] // Valid MD5
        [InlineData("098F6BCD4621D373CADE4E832627B4F6")] // Valid MD5 uppercase
        [InlineData("5D41402ABC4B2A76B9719D911017C592")] // Valid MD5 mixed case
        public void ValidateMD5_ValidHash_ReturnsTrue(string md5)
        {
            // Act
            var result = _validator.ValidateMD5(md5);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("")] // Empty
        [InlineData("   ")] // Whitespace
        [InlineData("5d41402abc4b2a76b9719d911017c59")] // Too short (31 chars)
        [InlineData("5d41402abc4b2a76b9719d911017c5921")] // Too long (33 chars)
        [InlineData("5d41402abc4b2a76b9719d911017c59g")] // Invalid character 'g'
        [InlineData("not a valid hash at all")]
        public void ValidateMD5_InvalidHash_ReturnsFalse(string md5)
        {
            // Act
            var result = _validator.ValidateMD5(md5);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region URL Validation Tests

        [Theory]
        [InlineData("http://example.com/file.jar")]
        [InlineData("https://example.com/file.jar")]
        [InlineData("http://cdn.example.com/mods/mod.jar")]
        [InlineData("https://example.com/file.jar?version=1.2&build=latest")]
        [InlineData("http://192.168.1.1/file.jar")]
        [InlineData("https://example.com:8080/path/to/file.jar")]
        public void ValidateUrl_ValidUrl_ReturnsTrue(string url)
        {
            // Act
            var result = _validator.ValidateUrl(url);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("")] // Empty URL is allowed (for delete operations)
        [InlineData("   ")] // Whitespace is treated as empty, allowed
        public void ValidateUrl_EmptyUrl_ReturnsTrue(string url)
        {
            // Act
            var result = _validator.ValidateUrl(url);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("not a url")]
        [InlineData("ftp://example.com/file.jar")] // FTP not supported
        [InlineData("htp://example.com")] // Typo in protocol
        [InlineData("http:/example.com")] // Missing slash
        [InlineData("example.com/file.jar")] // Missing protocol
        public void ValidateUrl_InvalidUrl_ReturnsFalse(string url)
        {
            // Act
            var result = _validator.ValidateUrl(url);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Path Validation Tests

        [Theory]
        [InlineData("mods/Botania-1.20.jar")]
        [InlineData("config/server.properties")]
        [InlineData("data/world/region/file.mca")]
        [InlineData("mods/Special-Mod_v1.0+build.jar")]
        public void ValidatePath_ValidPath_ReturnsTrue(string path)
        {
            // Act
            var result = _validator.ValidatePath(path);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("../../../etc/passwd")] // Path traversal
        [InlineData("mods/../../../secrets.txt")] // Path traversal
        [InlineData("config/../../file.txt")] // Path traversal
        public void ValidatePath_PathTraversal_ReturnsFalse(string path)
        {
            // Act
            var result = _validator.ValidatePath(path);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("/etc/passwd")] // Absolute Unix path
        [InlineData("/root/file.txt")] // Absolute Unix path
        [InlineData("\\Windows\\System32")] // Absolute Windows path
        public void ValidatePath_AbsolutePath_ReturnsFalse(string path)
        {
            // Act
            var result = _validator.ValidatePath(path);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("C:\\Windows\\System32")]
        [InlineData("D:\\data\\file.txt")]
        [InlineData("c:\\file.txt")]
        public void ValidatePath_WindowsDrivePath_ReturnsFalse(string path)
        {
            // Act
            var result = _validator.ValidatePath(path);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidatePath_EmptyPath_ReturnsFalse(string path)
        {
            // Act
            var result = _validator.ValidatePath(path);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Pack Validation Tests

        [Fact]
        public void ValidatePack_ValidNormalPack_ReturnsSuccess()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "mods/TestMod.jar",
                MD5 = "5d41402abc4b2a76b9719d911017c592",
                URL = "http://example.com/TestMod.jar",
                Delete = false,
                DownloadWhenNotExist = false
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeTrue();
            result.ErrorMessage.Should().BeNullOrEmpty();
        }

        [Fact]
        public void ValidatePack_ValidDeletePack_ReturnsSuccess()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "mods/OldMod",
                MD5 = "5d41402abc4b2a76b9719d911017c592",
                URL = "", // Delete operations don't need URL
                Delete = true,
                DownloadWhenNotExist = false
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidatePack_ValidDownloadWhenNotExist_ReturnsSuccess()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "config/optional.cfg",
                MD5 = "5d41402abc4b2a76b9719d911017c592",
                URL = "http://example.com/optional.cfg",
                Delete = false,
                DownloadWhenNotExist = true
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidatePack_InvalidPath_ReturnsFailure()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "../../../etc/passwd",
                MD5 = "5d41402abc4b2a76b9719d911017c592",
                URL = "http://example.com/file.jar"
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("路徑");
        }

        [Fact]
        public void ValidatePack_InvalidMD5_ReturnsFailure()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "mods/Mod.jar",
                MD5 = "invalid-md5-hash",
                URL = "http://example.com/Mod.jar"
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("MD5");
        }

        [Fact]
        public void ValidatePack_EmptyMD5_ReturnsSuccess()
        {
            // Arrange - Empty MD5 is allowed for some operations
            var pack = new Pack
            {
                Path = "mods/Mod.jar",
                MD5 = "",
                URL = "http://example.com/Mod.jar"
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ValidatePack_NormalPackWithoutURL_ReturnsFailure()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "mods/Mod.jar",
                MD5 = "5d41402abc4b2a76b9719d911017c592",
                URL = "", // Normal sync needs URL
                Delete = false,
                DownloadWhenNotExist = false
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("URL");
        }

        [Fact]
        public void ValidatePack_InvalidURL_ReturnsFailure()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "mods/Mod.jar",
                MD5 = "5d41402abc4b2a76b9719d911017c592",
                URL = "not-a-valid-url"
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("URL");
        }

        [Fact]
        public void ValidatePack_DownloadWhenNotExistWithoutURL_ReturnsFailure()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "config/file.cfg",
                MD5 = "5d41402abc4b2a76b9719d911017c592",
                URL = "",
                DownloadWhenNotExist = true
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("URL");
        }

        [Fact]
        public void ValidatePack_DeletePackWithInvalidPath_ReturnsFailure()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "/etc/passwd",
                MD5 = "",
                URL = "",
                Delete = true
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("路徑");
        }

        #endregion
    }
}

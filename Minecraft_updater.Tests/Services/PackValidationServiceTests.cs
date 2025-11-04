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

        #region SHA256 Validation Tests

        [Theory]
        [InlineData("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")] // Valid SHA256
        [InlineData("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855")] // Valid SHA256 uppercase
        [InlineData("E3b0c44298fc1C149afbf4c8996fb92427AE41e4649b934ca495991b7852B855")] // Valid SHA256 mixed case
        public void ValidateSHA256_ValidHash_ReturnsTrue(string sha256)
        {
            // Act
            var result = _validator.ValidateSHA256(sha256);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("")] // Empty
        [InlineData("   ")] // Whitespace
        [InlineData("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b85")] // Too short (63 chars)
        [InlineData("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b8555")] // Too long (65 chars)
        [InlineData("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b85g")] // Invalid character 'g'
        [InlineData("not a valid hash at all")]
        public void ValidateSHA256_InvalidHash_ReturnsFalse(string sha256)
        {
            // Act
            var result = _validator.ValidateSHA256(sha256);

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
                SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
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
                SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
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
                SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
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
                SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                URL = "http://example.com/file.jar"
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("路徑");
        }

        [Fact]
        public void ValidatePack_InvalidSHA256_ReturnsFailure()
        {
            // Arrange
            var pack = new Pack
            {
                Path = "mods/Mod.jar",
                SHA256 = "invalid-sha256-hash",
                URL = "http://example.com/Mod.jar"
            };

            // Act
            var result = _validator.ValidatePack(pack);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("SHA256");
        }

        [Fact]
        public void ValidatePack_EmptySHA256_ReturnsSuccess()
        {
            // Arrange - Empty SHA256 is allowed for some operations
            var pack = new Pack
            {
                Path = "mods/Mod.jar",
                SHA256 = "",
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
                SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
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
                SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
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
                SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
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
                SHA256 = "",
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

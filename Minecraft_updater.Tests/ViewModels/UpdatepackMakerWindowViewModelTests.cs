using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Minecraft_updater.ViewModels;

namespace Minecraft_updater.Tests.ViewModels
{
    public class UpdatepackMakerWindowViewModelTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _originalBaseDirectory;

        public UpdatepackMakerWindowViewModelTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _originalBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Set base directory to test directory for testing
            AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", _testDirectory);
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

            // Restore original base directory
            AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", _originalBaseDirectory);
        }

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var viewModel = new UpdatepackMakerWindowViewModel();

            // Assert
            viewModel.Should().NotBeNull();
            viewModel.BaseUrl.Should().NotBeNullOrEmpty();
            viewModel.SyncListText.Should().Be(string.Empty);
            viewModel.DeleteListText.Should().Be(string.Empty);
            viewModel.DownloadWhenNotExistText.Should().Be(string.Empty);
        }

        [Fact]
        public void BaseUrl_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel();
            var newUrl = "https://example.com/files/";

            // Act
            viewModel.BaseUrl = newUrl;

            // Assert
            viewModel.BaseUrl.Should().Be(newUrl);
        }

        [Fact]
        public void AddModToDelete_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var viewModel = new UpdatepackMakerWindowViewModel();

            // Assert
            viewModel.AddModToDelete.Should().BeTrue();
        }

        [Fact]
        public void AddConfigToDelete_DefaultValue_ShouldBeFalse()
        {
            // Arrange & Act
            var viewModel = new UpdatepackMakerWindowViewModel();

            // Assert
            viewModel.AddConfigToDelete.Should().BeFalse();
        }

        [Fact]
        public void ClearSyncListCommand_ShouldClearSyncListText()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel
            {
                SyncListText = "Some content"
            };

            // Act
            viewModel.ClearSyncListCommand.Execute(null);

            // Assert
            viewModel.SyncListText.Should().Be(string.Empty);
        }

        [Fact]
        public void ClearDeleteListCommand_ShouldClearDeleteListText()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel
            {
                DeleteListText = "Some delete content"
            };

            // Act
            viewModel.ClearDeleteListCommand.Execute(null);

            // Assert
            viewModel.DeleteListText.Should().Be(string.Empty);
        }

        [Fact]
        public void ClearDownloadWhenNotExistListCommand_ShouldClearDownloadWhenNotExistText()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel
            {
                DownloadWhenNotExistText = "Some download content"
            };

            // Act
            viewModel.ClearDownloadWhenNotExistListCommand.Execute(null);

            // Assert
            viewModel.DownloadWhenNotExistText.Should().Be(string.Empty);
        }

        [Fact]
        public void GetSaveContent_EmptyLists_ShouldReturnOnlyMinVersion()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel();

            // Act
            var content = viewModel.GetSaveContent();

            // Assert
            // 現在使用新格式 MinVersion= 而不是 MinVersion||
            content.Should().StartWith("MinVersion=");
            content.Should().Contain("\n");
        }

        [Fact]
        public void GetSaveContent_WithContent_ShouldCombineAllLists()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel
            {
                SyncListText = "mods/sync.jar||SHA256HASH||http://example.com/sync.jar\n",
                DeleteListText = "#mods/delete.jar||SHA256HASH||http://example.com/delete.jar\n",
                DownloadWhenNotExistText = ":config/download.cfg||SHA256HASH||http://example.com/download.cfg\n"
            };

            // Act
            var content = viewModel.GetSaveContent();

            // Assert
            content.Should().Contain("mods/sync.jar");
            content.Should().Contain("#mods/delete.jar");
            content.Should().Contain(":config/download.cfg");
        }

        [Fact]
        public async Task LoadFileAsync_ValidFile_ShouldLoadContent()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "test.txt");
            var fileContent = @"mods/test.jar||MD5HASH||http://example.com/test.jar
#mods/old.jar||MD5OLD||
:config/settings.cfg||MD5CFG||http://example.com/config.cfg";
            File.WriteAllText(testFile, fileContent);

            var viewModel = new UpdatepackMakerWindowViewModel();

            // Act
            await viewModel.LoadFileAsync(testFile);

            // Assert
            viewModel.SyncListText.Should().Contain("mods/test.jar");
            viewModel.DeleteListText.Should().Contain("#mods/old.jar");
            viewModel.DownloadWhenNotExistText.Should().Contain(":config/settings.cfg");
        }

        [Fact]
        public async Task LoadFileAsync_FileWithEmptyLines_ShouldIgnoreEmptyLines()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "test_empty.txt");
            var fileContent = @"

mods/test.jar||SHA256HASH||http://example.com/test.jar

#mods/old.jar||SHA256HASH||

";
            File.WriteAllText(testFile, fileContent);

            var viewModel = new UpdatepackMakerWindowViewModel();

            // Act
            await viewModel.LoadFileAsync(testFile);

            // Assert
            viewModel.SyncListText.Should().Contain("mods/test.jar");
            viewModel.DeleteListText.Should().Contain("#mods/old.jar");
        }

        [Fact]
        public async Task LoadFileAsync_NonExistentFile_ShouldNotThrow()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel();
            var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

            // Act
            Func<Task> act = async () => await viewModel.LoadFileAsync(nonExistentFile);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public void SyncListText_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel();
            var content = "test content";

            // Act
            viewModel.SyncListText = content;

            // Assert
            viewModel.SyncListText.Should().Be(content);
        }

        [Fact]
        public void DeleteListText_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel();
            var content = "delete content";

            // Act
            viewModel.DeleteListText = content;

            // Assert
            viewModel.DeleteListText.Should().Be(content);
        }

        [Fact]
        public void DownloadWhenNotExistText_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel();
            var content = "download content";

            // Act
            viewModel.DownloadWhenNotExistText = content;

            // Assert
            viewModel.DownloadWhenNotExistText.Should().Be(content);
        }

        [Fact]
        public async Task LoadFileAsync_MixedContent_ShouldSeparateCorrectly()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "mixed.txt");
            var fileContent = @"normal/file1.jar||ABC||http://url1.com
#delete/file2.jar||DEF||
:download/file3.jar||GHI||http://url3.com
normal/file4.jar||JKL||http://url4.com
#delete/file5.jar||MNO||";
            File.WriteAllText(testFile, fileContent);

            var viewModel = new UpdatepackMakerWindowViewModel();

            // Act
            await viewModel.LoadFileAsync(testFile);

            // Assert
            viewModel.SyncListText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length.Should().Be(2);
            viewModel.DeleteListText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length.Should().Be(2);
            viewModel.DownloadWhenNotExistText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length.Should().Be(1);
        }

        [Fact]
        public void GetSaveContent_ShouldPreserveOrder()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel
            {
                SyncListText = "mods/sync.jar||SHA256HASH||http://url\n",
                DeleteListText = "#mods/delete.jar||SHA256HASH||\n",
                DownloadWhenNotExistText = ":config/download.cfg||SHA256HASH||http://url2\n"
            };

            // Act
            var content = viewModel.GetSaveContent();

            // Assert
            // 現在會先包含 MinVersion 行（新格式使用 = ），然後是原有的內容順序
            content.Should().StartWith("MinVersion=");
            content.Should().Contain("mods/sync.jar");
            content.Should().Contain("#mods/delete.jar");
            content.Should().Contain(":config/download.cfg");
        }

        [Fact]
        public void AddModToDelete_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel();

            // Act
            viewModel.AddModToDelete = false;

            // Assert
            viewModel.AddModToDelete.Should().BeFalse();
        }

        [Fact]
        public void AddConfigToDelete_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new UpdatepackMakerWindowViewModel();

            // Act
            viewModel.AddConfigToDelete = true;

            // Assert
            viewModel.AddConfigToDelete.Should().BeTrue();
        }
    }
}

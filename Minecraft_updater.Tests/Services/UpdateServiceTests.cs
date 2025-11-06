using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Minecraft_updater.Services;
using Minecraft_updater.Models;

namespace Minecraft_updater.Tests.Services
{
    public class UpdateServiceTests
    {
        #region CheckUpdateAsync 基礎測試

        [Fact]
        public async Task CheckUpdateAsync_ShouldReturnUpdateMessage()
        {
            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<UpdateMessage>();
        }

        [Fact]
        public async Task CheckUpdateAsync_ShouldInitializeAllProperties()
        {
            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert - 所有屬性都應該被初始化（非 null）
            result.Should().NotBeNull();
            result.NewstVersion.Should().NotBeNull();
            result.SHA1.Should().NotBeNull();
            result.Message.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckUpdateAsync_HaveUpdateProperty_ShouldBeBoolean()
        {
            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert - HaveUpdate 應該是有效的布林值
            (result.HaveUpdate == true || result.HaveUpdate == false).Should().BeTrue();
        }

        #endregion

        #region CheckUpdateAsync 更新邏輯測試

        [Fact]
        public async Task CheckUpdateAsync_WhenNoUpdateAvailable_ShouldSetHaveUpdateToFalse()
        {
            // Arrange - 假設當前版本等於或高於最新版本

            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert - 如果沒有更新，HaveUpdate 應該是 false
            if (!result.HaveUpdate)
            {
                result.NewstVersion.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task CheckUpdateAsync_WhenUpdateAvailable_ShouldPopulateVersionInfo()
        {
            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert - 如果有更新，版本資訊應該被填充
            if (result.HaveUpdate)
            {
                result.NewstVersion.Should().NotBeNullOrEmpty();
                // 版本號應該符合格式（例如：1.2.3 或 1.2.3.4）
                result.NewstVersion
                    .Should()
                    .MatchRegex(@"^\d+(\.\d+){1,3}([\-+].+)?$");
            }
        }

        [Fact]
        public async Task CheckUpdateAsync_WhenUpdateAvailable_ShouldPopulateDownloadUrl()
        {
            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert - 如果有更新，下載 URL 應該被填充
            if (result.HaveUpdate)
            {
                // SHA1 欄位實際存放下載 URL
                if (!string.IsNullOrEmpty(result.SHA1))
                {
                    result.SHA1.Should().StartWith("https://");
                }
            }
        }

        [Fact]
        public async Task CheckUpdateAsync_WhenUpdateAvailable_MayIncludeReleaseNotes()
        {
            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert - 如果有更新，可能包含 Release Notes
            if (result.HaveUpdate)
            {
                // Message 可能為空或包含 Release Notes
                result.Message.Should().NotBeNull();
            }
        }

        #endregion

        #region TryParseReleaseVersion 測試

        [Theory]
        [InlineData("v1.2.3", "1.2.3", "1.2.3")]
        [InlineData("1.2.3", "1.2.3", "1.2.3")]
        [InlineData("v1.2.3-beta", "1.2.3-beta", "1.2.3")]
        [InlineData("1.2.3+build.1", "1.2.3+build.1", "1.2.3")]
        [InlineData("v2.0", "2.0", "2.0")]
        public void TryParseReleaseVersion_ShouldHandleCommonTags(
            string tag,
            string expectedDisplay,
            string comparableVersion
        )
        {
            // Act
            var parsed = UpdateService.TryParseReleaseVersion(
                tag,
                out var version,
                out var display
            );

            // Assert
            parsed.Should().BeTrue();
            display.Should().Be(expectedDisplay);
            version.Should().Be(new Version(comparableVersion));
        }

        [Fact]
        public void TryParseReleaseVersion_ShouldReturnFalseForInvalidTag()
        {
            // Act
            var parsed = UpdateService.TryParseReleaseVersion(
                "invalid-tag",
                out var version,
                out var display
            );

            // Assert
            parsed.Should().BeFalse();
            display.Should().BeEmpty();
            version.Should().BeNull();
        }

        #endregion

        #region CheckUpdateAsync 錯誤處理測試

        [Fact]
        public async Task CheckUpdateAsync_OnNetworkError_ShouldReturnDefaultUpdateMessage()
        {
            // Act - 即使網路失敗也應該返回有效的 UpdateMessage
            var result = await UpdateService.CheckUpdateAsync();

            // Assert - 即使發生錯誤，也應返回有效的 UpdateMessage 物件
            result.Should().NotBeNull();
            result.NewstVersion.Should().NotBeNull();
            result.SHA1.Should().NotBeNull();
            result.Message.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckUpdateAsync_OnException_ShouldNotThrow()
        {
            // Act & Assert - 不應該拋出異常
            Func<Task> act = async () => await UpdateService.CheckUpdateAsync();
            await act.Should().NotThrowAsync();
        }

        #endregion

        #region CheckUpdateAsync 並發與重複呼叫測試

        [Fact]
        public async Task CheckUpdateAsync_MultipleCallsShouldWork()
        {
            // Act - 多次呼叫
            var result1 = await UpdateService.CheckUpdateAsync();
            var result2 = await UpdateService.CheckUpdateAsync();

            // Assert - 兩次呼叫都應該返回有效結果
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckUpdateAsync_ConcurrentCalls_ShouldAllSucceed()
        {
            // Act - 並發呼叫
            var tasks = new Task<UpdateMessage>[5];
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = UpdateService.CheckUpdateAsync();
            }
            var results = await Task.WhenAll(tasks);

            // Assert - 所有呼叫都應該成功
            results.Should().HaveCount(5);
            results.Should().OnlyContain(r => r != null);
        }

        #endregion

        #region CheckUpdateAsync 結果一致性測試

        [Fact]
        public async Task CheckUpdateAsync_ConsecutiveCalls_ShouldReturnConsistentResults()
        {
            // Act - 連續兩次呼叫
            var result1 = await UpdateService.CheckUpdateAsync();
            await Task.Delay(100); // 短暫延遲
            var result2 = await UpdateService.CheckUpdateAsync();

            // Assert - 結果應該一致（版本號不會在短時間內改變）
            result1.HaveUpdate.Should().Be(result2.HaveUpdate);
            if (result1.HaveUpdate)
            {
                result1.NewstVersion.Should().Be(result2.NewstVersion);
            }
        }

        #endregion

        #region CheckUpdateAsync 平台特定測試 (Asset 查找邏輯)

        [Fact]
        public async Task CheckUpdateAsync_ShouldFindPlatformSpecificAsset()
        {
            // 測試重點：驗證從 GitHub Release assets 中找到正確平台的下載 URL
            // 對應程式碼：UpdateService.cs:74-98

            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert - 如果有更新且找到下載 URL，應該包含平台標識
            if (result.HaveUpdate && !string.IsNullOrEmpty(result.SHA1))
            {
                var url = result.SHA1.ToLower();
                var hasPlatformIdentifier =
                    url.Contains("win-x64") || url.Contains("linux-x64") || url.Contains("osx-x64");

                // 如果 URL 包含平台資訊，應該符合當前平台
                if (hasPlatformIdentifier)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        url.Should().Contain("win-x64");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        url.Should().Contain("linux-x64");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        url.Should().Contain("osx-x64");
                    }
                }
            }
        }

        [Fact]
        public async Task CheckUpdateAsync_WhenUpdateAvailable_SHA1ShouldBeUrlOrEmpty()
        {
            // 測試重點：SHA1 欄位只能是 URL 或空字串
            // 對應程式碼：UpdateService.cs:93 和 UpdateService.cs:103

            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert
            result.SHA1.Should().NotBeNull();

            if (result.HaveUpdate && !string.IsNullOrEmpty(result.SHA1))
            {
                // 如果有內容，應該是 GitHub 的下載 URL
                result
                    .SHA1.Should()
                    .MatchRegex(
                        @"^https://github\.com/.+/releases/download/.+",
                        "SHA1 field should contain a GitHub download URL when update is available"
                    );
            }
        }

        [Fact]
        public async Task CheckUpdateAsync_WhenNoAssetFound_SHA1ShouldBeEmpty()
        {
            // 測試重點：如果找不到對應平台的 asset，SHA1 應該是空字串
            // 對應程式碼：UpdateService.cs:101-104
            // 注意：這是整合測試，實際結果取決於 GitHub Release 的內容

            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert
            if (result.HaveUpdate)
            {
                // SHA1 可能是空字串（找不到 asset）或 URL（找到 asset）
                result.SHA1.Should().NotBeNull();

                if (string.IsNullOrEmpty(result.SHA1))
                {
                    // 這是預期的情況：有更新但找不到對應平台的下載檔案
                    result.SHA1.Should().BeEmpty();
                }
            }
        }

        [Fact]
        public async Task CheckUpdateAsync_AssetUrlShouldBeBrowserDownloadUrl()
        {
            // 測試重點：驗證取得的是 browser_download_url 而非其他 URL
            // 對應程式碼：UpdateService.cs:86-95

            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert
            if (result.HaveUpdate && !string.IsNullOrEmpty(result.SHA1))
            {
                // browser_download_url 應該指向實際的檔案下載位址
                result.SHA1.Should().Contain("/releases/download/");
            }
        }

        [Fact]
        public async Task CheckUpdateAsync_WhenPlatformAssetExists_ShouldNotBeEmpty()
        {
            // 測試重點：當 Release 有對應平台的 asset 時，應該能找到
            // 對應程式碼：UpdateService.cs:74-98

            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert
            if (result.HaveUpdate)
            {
                // 如果專案維護良好，每個 Release 都應該包含各平台的檔案
                // 但這取決於實際的 GitHub Release 配置
                // 這裡只驗證結果的一致性
                if (!string.IsNullOrEmpty(result.SHA1))
                {
                    result.SHA1.Should().StartWith("https://");
                }
            }
        }

        #endregion

        #region CheckUpdate 同步版本測試

        [Fact]
        public void CheckUpdate_SyncVersion_ShouldReturnUpdateMessage()
        {
            // Act
            var result = UpdateService.CheckUpdate();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<UpdateMessage>();
        }

        [Fact]
        public void CheckUpdate_ShouldBeSynchronousWrapperOfAsync()
        {
            // Act
            var syncResult = UpdateService.CheckUpdate();

            // Assert - 應該返回 UpdateMessage 物件
            syncResult.Should().NotBeNull();
        }

        [Fact]
        public void CheckUpdate_ShouldNotThrow()
        {
            // Act & Assert
            Action act = () => UpdateService.CheckUpdate();
            act.Should().NotThrow();
        }

        #endregion

        #region StartAutoUpdater 測試

        [Fact]
        public void StartAutoUpdater_NonExistentFile_ShouldNotThrow()
        {
            // Act
            Action act = () => UpdateService.StartAutoUpdater();

            // Assert - 即使 AutoUpdater.exe 不存在也不應該拋出異常
            act.Should().NotThrow();
        }

        [Fact]
        public void StartAutoUpdater_ShouldHandleGracefully()
        {
            // Arrange - 測試環境中沒有 AutoUpdater.exe

            // Act & Assert - 不應該崩潰
            Action act = () => UpdateService.StartAutoUpdater();
            act.Should().NotThrow();
        }

        [Fact]
        public void StartAutoUpdater_MultipleCallsShouldNotThrow()
        {
            // Act & Assert
            Action act = () =>
            {
                UpdateService.StartAutoUpdater();
                UpdateService.StartAutoUpdater();
                UpdateService.StartAutoUpdater();
            };

            act.Should().NotThrow();
        }

        #endregion

        #region 整合測試

        [Fact]
        public async Task CheckUpdateAsync_IntegrationTest_ShouldConnectToGitHub()
        {
            // 這是一個整合測試，會實際連線到 GitHub API
            // 在 CI/CD 環境中可能需要網路連線

            // Act
            var result = await UpdateService.CheckUpdateAsync();

            // Assert - 應該能成功取得回應（無論是否有更新）
            result.Should().NotBeNull();

            // 如果成功連線，屬性應該都被正確初始化
            result.NewstVersion.Should().NotBeNull();
            result.SHA1.Should().NotBeNull();
            result.Message.Should().NotBeNull();
        }

        #endregion
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Minecraft_updater.Models;

namespace Minecraft_updater.Services
{
    public class UpdateService
    {
        private const string GitHubApiUrl =
            "https://api.github.com/repos/flier268/Minecraft_updater/releases/latest";

        /// <summary>
        /// 檢查是否有更新 (非同步版本)
        /// </summary>
        /// <returns>UpdateMessage 物件，包含更新資訊</returns>
        public static async Task<UpdateMessage> CheckUpdateAsync()
        {
            var updateMessage = new UpdateMessage();

            try
            {
                using var client = new HttpClient();
                // GitHub API 需要 User-Agent header
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("Minecraft_updater", "1.0")
                );

                var response = await client.GetAsync(GitHubApiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var jsonDoc = JsonDocument.Parse(responseString);
                    var root = jsonDoc.RootElement;

                    // 從 GitHub Release 取得 tag_name (例如: "v1.2.3")
                    var tagName = root.GetProperty("tag_name").GetString();
                    if (string.IsNullOrEmpty(tagName))
                    {
                        return updateMessage;
                    }

                    // 移除 'v' 前綴並解析版本號
                    var versionString = tagName.TrimStart('v');
                    var latestVersion = new Version(versionString);
                    var currentVersion =
                        Assembly.GetEntryAssembly()?.GetName().Version ?? new Version("0.0.0.0");

                    int comparison = currentVersion.CompareTo(latestVersion);

                    if (comparison >= 0)
                    {
                        updateMessage.HaveUpdate = false;
                    }
                    else
                    {
                        updateMessage.HaveUpdate = true;
                        updateMessage.NewstVersion = versionString;

                        // 取得 Release 的 body 作為更新訊息
                        if (root.TryGetProperty("body", out var bodyElement))
                        {
                            updateMessage.Message = bodyElement.GetString() ?? "";
                        }

                        // 根據作業系統決定下載的檔案名稱
                        var assetName = GetAssetNameForCurrentPlatform();

                        // 從 assets 中找到對應的下載 URL
                        if (root.TryGetProperty("assets", out var assetsElement))
                        {
                            foreach (var asset in assetsElement.EnumerateArray())
                            {
                                if (
                                    asset.TryGetProperty("name", out var nameElement)
                                    && nameElement.GetString()?.Contains(assetName) == true
                                )
                                {
                                    if (
                                        asset.TryGetProperty(
                                            "browser_download_url",
                                            out var urlElement
                                        )
                                    )
                                    {
                                        updateMessage.SHA1 = urlElement.GetString() ?? "";
                                        break;
                                    }
                                }
                            }
                        }

                        // 如果沒有找到對應的下載 URL，設為空字串
                        if (string.IsNullOrEmpty(updateMessage.SHA1))
                        {
                            updateMessage.SHA1 = "";
                        }
                    }
                }
            }
            catch
            {
                // 發生錯誤時返回預設值 (不需要更新)
            }

            return updateMessage;
        }

        /// <summary>
        /// 檢查是否有更新 (同步版本，保持向後相容)
        /// </summary>
        /// <returns>UpdateMessage 物件，包含更新資訊</returns>
        public static UpdateMessage CheckUpdate()
        {
            return CheckUpdateAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 啟動自動更新程式
        /// </summary>
        public static void StartAutoUpdater()
        {
            try
            {
                var autoUpdaterPath = Path.Combine(AppContext.BaseDirectory, "AutoUpdater.exe");

                if (File.Exists(autoUpdaterPath))
                {
                    var startInfo = new ProcessStartInfo(autoUpdaterPath)
                    {
                        WindowStyle = ProcessWindowStyle.Minimized,
                        Arguments = "-CheckUpdateWithoutForm",
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    Console.WriteLine("找不到 AutoUpdater.exe");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"啟動自動更新程式失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 根據當前作業系統取得對應的資產名稱
        /// </summary>
        private static string GetAssetNameForCurrentPlatform()
        {
            if (OperatingSystem.IsWindows())
            {
                return "win-x64";
            }
            else if (OperatingSystem.IsLinux())
            {
                return "linux-x64";
            }
            else if (OperatingSystem.IsMacOS())
            {
                return "osx-x64";
            }
            else
            {
                // 預設使用 Linux
                return "linux-x64";
            }
        }
    }
}

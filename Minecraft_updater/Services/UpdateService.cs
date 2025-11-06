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

        internal static bool TryParseReleaseVersion(
            string? tagName,
            out Version version,
            out string displayVersion
        )
        {
            version = default!;
            displayVersion = string.Empty;

            if (string.IsNullOrWhiteSpace(tagName))
            {
                return false;
            }

            displayVersion = tagName!.Trim().TrimStart('v', 'V');

            var sanitizedVersion = displayVersion;
            var metadataIndex = sanitizedVersion.IndexOf('+');
            if (metadataIndex >= 0)
            {
                sanitizedVersion = sanitizedVersion[..metadataIndex];
            }

            var prereleaseIndex = sanitizedVersion.IndexOf('-');
            if (prereleaseIndex >= 0)
            {
                sanitizedVersion = sanitizedVersion[..prereleaseIndex];
            }

            sanitizedVersion = sanitizedVersion.Trim();
            if (Version.TryParse(sanitizedVersion, out var parsedVersion))
            {
                version = parsedVersion;
                return true;
            }

            var numericBuilder = new StringBuilder();
            foreach (var c in sanitizedVersion)
            {
                if (char.IsDigit(c) || c == '.')
                {
                    numericBuilder.Append(c);
                }
                else
                {
                    break;
                }
            }

            if (
                numericBuilder.Length > 0
                && Version.TryParse(numericBuilder.ToString(), out parsedVersion)
            )
            {
                version = parsedVersion;
                return true;
            }

            displayVersion = string.Empty;
            return false;
        }

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
                    if (
                        !TryParseReleaseVersion(
                            tagName,
                            out var latestVersion,
                            out var displayVersion
                        )
                    )
                    {
                        return updateMessage;
                    }

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
                        updateMessage.NewstVersion = displayVersion;

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

        public static void Cleanup()
        {
            var filename = GetExecutingFilePath();
            var tempFilename =
                Path.GetFileNameWithoutExtension(filename) + ".temp" + Path.GetExtension(filename);
            if (File.Exists(tempFilename))
            {
                File.Delete(tempFilename);
            }
        }

        public static string GetExecutingFilePath()
        {
            const string debuggingProcessPath = "/usr/share/dotnet/dotnet";
            if (
                Environment.ProcessPath is not null
                && Environment.ProcessPath != debuggingProcessPath
            )
            {
                return Environment.ProcessPath;
            }
            var fileNameFromProcess = Process.GetCurrentProcess().MainModule?.FileName;
            if (fileNameFromProcess is not null && fileNameFromProcess != debuggingProcessPath)
            {
                return fileNameFromProcess;
            }
            if (System.OperatingSystem.IsWindows())
            {
                if (
                    Environment
                        .GetCommandLineArgs()[0]
                        .EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                )
                {
                    return Environment.GetCommandLineArgs()[0];
                }
                else
                {
                    return Path.ChangeExtension(Environment.GetCommandLineArgs()[0], ".exe");
                }
            }
            else
            {
                return Environment.GetCommandLineArgs()[0];
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

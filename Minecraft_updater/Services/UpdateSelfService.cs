using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Minecraft_updater.Models;

namespace Minecraft_updater.Services
{
    public class UpdateSelfService
    {
        public static async Task ExecuteAsync(UpdateMessage _updateMessage)
        {
            // 檢查是否有下載 URL
            if (string.IsNullOrEmpty(_updateMessage.SHA1))
            {
                throw new Exception("無法取得更新下載連結，請確認您的作業系統是否支援");
            }

            var filename = GetExecutingFilePath();
            if (string.IsNullOrEmpty(filename))
            {
                throw new Exception("無法取得執行檔路徑");
            }

            var executableDir = Path.GetDirectoryName(filename);
            if (string.IsNullOrEmpty(executableDir))
            {
                throw new Exception("無法取得執行檔所在目錄");
            }

            var renamedFiles = new List<(string Original, string Temp)>();

            void RenameIfExists(string path)
            {
                if (!File.Exists(path))
                {
                    return;
                }

                var tempPath = GetTempFilePath(path);
                File.Move(path, tempPath, true);
                renamedFiles.Add((path, tempPath));
            }

            // 備份當前檔案與依賴的原生函式庫
            RenameIfExists(filename);
            foreach (var dependency in AvaloniaDependencyLibNames)
            {
                var dependencyPath = Path.Combine(executableDir, dependency);
                RenameIfExists(dependencyPath);
            }

            try
            {
                // 從 GitHub Release 下載對應平台的 zip 檔案
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Minecraft_updater/1.0");

                var zipUrl = _updateMessage.SHA1; // SHA1 欄位現在存放下載 URL
                var response = await httpClient.GetAsync(zipUrl);
                response.EnsureSuccessStatusCode();

                // 下載 zip 檔案到臨時位置
                var tempZipPath = Path.GetTempFileName();
                await using (var zipStream = await response.Content.ReadAsStreamAsync())
                await using (var zipFile = File.Create(tempZipPath))
                {
                    await zipStream.CopyToAsync(zipFile);
                }

                // 解壓縮 zip 檔案
                var tempExtractPath = Path.Combine(
                    Path.GetTempPath(),
                    $"Minecraft_updater_update_{Guid.NewGuid()}"
                );
                System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, tempExtractPath);

                // 尋找解壓縮後包含執行檔的目錄
                var executableName = Path.GetFileName(filename);
                Debug.WriteLine($"尋找執行檔: {executableName}");
                var sourceDir = FindExecutableDirectory(tempExtractPath, executableName);

                if (sourceDir == null)
                {
                    throw new Exception($"在更新檔案中找不到執行檔: {executableName}");
                }

                // 複製解壓縮後的所有檔案到執行檔目錄
                CopyDirectory(sourceDir, executableDir, true);

                // 清理臨時檔案
                File.Delete(tempZipPath);
                Directory.Delete(tempExtractPath, true);

                // 啟動新版本並結束當前程式
                var startInfo = new ProcessStartInfo(filename)
                {
                    Arguments = App.Command,
                    UseShellExecute = true,
                };
                Process.Start(startInfo);

                Environment.Exit(0);
            }
            catch
            {
                // 如果更新失敗，恢復舊版本
                for (var index = renamedFiles.Count - 1; index >= 0; index--)
                {
                    var (original, temp) = renamedFiles[index];
                    try
                    {
                        if (File.Exists(original))
                        {
                            File.Delete(original);
                        }

                        if (File.Exists(temp))
                        {
                            File.Move(temp, original, true);
                        }
                    }
                    catch
                    {
                        // 嘗試恢復所有檔案，即使其中一個失敗也繼續
                    }
                }
                throw;
            }
        }

        public static void Cleanup()
        {
            var filename = GetExecutingFilePath();
            var tempFilename = GetTempFilePath(filename);
            if (File.Exists(tempFilename))
            {
                File.Delete(tempFilename);
            }

            var executableDir = Path.GetDirectoryName(filename);
            if (string.IsNullOrEmpty(executableDir))
            {
                return;
            }

            foreach (var dependency in AvaloniaDependencyLibNames)
            {
                var tempDependencyPath = GetTempFilePath(Path.Combine(executableDir, dependency));
                if (File.Exists(tempDependencyPath))
                {
                    File.Delete(tempDependencyPath);
                }
            }
        }

        private static string GetExecutingFilePath()
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

        private static string? FindExecutableDirectory(string rootPath, string executableName)
        {
            // 先檢查根目錄是否包含執行檔
            var rootExePath = Path.Combine(rootPath, executableName);
            if (File.Exists(rootExePath))
            {
                return rootPath;
            }

            // 遞迴搜尋子目錄
            try
            {
                foreach (var subDir in Directory.GetDirectories(rootPath))
                {
                    var result = FindExecutableDirectory(subDir, executableName);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 忽略無權限存取的目錄
            }

            return null;
        }

        private static void CopyDirectory(string sourceDir, string destDir, bool overwrite)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"來源目錄不存在: {sourceDir}");
            }

            // 建立目標目錄（如果不存在）
            Directory.CreateDirectory(destDir);

            // 複製所有檔案
            foreach (var file in dir.GetFiles())
            {
                var targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, overwrite);
            }

            // 遞迴複製子目錄
            foreach (var subDir in dir.GetDirectories())
            {
                var newDestinationDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, overwrite);
            }
        }

        private static readonly HashSet<string> AvaloniaDependencyLibNames = new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "av_libglesv2.dll",
            "libHarfBuzzSharp.dll",
            "libSkiaSharp.dll",
            "libHarfBuzzSharp.so",
            "libSkiaSharp.so",
        };

        private static string GetTempFilePath(string originalPath)
        {
            var directory = Path.GetDirectoryName(originalPath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
            var extension = Path.GetExtension(originalPath);
            var tempFileName = $"{fileNameWithoutExtension}.temp{extension}";
            return directory is null ? tempFileName : Path.Combine(directory, tempFileName);
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Minecraft_updater.Models;
using Minecraft_updater.Services;

namespace Minecraft_updater.ViewModels
{
    public partial class UpdateSelfWindowViewModel : ViewModelBase
    {
        private readonly UpdateMessage _updateMessage;
        private readonly UpdatePreferencesService _updatePreferences;
        private readonly string? _initialSkippedVersion;
        private readonly bool _forceCheck;

        [ObservableProperty]
        private string _currentVersion = string.Empty;

        [ObservableProperty]
        private string _newVersion = string.Empty;

        [ObservableProperty]
        private string _message = string.Empty;

        [ObservableProperty]
        private string _updateButtonText = "更新";

        [ObservableProperty]
        private bool _isUpdateEnabled = true;

        [ObservableProperty]
        private bool _skipThisVersion;

        [ObservableProperty]
        private bool _disableFutureUpdates;

        public event EventHandler? UpdateCompleted;
        public event EventHandler? UpdateCancelled;

        public UpdateSelfWindowViewModel(
            UpdateMessage updateMessage,
            UpdatePreferencesService updatePreferences,
            bool forceCheck = false
        )
        {
            _updateMessage = updateMessage;
            _updatePreferences =
                updatePreferences ?? throw new ArgumentNullException(nameof(updatePreferences));
            _forceCheck = forceCheck;
            CurrentVersion =
                Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
            NewVersion = updateMessage.NewstVersion;
            Message = updateMessage.Message;
            _initialSkippedVersion = _updatePreferences.SkippedVersion;
            SkipThisVersion =
                !string.IsNullOrEmpty(_initialSkippedVersion)
                && string.Equals(
                    _initialSkippedVersion,
                    updateMessage.NewstVersion,
                    StringComparison.OrdinalIgnoreCase
                );
            DisableFutureUpdates = _updatePreferences.IsSelfUpdateDisabled;
        }

        [RelayCommand]
        private async Task UpdateAsync()
        {
            SavePreferences();
            UpdateButtonText = "下載中...";
            IsUpdateEnabled = false;

            try
            {
                // 檢查是否有下載 URL
                if (string.IsNullOrEmpty(_updateMessage.SHA1))
                {
                    throw new Exception("無法取得更新下載連結，請確認您的作業系統是否支援");
                }

                var filename = Environment.GetCommandLineArgs()[0];
                if (string.IsNullOrEmpty(filename))
                {
                    throw new Exception("無法取得執行檔路徑");
                }

                var tempFilename =
                    Path.GetFileNameWithoutExtension(filename)
                    + ".temp"
                    + Path.GetExtension(filename);

                // 備份當前檔案
                File.Move(filename, tempFilename, true);

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

                    // 獲取執行檔所在的目錄
                    var executableDir = Path.GetDirectoryName(filename);
                    if (string.IsNullOrEmpty(executableDir))
                    {
                        throw new Exception("無法取得執行檔所在目錄");
                    }

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

                    UpdateCompleted?.Invoke(this, EventArgs.Empty);
                    Environment.Exit(0);
                }
                catch
                {
                    // 如果更新失敗，恢復舊版本
                    if (File.Exists(filename))
                    {
                        File.Delete(filename);
                    }
                    File.Move(tempFilename, filename);
                    throw;
                }
            }
            catch (Exception ex)
            {
                UpdateButtonText = "更新失敗";
                IsUpdateEnabled = true;
                Message = $"更新失敗: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            SavePreferences();
            UpdateCancelled?.Invoke(this, EventArgs.Empty);
        }

        public void CommitPreferences() => SavePreferences();

        private void SavePreferences()
        {
            _updatePreferences.SetSelfUpdateDisabled(DisableFutureUpdates);

            if (SkipThisVersion)
            {
                _updatePreferences.SetSkippedVersion(_updateMessage.NewstVersion);
            }
            else if (
                !string.IsNullOrEmpty(_initialSkippedVersion)
                && string.Equals(
                    _initialSkippedVersion,
                    _updateMessage.NewstVersion,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                _updatePreferences.SetSkippedVersion(null);
            }
            else if (
                !_forceCheck
                && string.Equals(
                    _updatePreferences.SkippedVersion,
                    _updateMessage.NewstVersion,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                // If the current configuration still skips this version and the user unchecks it, clear it.
                _updatePreferences.SetSkippedVersion(null);
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

        private static string GetSHA1(string path)
        {
            try
            {
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var sha1 = SHA1.Create();
                var retval = sha1.ComputeHash(file);

                var sb = new StringBuilder();
                foreach (var b in retval)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }
    }
}

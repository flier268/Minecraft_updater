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

namespace Minecraft_updater.ViewModels
{
    public partial class UpdateSelfWindowViewModel : ViewModelBase
    {
        private readonly UpdateMessage _updateMessage;

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

        public event EventHandler? UpdateCompleted;
        public event EventHandler? UpdateCancelled;

        public UpdateSelfWindowViewModel(UpdateMessage updateMessage)
        {
            _updateMessage = updateMessage;
            CurrentVersion =
                Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
            NewVersion = updateMessage.NewstVersion;
            Message = updateMessage.Message;
        }

        [RelayCommand]
        private async Task UpdateAsync()
        {
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

                    // 找到解壓縮後的執行檔
                    var extractedFiles = Directory.GetFiles(
                        tempExtractPath,
                        Path.GetFileName(filename),
                        SearchOption.AllDirectories
                    );

                    if (extractedFiles.Length == 0)
                    {
                        throw new Exception(
                            $"在更新檔案中找不到執行檔: {Path.GetFileName(filename)}"
                        );
                    }

                    // 複製新版本到原位置
                    File.Copy(extractedFiles[0], filename, true);

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
            UpdateCancelled?.Invoke(this, EventArgs.Empty);
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

using System;
using System.Diagnostics;
using System.IO;
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
                var filename = Process.GetCurrentProcess().MainModule?.FileName;
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

                // 下載新版本
                using var httpClient = new HttpClient();
                var downloadUrl =
                    "https://gitlab.com/flier268/Minecraft_updater/raw/master/Release/Minecraft_updater.exe";
                var response = await httpClient.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();

                await using var fileStream = new FileStream(
                    filename,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );
                await response.Content.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
                fileStream.Close();

                // 驗證 SHA1
                var sha1 = GetSHA1(filename);
                if (!sha1.Equals(_updateMessage.SHA1, StringComparison.InvariantCultureIgnoreCase))
                {
                    // SHA1 不符，恢復舊版本
                    File.Delete(filename);
                    File.Move(tempFilename, filename);

                    UpdateButtonText = "更新";
                    IsUpdateEnabled = true;

                    throw new Exception("下載的檔案 SHA-1 驗證失敗，已恢復舊版本");
                }

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

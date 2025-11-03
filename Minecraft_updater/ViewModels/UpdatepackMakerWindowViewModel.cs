using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Minecraft_updater.Models;
using Minecraft_updater.Services;

namespace Minecraft_updater.ViewModels
{
    public partial class UpdatepackMakerWindowViewModel : ViewModelBase
    {
        private readonly IniFile _ini;
        private readonly string _appPath;
        private readonly char[] _delimiter = { '+', '-', '_' };

        [ObservableProperty]
        private string _baseUrl = "http://aaa.bb.com/";

        [ObservableProperty]
        private bool _addModToDelete = true;

        [ObservableProperty]
        private bool _addConfigToDelete = false;

        [ObservableProperty]
        private string _syncListText = string.Empty;

        [ObservableProperty]
        private string _deleteListText = string.Empty;

        [ObservableProperty]
        private string _downloadWhenNotExistText = string.Empty;

        public UpdatepackMakerWindowViewModel()
        {
            _appPath = AppContext.BaseDirectory;
            var configPath = Path.Combine(_appPath, "config.ini");
            _ini = new IniFile(configPath);

            // 載入設定
            var savedUrl = _ini.IniReadValue("Minecraft_updater", "updatepackMaker_BaseURL");
            if (!string.IsNullOrEmpty(savedUrl))
            {
                BaseUrl = savedUrl;
            }

            Log.LogFile = _ini.IniReadValue("Minecraft_updater", "LogFile").ToLower() == "true";
        }

        partial void OnBaseUrlChanged(string value)
        {
            _ini.IniWriteValue("Minecraft_updater", "updatepackMaker_BaseURL", value);
        }

        public async Task InitializeAsync()
        {
            // 檢查更新器自身的更新
            await CheckSelfUpdateAsync();
        }

        private async Task CheckSelfUpdateAsync()
        {
            Log.AddLine("檢查Minecraft updater是否有更新...");

            try
            {
                var filename = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                var tempFilename =
                    Path.GetFileNameWithoutExtension(filename)
                    + ".temp"
                    + Path.GetExtension(filename);
                if (File.Exists(tempFilename))
                {
                    File.Delete(tempFilename);
                }

                var updateMessage = await UpdateService.CheckUpdateAsync();
                if (updateMessage.HaveUpdate)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        var updateWindow = new Views.UpdateSelfWindow(
                            new UpdateSelfWindowViewModel(updateMessage)
                        );
                        if (
                            Avalonia.Application.Current?.ApplicationLifetime
                                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                            && desktop.MainWindow != null
                        )
                        {
                            await updateWindow.ShowDialog(desktop.MainWindow);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.AddLine($"檢查更新失敗: {ex.Message}");
            }
        }

        public void ProcessDroppedFiles(IEnumerable<string> paths, int targetListIndex)
        {
            var basepathLength = AppDomain.CurrentDomain.BaseDirectory.Length;
            var sb1 = new StringBuilder();
            var sb2 = new StringBuilder();
            var sb3 = new StringBuilder();

            foreach (var path in paths)
            {
                if (!path.StartsWith(AppDomain.CurrentDomain.BaseDirectory))
                {
                    // 檔案不在應用程式目錄下
                    continue;
                }

                if (File.Exists(path))
                {
                    ProcessFile(path, basepathLength, targetListIndex, sb1, sb2, sb3);
                }
                else if (Directory.Exists(path))
                {
                    var di = new DirectoryInfo(path);
                    foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        ProcessFile(fi.FullName, basepathLength, targetListIndex, sb1, sb2, sb3);
                    }
                }
            }

            SyncListText += sb1.ToString();
            DeleteListText += sb2.ToString();
            DownloadWhenNotExistText += sb3.ToString();
        }

        private void ProcessFile(
            string filePath,
            int basepathLength,
            int targetListIndex,
            StringBuilder sb1,
            StringBuilder sb2,
            StringBuilder sb3
        )
        {
            var name = filePath.Substring(basepathLength);
            var md5 = PrivateFunction.GetMD5(filePath);
            var url = BaseUrl + name.Replace("\\", "/");
            var delimiterIndex = name.IndexOfAny(_delimiter);

            switch (targetListIndex)
            {
                case 0: // 同步清單
                    sb1.AppendLine($"{name}||{md5}||{url}");
                    if (
                        (AddModToDelete && name.Contains("mod"))
                        || (AddConfigToDelete && name.Contains("config"))
                    )
                    {
                        var deleteName = name.Substring(
                            0,
                            delimiterIndex == -1 ? name.Length : delimiterIndex
                        );
                        sb2.AppendLine($"#{deleteName}||{md5}||");
                    }
                    break;

                case 1: // 刪除清單
                    var deleteNameDirect = name.Substring(
                        0,
                        delimiterIndex == -1 ? name.Length : delimiterIndex
                    );
                    sb2.AppendLine($"#{deleteNameDirect}||{md5}||");
                    break;

                case 2: // 不存在則添加清單
                    var downloadName = name.Substring(
                        0,
                        delimiterIndex == -1 ? name.Length : delimiterIndex
                    );
                    sb3.AppendLine($":{downloadName}||{md5}||{url}");
                    break;
            }
        }

        [RelayCommand]
        private void ClearSyncList()
        {
            SyncListText = string.Empty;
        }

        [RelayCommand]
        private void ClearDeleteList()
        {
            DeleteListText = string.Empty;
        }

        [RelayCommand]
        private void ClearDownloadWhenNotExistList()
        {
            DownloadWhenNotExistText = string.Empty;
        }

        [RelayCommand]
        private async Task SaveFileAsync()
        {
            // 儲存檔案將在 View 中處理（需要 StorageProvider）
            await Task.CompletedTask;
        }

        public async Task LoadFileAsync(string filePath)
        {
            try
            {
                var regex = new Regex("(.*?)\\|\\|(.*?)\\|\\|(.*)", RegexOptions.Singleline);
                var sb1 = new StringBuilder();
                var sb2 = new StringBuilder();
                var sb3 = new StringBuilder();

                using var reader = new StreamReader(filePath);
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line.StartsWith("#"))
                    {
                        sb2.AppendLine(line);
                    }
                    else if (line.StartsWith(":"))
                    {
                        sb3.AppendLine(line);
                    }
                    else
                    {
                        var match = regex.Match(line);
                        if (match.Success)
                        {
                            sb1.AppendLine(line);
                        }
                    }
                }

                SyncListText = sb1.ToString();
                DeleteListText = sb2.ToString();
                DownloadWhenNotExistText = sb3.ToString();
            }
            catch (Exception ex)
            {
                Log.AddLine($"載入檔案失敗: {ex.Message}");
            }
        }

        public string GetSaveContent()
        {
            var sb = new StringBuilder();
            sb.Append(SyncListText);
            sb.Append(DeleteListText);
            sb.Append(DownloadWhenNotExistText);
            return sb.ToString();
        }
    }
}

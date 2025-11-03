using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Minecraft_updater.Models;
using Minecraft_updater.Services;

namespace Minecraft_updater.ViewModels
{
    public partial class UpdaterWindowViewModel : ViewModelBase
    {
        private readonly IniFile _ini;
        private readonly string _appPath;
        private readonly string _url;
        private readonly bool _autoCloseAfterFinished;

        [ObservableProperty]
        private int _progressMax = 100;

        [ObservableProperty]
        private int _progressValue = 0;

        [ObservableProperty]
        private string _progressText = "目前進度：0/0";

        [ObservableProperty]
        private string _updateInfoText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _logMessages = new();

        public event EventHandler? SyncCompleted;

        public UpdaterWindowViewModel()
        {
            _appPath = AppContext.BaseDirectory;
            var configPath = Path.Combine(_appPath, "config.ini");
            _ini = new IniFile(configPath);

            _url = _ini.IniReadValue("Minecraft_updater", "scUrl");
            _autoCloseAfterFinished =
                _ini.IniReadValue("Minecraft_updater", "AutoClose_AfterFinishd").ToLower()
                == "true";
            Log.LogFile = _ini.IniReadValue("Minecraft_updater", "LogFile").ToLower() == "true";

            var version =
                Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            UpdateInfoText = $"Minecraft updater v{version}";
        }

        public async Task InitializeAsync()
        {
            // 檢查更新器自身的更新
            await CheckSelfUpdateAsync();

            // 開始檢查並同步包
            await CheckPackAsync();
        }

        private async Task CheckSelfUpdateAsync()
        {
            AddLog("檢查Minecraft updater是否有更新...");

            try
            {
                // 刪除舊的臨時檔案
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
                    // 需要在 UI 執行緒中顯示更新視窗
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
                else
                {
                    UpdateInfoText = "已經是最新版本";
                }
            }
            catch (Exception ex)
            {
                AddLog($"檢查更新失敗: {ex.Message}", "#FF0000");
            }
        }

        private async Task CheckPackAsync()
        {
            var list = new List<Pack>();

            // 下載清單
            var tempFile = PrivateFunction.CreateTmpFile();
            AddLog($"從 {_url} 下載Minecraft的Mod清單...");

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(_url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                await File.WriteAllTextAsync(tempFile, content, Encoding.UTF8);

                // 解析清單
                AddLog("解析中...");
                using var reader = new StreamReader(tempFile, Encoding.UTF8);
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        list.Add(Packs.Resolve(line));
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                AddLog($"取得PackList時失敗: {ex.Message}", "#FF0000");
                return;
            }
            catch (Exception ex)
            {
                AddLog($"取得PackList時失敗: {ex.Message}", "#FF0000");
                return;
            }
            finally
            {
                PrivateFunction.DeleteTmpFile(tempFile);
            }

            var totalCount = list.Count(x => !x.Delete);
            AddLog($"Minecraft的Mod清單下載完成，在清單上共有 {totalCount} 個檔案...");

            ProgressMax = totalCount;
            ProgressValue = 0;
            UpdateProgressText(0, totalCount);

            var haveUpdate = 0;

            try
            {
                var di = new DirectoryInfo(_appPath);
                var files = di.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Select(x => x.FullName)
                    .ToList();

                // 刪除檔案
                char[] delimiter = { '+', '-', '_' };
                var deleteList = list.Where(x => x.Delete).ToList();
                foreach (var item in deleteList)
                {
                    var matchedFiles = files
                        .Where(y =>
                        {
                            var temp = y.Substring(_appPath.Length + 1);
                            return temp.Length > item.Path.Length + 1
                                && delimiter.Contains(temp[item.Path.Length])
                                && temp.StartsWith(
                                    item.Path,
                                    StringComparison.InvariantCultureIgnoreCase
                                );
                        })
                        .ToList();

                    foreach (var file in matchedFiles)
                    {
                        if (PrivateFunction.GetMD5(file) != item.MD5)
                        {
                            try
                            {
                                File.Delete(file);
                                AddLog($"已刪除: {Path.GetFileName(file)}");
                            }
                            catch (IOException)
                            {
                                AddLog(
                                    $"刪除 {Path.GetFileName(file)} 時失敗，檔案正在使用中",
                                    "#FF0000"
                                );
                            }
                            catch (Exception e)
                            {
                                AddLog(
                                    $"刪除 {Path.GetFileName(file)} 時失敗: {e.Message}",
                                    "#FF0000"
                                );
                            }
                        }
                    }
                }

                // 新增/取代檔案
                var updateList = list.Where(x => !x.Delete && !x.DownloadWhenNotExist).ToList();
                foreach (var item in updateList)
                {
                    var filePath = Path.Combine(_appPath, item.Path);
                    var needUpdate = false;

                    if (File.Exists(filePath))
                    {
                        if (
                            !item.DownloadWhenNotExist
                            && PrivateFunction.GetMD5(filePath) != item.MD5
                        )
                        {
                            needUpdate = true;
                        }
                    }
                    else
                    {
                        AddLog($"{filePath} 不存在，檢查最新版本");
                        needUpdate = true;
                    }

                    if (needUpdate)
                    {
                        var success = await PrivateFunction.DownloadFileAsync(
                            item.URL,
                            filePath,
                            (msg) => AddLog(msg)
                        );

                        if (success)
                        {
                            AddLog($"{Path.GetFileName(item.Path)} 更新完成");
                        }
                        else
                        {
                            AddLog($"{Path.GetFileName(item.Path)} 更新失敗", "#FF0000");
                        }
                    }
                    else
                    {
                        AddLog($"{Path.GetFileName(item.Path)} 已是最新版本");
                    }

                    haveUpdate++;
                    UpdateProgress(haveUpdate, totalCount);
                }

                AddLog("同步完成！", "#00FF00");
                ProgressValue = totalCount;
                UpdateProgressText(totalCount, totalCount);

                SyncCompleted?.Invoke(this, EventArgs.Empty);

                if (_autoCloseAfterFinished)
                {
                    await Task.Delay(1000);
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                AddLog($"出現錯誤: {ex.Message}", "#FF0000");
            }
        }

        private void AddLog(string message, string? color = null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                LogMessages.Add(message);
                Log.AddLine(message);
            });
        }

        private void UpdateProgress(int current, int total)
        {
            var currentPercent = Math.Round((double)current / total, 2);
            var previousPercent = Math.Round((double)(current - 1) / total, 2);

            if (currentPercent - previousPercent > 0.01)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ProgressValue = current;
                    UpdateProgressText(current, total);
                });
            }
        }

        private void UpdateProgressText(int current, int total)
        {
            ProgressText = $"目前進度：{current}/{total}";
        }
    }
}

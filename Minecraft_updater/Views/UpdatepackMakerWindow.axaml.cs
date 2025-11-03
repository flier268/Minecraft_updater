using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Minecraft_updater.ViewModels;

namespace Minecraft_updater.Views
{
    public partial class UpdatepackMakerWindow : Window
    {
        private readonly UpdatepackMakerWindowViewModel? _viewModel;

        public UpdatepackMakerWindow()
        {
            InitializeComponent();
        }

        public UpdatepackMakerWindow(UpdatepackMakerWindowViewModel viewModel)
            : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;

            // 視窗載入後開始初始化
            Loaded += async (s, e) =>
            {
                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();
                }

                // 在 Loaded 事件中設定事件處理，確保控制項已初始化
                SetupEventHandlers();
            };
        }

        private void SetupEventHandlers()
        {
            // 訂閱自定義控制項的事件
            if (SyncListControl != null)
            {
                SyncListControl.SelectFilesRequested += OnSelectFilesRequested;
                SyncListControl.SelectFolderRequested += OnSelectFolderRequested;
                SetupControlDragDrop(SyncListControl);
            }

            if (DeleteListControl != null)
            {
                DeleteListControl.SelectFilesRequested += OnSelectFilesRequested;
                DeleteListControl.SelectFolderRequested += OnSelectFolderRequested;
                SetupControlDragDrop(DeleteListControl);
            }

            if (DownloadWhenNotExistListControl != null)
            {
                DownloadWhenNotExistListControl.SelectFilesRequested += OnSelectFilesRequested;
                DownloadWhenNotExistListControl.SelectFolderRequested += OnSelectFolderRequested;
                SetupControlDragDrop(DownloadWhenNotExistListControl);
            }
        }

        private void SetupControlDragDrop(FileListControl control)
        {
            var textBox = control.GetTextBox();
            if (textBox != null)
            {
                DragDrop.SetAllowDrop(textBox, true);
                textBox.AddHandler(DragDrop.DropEvent, OnDrop);
                textBox.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            }
        }

        private void OnSelectFilesRequested(object? sender, int listIndex)
        {
            _ = SelectFilesAsync(listIndex);
        }

        private void OnSelectFolderRequested(object? sender, int listIndex)
        {
            _ = SelectFolderAsync(listIndex);
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (e.DataTransfer.TryGetFiles() is not null)
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        private async void OnDrop(object? sender, DragEventArgs e)
        {
            if (_viewModel == null)
                return;

            var files = e.DataTransfer.TryGetFiles();
            if (files != null)
            {
                var paths = files.Select(f => f.Path.LocalPath).ToList();

                // 檢查檔案是否在應用程式目錄下
                var basePath = NormalizePath(AppDomain.CurrentDomain.BaseDirectory);
                if (
                    !paths.All(p =>
                        NormalizePath(p).StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    await ShowMessageAsync(
                        "錯誤",
                        "請將Minecraft_updater置於該目錄/檔案於同一目錄下\r\n詳情請查閱wiki"
                    );
                    return;
                }

                // 確定目標清單
                int listIndex = DetermineTargetList(e.Source);

                // 如果清單已有內容，詢問是否清除
                var currentText = listIndex switch
                {
                    0 => _viewModel.SyncListText,
                    1 => _viewModel.DeleteListText,
                    2 => _viewModel.DownloadWhenNotExistText,
                    _ => "",
                };

                if (!string.IsNullOrWhiteSpace(currentText))
                {
                    var result = await ShowConfirmAsync("已經含有資料", "不清除直接加入到最後?");
                    if (result == null)
                        return; // 取消

                    if (result == false) // 選擇清除
                    {
                        switch (listIndex)
                        {
                            case 0:
                                _viewModel.SyncListText = string.Empty;
                                break;
                            case 1:
                                _viewModel.DeleteListText = string.Empty;
                                break;
                            case 2:
                                _viewModel.DownloadWhenNotExistText = string.Empty;
                                break;
                        }
                    }
                }

                _viewModel.ProcessDroppedFiles(paths, listIndex);
            }
        }

        private int DetermineTargetList(object? source)
        {
            // 遍歷視覺樹找到對應的 FileListControl
            var element = source as Control;
            while (element != null)
            {
                if (element is FileListControl fileListControl)
                {
                    return fileListControl.ListIndex;
                }
                element = element.Parent as Control;
            }

            // 預設為同步清單
            return 0;
        }

        private async void OnLoadClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
                return;

            var files = await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Select a old sc File",
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("SC Files") { Patterns = new[] { "*.sc" } },
                        FilePickerFileTypes.All,
                    },
                    AllowMultiple = false,
                }
            );

            if (files.Count > 0)
            {
                await _viewModel.LoadFileAsync(files[0].Path.LocalPath);
            }
        }

        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
                return;

            var file = await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Save an sc File",
                    SuggestedFileName = "updatePackList.sc",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("SC Files") { Patterns = new[] { "*.sc" } },
                        FilePickerFileTypes.All,
                    },
                }
            );

            if (file != null)
            {
                var content = _viewModel.GetSaveContent();
                await File.WriteAllTextAsync(file.Path.LocalPath, content, Encoding.UTF8);
            }
        }

        private async Task SelectFilesAsync(int listIndex)
        {
            if (_viewModel == null)
                return;

            var files = await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions { Title = "選取檔案", AllowMultiple = true }
            );

            if (files.Count > 0)
            {
                var paths = files.Select(f => f.Path.LocalPath).ToList();
                await ProcessSelectedFiles(paths, listIndex);
            }
        }

        private async Task SelectFolderAsync(int listIndex)
        {
            if (_viewModel == null)
                return;

            var folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = "選取資料夾", AllowMultiple = false }
            );

            if (folders.Count > 0)
            {
                var paths = folders.Select(f => f.Path.LocalPath).ToList();
                await ProcessSelectedFiles(paths, listIndex);
            }
        }

        private async Task ProcessSelectedFiles(List<string> paths, int listIndex)
        {
            if (_viewModel == null)
                return;

            // 檢查檔案是否在應用程式目錄下
            var basePath = NormalizePath(AppDomain.CurrentDomain.BaseDirectory);
            if (
                !paths.All(p =>
                    NormalizePath(p).StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                await ShowMessageAsync(
                    "錯誤",
                    "請將Minecraft_updater置於該目錄/檔案於同一目錄下\r\n詳情請查閱wiki"
                );
                return;
            }

            // 如果清單已有內容，詢問是否清除
            var currentText = listIndex switch
            {
                0 => _viewModel.SyncListText,
                1 => _viewModel.DeleteListText,
                2 => _viewModel.DownloadWhenNotExistText,
                _ => "",
            };

            if (!string.IsNullOrWhiteSpace(currentText))
            {
                var result = await ShowConfirmAsync("已經含有資料", "不清除直接加入到最後?");
                if (result == null)
                    return; // 取消

                if (result == false) // 選擇清除
                {
                    switch (listIndex)
                    {
                        case 0:
                            _viewModel.SyncListText = string.Empty;
                            break;
                        case 1:
                            _viewModel.DeleteListText = string.Empty;
                            break;
                        case 2:
                            _viewModel.DownloadWhenNotExistText = string.Empty;
                            break;
                    }
                }
            }

            _viewModel.ProcessDroppedFiles(paths, listIndex);
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            var messageBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
                title,
                message,
                MsBox.Avalonia.Enums.ButtonEnum.Ok
            );
            await messageBox.ShowAsync();
        }

        private async Task<bool?> ShowConfirmAsync(string title, string message)
        {
            var messageBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
                title,
                message,
                MsBox.Avalonia.Enums.ButtonEnum.YesNoAbort
            );
            var result = await messageBox.ShowAsync();

            return result switch
            {
                MsBox.Avalonia.Enums.ButtonResult.Yes => true,
                MsBox.Avalonia.Enums.ButtonResult.No => false,
                _ => null,
            };
        }

        private string NormalizePath(string path)
        {
            // 標準化路徑：移除末尾斜線、統一使用正斜線
            var normalized = Path.GetFullPath(path).Replace('\\', '/');
            return normalized.TrimEnd('/');
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Minecraft_updater.Models;
using Minecraft_updater.ViewModels;
using Minecraft_updater.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Minecraft_updater;

public partial class App : Application
{
    public static List<string> Args { get; set; } = new List<string>();
    public static string Command { get; set; } = string.Empty;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // 根據命令列參數決定要顯示哪個視窗
            if (Args.Count > 0)
            {
                Command = Args[0];

                if (Args[0].Equals(ListCommand.UpdatepackMaker))
                {
                    // 創建 UpdatepackMaker 視窗
                    var viewModel = new UpdatepackMakerWindowViewModel();
                    desktop.MainWindow = new Views.UpdatepackMakerWindow(viewModel);
                }
                else if (Args[0].Equals(ListCommand.CheckUpdate))
                {
                    // 創建 Updater 視窗
                    var viewModel = new UpdaterWindowViewModel();
                    desktop.MainWindow = new Views.UpdaterWindow(viewModel);
                }
                else if (Args[0].Equals(ListCommand.CheckUpdaterVersion))
                {
                    // 檢查更新器版本並自動更新
                    desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    Task.Run(async () =>
                    {
                        var updateMessage = await Services.UpdateService.CheckUpdateAsync();
                        if (updateMessage.HaveUpdate)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                var updateWindow = new Views.UpdateSelfWindow(
                                    new UpdateSelfWindowViewModel(updateMessage)
                                );
                                desktop.MainWindow = updateWindow;
                                updateWindow.Show();
                            });
                        }
                        else
                        {
                            desktop.Shutdown();
                        }
                    });
                }
                else
                {
                    // 未知的命令參數
                    ShowUsageMessage(desktop);
                }
            }
            else
            {
                // 沒有參數，顯示使用說明
                ShowUsageMessage(desktop);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowUsageMessage(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var sb = new StringBuilder();
        sb.AppendLine("請使用附加參數啟動");
        sb.AppendLine("檢查Minecraft懶人包更新：");
        sb.AppendLine($"Minecraft_updater.exe {ListCommand.CheckUpdate}");
        sb.AppendLine();
        sb.AppendLine("Minecraft懶人包之檔案清單建立工具：");
        sb.AppendLine($"Minecraft_updater.exe {ListCommand.UpdatepackMaker}");

        // 使用 Avalonia 的訊息框顯示訊息
        var messageBox = MessageBoxManager.GetMessageBoxStandard(
            "Minecraft Updater",
            sb.ToString(),
            ButtonEnum.Ok
        );

        // 需要等待訊息框關閉後再關閉應用程式
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var mainWindow = new Window
        {
            Width = 1,
            Height = 1,
            SystemDecorations = Avalonia.Controls.SystemDecorations.None,
            ShowInTaskbar = false,
        };
        desktop.MainWindow = mainWindow;
        mainWindow.Show();

        Task.Run(async () =>
        {
            await Task.Delay(100); // 等待視窗完全載入
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await messageBox.ShowWindowAsync();
                desktop.Shutdown();
            });
        });
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access",
        Justification = "BindingPlugins.DataValidators is used in a controlled way that is safe for AOT"
    )]
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove = BindingPlugins
            .DataValidators.OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}

using System;
using System.Linq;
using Avalonia;
using Avalonia.Skia;
using Avalonia.X11;

namespace Minecraft_updater;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 儲存命令列參數供 App 使用
        App.Args = args.ToList();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new X11PlatformOptions { EnableMultiTouch = false, UseDBusMenu = true })
            .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 });
}

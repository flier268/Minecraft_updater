using System.Reflection;

namespace Minecraft_updater.ViewModels;

public class AboutWindowViewModel
{
    public string ApplicationName => "Minecraft Updater";

    public string Version { get; } = GetVersion();

    public string GitHubUrl => "https://github.com/flier268/Minecraft_updater";

    public string Description => "Minecraft伺服器更新工具，協助管理更新包與自動更新程序。";

    private static string GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version =
            assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? assembly.GetName().Version?.ToString();
        return string.IsNullOrWhiteSpace(version) ? "未知版本" : version;
    }
}

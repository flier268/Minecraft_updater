using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Minecraft_updater.Services;
using Minecraft_updater.ViewModels;

namespace Minecraft_updater.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        if (DataContext is null)
        {
            DataContext = new AboutWindowViewModel();
        }
    }

    private void OnGitHubLinkPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is AboutWindowViewModel vm)
        {
            LinkNavigator.OpenUrl(vm.GitHubUrl);
        }
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

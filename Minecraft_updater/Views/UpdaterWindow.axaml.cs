using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Minecraft_updater.ViewModels;

namespace Minecraft_updater.Views
{
    public partial class UpdaterWindow : Window
    {
        private readonly UpdaterWindowViewModel? _viewModel;

        public UpdaterWindow()
        {
            InitializeComponent();
        }

        public UpdaterWindow(UpdaterWindowViewModel viewModel)
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
            };

            // 同步完成後的處理
            if (_viewModel != null)
            {
                _viewModel.SyncCompleted += (s, e) => {
                    // 可以在這裡顯示完成訊息
                };
            }
        }

        private void OnUpdateInfoClicked(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "https://gitlab.com/flier268/Minecraft_updater/tags",
                    UseShellExecute = true,
                };
                Process.Start(psi);
            }
            catch
            {
                // 忽略錯誤
            }
        }
    }
}

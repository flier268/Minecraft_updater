using Avalonia.Controls;
using Minecraft_updater.ViewModels;

namespace Minecraft_updater.Views
{
    public partial class UpdateSelfWindow : Window
    {
        public UpdateSelfWindow()
        {
            InitializeComponent();
        }

        public UpdateSelfWindow(UpdateSelfWindowViewModel viewModel)
            : this()
        {
            DataContext = viewModel;

            // 訂閱事件以關閉視窗
            viewModel.UpdateCancelled += (s, e) => Close();
            viewModel.UpdateCompleted += (s, e) => Close();

            Closing += (_, _) => viewModel.CommitPreferences();
        }
    }
}

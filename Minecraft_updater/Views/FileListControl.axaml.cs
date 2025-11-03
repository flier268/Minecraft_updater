using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Minecraft_updater.Views
{
    public partial class FileListControl : UserControl
    {
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<
            FileListControl,
            string
        >(nameof(Title), "Title");

        public static readonly StyledProperty<string> ListTextProperty = AvaloniaProperty.Register<
            FileListControl,
            string
        >(nameof(ListText), string.Empty);

        public static readonly StyledProperty<ICommand?> ClearCommandProperty =
            AvaloniaProperty.Register<FileListControl, ICommand?>(nameof(ClearCommand));

        public static readonly StyledProperty<int> ListIndexProperty = AvaloniaProperty.Register<
            FileListControl,
            int
        >(nameof(ListIndex), 0);

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string ListText
        {
            get => GetValue(ListTextProperty);
            set => SetValue(ListTextProperty, value);
        }

        public ICommand? ClearCommand
        {
            get => GetValue(ClearCommandProperty);
            set => SetValue(ClearCommandProperty, value);
        }

        public int ListIndex
        {
            get => GetValue(ListIndexProperty);
            set => SetValue(ListIndexProperty, value);
        }

        // 用於父窗口訂閱的事件
        public event EventHandler<int>? SelectFilesRequested;
        public event EventHandler<int>? SelectFolderRequested;

        public FileListControl()
        {
            InitializeComponent();

            // 設置拖放支持
            Loaded += (s, e) => SetupDragDrop();
        }

        private void SetupDragDrop()
        {
            if (ListTextBox != null)
            {
                DragDrop.SetAllowDrop(ListTextBox, true);
                ListTextBox.AddHandler(DragDrop.DropEvent, OnDrop);
                ListTextBox.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            }
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

        private void OnDrop(object? sender, DragEventArgs e)
        {
            // 將拖放事件冒泡到父窗口處理
            var dropEvent = new RoutedEventArgs(DragDrop.DropEvent);
            RaiseEvent(dropEvent);

            // 傳遞原始的 DragEventArgs 給父窗口
            if (Parent is Control parent)
            {
                parent.RaiseEvent(e);
            }
        }

        private void OnSelectFiles(object? sender, RoutedEventArgs e)
        {
            SelectFilesRequested?.Invoke(this, ListIndex);
        }

        private void OnSelectFolder(object? sender, RoutedEventArgs e)
        {
            SelectFolderRequested?.Invoke(this, ListIndex);
        }

        // 提供獲取 TextBox 的方法，供父窗口使用
        public TextBox? GetTextBox() => ListTextBox;
    }
}

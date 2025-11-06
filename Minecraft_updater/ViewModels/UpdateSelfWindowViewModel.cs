using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Minecraft_updater.Models;
using Minecraft_updater.Services;

namespace Minecraft_updater.ViewModels
{
    public partial class UpdateSelfWindowViewModel : ViewModelBase
    {
        private readonly UpdateMessage _updateMessage;
        private readonly UpdatePreferencesService _updatePreferences;
        private readonly string? _initialSkippedVersion;
        private readonly bool _forceCheck;

        [ObservableProperty]
        private string _currentVersion = string.Empty;

        [ObservableProperty]
        private string _newVersion = string.Empty;

        [ObservableProperty]
        private string _message = string.Empty;

        [ObservableProperty]
        private string _updateButtonText = "更新";

        [ObservableProperty]
        private bool _isUpdateEnabled = true;

        [ObservableProperty]
        private bool _skipThisVersion;

        [ObservableProperty]
        private bool _disableFutureUpdates;

        public event EventHandler? UpdateCancelled;

        public UpdateSelfWindowViewModel(
            UpdateMessage updateMessage,
            UpdatePreferencesService updatePreferences,
            bool forceCheck = false
        )
        {
            _updateMessage = updateMessage;
            _updatePreferences =
                updatePreferences ?? throw new ArgumentNullException(nameof(updatePreferences));
            _forceCheck = forceCheck;
            CurrentVersion =
                Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
            NewVersion = updateMessage.NewstVersion;
            Message = updateMessage.Message;
            _initialSkippedVersion = _updatePreferences.SkippedVersion;
            SkipThisVersion =
                !string.IsNullOrEmpty(_initialSkippedVersion)
                && string.Equals(
                    _initialSkippedVersion,
                    updateMessage.NewstVersion,
                    StringComparison.OrdinalIgnoreCase
                );
            DisableFutureUpdates = _updatePreferences.IsSelfUpdateDisabled;
        }

        [RelayCommand]
        private async Task UpdateAsync()
        {
            SavePreferences();
            UpdateButtonText = "下載中...";
            IsUpdateEnabled = false;
            try
            {
                await UpdateSelfService.ExecuteAsync(_updateMessage);
            }
            catch (Exception ex)
            {
                UpdateButtonText = "更新失敗";
                IsUpdateEnabled = true;
                Message = $"更新失敗: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            SavePreferences();
            UpdateCancelled?.Invoke(this, EventArgs.Empty);
        }

        public void CommitPreferences() => SavePreferences();

        private void SavePreferences()
        {
            _updatePreferences.SetSelfUpdateDisabled(DisableFutureUpdates);

            if (SkipThisVersion)
            {
                _updatePreferences.SetSkippedVersion(_updateMessage.NewstVersion);
            }
            else if (
                !string.IsNullOrEmpty(_initialSkippedVersion)
                && string.Equals(
                    _initialSkippedVersion,
                    _updateMessage.NewstVersion,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                _updatePreferences.SetSkippedVersion(null);
            }
            else if (
                !_forceCheck
                && string.Equals(
                    _updatePreferences.SkippedVersion,
                    _updateMessage.NewstVersion,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                // If the current configuration still skips this version and the user unchecks it, clear it.
                _updatePreferences.SetSkippedVersion(null);
            }
        }
    }
}

using System;
using Minecraft_updater.Models;

namespace Minecraft_updater.Services
{
    public class UpdatePreferencesService
    {
        private const string Section = "Minecraft_updater";
        private const string DisableKey = "DisableSelfUpdate";
        private const string SkipKey = "SkippedVersion";

        private readonly IniFile _ini;

        public UpdatePreferencesService(IniFile ini)
        {
            _ini = ini ?? throw new ArgumentNullException(nameof(ini));
        }

        public bool IsSelfUpdateDisabled
        {
            get
            {
                var value = _ini.IniReadValue(Section, DisableKey);
                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public string? SkippedVersion
        {
            get
            {
                var value = _ini.IniReadValue(Section, SkipKey);
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        public void SetSelfUpdateDisabled(bool disabled) =>
            _ini.IniWriteValue(Section, DisableKey, disabled ? "true" : "false");

        public void SetSkippedVersion(string? version)
        {
            _ini.IniWriteValue(Section, SkipKey, version ?? string.Empty);
        }
    }
}

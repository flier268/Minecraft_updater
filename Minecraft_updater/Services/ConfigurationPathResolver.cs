using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Minecraft_updater.Models;

namespace Minecraft_updater.Services
{
    /// <summary>
    /// Provides helpers for resolving and preparing the configuration file path.
    /// </summary>
    public static class ConfigurationPathResolver
    {
        public const string DefaultFileName = "Minecraft_updater.ini";
        public const string LegacyFileName = "config.ini";

        public static string DetermineConfigPath(IEnumerable<string> args, string baseDirectory)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            baseDirectory ??= AppContext.BaseDirectory;

            var arguments = args.ToList();
            string? customPath = null;

            foreach (var argument in arguments)
            {
                if (
                    argument.StartsWith("--config=", StringComparison.OrdinalIgnoreCase)
                    || argument.StartsWith("-c=", StringComparison.OrdinalIgnoreCase)
                )
                {
                    var parts = argument.Split('=', 2);
                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                    {
                        customPath = parts[1].Trim('"');
                        break;
                    }
                }
            }

            if (customPath == null)
            {
                for (var i = 0; i < arguments.Count; i++)
                {
                    if (
                        string.Equals(arguments[i], "--config", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(arguments[i], "-c", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        if (i + 1 < arguments.Count && !arguments[i + 1].StartsWith("-"))
                        {
                            customPath = arguments[i + 1].Trim('"');
                        }
                        break;
                    }
                }
            }

            var effectivePath = string.IsNullOrWhiteSpace(customPath)
                ? Path.Combine(baseDirectory, DefaultFileName)
                : customPath!;

            if (!Path.IsPathRooted(effectivePath))
            {
                effectivePath = Path.Combine(baseDirectory, effectivePath);
            }

            return Path.GetFullPath(effectivePath);
        }

        public static string EnsureConfigurationFile(string configPath, string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                throw new ArgumentException(
                    "Config path cannot be null or empty.",
                    nameof(configPath)
                );
            }

            baseDirectory ??= AppContext.BaseDirectory;

            if (File.Exists(configPath))
            {
                return configPath;
            }

            var legacyPath = Path.Combine(baseDirectory, LegacyFileName);
            if (!File.Exists(legacyPath))
            {
                return configPath;
            }

            try
            {
                var legacyConfig = new IniFile(legacyPath);
                var scUrl = legacyConfig.IniReadValue("Minecraft_updater", "scUrl");
                if (!string.IsNullOrWhiteSpace(scUrl))
                {
                    var directory = Path.GetDirectoryName(configPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.Copy(legacyPath, configPath, overwrite: false);
                }
            }
            catch
            {
                // Swallow exceptions: if migration fails we fall back to the new empty file.
            }

            return configPath;
        }
    }
}

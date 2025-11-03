using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Minecraft_updater.Models
{
    /// <summary>
    /// 跨平台的 INI 檔案讀寫類別
    /// </summary>
    public class IniFile
    {
        public string Path { get; set; }
        private readonly Dictionary<string, Dictionary<string, string>> _data = new();

        public IniFile(string iniPath)
        {
            Path = iniPath;
            LoadFile();
        }

        private void LoadFile()
        {
            if (!File.Exists(Path))
                return;

            try
            {
                var currentSection = "";
                foreach (var line in File.ReadAllLines(Path, Encoding.UTF8))
                {
                    var trimmed = line.Trim();

                    // 略過空行和註解
                    if (
                        string.IsNullOrWhiteSpace(trimmed)
                        || trimmed.StartsWith(";")
                        || trimmed.StartsWith("#")
                    )
                        continue;

                    // 解析 section
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        currentSection = trimmed.Substring(1, trimmed.Length - 2);
                        if (!_data.ContainsKey(currentSection))
                            _data[currentSection] = new Dictionary<string, string>();
                        continue;
                    }

                    // 解析 key=value
                    var equalsIndex = trimmed.IndexOf('=');
                    if (equalsIndex > 0 && !string.IsNullOrEmpty(currentSection))
                    {
                        var key = trimmed.Substring(0, equalsIndex).Trim();
                        var value = trimmed.Substring(equalsIndex + 1).Trim();
                        _data[currentSection][key] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"載入 INI 檔案失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 寫入 INI 檔
        /// </summary>
        public void IniWriteValue(string section, string key, string value, string encode = "utf-8")
        {
            if (!_data.ContainsKey(section))
                _data[section] = new Dictionary<string, string>();

            _data[section][key] = value;
            SaveFile();
        }

        /// <summary>
        /// 讀取 INI 檔
        /// </summary>
        public string IniReadValue(
            string section,
            string key,
            string encodingName = "utf-8",
            int size = 1024
        )
        {
            if (_data.ContainsKey(section) && _data[section].ContainsKey(key))
                return _data[section][key];

            return string.Empty;
        }

        private void SaveFile()
        {
            try
            {
                using var writer = new StreamWriter(Path, false, Encoding.UTF8);
                foreach (var section in _data)
                {
                    writer.WriteLine($"[{section.Key}]");
                    foreach (var pair in section.Value)
                    {
                        writer.WriteLine($"{pair.Key}={pair.Value}");
                    }
                    writer.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"儲存 INI 檔案失敗: {ex.Message}");
            }
        }
    }
}

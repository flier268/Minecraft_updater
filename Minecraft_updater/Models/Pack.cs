using System;
using System.Text.RegularExpressions;

namespace Minecraft_updater.Models
{
    public struct Pack
    {
        public bool Delete { get; set; }
        public bool DownloadWhenNotExist { get; set; }
        public string MD5 { get; set; }
        public string URL { get; set; }
        public string Path { get; set; }
        public bool IsChecked { get; set; }
        public string? MinimumVersion { get; set; }
    }

    public static class Packs
    {
        static readonly Regex r = new Regex("(.*?)\\|\\|(.*?)\\|\\|(.*)", RegexOptions.Singleline);

        /// <summary>
        /// 解析 Pack List 行
        /// </summary>
        /// <param name="s">要解析的行</param>
        /// <param name="minimumVersion">檔案中定義的最低版本號（從 MinVersion||x.x.x.x 行讀取）</param>
        /// <returns>Pack 結構</returns>
        public static Pack Resolve(string s, string? minimumVersion = null)
        {
            Match m;
            bool delete = false;
            bool downloadWhenNotExist = false;

            if (s.StartsWith("#"))
            {
                delete = true;
                m = r.Match(s.Substring(1, s.Length - 1));
            }
            else if (s.StartsWith(":"))
            {
                downloadWhenNotExist = true;
                m = r.Match(s.Substring(1, s.Length - 1));
            }
            else
            {
                m = r.Match(s);
            }

            if (m.Success)
            {
                return new Pack
                {
                    Path = m.Groups[1].ToString(),
                    MD5 = m.Groups[2]?.ToString() ?? "",
                    URL = m.Groups[3]?.ToString() ?? "",
                    Delete = delete,
                    IsChecked = false,
                    DownloadWhenNotExist = downloadWhenNotExist,
                    MinimumVersion = minimumVersion,
                };
            }
            else
            {
                return new Pack { };
            }
        }

        /// <summary>
        /// 嘗試從行中解析最低版本號
        /// 格式: MinVersion||x.x.x.x 或 MinVersion||x.x.x.x||
        /// 註: 為了向後兼容，建議使用三個 || 分隔符的格式 (MinVersion||x.x.x.x||)
        ///     這樣舊版本在解析時只會嘗試查找名為 "MinVersion" 的檔案，不會出錯
        /// </summary>
        public static string? TryParseMinimumVersion(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var trimmedLine = line.Trim();
            if (
                trimmedLine.StartsWith("MinVersion||", StringComparison.OrdinalIgnoreCase)
                || trimmedLine.StartsWith("MinimumVersion||", StringComparison.OrdinalIgnoreCase)
            )
            {
                var parts = trimmedLine.Split(["||"], StringSplitOptions.None);
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    return parts[1].Trim();
                }
            }

            return null;
        }
    }
}

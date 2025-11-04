using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Minecraft_updater.Models;

namespace Minecraft_updater.Services
{
    /// <summary>
    /// 負責解析 Pack 檔案格式
    /// </summary>
    public class PackDeserializerService
    {
        private static readonly Regex PackLineRegex = new(
            "(.*?)\\|\\|(.*?)\\|\\|(.*)",
            RegexOptions.Singleline | RegexOptions.Compiled
        );

        /// <summary>
        /// 解析單行 Pack 字串
        /// </summary>
        /// <param name="line">要解析的行</param>
        /// <param name="minimumVersion">檔案中定義的最低版本號（選填）</param>
        /// <returns>解析後的 Pack，若格式不正確則返回 null</returns>
        public Pack? DeserializeLine(string line, string? minimumVersion = null)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            bool delete = false;
            bool downloadWhenNotExist = false;
            string processLine = line;

            // 檢查前綴
            if (line.StartsWith("#"))
            {
                delete = true;
                processLine = line.Substring(1);
            }
            else if (line.StartsWith(":"))
            {
                downloadWhenNotExist = true;
                processLine = line.Substring(1);
            }

            // 使用 Regex 解析
            var match = PackLineRegex.Match(processLine);
            if (!match.Success)
                return null;

            return new Pack
            {
                Path = match.Groups[1].ToString(),
                SHA256 = match.Groups[2]?.ToString() ?? "",
                URL = match.Groups[3]?.ToString() ?? "",
                Delete = delete,
                IsChecked = false,
                DownloadWhenNotExist = downloadWhenNotExist,
                MinimumVersion = minimumVersion,
            };
        }

        /// <summary>
        /// 解析完整的 Pack 檔案內容
        /// </summary>
        /// <param name="content">檔案內容</param>
        /// <returns>Pack 列表和最低版本號</returns>
        public (List<Pack> Packs, string? MinimumVersion) DeserializeFile(string content)
        {
            var packs = new List<Pack>();
            string? minVersion = null;

            if (string.IsNullOrWhiteSpace(content))
                return (packs, minVersion);

            var lines = content.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                // 嘗試解析版本號（支援新舊格式）
                var versionFromLine = TryParseMinimumVersion(trimmedLine);
                if (versionFromLine != null)
                {
                    minVersion = versionFromLine;
                    continue; // 版本行不是 Pack 資料，跳過
                }

                // 解析 Pack 行
                var pack = DeserializeLine(trimmedLine, minVersion);
                if (pack.HasValue)
                {
                    packs.Add(pack.Value);
                }
            }

            return (packs, minVersion);
        }

        /// <summary>
        /// 嘗試從行中解析最低版本號
        /// 支援格式:
        /// - 新格式: MinVersion=x.x.x.x
        /// - 舊格式: MinVersion||x.x.x.x 或 MinVersion||x.x.x.x||
        /// </summary>
        private string? TryParseMinimumVersion(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var trimmedLine = line.Trim();

            // 新格式: MinVersion=x.x.x.x
            if (
                trimmedLine.StartsWith("MinVersion=", StringComparison.OrdinalIgnoreCase)
                || trimmedLine.StartsWith("MinimumVersion=", StringComparison.OrdinalIgnoreCase)
            )
            {
                var parts = trimmedLine.Split('=', 2);
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    return parts[1].Trim();
                }
            }

            // 舊格式: MinVersion||x.x.x.x 或 MinVersion||x.x.x.x||
            if (
                trimmedLine.StartsWith("MinVersion||", StringComparison.OrdinalIgnoreCase)
                || trimmedLine.StartsWith("MinimumVersion||", StringComparison.OrdinalIgnoreCase)
            )
            {
                var parts = trimmedLine.Split(new[] { "||" }, StringSplitOptions.None);
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    return parts[1].Trim();
                }
            }

            return null;
        }
    }
}

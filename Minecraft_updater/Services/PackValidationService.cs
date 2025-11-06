using System;
using System.Text.RegularExpressions;
using Minecraft_updater.Models;

namespace Minecraft_updater.Services
{
    /// <summary>
    /// Pack 資料驗證結果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }

        public static ValidationResult Success() => new ValidationResult { IsValid = true };

        public static ValidationResult Failure(string errorMessage) =>
            new ValidationResult { IsValid = false, ErrorMessage = errorMessage };
    }

    /// <summary>
    /// 負責驗證 Pack 資料的完整性和安全性
    /// </summary>
    public class PackValidationService
    {
        private static readonly Regex SHA256Regex = new("^[a-fA-F0-9]{64}$", RegexOptions.Compiled);
        private static readonly Regex UrlRegex = new(
            @"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        /// <summary>
        /// 驗證 SHA256 雜湊值格式（64 個十六進位字元）
        /// </summary>
        public bool ValidateSHA256(string sha256)
        {
            if (string.IsNullOrWhiteSpace(sha256))
                return false;

            return SHA256Regex.IsMatch(sha256);
        }

        /// <summary>
        /// 驗證 URL 格式
        /// </summary>
        public bool ValidateUrl(string url)
        {
            // 允許空 URL（用於刪除操作）
            if (string.IsNullOrWhiteSpace(url))
                return true;

            return UrlRegex.IsMatch(url);
        }

        /// <summary>
        /// 驗證路徑安全性（檢查路徑穿越攻擊）
        /// </summary>
        public bool ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // 檢查路徑穿越模式
            if (path.Contains(".."))
                return false;

            // 檢查絕對路徑（不允許以 / 或 \ 開頭）
            if (path.StartsWith("/") || path.StartsWith("\\"))
                return false;

            // Windows 磁碟機路徑檢查（例如 C:\）
            if (path.Length >= 2 && path[1] == ':')
                return false;

            return true;
        }

        /// <summary>
        /// 全面驗證 Pack 物件
        /// </summary>
        public ValidationResult ValidatePack(Pack pack)
        {
            // 驗證路徑
            if (!ValidatePath(pack.Path))
            {
                return ValidationResult.Failure($"無效或不安全的路徑: {pack.Path}");
            }

            // SHA256 可以是空字串（用於某些特殊操作），但如果有值就要驗證格式
            if (!string.IsNullOrWhiteSpace(pack.SHA256) && !ValidateSHA256(pack.SHA256))
            {
                return ValidationResult.Failure($"無效的 SHA256 格式: {pack.SHA256}");
            }

            // 如果不是刪除操作，則需要有效的 URL（包括一般同步和"僅在不存在時下載"）
            if (!pack.Delete)
            {
                if (string.IsNullOrWhiteSpace(pack.URL))
                {
                    if (pack.DownloadWhenNotExist)
                    {
                        return ValidationResult.Failure("僅在不存在時下載操作需要提供 URL");
                    }
                    return ValidationResult.Failure("一般同步操作需要提供 URL");
                }

                if (!ValidateUrl(pack.URL))
                {
                    return ValidationResult.Failure($"無效的 URL 格式: {pack.URL}");
                }
            }

            return ValidationResult.Success();
        }
    }
}

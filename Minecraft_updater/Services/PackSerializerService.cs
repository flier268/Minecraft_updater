using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Minecraft_updater.Models;

namespace Minecraft_updater.Services
{
    /// <summary>
    /// 負責將 Pack 物件序列化為字串格式
    /// </summary>
    public class PackSerializerService
    {
        private const string Delimiter = "||";

        /// <summary>
        /// 將單個 Pack 序列化為一行字串
        /// 格式: Path||MD5||URL
        /// 前綴: # = 刪除, : = 僅在檔案不存在時下載
        /// </summary>
        public string SerializeLine(Pack pack)
        {
            var prefix =
                pack.Delete ? "#"
                : pack.DownloadWhenNotExist ? ":"
                : "";
            return $"{prefix}{pack.Path}{Delimiter}{pack.MD5}{Delimiter}{pack.URL}";
        }

        /// <summary>
        /// 將 Pack 集合序列化為完整檔案內容
        /// </summary>
        /// <param name="packs">Pack 集合</param>
        /// <param name="minVersion">最低版本號（選填）</param>
        /// <returns>完整的檔案內容</returns>
        public string SerializeFile(IEnumerable<Pack> packs, string? minVersion = null)
        {
            var sb = new StringBuilder();

            // 如果有最低版本號，寫入第一行
            if (!string.IsNullOrWhiteSpace(minVersion))
            {
                sb.AppendLine($"MinVersion={minVersion}");
            }

            // 序列化每個 Pack
            foreach (var pack in packs)
            {
                sb.AppendLine(SerializeLine(pack));
            }

            return sb.ToString();
        }
    }
}

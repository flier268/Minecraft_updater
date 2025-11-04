using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_updater.Services
{
    public class PrivateFunction
    {
        #region SHA256 計算
        public static string GetSHA256(string filepath)
        {
            using var targetFile = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            using var sha256 = SHA256.Create();
            return ByteToString(sha256.ComputeHash(targetFile));
        }

        private static readonly StringBuilder sb = new StringBuilder();

        private static string ByteToString(byte[] b)
        {
            sb.Clear();
            foreach (var i in b)
            {
                sb.Append(i.ToString("x2"));
            }
            return sb.ToString();
        }
        #endregion

        #region 暫存檔案處理
        /// <summary>
        /// 建立一暫存檔案
        /// </summary>
        /// <returns>暫存檔案檔名</returns>
        public static string CreateTmpFile()
        {
            string fileName = string.Empty;

            try
            {
                // Get the full name of the newly created Temporary file.
                // Note that the GetTempFileName() method actually creates
                // a 0-byte file and returns the name of the created file.
                fileName = Path.GetTempFileName();

                // Create a FileInfo object to set the file's attributes
                FileInfo fileInfo = new FileInfo(fileName);

                // Set the Attribute property of this file to Temporary.
                // Although this is not completely necessary, the .NET Framework is able
                // to optimize the use of Temporary files by keeping them cached in memory.
                fileInfo.Attributes = FileAttributes.Temporary;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Unable to create TEMP file or set its attributes: " + ex.Message
                );
            }

            return fileName;
        }

        /// <summary>
        /// 刪除暫存檔案
        /// </summary>
        /// <param name="tmpFile">暫存檔的檔名</param>
        public static void DeleteTmpFile(string tmpFile)
        {
            try
            {
                // Delete the temp file (if it exists)
                if (File.Exists(tmpFile))
                {
                    File.Delete(tmpFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting TEMP file: " + ex.Message);
            }
        }
        #endregion

        #region 檔案下載
        /// <summary>
        /// 下載檔案 (使用 HttpClient，支援 async/await)
        /// </summary>
        public static async Task<bool> DownloadFileAsync(
            string url,
            string path,
            Action<string>? logAction = null
        )
        {
            using var httpClient = new HttpClient();
            try
            {
                logAction?.Invoke($"正在下載: {Path.GetFileName(path)}");

                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                if (File.Exists(path))
                    File.Delete(path);

                // 先解碼再重新編碼 URL
                var decodedUrl = Uri.UnescapeDataString(url);
                logAction?.Invoke($"🔗 URL 解碼結果: {decodedUrl}");
                var uri = new Uri(decodedUrl);
                logAction?.Invoke("⬇️ 正在連線並取得檔案流...");
                // response.EnsureSuccessStatusCode();
                using var response = await httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                using var cloudefileStream = await response.Content.ReadAsStreamAsync();
                logAction?.Invoke(
                    $"✅ 成功取得檔案流。檔案大小 (可能為估計): {cloudefileStream.Length} bytes"
                );

                // 4. 將流寫入檔案
                logAction?.Invoke($"💾 正在寫入檔案到: {path}");

                await using var fileStream = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );
                await cloudefileStream.CopyToAsync(fileStream);
                fileStream.Flush();
                fileStream.Close();
                logAction?.Invoke("🎉 檔案下載並寫入完成！");

                return true;
            }
            catch (Exception e)
            {
                logAction?.Invoke($"出現以下錯誤: {Path.GetFileName(path)} - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 下載檔案 (同步版本，保持向後相容)
        /// </summary>
        public static bool DownloadFile(string url, string path, Action<string>? logAction = null)
        {
            return DownloadFileAsync(url, path, logAction).GetAwaiter().GetResult();
        }
        #endregion
    }
}

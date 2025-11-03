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
        #region MD5 計算
        public static string GetMD5(string filepath)
        {
            using var targetFile = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            using var md5 = MD5.Create();
            return ByteToString(md5.ComputeHash(targetFile));
        }

        private static readonly StringBuilder sb = new StringBuilder();

        private static string ByteToString(byte[] b)
        {
            sb.Clear();
            foreach (var i in b)
            {
                sb.Append(i.ToString("x2"));
            }
            return sb.ToString().ToUpper();
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
                var uri = new Uri(decodedUrl);
                var response = await httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                await using var fileStream = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );
                await response.Content.CopyToAsync(fileStream);

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

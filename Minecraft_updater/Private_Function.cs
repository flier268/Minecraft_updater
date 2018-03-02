using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Media;

namespace Minecraft_updater
{
    public class Private_Function
    {
        #region 私人方法
        public static string GetMD5(string filepath)
        {
            using (var tragetFile = new System.IO.FileStream(filepath, System.IO.FileMode.Open,FileAccess.Read))
            {
                MD5 m = MD5.Create();                
                return ByteToString(m.ComputeHash(tragetFile));
            }          
        }
        static System.Text.StringBuilder sb = new System.Text.StringBuilder();
        private static string ByteToString(byte[] b)
        {
            sb.Clear();
            foreach (var i in b)
            {
                sb.Append(i.ToString("x2"));
            }
            return (sb.ToString().ToUpper()); 
        }
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

                // Craete a FileInfo object to set the file's attributes
                FileInfo fileInfo = new FileInfo(fileName);

                // Set the Attribute property of this file to Temporary. 
                // Although this is not completely necessary, the .NET Framework is able 
                // to optimize the use of Temporary files by keeping them cached in memory.
                fileInfo.Attributes = FileAttributes.Temporary;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to create TEMP file or set its attributes: " + ex.Message);
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
                Console.WriteLine("Error deleteing TEMP file: " + ex.Message);
            }
        }

        public static bool DownloadFile(string url, string path, string log)
        {
            using (WebClient myWebClient = new WebClient())
            {
                try
                {
                    Log.AddLine(log, Colors.Black);
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    if (File.Exists(path))
                        File.Delete(path);
                    myWebClient.DownloadFile(Uri.EscapeUriString(Uri.UnescapeDataString( url)), path);
                    return true;
                }
                catch (Exception e) { Log.AddLine(String.Format("出現以下錯誤:{0}", Path.GetFileName(path) + e.Message), Colors.Red); return false; }
            }
        }
        #endregion
    }
}
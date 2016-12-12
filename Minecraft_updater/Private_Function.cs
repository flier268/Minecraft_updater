using System;
using System.IO;

namespace Minecraft_updater
{
    public class Private_Function
    {
        #region 私人方法
        public static string GetMD5(string filepath)
        {
            var tragetFile = new System.IO.FileStream(filepath, System.IO.FileMode.Open);
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hashbytes = md5.ComputeHash(tragetFile);
            tragetFile.Close();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < hashbytes.Length; i++)
            {
                sb.Append(hashbytes[i].ToString("x2"));
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
                    Console.WriteLine("TEMP file deleted.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleteing TEMP file: " + ex.Message);
            }
        }


        #endregion
    }
}
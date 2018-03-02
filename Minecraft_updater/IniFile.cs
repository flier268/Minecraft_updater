using System.Runtime.InteropServices;
using System.Text;

namespace InI_File_Merger
{
    public class IniFile
    {
        public string path;             //INI文件名  

        //聲明寫INI文件的API函數 
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(byte[] section, byte[] key, byte[] val, string filePath);

        //聲明讀INI文件的API函數 
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(byte[] section, byte[] key, byte[] def, byte[] retVal, int size, string filePath);

        //類的構造函數，傳遞INI文件的路徑和文件名
        public IniFile(string INIPath)
        {
            path = INIPath;
        }
        private static byte[] getBytes(string s, string encodingName)
        {
            return null == s ? null : Encoding.GetEncoding(encodingName).GetBytes(s);
        }        
        /// <summary>
        /// 寫入INI文件
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <param name="Encode"></param>
        public void IniWriteValue(string Section, string Key, string Value,string Encode = "utf-8")
        {
            WritePrivateProfileString(getBytes(Section, Encode), getBytes(Key, Encode), getBytes(Value, Encode), path);
        }


        /// <summary>
        /// 讀取INI文件 
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="encodingName"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public string IniReadValue(string section, string key, string encodingName = "utf-8", int size = 1024)
        {
            byte[] buffer = new byte[size];
            int count = GetPrivateProfileString(getBytes(section, encodingName), getBytes(key, encodingName), getBytes("", encodingName), buffer, size, path);
            return Encoding.GetEncoding(encodingName).GetString(buffer, 0, count).Trim();
        }
    }
}

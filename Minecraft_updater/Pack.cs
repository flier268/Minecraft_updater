using System.Text.RegularExpressions;

namespace Minecraft_updater
{
    public struct Pack
    {
        string _MD5, _URL, _Path;
        bool isChecked, delete;
        public bool Delete { get => delete; set => delete = value; }
        public bool DownloadWhenNotExist { get; set; }
        public string MD5 { get => _MD5; set => _MD5 = value; }
        public string URL { get => _URL; set => _URL = value; }
        public string Path { get => _Path; set => _Path = value; }
        public bool IsChecked { get => isChecked; set => isChecked = value; }
    }
    public static class Packs
    {
        static Regex r = new Regex("(.*?)\\|\\|(.*?)\\|\\|(.*)", RegexOptions.Singleline);
        public static Pack reslove(string s)
        {
            Match m;
            bool delete = false;
            bool DownloadWhenNotExist = false;
            if (s.StartsWith("#"))
            {
                delete = true;
                m = r.Match(s.Substring(1, s.Length - 1));
            }
            else if (s.StartsWith(":"))
            {
                DownloadWhenNotExist = true;
                m = r.Match(s.Substring(1, s.Length - 1));
            }
            else
                m = r.Match(s);
            if (m.Success)
            {
                return new Pack { Path = m.Groups[1].ToString(), MD5 = ((m.Groups[2] == null) ? "" : m.Groups[2].ToString()), URL = ((m.Groups[3] == null) ? "" : m.Groups[3].ToString()), Delete = delete, IsChecked = false, DownloadWhenNotExist = DownloadWhenNotExist };
            }
            else
                return new Pack { };
        }
    }
}

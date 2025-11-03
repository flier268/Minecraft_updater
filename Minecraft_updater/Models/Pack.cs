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
    }

    public static class Packs
    {
        static readonly Regex r = new Regex("(.*?)\\|\\|(.*?)\\|\\|(.*)", RegexOptions.Singleline);

        public static Pack Resolve(string s)
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
                };
            }
            else
            {
                return new Pack { };
            }
        }
    }
}

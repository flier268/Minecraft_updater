using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Minecraft_updater
{
    public struct Pack
    {
        string _name, _MD5, _URL,_subfolder;
        bool _searchFuzzy,isChecked;
        public string MD5
        {
            get
            {
                return _MD5;
            }

            set
            {
                _MD5 = value;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }

        public string URL
        {
            get
            {
                return _URL;
            }

            set
            {
                _URL = value;
            }
        }

        public bool IsChecked
        {
            get
            {
                return isChecked;
            }

            set
            {
                isChecked = value;
            }
        }

        public string Subfolder
        {
            get
            {
                return _subfolder;
            }

            set
            {
                _subfolder = value;
            }
        }

        public bool SearchFuzzy
        {
            get
            {
                return _searchFuzzy;
            }

            set
            {
                _searchFuzzy = value;
            }
        }
    }
    public static class Packs
    {
        static Regex r = new Regex("(.*?)\\|\\|(.*?)\\|\\|(.*?)\\|\\|(.*?)\\|\\|(.*)", RegexOptions.Singleline);
        public static Pack reslove(string s)
        {
            Match m = r.Match(s);
            if (m.Success)
            {
                return new Pack { Name = m.Groups[1].ToString(), Subfolder = m.Groups[2].ToString(), MD5 = ((m.Groups[3] == null) ? "" : m.Groups[3].ToString()), SearchFuzzy= Boolean.Parse(m.Groups[4].ToString()), URL = ((m.Groups[4] == null) ? "" : m.Groups[5].ToString()), IsChecked = false };
            }
            else
                return new Pack { };
        }
    }
}

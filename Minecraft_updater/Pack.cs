using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_updater
{
    struct Pack
    {
        string _name, _MD5, _URL,_subfolder;
        bool isChecked;
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
    }
}

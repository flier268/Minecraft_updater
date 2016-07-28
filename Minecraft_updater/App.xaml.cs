using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Minecraft_updater
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //判斷是否有帶入啟動參數
            if (e.Args != null && e.Args.Length > 0)
            {
                //若有的話把參數存放到應用程式本身底下的屬性
                //MyArg這個Key當中
                this.Properties["MyArg0"] = e.Args[0];
                if (e.Args != null && e.Args.Length > 1)
                    this.Properties["MyArg1"] = e.Args[1];
            }
            base.OnStartup(e);
        }
    }
}

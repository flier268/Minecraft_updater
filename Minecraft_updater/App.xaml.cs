using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace Minecraft_updater
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public static List<String> Args = new List<string>();
        protected override void OnStartup(StartupEventArgs e)
        {            
            Args = e.Args.ToList();
            if (Args.Count > 0)
            {
                if (Args[0].Equals(listCommand.updatepackMaker))
                {
                    var window = new updatepackMaker();
                    window.Show();
                }
                else if (Args[0].Equals(listCommand.Check_Update))
                {
                    var window = new updater();
                    window.Show();
                }
                else if (Args[0].Equals(listCommand.Check_updaterVersion))
                {
                    //Update();
                    //Application.Current.Shutdown();
                }
            }
            else
            {
                StringBuilder s = new StringBuilder();
                s.AppendLine("請使用附加參數啟動");
                s.AppendLine("檢查Minecraft懶人包更新：");
                s.AppendLine("Minecraft_updater.exe " + listCommand.Check_Update);                
                s.AppendLine();
                //s.AppendLine("檢查Minecraft_updater更新：");
                //s.AppendLine("Minecraft_updater.exe " + listCommand.Check_updaterVersion);
                s.AppendLine("Minecraft懶人包之檔案清單建立工具：");
                s.AppendLine("Minecraft_updater.exe " + listCommand.updatepackMaker);

                MessageBox.Show(s.ToString());
                Application.Current.Shutdown();              
            }
            base.OnStartup(e);
        }
        /// <summary>
        /// 檢查是否有更新
        /// </summary>
        /// <returns>true: 需要更新  false: 不需要更新</returns>
        public static bool CheckUpdate()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync("https://raw.githubusercontent.com/flier268/Minecraft_updater/master/Minecraft_updater/Properties/AssemblyInfo.cs").Result;

                    if (response.IsSuccessStatusCode)
                    {
                        // by calling .Result you are performing a synchronous call
                        var responseContent = response.Content;

                        // by calling .Result you are synchronously reading the result
                        string responseString = responseContent.ReadAsStringAsync().Result;
                        Regex r = new Regex(@"^\[assembly: AssemblyVersion.*?([\d|\.]+).*?\]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        var m = r.Match(responseString);

                        Version ver = new Version(m.Groups[1].ToString());
                        Version verson = Assembly.GetEntryAssembly().GetName().Version;
                        int tm = verson.CompareTo(ver);

                        if (tm >= 0)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
            catch {}
            return false;
        }
        void Update()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\AutoUpdater.exe");
            startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            startInfo.Arguments = "-CheckUpdateWithoutForm";
            Process.Start(startInfo);
        }
    }
}

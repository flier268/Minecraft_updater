using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Windows;

namespace Minecraft_updater
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public static List<String> Args = new List<string>();
        public static string Command="";
        protected override void OnStartup(StartupEventArgs e)
        {            
            Args = e.Args.ToList();
            if (Args.Count > 0)
            {
                Command = Args[0];
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
        public static UpdateMessage CheckUpdate()
        {
            UpdateMessage updateMessage = new UpdateMessage();
            try
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync("https://gitlab.com/flier268/Minecraft_updater/raw/master/Release/Version.txt").Result;

                    if (response.IsSuccessStatusCode)
                    {
                        // by calling .Result you are performing a synchronous call
                        var responseContent = response.Content;

                        // by calling .Result you are synchronously reading the result
                        string responseString = responseContent.ReadAsStringAsync().Result;
                        string[] package=responseString.Split('\n');


                        Version ver = new Version(package[0].ToString());
                        Version verson = Assembly.GetEntryAssembly().GetName().Version;
                        int tm = verson.CompareTo(ver);

                        if (tm >= 0)
                        {
                            updateMessage.HaveUpdate = false;
                        }
                        else
                        {
                            updateMessage.HaveUpdate = true;
                            updateMessage.NewstVersion = package[0].ToString();
                            updateMessage.SHA1 = package[1];
                            StringBuilder stringBuilder = new StringBuilder();
                            if(package.Length>2)
                            for(int i=2;i<package.Length;i++)
                                {
                                    stringBuilder.AppendLine(package[i]);
                                }
                            updateMessage.Message = stringBuilder.ToString();
                        }
                    }
                }
            }
            catch {}
            return updateMessage;
        }
        public class UpdateMessage
        {
            public UpdateMessage()
            {
                HaveUpdate = false;
            }
            public bool HaveUpdate { get; set; }
            public string NewstVersion { get; set; }
            public string SHA1 { get; set; }
            public string Message { get; set; }
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

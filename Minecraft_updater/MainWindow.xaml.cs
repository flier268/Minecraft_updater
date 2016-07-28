using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InI_File_Merger;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Minecraft_updater
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        IniFile ini = new IniFile(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.ini");

        string URL_PackList = "";
        string Minecraft_Version_pathBase = "";
        string Minecraft_Version="";
        string URL_Minecraft_updater_release="";
        Version Minecraft_updater_Version=new Version();
        bool AutoClose_AfterFinishd = false;
        List<Pack> list = new List<Pack>();

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
        private void Log_AddLine(string str, Color color)
        {

            Brush brush = new SolidColorBrush(color);
            TextRange tr = new TextRange(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd);
            tr.Text = str+"\r\n";
            try
            {                
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            }
            catch (FormatException) { }
            StreamWriter writer = new StreamWriter(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Minecraft_updater.log",true,Encoding.UTF8);
            writer.WriteLine(str);
            writer.Close();
        }
        #endregion
        private void Function_Loaded(object sender, RoutedEventArgs e)
        {
            URL_PackList = ini.IniReadValue("Minecraft_updater", "URL_PackList");
            Minecraft_Version_pathBase = ini.IniReadValue("Minecraft_updater", "Minecraft_Version_pathBase");
            Minecraft_Version = ini.IniReadValue("Minecraft_updater", "Minecraft_Version");
                       
            if (ini.IniReadValue("Minecraft_updater", "AutoClose_AfterFinishd").ToLower() == "true")
                AutoClose_AfterFinishd = true;
            

            
            if (App.Current.Properties["MyArg0"] != null)
            {
                string fname = App.Current.Properties["MyArg0"].ToString();
                if (fname.IndexOf("-CheckUpdate") != -1)
                {
                    Log_AddLine("自我檢查更新...", Colors.Black);
                    UpdateSelf();
                }
                else if (fname.IndexOf("-isnew") != -1)
                {
                    try
                    {
                        Process[] MyProcess = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(App.Current.Properties["MyArg1"].ToString()));
                        MyProcess[0].CloseMainWindow();
                        MyProcess[0].WaitForExit(1000);
                        if(!MyProcess[0].HasExited)
                        {
                            MyProcess[0].Kill();
                            MyProcess[0].WaitForExit(1000);
                        }
                        File.Delete(App.Current.Properties["MyArg1"].ToString());
                        File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, App.Current.Properties["MyArg1"].ToString());
                        Process p = new Process();
                        p.StartInfo.FileName = App.Current.Properties["MyArg1"].ToString();
                        p.StartInfo.Arguments = "-temp_clear \"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"";
                        p.Start();
                        this.Close();
                        return;
                    }
                    catch 
                    {

                    }
                }
                else if(fname.IndexOf("-temp_clear") != -1)
                {
                    try
                    {
                        Process[] MyProcess = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(App.Current.Properties["MyArg1"].ToString()));
                        MyProcess[0].CloseMainWindow();
                        MyProcess[0].WaitForExit(1000);
                        if (!MyProcess[0].HasExited)
                        {
                            MyProcess[0].Kill();
                            MyProcess[0].WaitForExit(1000);
                        }
                        File.Delete(App.Current.Properties["MyArg1"].ToString());
                    }
                    catch { }
                }
            }
            CheckPack();
        }

        private void UpdateSelf()
        {             
            Minecraft_updater_Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Version VersionNew = new Version();
            URL_Minecraft_updater_release = ini.IniReadValue("Minecraft_updater", "URL_Minecraft_updater_release");
            bool updateNeed = false;
            if (URL_Minecraft_updater_release != "")
                using (WebClient myWebClient = new WebClient())
                {
                    try
                    {
                        myWebClient.Encoding = Encoding.UTF8;
                        VersionNew = Version.Parse(myWebClient.DownloadString(URL_Minecraft_updater_release + "last.version"));
                        if (Minecraft_updater_Version.CompareTo(VersionNew) < 0)
                        {
                            updateNeed = true;
                        }
                    }
                    catch (System.Net.WebException e) { Log_AddLine(String.Format("取得最新版本號時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }
                    catch (Exception e) { Log_AddLine(String.Format("取得最新版本號時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }
                }
           if(updateNeed)
            {
                Log_AddLine(String.Format("目前版本號為{0}，將更新到{1}", Minecraft_updater_Version,VersionNew), Colors.Blue);
                try
                {
                    using (WebClient myWebClient = new WebClient())
                    {
                        myWebClient.Encoding = Encoding.UTF8;
                        myWebClient.DownloadFile(URL_Minecraft_updater_release + "last.exe", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\temp_" + System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                        Process p = new Process();
                        p.StartInfo.FileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\temp_" + System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        p.StartInfo.Arguments = "-isnew \"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"";
                        p.Start();
                        this.Close();
                    }
                }
                catch (System.Net.WebException e) { Log_AddLine(String.Format("取得最新版本時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }
                catch (Exception e) { Log_AddLine(String.Format("取得最新版本時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }
            }
        }

        private void CheckPack()
        {
            Log_AddLine(String.Format("從{0}下載Minecraft的Mod清單...", URL_PackList), Colors.Black);
            var result = string.Empty;
            try
            {
                using (WebClient myWebClient = new WebClient())
                {
                    myWebClient.DownloadFile(URL_PackList, System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\URL_PackList.sc");
                }
                StreamReader reader = new StreamReader(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\URL_PackList.sc");
                while (!reader.EndOfStream)
                {
                    string temp = reader.ReadLine();

                    Regex r = new Regex("(.*?)\\|\\|(.*?)\\|\\|(.*?)\\|\\|(.*)", RegexOptions.Singleline);
                    Match m = r.Match(temp);
                    if (m.Success)
                    {

                        list.Add(new Pack { Name = m.Groups[1].ToString(), Subfolder = m.Groups[2].ToString(), MD5 = ((m.Groups[3] == null) ? "" : m.Groups[3].ToString()), URL = ((m.Groups[4] == null) ? "" : m.Groups[4].ToString()), IsChecked = false });
                    }
                }
                reader.Close();
            }
            catch (System.Net.WebException e) { Log_AddLine(String.Format("取得最新最新PackList時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }
            catch (Exception e) { Log_AddLine(String.Format("取得最新PackList時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }
            try
            {
                if (File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\URL_PackList.sc"))
                    File.Delete(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\URL_PackList.sc");
            }
            catch (Exception e) { Log_AddLine(String.Format("刪除暫存的URL_PackList.sc時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }

            Log_AddLine(String.Format("Minecraft的Mod清單下載完成，在清單上共有{0}個MOD...", list.Count), Colors.Black);

            try
            {
                string[] files = System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Minecraft_Version_pathBase + "\\" + Minecraft_Version + "\\mods\\", "*.*", System.IO.SearchOption.AllDirectories);

                foreach (var temp in files)
                {
                    var t = list.FirstOrDefault(z => temp.Contains(z.Name));
                    if (t.Name != null)
                    {
                        if (t.URL == "" && t.MD5 == "")
                            try
                            {
                                File.Delete(temp);
                                Log_AddLine(String.Format("已刪除{0}", System.IO.Path.GetFileName(temp)), Colors.Black);

                            }
                            catch (IOException) { Log_AddLine(String.Format("刪除{0}時失敗，檔案正在使用中", System.IO.Path.GetFileName(temp)), Colors.Red); }
                            catch (Exception e) { Log_AddLine(String.Format("刪除{0}時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }
                        else if (t.MD5.ToUpper() != GetMD5(temp))
                        {
                            Log_AddLine(String.Format("{0}需要更新，刪除{0}並下載更新...", System.IO.Path.GetFileName(temp)), Colors.Black);
                            try
                            {
                                File.Delete(temp);
                                using (WebClient myWebClient = new WebClient())
                                {
                                    Regex r = new Regex(".*/(.*)", RegexOptions.Singleline);
                                    Match m = r.Match(t.URL);
                                    if (m.Success)
                                    {
                                        myWebClient.DownloadFile(t.URL, System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Minecraft_Version_pathBase + "\\" + Minecraft_Version + "\\mods" + t.Subfolder + m.Groups[1]);
                                        if (File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Minecraft_Version_pathBase + "\\" + Minecraft_Version + "\\mods" + t.Subfolder + m.Groups[1]))
                                            Log_AddLine(String.Format("{0}更新完成", "\\mods" + t.Subfolder + m.Groups[1]), Colors.Black);
                                    }
                                    else
                                        Log_AddLine(String.Format("{0}的URL錯誤", t.Name), Colors.Red);
                                }
                            }
                            catch (Exception e)
                            {
                                Log_AddLine(String.Format("出現了以下錯誤訊息：{0}", e.Message), Colors.Red);
                            }
                        }

                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i].Equals(t))
                            {
                                Pack y = t;
                                y.IsChecked = true;
                                list[i] = y;
                            }
                        }
                    }
                }
            }
            catch (Exception e) { Log_AddLine(String.Format("出現以下訊息：{0}", e.Message), Colors.Red); }
            var NewFile=list.Where(z => z.IsChecked == false).ToList();
            foreach(var temp in NewFile)
            {
                try
                {
                    using (WebClient myWebClient = new WebClient())
                    {
                        Regex r = new Regex(".*/(.*)", RegexOptions.Singleline);
                        Match m = r.Match(temp.URL);
                        Log_AddLine(String.Format("{0}需要更新，開始下載更新...", m.Groups[1].ToString()), Colors.Black);
                        if (m.Success)
                        {
                            myWebClient.DownloadFile(temp.URL, System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Minecraft_Version_pathBase + "\\" + Minecraft_Version + "\\mods" + temp.Subfolder + m.Groups[1]);
                            if (File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + Minecraft_Version_pathBase + "\\" + Minecraft_Version + "\\mods" + temp.Subfolder + m.Groups[1]))
                                Log_AddLine(String.Format("{0}更新完成",  "\\mods" + temp.Subfolder + m.Groups[1]), Colors.Black);
                        }
                        else
                            Log_AddLine(String.Format("{0}的URL錯誤", temp.Name), Colors.Red);
                    }
                }
                catch (Exception e)
                {
                    Log_AddLine(String.Format("出現了以下錯誤訊息：{0}", e.Message), Colors.Red);
                }
            }
            Log_AddLine("確認完成！", Colors.Green);
            if (AutoClose_AfterFinishd)
                Close();
        }        
    }
}

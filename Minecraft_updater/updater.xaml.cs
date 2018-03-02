using InI_File_Merger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Minecraft_updater
{
    /// <summary>
    /// update.xaml 的互動邏輯
    /// </summary>
    public partial class updater : Window
    {
        string URL = "";
        public updater()
        {
            this.URL = ini.IniReadValue("Minecraft_updater", "scUrl");
            InitializeComponent();
        }
        private string _AppPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);


        IniFile ini = new IniFile(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.ini");
        bool AutoClose_AfterFinishd = false;
        List<Pack> list = new List<Pack>();


        public string AppPath
        {
            get
            {
                return _AppPath;
            }

            set
            {
                _AppPath = value;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ini.IniReadValue("Minecraft_updater", "AutoClose_AfterFinishd").ToLower() == "true")
                AutoClose_AfterFinishd = true;
            if (ini.IniReadValue("Minecraft_updater", "LogFile").ToLower() == "true")
                Log.LogFile = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(CheckPack));
            //CheckPack("");
        }



        #region 跨執行緒存取UI
        void updateControl(Label Label, string Result)
        {
            Label.Content = Result;
        }
        public delegate void UpdateTextCallback(Label l, string message);

        void CrossThread_EditeLabelContent(Label l, string s)
        {
            label.Dispatcher.BeginInvoke(
                new UpdateTextCallback(this.updateControl),
                l, s);
        }

        void updateControl(ProgressBar progressBar, int max, int min, int value)
        {
            progressBar.Maximum = max;
            //progressBar.Minimum = min;
            progressBar.Value = value;
        }
        public delegate void UpdateProgressBarCallback(ProgressBar pb, int max, int min, int value);
        void CrossThread_EditeProgressBar(ProgressBar pb, int max, int min, int value)
        {
            pb.Dispatcher.BeginInvoke(
                new UpdateProgressBarCallback(this.updateControl),
                pb, max, min, value);
        }
        public delegate void CloseDelagate();
        void CrossThread_Close()
        {
            this.Dispatcher.Invoke((CloseDelagate)delegate
            {
                this.Close();
            });
        }
        #endregion


        private void CheckPack(object number)
        {
            //建立暫存
            string tempfile = Private_Function.CreateTmpFile();

            Log.AddLine(String.Format("從{0}下載Minecraft的Mod清單...", URL), Colors.Black);
            CrossThread_EditeLabelContent(label1, String.Format("從{0}下載Minecraft的Mod清單...", URL));
            try
            {
                using (WebClient myWebClient = new WebClient())
                {
                    myWebClient.DownloadFile(URL, tempfile);
                }
                CrossThread_EditeLabelContent(label1, String.Format("解析中..."));
                StreamReader reader = new StreamReader(tempfile);
                while (!reader.EndOfStream)
                {
                    string temp = reader.ReadLine();
                    if (temp != "")
                        list.Add(Packs.reslove(temp));
                }
                reader.Close();
            }
            catch (System.Net.WebException e) { Log.AddLine(String.Format("取得最新最新PackList時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }
            catch (Exception e) { Log.AddLine(String.Format("取得最新PackList時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }

            //刪除暫存
            Private_Function.DeleteTmpFile(tempfile);



            int totalCount = list.Where(x => !x.Delete).ToList().Count;
            Log.AddLine(String.Format("Minecraft的Mod清單下載完成，在清單上共有{0}個檔案...", totalCount), Colors.Black);
            CrossThread_EditeLabelContent(label1, String.Format("0/{0}", totalCount));
            CrossThread_EditeProgressBar(progressBar, totalCount, 0, 0);
            int haveUpdate = 0;
            try
            {
                DirectoryInfo di = new DirectoryInfo(AppPath);
                List<string> files = di.EnumerateFiles("*", SearchOption.AllDirectories).Select(x => x.FullName).ToList<string>();



                //刪除檔案
                var templist = list.Where(x => x.Delete).ToList();
                templist.ForEach(x => files.Where(y => y.StartsWith(x.Path)).ToList().ForEach(z =>
                {
                    if (Private_Function.GetMD5(z) != x.MD5)
                    {
                        try { File.Delete(z); }
                        catch (IOException) { Log.AddLine(String.Format("刪除{0}時失敗，檔案正在使用中", System.IO.Path.GetFileName(z)), Colors.Red); }
                        catch (Exception e) { Log.AddLine(String.Format("刪除{0}時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }
                    }
                }));

                //新增/取代檔案
                templist = list.Where(x => !x.Delete).ToList();
                string AppDicPath=Path.GetDirectoryName(AppPath);
                string filepath ;
                
                foreach (var temp in templist)
                {
                    filepath = Path.Combine(AppDicPath, temp.Path);                    
                    if ((File.Exists(filepath) && (Private_Function.GetMD5(filepath) != temp.MD5)) || !File.Exists(filepath))
                    {
                        if (Private_Function.DownloadFile(temp.URL, Path.Combine(AppPath, temp.Path), String.Format("{0}需要更新，開始下載更新...", Path.GetFileName(temp.Path))))
                        {
                            Log.AddLine(String.Format("{0}更新完成", "\\mods" + temp.Path + Path.GetFileName(temp.Path)), Colors.Black);
                            if (Math.Round(((double)(haveUpdate + 1) / (double)totalCount), 2) - Math.Round((((double)haveUpdate / (double)totalCount)), 2) > 0.01)
                            {
                                haveUpdate++;
                                CrossThread_EditeLabelContent(label1, String.Format("{0}/{1}", haveUpdate, totalCount));
                                CrossThread_EditeProgressBar(progressBar, totalCount, 0, haveUpdate);
                            }
                            else
                                haveUpdate++;
                        }
                        else
                            Log.AddLine(String.Format("{0}更新失敗", "\\mods" + temp.Path + Path.GetFileName(temp.Path)), Colors.Red);
                    }
                    else
                    {
                        Log.AddLine(String.Format("{0}更新完成", "\\mods" + temp.Path + Path.GetFileName(temp.Path)), Colors.Black);
                        if (Math.Round(((double)(haveUpdate + 1) / (double)totalCount), 2) - Math.Round((((double)haveUpdate / (double)totalCount)), 2) > 0.01)
                        {
                            haveUpdate++;
                            CrossThread_EditeLabelContent(label1, String.Format("{0}/{1}", haveUpdate, totalCount));
                            CrossThread_EditeProgressBar(progressBar, totalCount, 0, haveUpdate);
                        }
                        else
                            haveUpdate++;
                    }
                }                

                Log.AddLine("同步完成！", Colors.Green);
                haveUpdate = totalCount;
                CrossThread_EditeLabelContent(label1, String.Format("{0}/{1}", haveUpdate, totalCount));
                CrossThread_EditeProgressBar(progressBar, totalCount, 0, haveUpdate);
            }
            catch (Exception e) { Log.AddLine(String.Format("出現以下訊息：{0}", e.Message), Colors.Red); }

            if (AutoClose_AfterFinishd)
            {
                CrossThread_Close();
            }
            else
                MessageBox.Show("同步完成");



        }


    }
}
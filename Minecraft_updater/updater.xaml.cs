using InI_File_Merger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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
        public updater(string url)
        {
            this.URL = url;
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
            AppPath = AppPath;
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
                    list.Add(Packs.reslove(temp));
                }
                reader.Close();
            }
            catch (System.Net.WebException e) { Log.AddLine(String.Format("取得最新最新PackList時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }
            catch (Exception e) { Log.AddLine(String.Format("取得最新PackList時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }

            //刪除暫存
            Private_Function.DeleteTmpFile(tempfile);




            Log.AddLine(String.Format("Minecraft的Mod清單下載完成，在清單上共有{0}個檔案...", list.Count), Colors.Black);
            CrossThread_EditeLabelContent(label1, String.Format("0/{0}", list.Count));
            CrossThread_EditeProgressBar(progressBar, list.Count, 0, 0);
            int haveUpdate = 0;
            try
            {
                DirectoryInfo di = new DirectoryInfo(AppPath);
                List<string> files = di.EnumerateFiles("*", SearchOption.AllDirectories).Select(x => x.FullName).ToList<string>();


                #region 刪除不要的檔案
                //列出要刪除的檔案的清單
                var templist = list.Where(x => x.MD5.Equals("", StringComparison.Ordinal) && x.URL.Equals("", StringComparison.Ordinal)).ToList();
                List<string> filepaths = new List<string>();
                foreach (var temp in templist)
                {
                    var temp2 = files.Where(x => (temp.SearchFuzzy ? (Path.GetFileName(x).Contains(temp.Name)) : (Path.GetFileName(x).Equals(temp.Name, StringComparison.Ordinal))) && (AppPath + "\\" + temp.Subfolder).Equals(Path.GetDirectoryName(x) + "\\", StringComparison.OrdinalIgnoreCase)).ToList<string>();

                    if (temp2.Count > 0)
                    {
                        filepaths.AddRange(temp2);
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i].Equals(temp))
                            {
                                Pack y = temp;
                                y.IsChecked = true;
                                list[i] = y;
                            }
                        }
                    }
                }

                //刪除清單內的檔案
                foreach (var temp in filepaths)
                {
                    try
                    {
                        File.Delete(temp);
                        Log.AddLine(String.Format("已刪除{0}", System.IO.Path.GetFileName(temp)), Colors.Black);

                        if (Math.Round(((double)(haveUpdate + 1) / (double)list.Count), 2)
                         - Math.Round((((double)haveUpdate / (double)list.Count)), 2) > 0.01)
                        {
                            haveUpdate++;
                            CrossThread_EditeLabelContent(label1, String.Format("{0}/{1}", haveUpdate, list.Count));
                            CrossThread_EditeProgressBar(progressBar, list.Count, 0, haveUpdate);
                        }
                        else
                            haveUpdate++;
                    }
                    catch (IOException) { Log.AddLine(String.Format("刪除{0}時失敗，檔案正在使用中", System.IO.Path.GetFileName(temp)), Colors.Red); }
                    catch (Exception e) { Log.AddLine(String.Format("刪除{0}時失敗，出現以下訊息：{0}", e.Message), Colors.Red); }
                }
                #endregion
                //加入MOD及URL不等於空的Pack
                templist = list.Where(x => !x.MD5.Equals("", StringComparison.Ordinal) && !x.URL.Equals("", StringComparison.Ordinal)).ToList();
                filepaths.Clear();
                foreach (var temp in templist)
                {
                    //加入路徑相同，檔名相同或相似(依SearchFuzzy決定)的檔名

                    var temp2 = files.Where(x => (temp.SearchFuzzy ? (Path.GetFileName(x).Contains(temp.Name)) : (Path.GetFileName(x).Equals(temp.Name, StringComparison.Ordinal))) && (AppPath + "\\" + temp.Subfolder).Equals(Path.GetDirectoryName(x) + "\\", StringComparison.OrdinalIgnoreCase)).ToList<string>();

                    if (temp2.Count > 0)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i].Equals(temp))
                            {
                                Pack y = temp;
                                y.IsChecked = true;
                                list[i] = y;
                            }
                        }
                    }
                    //進一步篩選，只加入MD5不同的檔案
                    var temp3 = temp2.Where(x => !Private_Function.GetMD5(x).Equals(temp.MD5)).ToList<string>();
                    if (temp3.Count > 0)
                        filepaths.AddRange(temp3);
                    if (Math.Round(((double)(haveUpdate + (temp2.Count - temp3.Count)) / (double)list.Count), 2)
                        - Math.Round((((double)haveUpdate / (double)list.Count)), 2) > 0.01)
                    {
                        haveUpdate = haveUpdate + (temp2.Count - temp3.Count);
                        CrossThread_EditeLabelContent(label1, String.Format("{0}/{1}", haveUpdate, list.Count));
                        CrossThread_EditeProgressBar(progressBar, list.Count, 0, haveUpdate);
                    }
                    else
                        haveUpdate = haveUpdate + (temp2.Count - temp3.Count);
                    /**說明：只要有名稱相同的Pack，Pack皆標記為IsChecked
                     * 如果MD5相同，代表檔案一樣，則標記為已經確認過是沒錯的
                     * 如果MD5不同，代表檔案不一樣，但是後面一段會處理，因此可行**/
                }

                foreach (var temp in filepaths)
                {
                    var t = list.FirstOrDefault(z => System.IO.Path.GetFileName(temp).Contains(z.Name));
                    Log.AddLine(String.Format("{0}需要更新，刪除{0}並下載更新...", System.IO.Path.GetFileName(temp)), Colors.Black);
                    try
                    {
                        File.Delete(temp);
                        using (WebClient myWebClient = new WebClient())
                        {
                            Regex r = new Regex(".*/(.*)", RegexOptions.Singleline);
                            Match m = r.Match(t.URL);
                            if (m.Success)
                            {
                                if (!Directory.Exists(AppPath + "\\" + t.Subfolder))
                                    Directory.CreateDirectory(AppPath + "\\" + t.Subfolder);
                                myWebClient.DownloadFile(t.URL, AppPath + "\\" + t.Subfolder + m.Groups[1]);
                                if (File.Exists(AppPath + "\\" + t.Subfolder + m.Groups[1]))
                                {
                                    Log.AddLine(String.Format("{0}更新完成", "\\mods" + t.Subfolder + m.Groups[1]), Colors.Black);
                                    if (Math.Round(((double)(haveUpdate + 1) / (double)list.Count), 2) - Math.Round((((double)haveUpdate / (double)list.Count)), 2) > 0.01)
                                    {
                                        haveUpdate++;
                                        CrossThread_EditeLabelContent(label1, String.Format("{0}/{1}", haveUpdate, list.Count));
                                        CrossThread_EditeProgressBar(progressBar, list.Count, 0, haveUpdate);
                                    }
                                    else
                                        haveUpdate++;
                                }
                            }
                            else
                                Log.AddLine(String.Format("{0}的URL錯誤", t.Name), Colors.Red);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.AddLine(String.Format("出現了以下錯誤訊息：{0}", e.Message), Colors.Red);
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



                //加入找不到的檔案         

                var NewFile = list.Where(z => z.IsChecked == false).ToList();
                foreach (var temp in NewFile)
                {
                    try
                    {
                        using (WebClient myWebClient = new WebClient())
                        {
                            Regex r = new Regex(".*/(.*)", RegexOptions.Singleline);
                            Match m = r.Match(temp.URL);
                            Log.AddLine(String.Format("{0}需要更新，開始下載更新...", m.Groups[1].ToString()), Colors.Black);
                            if (m.Success)
                            {
                                if (!Directory.Exists(AppPath + "\\" + temp.Subfolder))
                                    Directory.CreateDirectory(AppPath + "\\" + temp.Subfolder);
                                myWebClient.DownloadFile((temp.URL).Replace("#", "%23"), AppPath + "\\" + temp.Subfolder + m.Groups[1]);
                                if (File.Exists(AppPath + "\\" + temp.Subfolder + m.Groups[1]))
                                {
                                    Log.AddLine(String.Format("{0}更新完成", "\\mods" + temp.Subfolder + m.Groups[1]), Colors.Black);
                                    if (Math.Round(((double)(haveUpdate + 1) / (double)list.Count), 2) - Math.Round((((double)haveUpdate / (double)list.Count)), 2) > 0.01)
                                    {
                                        haveUpdate++;
                                        CrossThread_EditeLabelContent(label1, String.Format("{0}/{1}", haveUpdate, list.Count));
                                        CrossThread_EditeProgressBar(progressBar, list.Count, 0, haveUpdate);
                                    }
                                    else
                                        haveUpdate++;
                                }
                            }
                            else
                                Log.AddLine(String.Format("{0}的URL錯誤", temp.Name), Colors.Red);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.AddLine(String.Format("出現了以下錯誤訊息：{0}", e.Message), Colors.Red);
                    }
                }
                Log.AddLine("確認完成！", Colors.Green);
                haveUpdate = list.Count;
                CrossThread_EditeLabelContent(label1, String.Format("{0}/{1}", haveUpdate, list.Count));
                CrossThread_EditeProgressBar(progressBar, list.Count, 0, haveUpdate);
            }
            catch (Exception e) { Log.AddLine(String.Format("出現以下訊息：{0}", e.Message), Colors.Red); }

            if (AutoClose_AfterFinishd)
            {
                CrossThread_Close();
            }




        }


    }
}
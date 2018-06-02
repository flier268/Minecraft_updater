using InI_File_Merger;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Minecraft_updater
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class updatepackMaker : Window
    {
        public updatepackMaker()
        {
            InitializeComponent();
            this.Title = String.Format("Minecraft updater   v{0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }
        IniFile ini = new IniFile(Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "config.ini"));
        char[] Delimiter = new char[] { '+', '-', '_'};
        List<Pack> list = new List<Pack>();



        private void TextBlock_Drop(object sender, DragEventArgs e)
        {
            TextBlock textBlock = ((TextBlock)e.Source);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (!((string[])e.Data.GetData(DataFormats.FileDrop))[0].StartsWith(AppDomain.CurrentDomain.BaseDirectory))
                {
                    MessageBox.Show("請將Minecraft_updater置於該目錄/檔案於同一目錄下\r\n詳情請查閱wiki");
                    return;
                }
                if (textBlock.Text.Trim() != "")
                    switch (MessageBox.Show("不清除直接加入到最後?", "已經含有資料", MessageBoxButton.YesNoCancel))
                    {
                        case MessageBoxResult.No:
                            textBlock.Text = "";
                            break;
                        case MessageBoxResult.Yes:
                            break;
                        default:
                            return;
                    }
                string[] docPath = (string[])e.Data.GetData(DataFormats.FileDrop);
                int basepathLength = AppDomain.CurrentDomain.BaseDirectory.Length;
                string name, MD5, URL;
                StringBuilder sb = new StringBuilder(), sb2 = new StringBuilder(), sb3 = new StringBuilder();
                bool addmodtodelete = checkbox_addmodtodelete.IsChecked.Value;
                bool addconfigtodelete = checkbox_addconfigtodelete.IsChecked.Value;
                int TextblockIndex = -1;
                if (textBlock.Equals(TextBlock1))
                    TextblockIndex = 0;
                else if (textBlock.Equals(TextBlock2))
                    TextblockIndex = 1;
                else if (textBlock.Equals(TextBlock3))
                    TextblockIndex = 2;
                foreach (string path in docPath)
                {
                    if (File.Exists(path))
                    {
                        name = path.Substring(basepathLength, path.Length - basepathLength);
                        MD5 = Private_Function.GetMD5(path);
                        URL = textBox.Text + name;
                        int temp = name.IndexOfAny(Delimiter);
                        switch (TextblockIndex)
                        {
                            case 0:
                                sb.AppendLine(String.Format("{0}||{1}||{2}", name, MD5, URL));
                                if ((addmodtodelete && name.Contains("mod")) || (addconfigtodelete && name.Contains("config")))
                                    sb2.AppendLine(String.Format("#{0}||{1}||", name.Substring(0, temp == -1 ? name.Length : temp), MD5));
                                break;
                            case 1:
                                sb2.AppendLine(String.Format("#{0}||{1}||", name.Substring(0, temp == -1 ? name.Length : temp), MD5));
                                break;
                            case 2:
                                sb3.AppendLine(String.Format(":{0}||{1}||{2}", name.Substring(0, temp == -1 ? name.Length : temp), MD5, URL));
                                break;
                        }
                    }
                    else if (Directory.Exists(path))
                    {
                        DirectoryInfo di = new DirectoryInfo(path);
                        foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                        {
                            name = fi.FullName.Substring(basepathLength, fi.FullName.Length - basepathLength);
                            MD5 = Private_Function.GetMD5(fi.FullName);
                            URL = textBox.Text + Path.GetFileName(path) + fi.FullName.Substring(path.Length).Replace("\\", "/");
                            int temp = name.IndexOfAny(Delimiter);
                            switch (TextblockIndex)
                            {
                                case 0:
                                    sb.AppendLine(String.Format("{0}||{1}||{2}", name, MD5, URL));
                                    if ((addmodtodelete && name.Contains("mod")) || (addconfigtodelete && name.Contains("config")))
                                        sb2.AppendLine(String.Format("#{0}||{1}||", name.Substring(0, temp == -1 ? name.Length : temp), MD5));
                                    break;
                                case 1:
                                    sb2.AppendLine(String.Format("#{0}||{1}||", name.Substring(0, temp == -1 ? name.Length : temp), MD5));
                                    break;
                                case 2:
                                    sb3.AppendLine(String.Format(":{0}||{1}||{2}", name.Substring(0, temp == -1 ? name.Length : temp), MD5, URL));
                                    break;
                            }
                        }
                    }
                }
                TextBlock1.Text += (sb.ToString());
                TextBlock2.Text += (sb2.ToString());
                TextBlock3.Text += (sb3.ToString());
            }
        }
        private void TextBlock_DropOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = false;
        }


        private void textbox_TextChange(object sender, TextChangedEventArgs e)
        {
            ini.IniWriteValue("Minecraft_updater", "updatepackMaker_BaseURL", textBox.Text);
        }

        private void button_savefile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.FileName = "updatePackList.sc";
            saveFileDialog1.Filter = "sc|*.sc";
            saveFileDialog1.Title = "Save an sc File";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {                
                StreamWriter r = new StreamWriter(saveFileDialog1.OpenFile(), Encoding.UTF8);
                r.WriteLine(TextBlock1.Text);
                r.WriteLine(TextBlock2.Text);
                r.Flush();
                r.Close();
            }
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            if (ini.IniReadValue("Minecraft_updater", "updatepackMaker_BaseURL") != "")
                textBox.Text = ini.IniReadValue("Minecraft_updater", "updatepackMaker_BaseURL");
            if (ini.IniReadValue("Minecraft_updater", "LogFile").ToLower() == "true")
                Log.LogFile = true;
            this.textBox.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.textbox_TextChange);
        }

        private void Clear(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            if (b == Button1)
                TextBlock1.Text = "";
            else
                TextBlock2.Text = "";
        }

        private void LoadList(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "sc|*.sc";
            openFileDialog.FileName = "updatePackList.sc";
            openFileDialog.Title = "Select a old sc File";
            if (openFileDialog.ShowDialog() == true)
            {
                Regex r = new Regex("(.*?)\\|\\|(.*?)\\|\\|(.*)", RegexOptions.Singleline);
                StringBuilder stringBuilder1 = new StringBuilder();
                StringBuilder stringBuilder2 = new StringBuilder();
                StringBuilder stringBuilder3 = new StringBuilder();
                using (StreamReader streamReader = new StreamReader(openFileDialog.FileName))
                {
                    while (!streamReader.EndOfStream)
                    {
                        string s = streamReader.ReadLine();
                        if (s.StartsWith("#"))
                        {
                            stringBuilder2.AppendLine(s);
                        }
                        else if (s.StartsWith(":"))
                        {
                            stringBuilder3.AppendLine(s);
                        }
                        else
                        {
                            Match m = r.Match(s);
                            if (m.Success)
                            {
                                stringBuilder1.AppendLine(s);
                            }
                        }
                    }
                }
                TextBlock1.Text = stringBuilder1.ToString();
                TextBlock2.Text = stringBuilder2.ToString();
                TextBlock3.Text = stringBuilder3.ToString();
            }
        }
    }
}

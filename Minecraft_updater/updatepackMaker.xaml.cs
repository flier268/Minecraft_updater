using InI_File_Merger;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

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
        }
        IniFile ini = new IniFile(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.ini");
        
        List<Pack> list = new List<Pack>();
  
        

        private void RichTextBox_DragOver(object sender, DragEventArgs e)
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

        private void RichTextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                richTextBox.Document.Blocks.Clear();
                string[] docPath = (string[])e.Data.GetData(DataFormats.FileDrop);

                // By default, open as Rich Text (RTF).  
                var dataFormat = DataFormats.Rtf;

                // If the Shift key is pressed, open as plain text.  
                if (e.KeyStates == DragDropKeyStates.ShiftKey)
                {
                    dataFormat = DataFormats.Text;
                }
                string name, MD5, URL;
                StringBuilder sb = new StringBuilder();
                foreach (string path in docPath)
                {

                    if (File.Exists(path))
                    {
                        name = System.IO.Path.GetFileName(path);
                        MD5 = Private_Function.GetMD5(path);
                        URL = textBox.Text + name;
                        sb.Append(String.Format("{0}||.minecraft\\{1}||{2}||{3}||{4}", name, "", MD5,false, URL));
                        sb.Append("\r\n");
                    }
                    else if (Directory.Exists(path))
                    {
                        DirectoryInfo di = new DirectoryInfo(path);
                        foreach (var fi in di.EnumerateFiles("*",SearchOption.AllDirectories))
                        {
                            name = System.IO.Path.GetFileName(fi.FullName);
                            MD5 = Private_Function.GetMD5(fi.FullName);
                            URL = textBox.Text + Path.GetFileName(path) + fi.FullName.Substring(path.Length).Replace("\\","/");
                             sb.Append(String.Format("{0}||.minecraft\\{1}||{2}||{3}||{4}"
                                , name
                                , Path.GetFileName(path) + fi.FullName.Substring(path.Length, (fi.FullName.Length - path.Length)-fi.Name.Length)
                                , MD5
                                , Path.GetDirectoryName(fi.FullName).ToLower().Equals("mods")?checkBox.IsChecked:false
                                , URL ));
                            sb.Append("\r\n");
                            Debug.Print(Path.GetFileName(path));
                        }
                    }
                    else
                    {

                    }
                }
                richTextBox.AppendText(sb.ToString());
            }
        }

        private void textbox_TextChange(object sender, TextChangedEventArgs e)
        {
            ini.IniWriteValue("Minecraft_updater", "updatepackMaker_BaseURL", textBox.Text);
        }

        private void button_savefile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "sc|*.sc";
            saveFileDialog1.Title = "Save an sc File";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                StreamWriter r = new StreamWriter(saveFileDialog1.OpenFile(), Encoding.UTF8);
                r.Write(new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text);
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
    }
}

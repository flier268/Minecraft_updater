using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Threading;

namespace Minecraft_updater
{
    public class Log
    {
        private static bool _logFile = false;

        public static bool LogFile
        {
            get
            {
                return _logFile;
            }

            set
            {
                _logFile = value;
            }
        }

        public static void AddLine(string str, Color color , RichTextBox richTextBox=null)
        {
            if (richTextBox != null)
            {
                Brush brush = new SolidColorBrush(color);
                TextRange tr = new TextRange(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd);
                tr.Text = str + "\r\n";
                try
                {
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                }
                catch (FormatException) { }
            }
            if (_logFile)
            {
                StreamWriter writer = new StreamWriter(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Minecraft_updater.log", true, Encoding.UTF8);
                writer.WriteLine(str);
                writer.Close();
            }
        }
    }
}

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Minecraft_updater
{
    /// <summary>
    /// Window_UpdateSelf.xaml 的互動邏輯
    /// </summary>
    public partial class Window_UpdateSelf : Window, INotifyPropertyChanged
    {
        App.UpdateMessage updateMessage;
        public Window_UpdateSelf(App.UpdateMessage updateMessage)
        {
            this.updateMessage = updateMessage;
            DataContext = this;
            NewVersion = updateMessage.NewstVersion;
            CurrentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            Message = updateMessage.Message;
            InitializeComponent();
        }
        public string CurrentVersion { get => _CurrentVersion; set { _CurrentVersion = value;OnPropertyChanged("CurrentVersion"); } }
        public string NewVersion { get => _NewVersion; set { _NewVersion = value; OnPropertyChanged("NewVersion"); } }
        public string Message { get => _Message; set { _Message = value; OnPropertyChanged("Message"); } }

        private string _CurrentVersion, _NewVersion, _Message;

        private void Button_Update_Click(object sender, RoutedEventArgs e)
        {
            string Button_Update_Content = (string)Button_Update.Content;
            Button_Update.Content = "下載中..";
            Button_Update.IsEnabled = false;
            var task = Task.Run(() =>
            {
                string filename = Process.GetCurrentProcess().MainModule.FileName;
                File.Move(filename, Path.GetFileNameWithoutExtension(filename) + ".temp" + Path.GetExtension(filename));
                WebClient webClient = new WebClient();
                webClient.DownloadFile(new System.Uri("https://gitlab.com/flier268/Minecraft_updater/raw/master/Release/Minecraft_updater.exe"), filename);
                if (!GetSHA1(filename).Equals(updateMessage.SHA1, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    File.Delete(filename);
                    File.Move(Path.GetFileNameWithoutExtension(filename) + ".temp" + Path.GetExtension(filename), filename);
                    MessageBox.Show("SHA-1 of New version is error");
                    CrossThread_Close();
                }
                else
                {
                    Process.Start(filename, App.Command);
                    Environment.Exit(0);
                }
            });            
        }
        static string GetSHA1(string path)
        {
            try
            {
                FileStream file = new FileStream(path, FileMode.Open);
                SHA1 sha1 = new SHA1CryptoServiceProvider();
                byte[] retval = sha1.ComputeHash(file);
                file.Close();

                StringBuilder sc = new StringBuilder();
                for (int i = 0; i < retval.Length; i++)
                {
                    sc.Append(retval[i].ToString("x2"));
                }
                return sc.ToString();
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
            return "";
        }
        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public delegate void CloseDelagate();
        void CrossThread_Close()
        {
            this.Dispatcher.Invoke((CloseDelagate)delegate
            {
                this.Close();
            });
        }
    }
}

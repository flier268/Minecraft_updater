using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Minecraft_updater.Models;

namespace Minecraft_updater.Services
{
    public class UpdateService
    {
        private const string VersionUrl =
            "https://gitlab.com/flier268/Minecraft_updater/raw/master/Release/Version.txt";

        /// <summary>
        /// 檢查是否有更新 (非同步版本)
        /// </summary>
        /// <returns>UpdateMessage 物件，包含更新資訊</returns>
        public static async Task<UpdateMessage> CheckUpdateAsync()
        {
            var updateMessage = new UpdateMessage();

            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(VersionUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var package = responseString.Split('\n');

                    var ver = new Version(package[0].ToString());
                    var currentVersion =
                        Assembly.GetEntryAssembly()?.GetName().Version ?? new Version("0.0.0.0");
                    int comparison = currentVersion.CompareTo(ver);

                    if (comparison >= 0)
                    {
                        updateMessage.HaveUpdate = false;
                    }
                    else
                    {
                        updateMessage.HaveUpdate = true;
                        updateMessage.NewstVersion = package[0].ToString();
                        updateMessage.SHA1 = package.Length > 1 ? package[1] : "";

                        var stringBuilder = new StringBuilder();
                        if (package.Length > 2)
                        {
                            for (int i = 2; i < package.Length; i++)
                            {
                                stringBuilder.AppendLine(package[i]);
                            }
                        }
                        updateMessage.Message = stringBuilder.ToString();
                    }
                }
            }
            catch
            {
                // 發生錯誤時返回預設值 (不需要更新)
            }

            return updateMessage;
        }

        /// <summary>
        /// 檢查是否有更新 (同步版本，保持向後相容)
        /// </summary>
        /// <returns>UpdateMessage 物件，包含更新資訊</returns>
        public static UpdateMessage CheckUpdate()
        {
            return CheckUpdateAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 啟動自動更新程式
        /// </summary>
        public static void StartAutoUpdater()
        {
            try
            {
                var autoUpdaterPath = Path.Combine(AppContext.BaseDirectory, "AutoUpdater.exe");

                if (File.Exists(autoUpdaterPath))
                {
                    var startInfo = new ProcessStartInfo(autoUpdaterPath)
                    {
                        WindowStyle = ProcessWindowStyle.Minimized,
                        Arguments = "-CheckUpdateWithoutForm",
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    Console.WriteLine("找不到 AutoUpdater.exe");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"啟動自動更新程式失敗: {ex.Message}");
            }
        }
    }
}

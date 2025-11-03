using System;
using System.IO;
using System.Text;

namespace Minecraft_updater.Services
{
    public class Log
    {
        public static bool LogFile { get; set; } = false;

        /// <summary>
        /// 新增一行日誌 (使用 Action 回調來處理 UI 更新)
        /// </summary>
        /// <param name="str">日誌訊息</param>
        /// <param name="uiLogAction">UI 日誌回調函數 (可選)</param>
        public static void AddLine(string str, Action<string>? uiLogAction = null)
        {
            // 呼叫 UI 日誌回調 (由呼叫者決定如何顯示在 UI 上)
            uiLogAction?.Invoke(str);

            // 寫入檔案日誌
            if (LogFile)
            {
                try
                {
                    var logPath = Path.Combine(AppContext.BaseDirectory, "Minecraft_updater.log");

                    using var writer = new StreamWriter(logPath, true, Encoding.UTF8);
                    writer.WriteLine(str);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"無法寫入日誌檔案: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 新增帶顏色的日誌 (使用 Action 回調)
        /// </summary>
        /// <param name="str">日誌訊息</param>
        /// <param name="colorHex">顏色的十六進位代碼 (例如: "#FF0000" 表示紅色)</param>
        /// <param name="uiLogAction">UI 日誌回調函數，接收訊息和顏色</param>
        public static void AddLine(
            string str,
            string colorHex,
            Action<string, string>? uiLogAction = null
        )
        {
            // 呼叫帶顏色的 UI 日誌回調
            uiLogAction?.Invoke(str, colorHex);

            // 寫入檔案日誌 (不包含顏色資訊)
            if (LogFile)
            {
                try
                {
                    var logPath = Path.Combine(AppContext.BaseDirectory, "Minecraft_updater.log");

                    using var writer = new StreamWriter(logPath, true, Encoding.UTF8);
                    writer.WriteLine(str);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"無法寫入日誌檔案: {ex.Message}");
                }
            }
        }
    }
}

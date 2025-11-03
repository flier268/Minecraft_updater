namespace Minecraft_updater.Models
{
    /// <summary>
    /// 更新訊息模型
    /// </summary>
    public class UpdateMessage
    {
        public UpdateMessage()
        {
            HaveUpdate = false;
            NewstVersion = string.Empty;
            SHA1 = string.Empty;
            Message = string.Empty;
        }

        /// <summary>
        /// 是否有可用的更新
        /// </summary>
        public bool HaveUpdate { get; set; }

        /// <summary>
        /// 最新版本號
        /// </summary>
        public string NewstVersion { get; set; }

        /// <summary>
        /// 下載 URL（從 GitHub Release 取得）
        /// 注意：欄位名稱保留為 SHA1 以維持向後相容，但實際存放的是下載連結
        /// </summary>
        public string SHA1 { get; set; }

        /// <summary>
        /// 更新說明（從 GitHub Release body 取得）
        /// </summary>
        public string Message { get; set; }
    }
}

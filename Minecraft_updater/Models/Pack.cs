namespace Minecraft_updater.Models
{
    /// <summary>
    /// Pack 資料傳輸物件 (DTO)
    /// 表示一個檔案同步項目的元資料
    /// </summary>
    public struct Pack
    {
        public bool Delete { get; set; }
        public bool DownloadWhenNotExist { get; set; }
        public string SHA256 { get; set; }
        public string URL { get; set; }
        public string Path { get; set; }
        public bool IsChecked { get; set; }
        public string? MinimumVersion { get; set; }
    }
}

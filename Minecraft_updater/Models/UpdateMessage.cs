namespace Minecraft_updater.Models
{
    public class UpdateMessage
    {
        public UpdateMessage()
        {
            HaveUpdate = false;
            NewstVersion = string.Empty;
            SHA1 = string.Empty;
            Message = string.Empty;
        }

        public bool HaveUpdate { get; set; }
        public string NewstVersion { get; set; }
        public string SHA1 { get; set; }
        public string Message { get; set; }
    }
}

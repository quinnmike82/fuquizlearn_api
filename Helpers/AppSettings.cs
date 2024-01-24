namespace fuquizlearn_api.Helpers
{
    public class AppSettings
    {
        public string AccessTokenSecret { get; set; }
        public int AccessTokenTTL { get; set; } = 24; //as hour

        public int RefreshTokenTTL { get; set; } = 30; //as day

        public string EmailFrom { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
    }
}

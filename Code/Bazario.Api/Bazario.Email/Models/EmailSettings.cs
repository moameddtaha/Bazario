namespace Bazario.Email.Models
{
    /// <summary>
    /// Configuration model for email settings
    /// </summary>
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public bool UseDefaultCredentials { get; set; } = false;
    }
}

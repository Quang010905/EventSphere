﻿namespace EventSphere.Models.ModelViews
{
    public class MailSettings
    {
        public string SmtpServer { get; set; } = "";
        public int SmtpPort { get; set; } = 0;
        public string SenderName { get; set; } = "";
        public string SenderEmail { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool UseSsl {  get; set; } 
    }
}

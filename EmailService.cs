using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public void SendResetLink(string toEmail, string resetLink)
    {
        var smtpClient = new SmtpClient(_config["EmailSettings:SmtpServer"])
        {
            Port = int.Parse(_config["EmailSettings:SmtpPort"]),
            Credentials = new NetworkCredential(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:SenderPassword"]),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_config["EmailSettings:SenderEmail"], _config["EmailSettings:SenderName"]),
            Subject = "Reset your WatchWave password",
            Body = $"Click here to reset your password: {resetLink}",
            IsBodyHtml = true,
        };

        mailMessage.To.Add(toEmail);
        smtpClient.Send(mailMessage);
    }
}

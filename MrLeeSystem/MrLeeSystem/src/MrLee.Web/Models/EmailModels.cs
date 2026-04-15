namespace MrLee.Web.Models;

public class SmtpSettings
{
    public string Host { get; set; } = "smtp-relay.brevo.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "a55307001@smtp-brevo.com";
    public string Password { get; set; } = "bsknqvR67J8cubC";
    public string FromEmail { get; set; } = "bryanjaencontreras@gmail.com";
    public string FromName { get; set; } = "Mr Lee System";
    public bool EnableSsl { get; set; } = true;
}

using Mailjet.Client;
using Mailjet.Client.Resources;
using Mailjet.Client.TransactionalEmails;

namespace Seng2250A3.Services;

public interface IMailjetService
{
    Task SendVerificationEmailAsync(string recipientEmail, string verificationCode);
    Task SendUserDetailsEmailAsync(string recipientEmail, string username, string password);
}

public class MailjetService : IMailjetService
{
    private readonly MailjetClient _client;

    public MailjetService(IConfiguration configuration)
    {
        var apiKey = configuration["Mailjet:ApiKey"];
        var apiSecret = configuration["Mailjet:ApiSecret"];
        _client = new MailjetClient(apiKey, apiSecret);
    }

    public async Task SendVerificationEmailAsync(string recipientEmail, string verificationCode)
    {
       
        var email = new TransactionalEmailBuilder()
                .WithFrom(new SendContact("batesysgaming@gmail.com"))
                .WithSubject("Verification Code")
                .WithHtmlPart($"<h1>{verificationCode}</h1>")
                .WithTo(new SendContact("batesysgaming@gmail.com"))
                .Build();
        
        await _client.SendTransactionalEmailAsync(email);
    }
    
    public async Task SendUserDetailsEmailAsync(string recipientEmail, string username, string password)
    {
        var email = new TransactionalEmailBuilder()
            .WithFrom(new SendContact("batesysgaming@gmail.com"))
            .WithSubject("Your Account Details")
            .WithHtmlPart($@"
                <h1>Welcome, {username}!</h1>
                <p><strong>Username:</strong> {username}</p>
                <p><strong>Password:</strong> {password}</p>")
            .WithTo(new SendContact("batesysgaming@gmail.com"))
            .Build();

        var response = await _client.SendTransactionalEmailAsync(email);
    }
}
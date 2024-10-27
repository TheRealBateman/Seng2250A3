using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace Seng2250A3.Services;

public interface IMailjetService
{
    Task SendVerificationEmailAsync(string recipientEmail, string verificationCode);
}

public class MailjetService : IMailjetService
{
    private readonly MailjetClient _client;

    public MailjetService(IConfiguration configuration)
    {
        var apiKey = configuration["Mailjet:ApiKey"];
        var apiSecret = configuration["Mailjet:ApiSecret"];
        _client = new MailjetClient(apiKey, apiSecret);
        Console.WriteLine("MailjetService instantiated");
    }

    public async Task SendVerificationEmailAsync(string recipientEmail, string verificationCode)
    {
        var request = new MailjetRequest
        {
            Resource = Send.Resource
        }
        .Property(Send.Messages, new JArray {
            new JObject {
                { "From", new JObject {
                    { "Email", "c3259091@uon.edu.au" },
                    { "Name", "Your Name" }
                }},
                { "To", new JArray {
                    new JObject {
                        { "Email", recipientEmail }
                    }
                }},
                { "Subject", "Your Verification Code" },
                { "TextPart", $"Your verification code is: {verificationCode}" }
            }
        });

        await _client.PostAsync(request);
    }
}

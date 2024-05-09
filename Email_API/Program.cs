using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net;
using System.Net.Mail;

namespace Email_API;

internal class Program
{
    public static IConfiguration _configuration { get; set; }
    private static SmtpClient _smtpClient;

    private static void Main()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>();

        _configuration = builder.Build();

        InitializeSmtpClient();

        var url = _configuration["RabbitMQ:RMQ_URL"];
        var factory = new ConnectionFactory()
        {
            Uri = new Uri(url ?? throw new Exception()),
        };

        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "errorQueue", durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueDeclare(queue: "successQueue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var errorConsumer = new EventingBasicConsumer(channel);
            errorConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Error Received {0}", message);
                SendFailureEmail();
            };
            channel.BasicConsume(queue: "errorQueue", autoAck: true, consumer: errorConsumer);

            var successConsumer = new EventingBasicConsumer(channel);
            successConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Success Received {0}", message);
                SendSuccessEmail();
            };
            channel.BasicConsume(queue: "successQueue", autoAck: true, consumer: successConsumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }

    private static void InitializeSmtpClient()
    {
        _smtpClient = new SmtpClient("sandbox.smtp.mailtrap.io", 2525)
        {
            Credentials = new NetworkCredential(
                _configuration["MailTrap:USERNAME"],
                _configuration["MailTrap:PASSWORD"]),
            EnableSsl = true
        };
    }

    private static void SendFailureEmail()
    {
        try
        {
            _smtpClient.Send("6230fc3602-52e247@inbox.mailtrap.io", "to@example.com", "Your ticket purchase was not successful", "There was an error with your transaction. \n\nPlease ensure that you have sufficient money on your account.");
            Console.WriteLine("Sent");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send failure email: {ex.Message}");
        }
    }

    private static void SendSuccessEmail()
    {
        try
        {
            _smtpClient.Send("6230fc3602-52e247@inbox.mailtrap.io", "to@example.com", "Your ticket purchase was successful", "Here is your ticket information.");
            Console.WriteLine("Sent");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send success email: {ex.Message}");
        }
    }
}

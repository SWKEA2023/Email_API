using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net;
using System.Net.Mail;

namespace Email_API;

internal class Program
{
    private static SmtpClient _smtpClient;

    private static void Main()
    {
        var rabbitUrl = Environment.GetEnvironmentVariable("RMQ_URL");
        var declineQueue = Environment.GetEnvironmentVariable("TRANSACTION_QUEUE");
        var successQueue = Environment.GetEnvironmentVariable("ADMIN_QUEUE");

        InitializeSmtpClient();

        var factory = new ConnectionFactory()
        {
            Uri = new Uri(rabbitUrl ?? throw new ArgumentNullException())
        };

        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: declineQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueDeclare(queue: successQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var errorConsumer = new EventingBasicConsumer(channel);
            errorConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Error Received {0}", message);
                SendFailureEmail(message);
            };
            channel.BasicConsume(queue: declineQueue, autoAck: true, consumer: errorConsumer);

            var successConsumer = new EventingBasicConsumer(channel);
            successConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Success Received {0}", message);
                SendSuccessEmail(message);
            };

            channel.BasicConsume(queue: successQueue, autoAck: true, consumer: successConsumer);

            Console.WriteLine("Application started. Press Ctrl+C to exit.");
            var resetEvent = new ManualResetEvent(false);
            resetEvent.WaitOne();
        }
    }

    private static void InitializeSmtpClient()
    {
        var mailUsername = Environment.GetEnvironmentVariable("MAIL_USERNAME");
        var mailPassword = Environment.GetEnvironmentVariable("MAIL_PASSWORD");
        _smtpClient = new SmtpClient("sandbox.smtp.mailtrap.io", 2525)
        {
            Credentials = new NetworkCredential(
                userName: mailUsername,
                password: mailPassword),
            EnableSsl = true
        };
    }

    private static void SendFailureEmail(string jsonMessage)
    {
        var ticketInfo = System.Text.Json.JsonSerializer.Deserialize<TicketMessage>(jsonMessage);

        if (ticketInfo == null)
        {
            Console.WriteLine("Failed to parse ticket information.");
            return;
        }

        var customer = ticketInfo.Data?.Order?.Customer;

        Console.WriteLine($"Customer Info: {System.Text.Json.JsonSerializer.Serialize(customer)}");

        if (customer == null)
        {
            Console.WriteLine("Customer information is missing.");
            return;
        }

        try
        {
            string subject = "Important: Issue with Your Ticket Purchase";
            string body = $"Dear {customer.FirstName} {customer.LastName},\n\n" +
                          "We're sorry to inform you that your recent attempt to purchase tickets through CinemaHub was not successful. The transaction failed due to insufficient funds in your account.\n\n" +
                          "To resolve this issue, please:\n" +
                          "- Check your account for sufficient funds.\n" +
                          "- Verify your payment information for accuracy.\n" +
                          "- Try making the purchase again.\n\n" +
                          "If you continue to experience problems, or if you believe this message is in error, please do not hesitate to reach out to our customer support at support@cinemahub.com or call us at 42069420.\n\n" +
                          "We apologize for any inconvenience and thank you for your understanding.\n\n" +
                          "Warm regards,\n" +
                          "The CinemaHub Team";

            _smtpClient.Send("6230fc3602-52e247@inbox.mailtrap.io", customer.Email, subject, body);
            Console.WriteLine("Decline Email sent");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send failure email: {ex.Message}");
        }
    }

    private static void SendSuccessEmail(string jsonMessage)
    {
        var ticketInfo = System.Text.Json.JsonSerializer.Deserialize<TicketMessage>(jsonMessage);
        if (ticketInfo == null)
        {
            Console.WriteLine("Failed to parse ticket information.");
            return;
        }

        var customer = ticketInfo.Data?.Order?.Customer;
        var screening = ticketInfo.Data?.Screening;
        var seat = ticketInfo.Data?.Seat;
        var movie = screening?.Movie;
        var hall = screening?.Hall;

        Console.WriteLine($"Customer Info: {System.Text.Json.JsonSerializer.Serialize(customer)}");
        Console.WriteLine($"Screening Info: {System.Text.Json.JsonSerializer.Serialize(screening)}");
        Console.WriteLine($"Seat Info: {System.Text.Json.JsonSerializer.Serialize(seat)}");
        Console.WriteLine($"Movie Info: {System.Text.Json.JsonSerializer.Serialize(movie)}");
        Console.WriteLine($"Hall Info: {System.Text.Json.JsonSerializer.Serialize(hall)}");

        if (customer == null || screening == null || seat == null || movie == null || hall == null)
        {
            Console.WriteLine("Necessary information is missing from the ticket message.");
            return;
        }

        try
        {
            string subject = $"Your ticket for {movie.Title} is confirmed!";
            string body = $"Dear {customer.FirstName} {customer.LastName},\n" +
                          $"Your ticket for '{movie.Title}' on {screening.StartTime} at {hall.HallName} is confirmed.\n" +
                          $"Seat: Row {seat.SeatRow}, Number {seat.SeatNumber}\n" +
                          $"Enjoy your movie!\n";

            _smtpClient.Send("6230fc3602-52e247@inbox.mailtrap.io", customer.Email, subject, body);
            Console.WriteLine("Success Email sent");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send success email: {ex.Message}");
        }
    }
}

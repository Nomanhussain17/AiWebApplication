namespace AichatBot3.Services
{
    public class SmsSender : ISmsSender
    {
        public Task SendSmsAsync(string number, string message)
        {
            // For demo purposes
            Console.WriteLine($"SMS sent to {number}: {message}");
            return Task.CompletedTask;
        }
    }

}

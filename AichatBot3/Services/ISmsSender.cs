namespace AichatBot3.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }

}

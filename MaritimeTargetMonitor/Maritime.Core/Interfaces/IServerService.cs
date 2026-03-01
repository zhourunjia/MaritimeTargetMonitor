namespace Maritime.Core.Interfaces
{
    public interface IServerService
    {
        bool Connect();
        void Disconnect();
        bool IsConnected { get; }
        T SendRequest<T>(string endpoint, object data = null);
    }
}

namespace Maritime.Core.Interfaces
{
    public interface ICameraService
    {
        bool Connect();
        void Disconnect();
        bool IsConnected { get; }
        void StartStream();
        void StopStream();
    }
}

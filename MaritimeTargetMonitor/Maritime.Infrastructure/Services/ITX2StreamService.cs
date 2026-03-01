using System;
using System.Drawing;

namespace Maritime.Infrastructure.Services
{
    public enum TX2StreamState
    {
        Disconnected,
        Connecting,
        Handshaking,
        Streaming,
        ErrorBackoff
    }

    public interface ITX2StreamService
    {
        TX2StreamState State { get; }
        string IpAddress { get; }
        int Port { get; }
        bool IsConnected { get; }
        int Fps { get; }

        event EventHandler<Bitmap> FrameReceived;
        event EventHandler<TX2StreamState> StateChanged;
        event EventHandler<string> ErrorOccurred;

        void Connect();
        void Disconnect();
        void Start();
        void Stop();
    }
}
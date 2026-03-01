using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Maritime.Core.Logging;

namespace Maritime.Infrastructure.Services
{
    public class VisualStreamService : ITX2StreamService
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource;
        private TX2StreamState _state;
        private int _backoffIndex;
        private readonly int[] _backoffDelays = new int[] { 1, 2, 5, 10 };
        private int _fps;
        private int _frameCount;
        private DateTime _lastFpsUpdate;

        public VisualStreamService(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
            _state = TX2StreamState.Disconnected;
            _fps = 0;
        }

        public TX2StreamState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    Logger.Info($"可视化流状态变更: {_state}");
                    StateChanged?.Invoke(this, _state);
                }
            }
        }

        public string IpAddress => _ipAddress;
        public int Port => _port;
        public bool IsConnected => State == TX2StreamState.Streaming;
        public int Fps => _fps;

        public event EventHandler<Bitmap> FrameReceived;
        public event EventHandler<TX2StreamState> StateChanged;
        public event EventHandler<string> ErrorOccurred;

        public void Connect()
        {
            if (State != TX2StreamState.Disconnected)
            {
                Logger.Warn($"可视化流已连接，当前状态: {State}");
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    await ConnectAsync(_cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Logger.Error("可视化流连接失败", ex);
                    ErrorOccurred?.Invoke(this, $"连接失败: {ex.Message}");
                    EnterErrorBackoff();
                }
            });
        }

        public void Disconnect()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _stream?.Close();
                _tcpClient?.Close();
                State = TX2StreamState.Disconnected;
                _backoffIndex = 0;
                Logger.Info("可视化流已断开");
            }
            catch (Exception ex)
            {
                Logger.Error("可视化流断开失败", ex);
            }
        }

        public void Start()
        {
            Connect();
        }

        public void Stop()
        {
            Disconnect();
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                State = TX2StreamState.Connecting;
                _tcpClient = new TcpClient();
                using (cancellationToken.Register(() => _tcpClient.Close()))
                {
                    await _tcpClient.ConnectAsync(_ipAddress, _port);
                }
                _stream = _tcpClient.GetStream();

                State = TX2StreamState.Handshaking;
                await HandshakeAsync(cancellationToken);

                State = TX2StreamState.Streaming;
                _backoffIndex = 0;

                await ReceiveFramesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.Info("可视化流连接已取消");
                State = TX2StreamState.Disconnected;
            }
            catch (Exception ex)
            {
                Logger.Error("可视化流连接异常", ex);
                ErrorOccurred?.Invoke(this, $"连接异常: {ex.Message}");
                throw;
            }
        }

        private async Task HandshakeAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.Info("开始可视化流握手");

                byte[] servInfo = Encoding.ASCII.GetBytes("SERV_INFO");
                await _stream.WriteAsync(servInfo, 0, servInfo.Length, cancellationToken);

                byte[] buffer = new byte[1024];
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                Logger.Info($"握手响应: {response}");

                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Error("可视化流握手失败", ex);
                throw;
            }
        }

        private async Task ReceiveFramesAsync(CancellationToken cancellationToken)
        {
            _frameCount = 0;
            _lastFpsUpdate = DateTime.Now;
            var pingTimer = new PeriodicTimer(TimeSpan.FromSeconds(2));

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (await pingTimer.WaitForNextTickAsync(cancellationToken))
                    {
                        await SendPingAsync(cancellationToken);
                    }

                    byte[] buffer = new byte[65536];
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0)
                    {
                        Logger.Warn("可视化流接收到0字节，触发重连");
                        throw new IOException("连接断开");
                    }

                    ProcessJpegFrame(buffer, bytesRead);
                    UpdateFps();
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Info("可视化流接收已取消");
            }
            catch (Exception ex)
            {
                Logger.Error("可视化流接收异常", ex);
                ErrorOccurred?.Invoke(this, $"接收异常: {ex.Message}");
                throw;
            }
        }

        private async Task SendPingAsync(CancellationToken cancellationToken)
        {
            try
            {
                byte[] ping = Encoding.ASCII.GetBytes("PING");
                await _stream.WriteAsync(ping, 0, ping.Length, cancellationToken);
                Logger.Debug("发送可视化流PING");
            }
            catch (Exception ex)
            {
                Logger.Error("发送PING失败", ex);
            }
        }

        private void ProcessJpegFrame(byte[] buffer, int length)
        {
            try
            {
                using (var ms = new MemoryStream(buffer, 0, length))
                {
                    var bitmap = new Bitmap(ms);
                    FrameReceived?.Invoke(this, bitmap);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("处理JPEG帧失败", ex);
            }
        }

        private void UpdateFps()
        {
            _frameCount++;
            var elapsed = DateTime.Now - _lastFpsUpdate;

            if (elapsed.TotalSeconds >= 1)
            {
                _fps = (int)(_frameCount / elapsed.TotalSeconds);
                _frameCount = 0;
                _lastFpsUpdate = DateTime.Now;
            }
        }

        private void EnterErrorBackoff()
        {
            State = TX2StreamState.ErrorBackoff;

            if (_backoffIndex < _backoffDelays.Length)
            {
                int delay = _backoffDelays[_backoffIndex];
                Logger.Info($"可视化流退避重连，延迟{delay}秒");
                Task.Delay(delay * 1000).ContinueWith(t => Connect());
                _backoffIndex++;
            }
            else
            {
                Logger.Error("可视化流退避重连次数已达上限");
                State = TX2StreamState.Disconnected;
                _backoffIndex = 0;
            }
        }
    }

    public static class TaskExtensions
    {
        public static Task ContinueWith(this Task task, Action<Task> continuation)
        {
            return task.ContinueWith(t => continuation(t), TaskScheduler.Default);
        }
    }

}

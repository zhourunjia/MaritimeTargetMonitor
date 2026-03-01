using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Maritime.Core.Logging;

namespace Maritime.Infrastructure.Services
{
    public class ThermalStreamService : ITX2StreamService
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly int _width;
        private readonly int _height;
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource;
        private TX2StreamState _state;
        private int _backoffIndex;
        private readonly int[] _backoffDelays = new int[] { 1, 2, 5, 10 };
        private int _fps;
        private int _frameCount;
        private DateTime _lastFpsUpdate;
        private DateTime _lastFrameTime;

        public ThermalStreamService(string ipAddress, int port, int width, int height)
        {
            _ipAddress = ipAddress;
            _port = port;
            _width = width;
            _height = height;
            _state = TX2StreamState.Disconnected;
            _fps = 0;
            _lastFrameTime = DateTime.Now;
        }

        public TX2StreamState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    Logger.Info($"热成像流状态变更: {_state}");
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
                Logger.Warn($"热成像流已连接，当前状态: {State}");
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
                    Logger.Error("热成像流连接失败", ex);
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
                Logger.Info("热成像流已断开");
            }
            catch (Exception ex)
            {
                Logger.Error("热成像流断开失败", ex);
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

                State = TX2StreamState.Streaming;
                _backoffIndex = 0;

                await ReceiveFramesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.Info("热成像流连接已取消");
                State = TX2StreamState.Disconnected;
            }
            catch (Exception ex)
            {
                Logger.Error("热成像流连接异常", ex);
                ErrorOccurred?.Invoke(this, $"连接异常: {ex.Message}");
                throw;
            }
        }

        private async Task ReceiveFramesAsync(CancellationToken cancellationToken)
        {
            _frameCount = 0;
            _lastFpsUpdate = DateTime.Now;
            _lastFrameTime = DateTime.Now;
            var pingTimer = new PeriodicTimer(TimeSpan.FromSeconds(2));

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (await pingTimer.WaitForNextTickAsync(cancellationToken))
                    {
                        await SendPingAsync(cancellationToken);
                    }

                    int frameSize = _width * _height * 2;
                    byte[] buffer = new byte[frameSize];
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0)
                    {
                        Logger.Warn("热成像流接收到0字节，触发重连");
                        throw new IOException("连接断开");
                    }

                    ProcessYuyvFrame(buffer, bytesRead);
                    UpdateFps();
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Info("热成像流接收已取消");
            }
            catch (Exception ex)
            {
                Logger.Error("热成像流接收异常", ex);
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
                Logger.Debug("发送热成像流PING");
            }
            catch (Exception ex)
            {
                Logger.Error("发送PING失败", ex);
            }
        }

        private void ProcessYuyvFrame(byte[] buffer, int length)
        {
            try
            {
                int frameSize = _width * _height * 2;
                if (length < frameSize)
                {
                    Logger.Warn($"热成像帧数据不完整，期望{frameSize}字节，实际{length}字节");
                    return;
                }

                var bitmap = ConvertYuyvToRgb24(buffer, _width, _height);
                FrameReceived?.Invoke(this, bitmap);
            }
            catch (Exception ex)
            {
                Logger.Error("处理YUYV帧失败", ex);
            }
        }

        private Bitmap ConvertYuyvToRgb24(byte[] yuyvData, int width, int height)
        {
            int rgbDataSize = width * height * 3;
            byte[] rgbData = new byte[rgbDataSize];

            for (int i = 0; i < width * height; i++)
            {
                int yuyvIndex = i * 4;
                int y0 = yuyvData[yuyvIndex];
                int u = yuyvData[yuyvIndex + 1];
                int y1 = yuyvData[yuyvIndex + 2];
                int v = yuyvData[yuyvIndex + 3];

                int rgbIndex = i * 3;

                int c = y0 - 16;
                int d = u - 128;
                int e = v - 128;

                byte r = ClampToByte((298 * c + 409 * e + 128) >> 8);
                byte g = ClampToByte((298 * c - 100 * d - 208 * e + 128) >> 8);
                byte b = ClampToByte((298 * c + 516 * d + 128) >> 8);

                rgbData[rgbIndex] = r;
                rgbData[rgbIndex + 1] = g;
                rgbData[rgbIndex + 2] = b;

                c = y1 - 16;
                r = ClampToByte((298 * c + 409 * e + 128) >> 8);
                g = ClampToByte((298 * c - 100 * d - 208 * e + 128) >> 8);
                b = ClampToByte((298 * c + 516 * d + 128) >> 8);

                rgbData[rgbIndex + 3] = r;
                rgbData[rgbIndex + 4] = g;
                rgbData[rgbIndex + 5] = b;
            }

            return CreateBitmapFromRgb24(rgbData, width, height);
        }

        private static byte ClampToByte(int value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (byte)value;
        }

        private Bitmap CreateBitmapFromRgb24(byte[] rgbData, int width, int height)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            IntPtr ptr = bmpData.Scan0;
            Marshal.Copy(rgbData, 0, ptr, rgbData.Length);
            bitmap.UnlockBits(bmpData);

            return bitmap;
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
                Logger.Info($"热成像流退避重连，延迟{delay}秒");
                Task.Delay(delay * 1000).ContinueWith(t => Connect());
                _backoffIndex++;
            }
            else
            {
                Logger.Error("热成像流退避重连次数已达上限");
                State = TX2StreamState.Disconnected;
                _backoffIndex = 0;
            }
        }
    }

    public class PeriodicTimer
    {
        private readonly TimeSpan _period;
        private DateTime _nextTick;

        public PeriodicTimer(TimeSpan period)
        {
            _period = period;
            _nextTick = DateTime.Now + period;
        }

        public async Task<bool> WaitForNextTickAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            if (now >= _nextTick)
            {
                _nextTick = now + _period;
                return true;
            }

            var delay = _nextTick - now;
            await Task.Delay(delay, cancellationToken);
            _nextTick = _nextTick + _period;
            return true;
        }
    }
}

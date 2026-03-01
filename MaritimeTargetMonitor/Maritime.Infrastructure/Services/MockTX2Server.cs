using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Maritime.Core.Logging;

namespace Maritime.Infrastructure.Services
{
    public class MockTX2Server
    {
        private readonly int _visualPort;
        private readonly int _thermalPort;
        private TcpListener _visualListener;
        private TcpListener _thermalListener;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;
        private Timer _frameTimer;
        private int _frameCount;

        public MockTX2Server(int visualPort = 5000, int thermalPort = 5001)
        {
            _visualPort = visualPort;
            _thermalPort = thermalPort;
            _isRunning = false;
            _frameCount = 0;
        }

        public bool IsRunning => _isRunning;

        public void Start()
        {
            if (_isRunning)
            {
                Logger.Warn("模拟TX2服务器已在运行");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;

            Task.Run(() => StartVisualServerAsync(_cancellationTokenSource.Token));
            Task.Run(() => StartThermalServerAsync(_cancellationTokenSource.Token));

            _frameTimer = new Timer(SendFrames, null, 0, 33);
            Logger.Info($"模拟TX2服务器已启动，可视化端口: {_visualPort}, 热成像端口: {_thermalPort}");
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _cancellationTokenSource?.Cancel();
            _visualListener?.Stop();
            _thermalListener?.Stop();
            _frameTimer?.Dispose();
            _isRunning = false;
            Logger.Info("模拟TX2服务器已停止");
        }

        private async Task StartVisualServerAsync(CancellationToken cancellationToken)
        {
            try
            {
                _visualListener = new TcpListener(IPAddress.Any, _visualPort);
                _visualListener.Start();

                Logger.Info($"可视化流服务器已启动，监听端口: {_visualPort}");

                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await _visualListener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleVisualClientAsync(client, cancellationToken));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("可视化流服务器异常", ex);
            }
        }

        private async Task StartThermalServerAsync(CancellationToken cancellationToken)
        {
            try
            {
                _thermalListener = new TcpListener(IPAddress.Any, _thermalPort);
                _thermalListener.Start();

                Logger.Info($"热成像流服务器已启动，监听端口: {_thermalPort}");

                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await _thermalListener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleThermalClientAsync(client, cancellationToken));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("热成像流服务器异常", ex);
            }
        }

        private async Task HandleVisualClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                Logger.Info($"可视化流客户端已连接: {client.Client.RemoteEndPoint}");
                var stream = client.GetStream();

                await HandleHandshakeAsync(stream, cancellationToken);
                await SendPingLoopAsync(stream, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Error("处理可视化流客户端异常", ex);
            }
            finally
            {
                client?.Close();
                Logger.Info($"可视化流客户端已断开");
            }
        }

        private async Task HandleThermalClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                Logger.Info($"热成像流客户端已连接: {client.Client.RemoteEndPoint}");
                var stream = client.GetStream();

                await SendPingLoopAsync(stream, cancellationToken);
                await SendThermalFramesAsync(stream, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Error("处理热成像流客户端异常", ex);
            }
            finally
            {
                client?.Close();
                Logger.Info($"热成像流客户端已断开");
            }
        }

        private async Task HandleHandshakeAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (bytesRead > 0)
                {
                    string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Logger.Info($"收到握手请求: {request}");

                    if (request.Contains("SERV_INFO"))
                    {
                        string response = "{\"status\":\"ok\",\"width\":640,\"height\":480}";
                        byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
                        Logger.Info("发送握手响应");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("处理握手异常", ex);
            }
        }

        private async Task SendPingLoopAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            try
            {
                byte[] ping = Encoding.ASCII.GetBytes("PING");

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(2000, cancellationToken);
                    await stream.WriteAsync(ping, 0, ping.Length, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("发送PING异常", ex);
            }
        }

        private async Task SendThermalFramesAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            try
            {
                int width = 640;
                int height = 480;
                int frameSize = width * height * 2;

                while (!cancellationToken.IsCancellationRequested)
                {
                    byte[] frame = GenerateThermalFrame(width, height);
                    await stream.WriteAsync(frame, 0, frame.Length, cancellationToken);
                    await Task.Delay(33, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("发送热成像帧异常", ex);
            }
        }

        private void SendFrames(object state)
        {
            _frameCount++;
            Logger.Debug($"模拟TX2服务器已发送{_frameCount}帧");
        }

        private byte[] GenerateThermalFrame(int width, int height)
        {
            int frameSize = width * height * 2;
            byte[] frame = new byte[frameSize];

            for (int i = 0; i < frameSize; i += 4)
            {
                byte y = (byte)_random.Next(16, 240);
                byte u = (byte)_random.Next(0, 256);
                byte v = (byte)_random.Next(0, 256);

                frame[i] = y;
                frame[i + 1] = u;
                frame[i + 2] = y;
                frame[i + 3] = v;
            }

            return frame;
        }

        private readonly Random _random = new Random();
    }
}
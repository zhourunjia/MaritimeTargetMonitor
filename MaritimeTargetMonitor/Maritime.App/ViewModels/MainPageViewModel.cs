using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Maritime.Core.Logging;
using Maritime.Infrastructure.Services;

namespace Maritime.App.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private bool _isDroneConnected;
        private string _droneStatus;
        private int _speed;
        private int _selectedTabIndex;
        private TX2StreamState _visualStreamState;
        private TX2StreamState _thermalStreamState;
        private int _visualFps;
        private int _thermalFps;
        private Bitmap _visualFrame;
        private Bitmap _thermalFrame;
        private bool _useMockServer;
        private MockTX2Server _mockServer;
        private ITX2StreamService _visualStreamService;
        private ITX2StreamService _thermalStreamService;

        public MainPageViewModel()
        {
            InitializeCommands();
            InitializeData();
            InitializeStreamServices();
        }

        private void InitializeCommands()
        {
            StartInspectionCommand = new RelayCommand(OnStartInspection);
            StopInspectionCommand = new RelayCommand(OnStopInspection);
            SettingsCommand = new RelayCommand(OnSettings);
            ConnectCommand = new RelayCommand(OnConnect);
            DisconnectCommand = new RelayCommand(OnDisconnect);
            ForwardCommand = new RelayCommand(OnForward);
            BackwardCommand = new RelayCommand(OnBackward);
            LeftCommand = new RelayCommand(OnLeft);
            RightCommand = new RelayCommand(OnRight);
            StartStreamsCommand = new RelayCommand(OnStartStreams);
            StopStreamsCommand = new RelayCommand(OnStopStreams);
        }

        private void InitializeData()
        {
            IsDroneConnected = false;
            DroneStatus = "未连接";
            Speed = 50;
            SelectedTabIndex = 0;
            VisualStreamState = TX2StreamState.Disconnected;
            ThermalStreamState = TX2StreamState.Disconnected;
            VisualFps = 0;
            ThermalFps = 0;
            UseMockServer = true;

            DroneInfoItems = new ObservableCollection<DroneInfoItem>
            {
                new DroneInfoItem { Name = "电池电量", Value = "85%" },
                new DroneInfoItem { Name = "信号强度", Value = "强" },
                new DroneInfoItem { Name = "飞行高度", Value = "100m" },
                new DroneInfoItem { Name = "飞行速度", Value = "15m/s" },
                new DroneInfoItem { Name = "GPS坐标", Value = "31.2304, 121.4737" },
                new DroneInfoItem { Name = "飞行时间", Value = "00:15:30" }
            };
        }

        private void InitializeStreamServices()
        {
            _mockServer = new MockTX2Server(5000, 5001);
            _visualStreamService = new VisualStreamService("127.0.0.1", 5000);
            _thermalStreamService = new ThermalStreamService("127.0.0.1", 5001, 640, 480);

            _visualStreamService.StateChanged += OnVisualStreamStateChanged;
            _visualStreamService.FrameReceived += OnVisualFrameReceived;
            _visualStreamService.ErrorOccurred += OnVisualStreamError;

            _thermalStreamService.StateChanged += OnThermalStreamStateChanged;
            _thermalStreamService.FrameReceived += OnThermalFrameReceived;
            _thermalStreamService.ErrorOccurred += OnThermalStreamError;
        }

        public ICommand StartInspectionCommand { get; private set; }
        public ICommand StopInspectionCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand ConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand ForwardCommand { get; private set; }
        public ICommand BackwardCommand { get; private set; }
        public ICommand LeftCommand { get; private set; }
        public ICommand RightCommand { get; private set; }
        public ICommand StartStreamsCommand { get; private set; }
        public ICommand StopStreamsCommand { get; private set; }

        public bool IsDroneConnected
        {
            get => _isDroneConnected;
            set
            {
                _isDroneConnected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConnectButtonVisibility));
                OnPropertyChanged(nameof(DisconnectButtonVisibility));
            }
        }

        public string DroneStatus
        {
            get => _droneStatus;
            set
            {
                _droneStatus = value;
                OnPropertyChanged();
            }
        }

        public int Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                OnPropertyChanged();
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DroneInfoItem> DroneInfoItems { get; set; }

        public string ConnectButtonVisibility => IsDroneConnected ? "Collapsed" : "Visible";
        public string DisconnectButtonVisibility => IsDroneConnected ? "Visible" : "Collapsed";

        public TX2StreamState VisualStreamState
        {
            get => _visualStreamState;
            set
            {
                _visualStreamState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VisualStreamStateText));
            }
        }

        public TX2StreamState ThermalStreamState
        {
            get => _thermalStreamState;
            set
            {
                _thermalStreamState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThermalStreamStateText));
            }
        }

        public int VisualFps
        {
            get => _visualFps;
            set
            {
                _visualFps = value;
                OnPropertyChanged();
            }
        }

        public int ThermalFps
        {
            get => _thermalFps;
            set
            {
                _thermalFps = value;
                OnPropertyChanged();
            }
        }

        public Bitmap VisualFrame
        {
            get => _visualFrame;
            set
            {
                _visualFrame = value;
                OnPropertyChanged();
            }
        }

        public Bitmap ThermalFrame
        {
            get => _thermalFrame;
            set
            {
                _thermalFrame = value;
                OnPropertyChanged();
            }
        }

        public bool UseMockServer
        {
            get => _useMockServer;
            set
            {
                _useMockServer = value;
                OnPropertyChanged();
            }
        }

        public string VisualStreamStateText => GetStreamStateText(VisualStreamState);
        public string ThermalStreamStateText => GetStreamStateText(ThermalStreamState);

        private string GetStreamStateText(TX2StreamState state)
        {
            switch (state)
            {
                case TX2StreamState.Disconnected:
                    return "未连接";
                case TX2StreamState.Connecting:
                    return "连接中";
                case TX2StreamState.Handshaking:
                    return "握手中";
                case TX2StreamState.Streaming:
                    return "流传输中";
                case TX2StreamState.ErrorBackoff:
                    return "退避重连";
                default:
                    return "未知";
            }
        }

        private void OnStartInspection()
        {
            Logger.Info("开始巡检");
        }

        private void OnStopInspection()
        {
            Logger.Info("停止巡检");
        }

        private void OnSettings()
        {
            Logger.Info("打开设置");
        }

        private void OnConnect()
        {
            Logger.Info("连接无人机");
            IsDroneConnected = true;
            DroneStatus = "已连接";
        }

        private void OnDisconnect()
        {
            Logger.Info("断开无人机");
            IsDroneConnected = false;
            DroneStatus = "未连接";
        }

        private void OnForward()
        {
            Logger.Info($"前进，速度：{Speed}%");
        }

        private void OnBackward()
        {
            Logger.Info($"后退，速度：{Speed}%");
        }

        private void OnLeft()
        {
            Logger.Info($"左转，速度：{Speed}%");
        }

        private void OnRight()
        {
            Logger.Info($"右转，速度：{Speed}%");
        }

        private void OnStartStreams()
        {
            Logger.Info("启动流服务");

            if (UseMockServer && !_mockServer.IsRunning)
            {
                _mockServer.Start();
                Logger.Info("模拟TX2服务器已启动");
            }

            _visualStreamService.Connect();
            _thermalStreamService.Connect();
        }

        private void OnStopStreams()
        {
            Logger.Info("停止流服务");

            _visualStreamService.Disconnect();
            _thermalStreamService.Disconnect();

            if (_mockServer.IsRunning)
            {
                _mockServer.Stop();
                Logger.Info("模拟TX2服务器已停止");
            }
        }

        private void OnVisualStreamStateChanged(object sender, TX2StreamState state)
        {
            VisualStreamState = state;
            Logger.Info($"可视化流状态变更: {state}");
        }

        private void OnVisualFrameReceived(object sender, Bitmap frame)
        {
            VisualFrame = frame;
            VisualFps = _visualStreamService.Fps;
        }

        private void OnVisualStreamError(object sender, string error)
        {
            Logger.Error($"可视化流错误: {error}");
        }

        private void OnThermalStreamStateChanged(object sender, TX2StreamState state)
        {
            ThermalStreamState = state;
            Logger.Info($"热成像流状态变更: {state}");
        }

        private void OnThermalFrameReceived(object sender, Bitmap frame)
        {
            ThermalFrame = frame;
            ThermalFps = _thermalStreamService.Fps;
        }

        private void OnThermalStreamError(object sender, string error)
        {
            Logger.Error($"热成像流错误: {error}");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DroneInfoItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }
}

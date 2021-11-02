using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using VisualServoCore;
using VisualServoCore.Communication;
using VisualServoCore.Controller;
using Husty;
using Husty.OpenCvSharp.DepthCamera;

namespace WpfApp
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {

        // Primary processes
        private IDisposable _stream;
        private ICommunication<string> _server;
        private Realsense _depthCamera;
        private BgrXyzPlayer _player;
        private DummyDepthFusedController _controller;
        private DataLogger<double> _log;

        // Temporary datas and flags
        private string _initDir;
        private int _visionSelectedIndex;
        private int _communicationSelectedIndex;
        private bool _isSocket;
        private bool _logOn;
        private bool _recOn;
        private double _steer;
        private double _speed;
        private double _gain;
        private int _maxWidth;
        private int _maxDistance;
        private readonly OpenCvSharp.Size _size = new(640, 360);
        private record Preset(
            string InitDir = "C:", int VisionSelectedIndex = 0, int CommunicationSelectedIndex = 0, double Gain = 1.0,
            bool LogOn = false, bool RecOn = false, int MaxWidth = 3000, int MaxDistance = 8000
        );


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            var setting = new UserSetting<Preset>(new());
            var val = setting.Load();
            _initDir = val.InitDir;
            _gain = val.Gain;
            _visionSelectedIndex = val.VisionSelectedIndex;
            _communicationSelectedIndex = val.CommunicationSelectedIndex;
            _logOn = val.LogOn;
            _recOn = val.RecOn;
            _maxWidth = val.MaxWidth;
            _maxDistance = val.MaxDistance;
            SourceCombo.SelectedIndex = _visionSelectedIndex;
            CommunicationCombo.SelectedIndex = _communicationSelectedIndex;
            LogCheck.IsChecked = _logOn;
            RecCheck.IsChecked = _recOn;
            GainText.Text = _gain.ToString();
            MaxWidthText.Text = _maxWidth.ToString();
            MaxDistanceText.Text = _maxDistance.ToString();
            if (!(bool)LogCheck.IsChecked)
                RecCheck.IsEnabled = false;
            Closed += (sender, args) =>
            {
                GC.Collect();
                _log?.Dispose();
                _stream?.Dispose();
                _player?.Dispose();
                _depthCamera?.Dispose();
                setting.Save(new(
                    _initDir, _visionSelectedIndex, _communicationSelectedIndex,
                    _gain, _logOn, _recOn, _maxWidth, _maxDistance)
                );
            };
        }

        private void VehicleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_server is null)
            {
                VehicleButton.IsEnabled = false;
                _communicationSelectedIndex = CommunicationCombo.SelectedIndex;
                _isSocket = _communicationSelectedIndex is 0;
                Task.Run(() =>
                {
                    if (_isSocket)
                        _server = new SocketServer(3000);
                    else
                        _server = new CanHandlerForEv();
                    Dispatcher.Invoke(() =>
                    {
                        VehicleButton.IsEnabled = true;
                        VehicleButton.Background = Brushes.Red;
                    });
                    while (true)
                    {
                        try
                        {
                            Thread.Sleep(50);
                            if (_isSocket)
                                _server?.Send($"alive,{_steer},{_speed}");
                            else
                                _server?.Send($"{_steer}");
                        }
                        catch
                        {
                            _server?.Dispose();
                            _server = null;
                            Dispatcher.Invoke(() => VehicleButton.Background = Brushes.CornflowerBlue);
                            break;
                        }
                    }
                });
            }
            else
            {
                Thread.Sleep(50);
                if (_isSocket)
                    _server?.Send($"die,{0},{0}");
                else
                    _server?.Send($"{0}");
                Thread.Sleep(1000);
                _server?.Dispose();
                _server = null;
                VehicleButton.Background = Brushes.CornflowerBlue;
            }
        }

        private void VisionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_depthCamera is null && _player is null)
            {
                try
                {
                    var gain = double.Parse(GainText.Text);
                    var maxDistance = int.Parse(MaxDistanceText.Text);
                    var maxWidth = int.Parse(MaxWidthText.Text);
                    if (gain < 0) throw new();
                    if (maxDistance <= 0) throw new();
                    if (maxWidth <= 0) throw new();
                    _gain = gain;
                    _maxDistance = maxDistance;
                    _maxWidth = maxWidth;
                    _logOn = (bool)LogCheck.IsChecked;
                    _recOn = (bool)RecCheck.IsChecked;
                    _log = _logOn ? new(_recOn ? _size : null) : null;
                    _visionSelectedIndex = SourceCombo.SelectedIndex;
                }
                catch
                {
                    return;
                }
                _controller = new(_gain, _maxWidth, _maxDistance);
                switch (SourceCombo.SelectedIndex)
                {
                    case 0:
                        var cofd = new CommonOpenFileDialog()
                        {
                            Title = "動画ファイルを選択",
                            InitialDirectory = _initDir,
                            IsFolderPicker = false,
                        };
                        if (cofd.ShowDialog() is CommonFileDialogResult.Ok)
                        {
                            _initDir = Path.GetDirectoryName(cofd.FileName);
                            _player = new(cofd.FileName);
                            _stream = _player.ReactiveFrame
                                .Where(f => f is not null && !f.Empty())
                                .Subscribe(frame =>
                                {
                                    var view = frame.Clone();
                                    if (_recOn) _log?.Write(frame);
                                    var obj = _controller.Run(view);
                                    _log?.Write(obj);
                                    var radar = _controller.GetGroundCoordinateResults();
                                    _steer = obj.Steer;
                                    _speed = obj.Speed;
                                    ProcessUserThread(view.BGR, radar);
                                });
                            VisionButton.Background = Brushes.Red;
                        }
                        cofd.Dispose();
                        break;
                    case 1:
                        _depthCamera = new(_size);
                        _stream = _depthCamera.ReactiveFrame
                            .Where(f => f is not null && !f.Empty())
                            .Subscribe(frame =>
                            {
                                var view = frame.Clone();
                                if (_recOn) _log?.Write(frame);
                                var obj = _controller.Run(view);
                                _log?.Write(obj);
                                var radar = _controller.GetGroundCoordinateResults();
                                _steer = obj.Steer;
                                _speed = obj.Speed;
                                ProcessUserThread(view.BGR, radar);
                            });
                        VisionButton.Background = Brushes.Red;
                        break;
                }
            }
            else
            {
                _log?.Dispose();
                _stream?.Dispose();
                _player?.Dispose();
                _player = null;
                _depthCamera?.Dispose();
                _depthCamera = null;
                VisionButton.Background = Brushes.CornflowerBlue;
                GC.Collect();
            }
        }

        private void ProcessUserThread(Mat view, Mat radar)
        {
            Dispatcher.Invoke(() =>
            {
                LeftImage.Source = radar.ToBitmapSource();
                RightImage.Source = view.ToBitmapSource();
                SendCommandLabel.Content = $"{_steer:f1} deg : {_speed:f1} m/s";
            });
        }

        private void LogCheck_Checked(object sender, RoutedEventArgs e)
        {
            RecCheck.IsEnabled = true;
        }

        private void LogCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            RecCheck.IsEnabled = false;
        }

        private void RecCheck_Checked(object sender, RoutedEventArgs e)
        {
            LogCheck.IsEnabled = false;
        }

        private void RecCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            LogCheck.IsEnabled = true;
        }

        private void SourceCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _visionSelectedIndex = SourceCombo.SelectedIndex;
        }

    }
}

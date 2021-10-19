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
using VisualServoCore.Vision;
using VisualServoCore.Communication;
using VisualServoCore.Controller;
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
        private CanHandlerForEv _server;
        private Realsense _depthCamera;
        private DepthFusedController _controller;
        private BGRXYZStream _video;
        private DataLogger<short> _log;

        // Temporary datas and flags
        private string _initDir;
        private int _visionSelectedIndex;
        private bool _logOn;
        private bool _recOn;
        private short _steer;
        private double _gain;
        private int _maxWidth;
        private int _maxDistance;
        private readonly OpenCvSharp.Size _size = new(640, 360);

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            var cacheFile = "cache";
            try
            {
                using var sr = new StreamReader(cacheFile);
                _initDir = sr.ReadLine();
                _gain = int.Parse(sr.ReadLine());
                _visionSelectedIndex = int.Parse(sr.ReadLine());
                _logOn = sr.ReadLine() is "LogOn" ? true : false;
                _recOn = sr.ReadLine() is "RecOn" ? true : false;
                _maxWidth = int.Parse(sr.ReadLine());
                _maxDistance = int.Parse(sr.ReadLine());
            }
            catch
            {
                _initDir = "C:";
                _gain = 1.0;
                _visionSelectedIndex = 0;
                _maxWidth = 3000;
                _maxDistance = 8000;
            }
            SourceCombo.SelectedIndex = _visionSelectedIndex;
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
                _stream?.Dispose();
                using var sw = new StreamWriter(cacheFile);
                sw.WriteLine(_initDir);
                sw.WriteLine(_gain);
                sw.WriteLine(_visionSelectedIndex);
                sw.WriteLine(_logOn ? "LogOn" : "LogOff");
                sw.WriteLine(_recOn ? "RecOn" : "RecOff");
                sw.WriteLine(_maxWidth);
                sw.WriteLine(_maxDistance);
            };
        }

        private void VehicleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_server is null)
            {
                VehicleButton.IsEnabled = false;
                Task.Run(() =>
                {
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
                            _server?.Send(_steer);
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
                _server?.Dispose();
                _server = null;
                VehicleButton.Background = Brushes.CornflowerBlue;
            }
        }

        private void VisionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_video is null)
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
                _controller = new DepthFusedController(_gain, _maxWidth, _maxDistance);
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
                            _video = new(cofd.FileName);
                            _stream = _video.Connect()
                                .Subscribe(frame =>
                                {
                                    var view = frame.Clone();
                                    if (_recOn) _log?.Write(frame);
                                    var obj = _controller.Run(view);
                                    var radar = _controller.GetGroundCoordinateResults();
                                    _steer = obj.Steer;
                                    _log?.Write(obj);
                                    ProcessUserThread(view.BGR, radar);
                                });
                            VisionButton.Background = Brushes.Red;
                        }
                        cofd.Dispose();
                        break;
                    case 1:
                        _depthCamera = new(_size);
                        _video = new(_depthCamera);
                        _stream = _video.Connect()
                            .Subscribe(frame =>
                            {
                                var view = frame.Clone();
                                if (_recOn) _log?.Write(frame);
                                var obj = _controller.Run(view);
                                var radar = _controller.GetGroundCoordinateResults();
                                _steer = obj.Steer;
                                _log?.Write(obj);
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
                _video?.Disconnect();
                _video = null;
                _depthCamera?.Disconnect();
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
                SendCommandLabel.Content = $"{_steer:f1} deg";
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using KinectDataManagement.DVR;
using Microsoft.Win32;
using System.IO;
using Microsoft.Kinect;

namespace KinectDataManagement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        KinectSensor _sensor = null;
        KinectRecorder _recorder = null;

        ColorFrameReader _colorReader = null;
        DepthFrameReader _depthReader = null;
        InfraredFrameReader _infraredReader = null;
        FrameTypes _displayType = FrameTypes.Color;
        ColorFrameBitmap _colorBitmap = new ColorFrameBitmap();
        DepthFrameBitmap _depthBitmap = new DepthFrameBitmap();
        InfraredFrameBitmap _infraredBitmap = new InfraredFrameBitmap();

        bool _isStarted = false;


        int _colorDelay=0;
        int _depthDelay=0;
        int _infraredDelay=0;

        public MainWindow()
        {
            InitializeComponent();

            RecordButton.Click += RecordButton_Click;

            ColorFpsCompressionCombo.Items.Add("100%");
            ColorFpsCompressionCombo.Items.Add("75%");
            ColorFpsCompressionCombo.Items.Add("50%");
            ColorFpsCompressionCombo.Items.Add("25%");
            ColorFpsCompressionCombo.SelectionChanged += ColorFpsCompressionCombo_SelectionChanged;
            ColorFpsCompressionCombo.SelectedIndex = 0;

            DepthFpsCompressionCombo.Items.Add("100%");
            DepthFpsCompressionCombo.Items.Add("75%");
            DepthFpsCompressionCombo.Items.Add("50%");
            DepthFpsCompressionCombo.Items.Add("25%");           
            DepthFpsCompressionCombo.SelectionChanged += DepthFpsCompressionCombo_SelectionChanged;
            DepthFpsCompressionCombo.SelectedIndex = 0;

            InfraredFpsCompressionCombo.Items.Add("100%");
            InfraredFpsCompressionCombo.Items.Add("75%");
            InfraredFpsCompressionCombo.Items.Add("50%");
            InfraredFpsCompressionCombo.Items.Add("25%");
            InfraredFpsCompressionCombo.SelectionChanged += InfraredFpsCompressionCombo_SelectionChanged;
            InfraredFpsCompressionCombo.SelectedIndex = 0;

            ColorCompressionCombo.Items.Add("Raw (1920x1080)");
            ColorCompressionCombo.Items.Add("Jpeg (1920x1080)");
            ColorCompressionCombo.Items.Add("Jpeg (1280x720)");
            ColorCompressionCombo.Items.Add("Jpeg (640x360)");
            ColorCompressionCombo.SelectedIndex = 0;

            DepthCompressionCombo.Items.Add("Raw (512×424)");
            DepthCompressionCombo.Items.Add("Jpeg (512×424)");
            DepthCompressionCombo.Items.Add("Png (512×424)");
            DepthCompressionCombo.SelectedIndex = 0;

            InfraredCompressionCombo.Items.Add("Raw (512×424)");
            InfraredCompressionCombo.Items.Add("Jpeg (512×424)");
            InfraredCompressionCombo.Items.Add("Png (512×424)");
            InfraredCompressionCombo.SelectedIndex = 0;

            DisplayCombo.Items.Add("Disabled");
            DisplayCombo.Items.Add("Color");
            DisplayCombo.Items.Add("Depth");
            DisplayCombo.Items.Add("Infrared");
           DisplayCombo.SelectionChanged += DisplayCombo_SelectionChanged;
            DisplayCombo.SelectedIndex = 1;

            _sensor = KinectSensor.GetDefault();

            _colorReader = _sensor.ColorFrameSource.OpenReader();
            _colorReader.FrameArrived += _colorReader_FrameArrived;

            _depthReader = _sensor.DepthFrameSource.OpenReader();
            _depthReader.FrameArrived += _depthReader_FrameArrived;

            _infraredReader = _sensor.InfraredFrameSource.OpenReader();
            _infraredReader.FrameArrived += _infraredReader_FrameArrived;

            _sensor.Open();
            OutputImage.Source = _colorBitmap.Bitmap;
        }

        private void _infraredReader_FrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            if (_displayType == FrameTypes.Infrared)
            {
                _infraredBitmap.Update(e.FrameReference);
                
            }
            if (_isStarted)
                infraredFps.Text = _recorder.DisplayInfraredFrames.ToString();
        }

        private void _depthReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            if (_displayType == FrameTypes.Depth)
            {
                _depthBitmap.Update(e.FrameReference);
                
            }
            if (_isStarted)
                depthFps.Text = _recorder.DisplayDepthFrames.ToString();
        }

        private void _colorReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            if (_displayType == FrameTypes.Color)
            {
                _colorBitmap.Update(e.FrameReference);
                
            }
            if (_isStarted)
                colorFps.Text = _recorder.DisplayColorFrames.ToString();
        }

        private void DisplayCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DisplayCombo.SelectedIndex == 0)
            {
                OutputImage.Source = null;
            }
            else if (DisplayCombo.SelectedIndex == 1)
            {
                _displayType = FrameTypes.Color;
                OutputImage.Source = _colorBitmap.Bitmap;
            }
            else if (DisplayCombo.SelectedIndex == 2)
            {
                _displayType = FrameTypes.Depth;
                OutputImage.Source = _depthBitmap.Bitmap;
            }
            else
            {
                _displayType = FrameTypes.Infrared;
                OutputImage.Source = _infraredBitmap.Bitmap;
            }
        }

        private void ColorFpsCompressionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (ColorFpsCompressionCombo.SelectedIndex)
            {
                case 0:
                    _colorDelay = (int) FramerateTypes.Hundred;
                    break;
                case 1:
                    _colorDelay = (int)FramerateTypes.SeventyFive;
                    break;
                case 2:
                    _colorDelay = (int)FramerateTypes.Fifty;
                    break;
                case 3:
                    _colorDelay = (int)FramerateTypes.TwentyFive;
                    break;
            }
        }
        private void DepthFpsCompressionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (DepthFpsCompressionCombo.SelectedIndex)
            {
                case 0:
                    _depthDelay = (int)FramerateTypes.Hundred;
                    break;
                case 1:
                    _depthDelay = (int)FramerateTypes.SeventyFive;
                    break;                
                case 2:
                    _depthDelay = (int)FramerateTypes.Fifty;
                    break;               
                case 3:
                    _depthDelay = (int)FramerateTypes.TwentyFive;
                    break;           
            }
        }
        private void InfraredFpsCompressionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (InfraredFpsCompressionCombo.SelectedIndex)
            {
                case 0:
                    _infraredDelay = (int)FramerateTypes.Hundred;
                    break;
                case 1:
                    _infraredDelay = (int)FramerateTypes.SeventyFive;
                    break;                
                case 2:
                    _infraredDelay = (int)FramerateTypes.Fifty;
                    break;              
                case 3:
                    _infraredDelay = (int)FramerateTypes.TwentyFive;
                    break;                
            }
        }

        async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (_recorder == null)
            {
                var dlg = new SaveFileDialog()
                {
                    FileName = DateTime.Now.ToString("hh-mm-ss"),
                };

                if (dlg.ShowDialog().GetValueOrDefault())
                {
                    
                    _recorder = new KinectRecorder(dlg.FileName, _sensor);
                    _recorder.EnableColorRecorder = ColorCheckBox.IsChecked.GetValueOrDefault();
                    _recorder.EnableDepthRecorder = DepthCheckBox.IsChecked.GetValueOrDefault();
                    _recorder.EnableInfraredRecorder = InfraredCheckBox.IsChecked.GetValueOrDefault();

                    int colorCompressionType = ColorCompressionCombo.SelectedIndex / 3;
                    int colorCompressionSize = ColorCompressionCombo.SelectedIndex % 3;

                    if (colorCompressionType == 1)
                        _recorder.ColorRecorderCodec = new JpegCodec();
                    if (colorCompressionSize == 1) // 1280 x 720
                    {
                        _recorder.ColorRecorderCodec.OutputWidth = 1280;
                        _recorder.ColorRecorderCodec.OutputHeight = 720;
                    }
                    else if (colorCompressionSize == 2) // 640 x 360
                    {
                        _recorder.ColorRecorderCodec.OutputWidth = 640;
                        _recorder.ColorRecorderCodec.OutputHeight = 360;
                    }

                    int depthCompressionType = DepthCompressionCombo.SelectedIndex;

                    if (depthCompressionType == 1)
                        _recorder.DepthRecorderCodec = new JpegCodec();
                    if (depthCompressionType == 2)
                        _recorder.DepthRecorderCodec = new PngCodec();

                    int infraredCompressionType = InfraredCompressionCombo.SelectedIndex;

                    if (infraredCompressionType == 1)
                        _recorder.InfraredRecorderCodec = new JpegCodec();
                    if (infraredCompressionType == 2)
                        _recorder.InfraredRecorderCodec = new PngCodec();

                    _recorder.ColorFramerate = _colorDelay;
                    _recorder.DepthFramerate = _depthDelay;
                    _recorder.InfraredFramerate = _infraredDelay;

                    _recorder.Start();
                    _isStarted = true;

                    RecordButton.Content = "Stop Recording";
                    ColorCheckBox.IsEnabled = false;
                    DepthCheckBox.IsEnabled = false;
                    InfraredCheckBox.IsEnabled = false;
                    ColorCompressionCombo.IsEnabled = false;
                    DepthCompressionCombo.IsEnabled = false;
                    InfraredCompressionCombo.IsEnabled = false;
                    ColorFpsCompressionCombo.IsEnabled = false;
                    DepthFpsCompressionCombo.IsEnabled = false;
                    InfraredFpsCompressionCombo.IsEnabled = false;
                }
            }
            else
            {
                RecordButton.IsEnabled = false;
                _isStarted = false;
                colorFps.Text = "--";
                await _recorder.StopAsync();
                _recorder = null;

                RecordButton.Content = "Record";
                RecordButton.IsEnabled = true;
                ColorCheckBox.IsEnabled = true;
                DepthCheckBox.IsEnabled = true;
                InfraredCheckBox.IsEnabled = true;
                ColorCompressionCombo.IsEnabled = true;
                DepthCompressionCombo.IsEnabled = true;
                InfraredCompressionCombo.IsEnabled = true;
                ColorFpsCompressionCombo.IsEnabled = true;
                DepthFpsCompressionCombo.IsEnabled = true;
                InfraredFpsCompressionCombo.IsEnabled = true;
            }
        }

    }
}

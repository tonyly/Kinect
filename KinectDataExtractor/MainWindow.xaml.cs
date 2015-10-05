using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using KinectDataManagement.DVR;
using Newtonsoft.Json;


namespace KinectDataManagement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectPlay _replay;
        bool _locationSetByHand = false;

        FrameTypes _displayType = FrameTypes.Color;

        ColorFrameBitmap _colorBitmap = null;
        DepthFrameBitmap _depthBitmap = null;
        InfraredFrameBitmap _infraredBitmap = null;


        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            OpenButton.Click += OpenButton_Click;
            PlayButton.Click += PlayButton_Click;
            OutputCombo.SelectionChanged += OutputCombo_SelectionChanged;
            LocationSlider.ValueChanged += LocationSlider_ValueChanged;
        }

        void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_replay != null)
            {
                if (_replay.IsStarted)
                    _replay.Stop();

                _replay.PropertyChanged -= _play_PropertyChanged;

                if (_replay.HasColorFrames)
                    _replay.ColorFrameArrived -= _replay_ColorFrameArrived;
                if (_replay.HasDepthFrames)
                    _replay.DepthFrameArrived -= _replay_DepthFrameArrived;
                if (_replay.HasInfraredFrames)
                    _replay.InfraredFrameArrived -= _replay_InfraredFrameArrived;
                _replay = null;
            }

            var dlg = new OpenFileDialog()
            {
                //DefaultExt = ".kdvr",
                //Filter = "KinectEx.DVR Files (*.kdvr)|*.kdvr"
            };

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                _colorBitmap = null; // reset to force recreation for new file
                OutputCombo.Items.Clear();

                Stream _metadataStream = File.Open(dlg.FileName, FileMode.Open, FileAccess.Read);

                BinaryReader _metadataReader = new BinaryReader(_metadataStream);

                var metadata = JsonConvert.DeserializeObject<FileMetadata>(_metadataReader.ReadString());
                //Version version = this.GetType().GetTypeInfo().Assembly.GetName().Version; // default to this
                //Version.TryParse(metadata.Version, out version);
                if (metadata.ColorCodecId == 0)
                    colorCodec.Text = "Raw";
                else if (metadata.ColorCodecId == 1)
                    colorCodec.Text = "Jpeg";

                if (metadata.DepthCodecId == 0)
                    depthCodec.Text = "Raw";
                else if (metadata.DepthCodecId == 1)
                    depthCodec.Text = "Jpeg";
                else if (metadata.DepthCodecId == 2)
                    depthCodec.Text = "Png";

                if (metadata.InfraredCodecId == 0)
                    infraredCodec.Text = "Raw";
                else if (metadata.InfraredCodecId == 1)
                    infraredCodec.Text = "Jpeg";
                else if (metadata.InfraredCodecId == 2)
                    infraredCodec.Text = "Png";



                colorFps.Text = metadata.FpsColor.ToString()+"%";
                depthFps.Text = metadata.FpsDepth.ToString()+"%";
                infraredFps.Text = metadata.FpsInfrared.ToString()+"%";

                _replay = new KinectPlay(metadata.ColorLocation,metadata.DepthLocation,metadata.InfraredLocation,metadata);
                _replay.PropertyChanged += _play_PropertyChanged;
                
                if (_replay.HasColorFrames)
                {
                    _replay.ColorFrameArrived += _replay_ColorFrameArrived;
                    OutputCombo.Items.Add("Color");
                }
                if (_replay.HasDepthFrames)
                {
                    _replay.DepthFrameArrived += _replay_DepthFrameArrived;
                    OutputCombo.Items.Add("Depth");
                }
                if (_replay.HasInfraredFrames)
                {
                    _replay.InfraredFrameArrived += _replay_InfraredFrameArrived;
                    OutputCombo.Items.Add("Infrared");
                }

                if (OutputCombo.Items.Count > 0)
                {
                    OutputCombo.SelectedIndex = 0;
                }
                else
                {
                    PlayButton.IsEnabled = false;
                }
            }
        }

        void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_replay == null)
                return;

            if (!_replay.IsStarted)
            {
                _replay.Start();
                PlayButton.Content = "Pause";
            }
            else
            {
                _replay.Stop();
                PlayButton.Content = "Play";
            }
        }

        void OutputCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (OutputCombo.SelectedValue == null)
                return;

           if (OutputCombo.SelectedValue.ToString() == "Color")
            {
                _displayType = FrameTypes.Color;
                if (_colorBitmap != null)
                    OutputImage.Source = _colorBitmap.Bitmap;
            }
            else if (OutputCombo.SelectedValue.ToString() == "Depth")
            {
                _displayType = FrameTypes.Depth;
                if (_depthBitmap != null)
                    OutputImage.Source = _depthBitmap.Bitmap;
            }
            else
            {
                _displayType = FrameTypes.Infrared;
                if (_infraredBitmap != null)
                    OutputImage.Source = _infraredBitmap.Bitmap;
            }
        }

        void LocationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_locationSetByHand)
            {
                if (_replay != null)
                    _replay.ScrubTo(TimeSpan.FromMilliseconds((LocationSlider.Value / 100.0) * _replay.Duration.TotalMilliseconds));
            }
            else
            {
                _locationSetByHand = true;
            }
        }

        void _play_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == KinectPlay.IsFinishedPropertyName)
            {
                PlayButton.Content = "Play";
            }
            else if (e.PropertyName == KinectPlay.LocationPropertyName)
            {
                _locationSetByHand = false;
                LocationSlider.Value = 100 - (100 * ((_replay.Duration.TotalMilliseconds - _replay.Location.TotalMilliseconds) / _replay.Duration.TotalMilliseconds));
            }
        }

        void _replay_ColorFrameArrived(object sender, PlayFrameArrivedEventArgs<PlayColorFrame> e)
        {
            if (_displayType == FrameTypes.Color)
            {
                if (_colorBitmap == null)
                {
                    _colorBitmap = new ColorFrameBitmap(e.Frame);
                    OutputImage.Source = _colorBitmap.Bitmap;
                }
                _colorBitmap.Update(e.Frame);
            }
        }

        void _replay_DepthFrameArrived(object sender, PlayFrameArrivedEventArgs<PlayDepthFrame> e)
        {
            if (_displayType == FrameTypes.Depth)
            {
                if (_depthBitmap == null)
                {
                    _depthBitmap = new DepthFrameBitmap(e.Frame.Width, e.Frame.Height);
                    OutputImage.Source = _depthBitmap.Bitmap;
                }
                _depthBitmap.Update(e.Frame);
            }
        }

        void _replay_InfraredFrameArrived(object sender, PlayFrameArrivedEventArgs<PlayInfraredFrame> e)
        {
            if (_displayType == FrameTypes.Infrared)
            {
                if (_infraredBitmap == null)
                {
                    _infraredBitmap = new InfraredFrameBitmap(e.Frame.Width, e.Frame.Height);
                    OutputImage.Source = _infraredBitmap.Bitmap;
                }
                _infraredBitmap.Update(e.Frame);
            }
        }
    }
}

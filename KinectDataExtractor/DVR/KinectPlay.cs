using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace KinectDataManagement.DVR
{
    internal class KinectPlay : IDisposable, INotifyPropertyChanged
    {
        #region Attributes

        private BinaryReader _metadataReader;
        private BinaryReader _colorReader;
        private BinaryReader _depthReader;
        private BinaryReader _infraredReader;
        private Stream _metadataStream;
        private Stream _colorStream;
        private Stream _depthStream;
        private Stream _infraredStream;


        private readonly DispatcherTimer _timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromTicks(FrameTime.Ticks / 2)
        };
        private readonly Stopwatch _actualElapsedTimeStopwatch = new Stopwatch();

        // Replay
        private PlayColorSystem _colorPlay;
        private PlayDepthSystem _depthPlay;
        private PlayInfraredSystem _infraredPlay;

        private List<PlaySystem> _activePlaySystems = new List<PlaySystem>();

        private TimeSpan _minTimespan = TimeSpan.MaxValue;
        private TimeSpan _maxTimespan = TimeSpan.MinValue;

        // Property Backers
        private bool _isStarted = false;
        private TimeSpan _location = TimeSpan.Zero;

        #endregion

        #region Public Static Members
        public static TimeSpan FrameTime = TimeSpan.FromTicks(333333);
        public static readonly string IsStartedPropertyName = "IsStarted";
        public static readonly string IsFinishedPropertyName = "IsFinished";
        public static readonly string LocationPropertyName = "Location";
        #endregion

        #region Events

        public event EventHandler<PlayFrameArrivedEventArgs<PlayColorFrame>> ColorFrameArrived;
        public event EventHandler<PlayFrameArrivedEventArgs<PlayDepthFrame>> DepthFrameArrived;
        public event EventHandler<PlayFrameArrivedEventArgs<PlayInfraredFrame>> InfraredFrameArrived;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        public bool IsStarted
        {
            get { return _isStarted; }
            internal set
            {
                _isStarted = value;
                NotifyPropertyChanged(IsStartedPropertyName);
            }
        }
        public bool IsFinished
        {
            get
            {
                foreach (var replaySystem in _activePlaySystems)
                    if (!replaySystem.IsFinished)
                        return false;

                return true;
            }
        }
        public TimeSpan Location
        {
            get
            {
                return _location;
            }
            private set
            {
                _location = value;
                NotifyPropertyChanged(LocationPropertyName);
            }
        }

        public TimeSpan Duration { get; private set; }

        public bool HasColorFrames
        {
            get { return _colorPlay != null; }
        }

        public bool HasDepthFrames
        {
            get { return _depthPlay != null; }
        }
        public bool HasInfraredFrames
        {
            get { return _infraredPlay != null; }
        }
        #endregion

        #region Constructor / Destructor

        public KinectPlay(string colorLocation, string depthLocation, string infraredLocation, FileMetadata metadata)
        {
            
            _timer.Tick += _timer_Tick;

            this._colorStream = File.Open(colorLocation, FileMode.Open, FileAccess.Read);
            this._depthStream = File.Open(depthLocation, FileMode.Open, FileAccess.Read);
            this._infraredStream = File.Open(infraredLocation, FileMode.Open, FileAccess.Read);

            _colorReader = new BinaryReader(_colorStream);
            _depthReader = new BinaryReader(_depthStream);
            _infraredReader = new BinaryReader(_infraredStream);

            while (_colorReader.BaseStream.Position != _colorReader.BaseStream.Length)
            {

                FrameTypes type = (FrameTypes)_colorReader.ReadInt32();
                if (_colorPlay == null)
                {
                    ICodec codec = new RawCodec();
                    if (metadata.ColorCodecId == Codecs.JpegColor.ColorCodecId)
                        codec = new JpegCodec();

                    _colorPlay = new PlayColorSystem(codec);
                    _activePlaySystems.Add(_colorPlay);
                    _colorPlay.PropertyChanged += play_PropertyChanged;
                    _colorPlay.FrameArrived += colorPlay_FrameArrived;
                }
                _colorPlay.AddFrame(_colorReader);
            }
            while (_depthReader.BaseStream.Position != _depthReader.BaseStream.Length)
            {
                FrameTypes type = (FrameTypes)_depthReader.ReadInt32();
                if (_depthPlay == null)
                {
                    ICodec codec = new RawCodec();
                    if (metadata.DepthCodecId == Codecs.JpegDepth.DepthCodecId)
                        codec = new JpegCodec();
                    else if (metadata.DepthCodecId == Codecs.PngDepth.DepthCodecId)
                        codec = new PngCodec();

                    _depthPlay = new PlayDepthSystem(codec);
                    _activePlaySystems.Add(_depthPlay);
                    _depthPlay.PropertyChanged += play_PropertyChanged;
                    _depthPlay.FrameArrived += depthPlay_FrameArrived;
                }
                _depthPlay.AddFrame(_depthReader);
            }
            while (_infraredReader.BaseStream.Position != _infraredReader.BaseStream.Length)
            {
                FrameTypes type = (FrameTypes)_infraredReader.ReadInt32();
                if (_infraredPlay == null)
                {
                    ICodec codec = new RawCodec();
                    if (metadata.InfraredCodecId == Codecs.JpegInfrared.InfraredCodecId)
                        codec = new JpegCodec();
                    else if (metadata.InfraredCodecId == Codecs.PngInfrared.InfraredCodecId)
                        codec = new PngCodec();

                    _infraredPlay = new PlayInfraredSystem(codec);
                    _activePlaySystems.Add(_infraredPlay);
                    _infraredPlay.PropertyChanged += play_PropertyChanged;
                    _infraredPlay.FrameArrived += infraredPlay_FrameArrived;
                }
                _infraredPlay.AddFrame(_infraredReader);
            }
                  
                

            foreach (var playSystem in _activePlaySystems)
            {
                if (playSystem.Frames.Count > 0)
                {
                    playSystem.Frames.Sort();

                    for (var i = 0; i < playSystem.Frames.Count; i++)
                    {
                        playSystem.FrameTimeToIndex[playSystem.Frames[i].RelativeTime] = i;
                    }

                    var first = playSystem.Frames.First().RelativeTime;
                    var last = playSystem.Frames.Last().RelativeTime;
                    if (first < _minTimespan)
                        _minTimespan = first;
                    if (last > _maxTimespan)
                        _maxTimespan = last;
                }
            }

            bool hasFrames = false;

            foreach (var playSystem in _activePlaySystems)
            {
                if (playSystem.Frames.Count > 0)
                {
                    playSystem.StartingOffset = _minTimespan;
                    hasFrames = true;
                }
            }

            if (hasFrames)
            {
                this.Duration = _maxTimespan - _minTimespan;
            }
            else
            {
                this.Duration = TimeSpan.Zero;
            }
        }
        ~KinectPlay()
        {
            this.Dispose(false);
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();

                _colorPlay = null;
                _depthPlay = null;
                _infraredPlay = null;

                if (_colorReader != null)
                {
                    _colorReader.Dispose();
                    _colorReader = null;
                }

                if (_colorStream != null)
                {
                    _colorStream.Dispose();
                    _colorStream = null;
                }
                if (_depthReader != null)
                {
                    _depthReader.Dispose();
                    _depthReader = null;
                }

                if (_depthStream != null)
                {
                    _depthStream.Dispose();
                    _depthStream = null;
                }
            
                if (_infraredReader != null)
                {
                    _infraredReader.Dispose();
                    _infraredReader = null;
                }

                 if (_infraredStream != null)
            {
                    _infraredStream.Dispose();
                    _infraredStream = null;
            }
        }
    }
        
        #endregion

        #region Public Methods
        public void Start()
        {
            _timer.Start();
            IsStarted = true;
        }
        public void Stop()
        {
            _timer.Stop();
            _actualElapsedTimeStopwatch.Reset();
            IsStarted = false;
        }
        public void ScrubTo(TimeSpan newLocation)
        {
            if (newLocation > this.Duration)
                newLocation = this.Duration;

            this.Location = newLocation;

            foreach (var replaySystem in _activePlaySystems)
            {
                replaySystem.CurrentRelativeTime = _minTimespan + newLocation;
                replaySystem.PushCurrentFrame();
            }
        }
        public async Task ExportColorFramesAsync(string exportDir)
        {
            if (!this.HasColorFrames || _colorPlay == null)
            {
                throw new InvalidOperationException("KDVR file has no color frames.");
            }

            int frameCounter = 0;
            var jpegCodec = Codecs.JpegColor;
            var jpegCodecId = jpegCodec.ColorCodecId;
            var lastRelativeTime = TimeSpan.MaxValue;
            foreach (var frame in _colorPlay.Frames)
            {
                PlayColorFrame rcf = frame as PlayColorFrame;

                var elapsed = rcf.RelativeTime - lastRelativeTime;
                lastRelativeTime = rcf.RelativeTime;
                var numFrames = 1;
                var mills = (int)Math.Ceiling(elapsed.TotalMilliseconds);
                if (mills > 60)
                {
                    numFrames = mills % 33;
                }

                for (int i = 0; i < numFrames; i++)
                {
                    var fileName = string.Format("\\{0:000000}.jpeg", frameCounter++);

                    using (var jpegStream = new FileStream(exportDir + fileName, FileMode.Create, FileAccess.Write))
                    {
                        using (var jpegWriter = new BinaryWriter(jpegStream))
                        {
                            var bytes = rcf.GetRawFrameData();

                            if (rcf.Codec.ColorCodecId == jpegCodecId)
                            {
                                jpegWriter.Write(bytes);
                            }
                            else
                            {
                                await jpegCodec.EncodeColorAsync(bytes, jpegWriter);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Support Code

        void _timer_Tick(object sender, EventArgs e)
        {
            this.Location += _actualElapsedTimeStopwatch.Elapsed;
            _actualElapsedTimeStopwatch.Restart();

            foreach (var playSystem in _activePlaySystems)
                playSystem.CurrentRelativeTime = playSystem.StartingOffset + this.Location;
        }

        private void play_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == PlaySystem.IsFinishedPropertyName)
            {
                if (this.IsFinished) // checks all replay systems
                {
                    foreach (var playSystem in _activePlaySystems)
                        playSystem.CurrentRelativeTime = playSystem.StartingOffset;

                    Stop();
                    this.Location = TimeSpan.Zero;
                    NotifyPropertyChanged(IsFinishedPropertyName);
                }
            }
        }

        private void colorPlay_FrameArrived(PlayColorFrame frame)
        {
            if (ColorFrameArrived != null)
                ColorFrameArrived(this, new PlayFrameArrivedEventArgs<PlayColorFrame> { Frame = frame });
        }
        private void depthPlay_FrameArrived(PlayDepthFrame frame)
        {
            if (DepthFrameArrived != null)
                DepthFrameArrived(this, new PlayFrameArrivedEventArgs<PlayDepthFrame> { Frame = frame });
        }
        private void infraredPlay_FrameArrived(PlayInfraredFrame frame)
        {
            if (InfraredFrameArrived != null)
                InfraredFrameArrived(this, new PlayFrameArrivedEventArgs<PlayInfraredFrame> { Frame = frame });
        }
        void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
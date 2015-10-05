using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace KinectDataManagement.DVR
{
    internal abstract class PlaySystem : INotifyPropertyChanged
    {
        private int _mostRecentCurrentFrame = -1;

        private TimeSpan _currentRelativeTime;
        public static readonly string IsFinishedPropertyName = "IsFinished";
        private bool _isFinished = false;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the list of frames.
        /// </summary>
        public List<PlayFrame> Frames { get; set; }

        /// <summary>
        /// Gets or sets the dictionary relating FrameTime to the frame's index
        /// in the Frames list.
        /// </summary>
        public Dictionary<TimeSpan, int> FrameTimeToIndex { get; set; }

        /// <summary>
        /// Gets or sets the starting offset.
        /// </summary>
        public TimeSpan StartingOffset { get; set; }

        public TimeSpan CurrentRelativeTime
        {
            get { return _currentRelativeTime; }
            set
            {
                if (value == _currentRelativeTime)
                    return;

                _currentRelativeTime = value;

                var frame = this.CurrentFrame;
                if (frame != _mostRecentCurrentFrame)
                {
                    _mostRecentCurrentFrame = frame;
                    this.PushCurrentFrame();
                }
                if (frame == this.FrameCount - 1)
                    IsFinished = true;
                else
                    IsFinished = false;
            }
        }

        public int CurrentFrame
        {
            get
            {
                var key = this.FrameTimeToIndex.Keys.LastOrDefault(ts => ts <= _currentRelativeTime);
                if (key == TimeSpan.Zero)
                    return 0;
                else
                    return FrameTimeToIndex[key];
            }
        }

        public int FrameCount
        {
            get
            {
                return this.Frames.Count;
            }
        }
        public bool IsFinished
        {
            get { return _isFinished; }
            protected set
            {
                if (value == _isFinished)
                    return;

                _isFinished = value;
                NotifyPropertyChanged(IsFinishedPropertyName);
            }
        }
        public PlaySystem()
        {
            this.Frames = new List<PlayFrame>();
            this.FrameTimeToIndex = new Dictionary<TimeSpan, int>();
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        }
        public abstract void PushCurrentFrame();
    }
}
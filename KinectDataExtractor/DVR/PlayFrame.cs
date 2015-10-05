using System;

namespace KinectDataManagement.DVR
{
    public abstract class PlayFrame : IComparable
    {
        internal readonly static String EndOfFrameMarker = "[EOF]";

        internal long FrameSize;

        /// <summary>
        /// The type of frame represented by this <c>ReplayFrame</c>.
        /// </summary>
        public FrameTypes FrameType { get; set; }

        /// <summary>
        /// The unique relative time at which this frame was captured.
        /// </summary>
        public TimeSpan RelativeTime { get; set; }

        /// <summary>
        /// Compare this frame to another for the purposes of sorting.
        /// </summary>
        public int CompareTo(object obj)
        {
            if (obj is PlayFrame)
                return this.RelativeTime.CompareTo(((PlayFrame)obj).RelativeTime);
            return 0;
        }
    }
}
using System;

namespace KinectDataManagement.DVR
{
    public abstract class RecordFrame : IComparable
    {
        internal readonly static String EndOfFrameMarker = "[EOF]";

        private long FrameSize;

        public FrameTypes FrameType { get; set; }
        public TimeSpan RelativeTime { get; set; }
        public int CompareTo(object obj)
        {
            if (obj is RecordFrame)
                return this.RelativeTime.CompareTo(((RecordFrame)obj).RelativeTime);
            return 0;
        }
    }
}

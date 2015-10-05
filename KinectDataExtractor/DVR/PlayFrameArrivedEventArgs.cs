using System;

namespace KinectDataManagement.DVR
{
    public class PlayFrameArrivedEventArgs<T> : EventArgs where T : PlayFrame
    {
        public T Frame { get; internal set; }
    }
}
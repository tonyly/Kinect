using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace KinectDataManagement.DVR
{
    public class RecordDepthFrame : RecordFrame
    {
        private ushort[] _frameData = null;

        private ICodec Codec;
        public uint DepthMinReliableDistance { get; set; }
        public uint DepthMaxReliableDistance { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public uint BytesPerPixel { get; private set; }
        public ushort[] FrameData
        {
            get { return _frameData; }
            set { _frameData = value; }
        }

        public RecordDepthFrame(DepthFrame frame)
        {
            this.Codec = Codecs.RawColor;

            this.FrameType = FrameTypes.Depth;
            this.RelativeTime = frame.RelativeTime;

            this.DepthMinReliableDistance = frame.DepthMinReliableDistance;
            this.DepthMaxReliableDistance = frame.DepthMaxReliableDistance;

            this.Width = frame.FrameDescription.Width;
            this.Height = frame.FrameDescription.Height;
            this.BytesPerPixel = frame.FrameDescription.BytesPerPixel;

            _frameData = new ushort[this.Width * this.Height];

            frame.CopyFrameDataToArray(_frameData);
        }

        ~RecordDepthFrame()
        {
            this.Dispose (false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_frameData != null)
                {
                    _frameData = null;
                }
                
            }
        }

    }
}

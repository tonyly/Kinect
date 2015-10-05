using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace KinectDataManagement.DVR
{
    public class RecordInfraredFrame : RecordFrame
    {
        private ushort[] _frameData = null;
        private ICodec Codec;

        public int Width { get; set; }
        public int Height { get; set; }
        public uint BytesPerPixel { get; private set; }
        public ushort[] FrameData
        {
            get { return _frameData; }
            set { _frameData = value; }
        }

        public RecordInfraredFrame(InfraredFrame frame)
        {
            this.Codec = Codecs.RawColor;

            this.FrameType = FrameTypes.Infrared;
            this.RelativeTime = frame.RelativeTime;

            this.Width = frame.FrameDescription.Width;
            this.Height = frame.FrameDescription.Height;
            this.BytesPerPixel = frame.FrameDescription.BytesPerPixel;

            _frameData = new ushort[this.Width * this.Height];

            frame.CopyFrameDataToArray(_frameData);
        }
        ~RecordInfraredFrame()
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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Media.Imaging;

using Microsoft.Kinect;

namespace KinectDataManagement.DVR
{
    public class RecordColorFrame : RecordFrame, IDisposable
    {
        private byte[] _frameData = null;

        private ICodec Codec;

        internal int FrameDataSize { get; set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public byte[] FrameData
        {
            get { return _frameData; }
            set { _frameData = value; }
        }

        public RecordColorFrame(ColorFrame frame)
        {
            this.Codec = Codecs.RawColor;

            this.FrameType = FrameTypes.Color;
            this.RelativeTime = frame.RelativeTime;

            this.Width = frame.FrameDescription.Width;
            this.Height = frame.FrameDescription.Height;

            this.FrameDataSize = this.Width * this.Height * 4;
            this._frameData = new Byte[this.FrameDataSize];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(_frameData);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(_frameData, ColorImageFormat.Bgra);
            }
        }
        ~RecordColorFrame()
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

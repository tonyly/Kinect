using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Kinect;

namespace KinectDataManagement.DVR
{
    public class ColorFrameBitmap : IDisposable
    {
        private WriteableBitmap _bitmap = null;
        private byte[] _bytes = null;
        private Int32Rect _dirtyRect;

        public WriteableBitmap Bitmap
        {
            get { return _bitmap; }
        }

        public ColorFrameBitmap()
        {
            var sensor = KinectSensor.GetDefault();
            int width = sensor.ColorFrameSource.FrameDescription.Width;
            int height = sensor.ColorFrameSource.FrameDescription.Height;
            _bitmap = BitmapFactory.New(width, height);
            _bytes = new byte[width * height * 4];
            _dirtyRect = new Int32Rect(0, 0, width, height);
        }

        ~ColorFrameBitmap()
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
            _bitmap = null;
        }


        public void Update(ColorFrameReference frameReference)
        {
            using (var frame = frameReference.AcquireFrame())
            {
                Update(frame);
            }
        }
        public void Update(ColorFrame frame)
        {
            if (frame != null)
            {
                _bitmap.Lock();
                frame.CopyConvertedFrameDataToIntPtr(_bitmap.BackBuffer, (uint)_bytes.Length, ColorImageFormat.Bgra);
                _bitmap.AddDirtyRect(_dirtyRect);
                _bitmap.Unlock();
            }
        }
    }
}
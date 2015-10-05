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

        public ColorFrameBitmap(PlayColorFrame frame)
        {
            // force population of PixelFormat
            var data = frame.GetFrameDataAsync().Result;
            _bitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, frame.Codec.PixelFormat, null);
            _bytes = new byte[_bitmap.PixelWidth * _bitmap.PixelHeight * (_bitmap.Format.BitsPerPixel / 8)];
            _dirtyRect = new Int32Rect(0, 0, frame.Width, frame.Height);
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
        
       
        public void Update(PlayColorFrame frame)
        {
            if (frame != null)
            {
                frame.GetFrameDataAsync().ContinueWith(async (pixels) =>
                {
                    await _bitmap.Dispatcher.InvokeAsync(() => {
                        _bitmap.FromByteArray(pixels.Result);
                    });
                });
            }
        }
        public async void UpdateAsync(byte[] bytes)
        {
            await _bitmap.Dispatcher.InvokeAsync(() => {
                _bitmap.FromByteArray(bytes);
            });
        }
    }
}
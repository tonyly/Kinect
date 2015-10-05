using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Kinect;

namespace KinectDataManagement.DVR
{
    public class DepthFrameBitmap : IDisposable
    {
        private WriteableBitmap _bitmap = null;
        private byte[] _bytes = null;
        private Int32Rect _dirtyRect;

        public WriteableBitmap Bitmap
        {
            get { return _bitmap; }
        }

        public DepthFrameBitmap()
        {
            var sensor = KinectSensor.GetDefault();
            Init(sensor.DepthFrameSource.FrameDescription.Width, sensor.DepthFrameSource.FrameDescription.Height);
        }
        public DepthFrameBitmap(int width, int height)
        {
            Init(width, height);
        }

        private void Init(int width, int height)
        {
          
            _bitmap = BitmapFactory.New(width, height);
            _bytes = new byte[width * height * 4];
            _dirtyRect = new Int32Rect(0, 0, width, height);
        }
        ~DepthFrameBitmap()
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
            _bytes = null;
            _bitmap = null;
        }
        public void Update(PlayDepthFrame frame)
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
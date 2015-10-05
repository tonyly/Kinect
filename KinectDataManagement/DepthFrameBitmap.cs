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
        private ushort[] _data = null;
        private byte[] _bytes = null;
        private Int32Rect _dirtyRect;
        private int _stride = 0;

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
            _data = new ushort[width * height];
            _bytes = new byte[width * height * 4];
            _dirtyRect = new Int32Rect(0, 0, width, height);
            _stride = width * 4;
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
            _data = null;
            _bytes = null;
            _bitmap = null;
        }

        public async void Update(DepthFrameReference frameReference)
        {
            bool processed = false;
            ushort minDepth = 0;
            ushort maxDepth = 0;
            using (var frame = frameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    frame.CopyFrameDataToArray(_data);
                    minDepth = frame.DepthMinReliableDistance;
                    maxDepth = frame.DepthMaxReliableDistance;
                    processed = true;
                }
            }

            if (processed)
            {
                await UpdateAsync(_data, minDepth, maxDepth);
            }
        }
        public async void Update(DepthFrame frame)
        {
            if (frame != null)
            {
                frame.CopyFrameDataToArray(_data);
                await UpdateAsync(_data, frame.DepthMinReliableDistance, frame.DepthMaxReliableDistance);
            }
        }
        public async void Update(ushort[] data, ushort minDepth, ushort maxDepth)
        {
            await UpdateAsync(data, minDepth, maxDepth);
        }
        public async Task UpdateAsync(ushort[] data, ushort minDepth, ushort maxDepth)
        {
            await Task.Run(async () =>
            {
                int colorPixelIndex = 0;
                for (int i = 0; i < data.Length; ++i)
                {
                    ushort depth = data[i];
                    byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);
                    _bytes[colorPixelIndex++] = intensity; // B
                    _bytes[colorPixelIndex++] = intensity; // G
                    _bytes[colorPixelIndex++] = intensity; // R
                    _bytes[colorPixelIndex++] = 255;       // A
                }
                await _bitmap.Dispatcher.InvokeAsync(() =>
                {
                    _bitmap.FromByteArray(_bytes);
                });
            });
        }
    }
}
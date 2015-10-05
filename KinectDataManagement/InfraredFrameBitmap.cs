using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Kinect;
using System.Linq;

namespace KinectDataManagement.DVR
{
    internal class InfraredFrameBitmap
    {
        private WriteableBitmap _bitmap = null;
        private ushort[] _data = null;
        private byte[] _bytes = null;
        private int _stride = 0;
        private Int32Rect _dirtyRect;

        private float _infraredOutputValueMinimum = 0.01f;
        private float _infraredOutputValueMaximum = 1.0f;
        private int _resetAvgSdCounter = 15;
        private float _avg = 0.08f;
        private float _sd = 3.0f;
        private float _max = (float)ushort.MaxValue;

        public WriteableBitmap Bitmap
        {
            get { return _bitmap; }
        }

        public InfraredFrameBitmap()
        {
            var sensor = KinectSensor.GetDefault();
            Init(sensor.DepthFrameSource.FrameDescription.Width, sensor.DepthFrameSource.FrameDescription.Height);
        }
        public InfraredFrameBitmap(int width, int height)
        {
            Init(width, height);
        }

        private void Init(int width, int height)
        {
            _bitmap = BitmapFactory.New(width, height);
            _data = new ushort[width * height];
            _bytes = new byte[width * height * 4];
            _stride = width * 4;
            _dirtyRect = new Int32Rect(0, 0, width, height);
        }
        ~InfraredFrameBitmap()
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

        public async void Update(InfraredFrameReference frameReference)
        {
            bool processed = false;
            using (var frame = frameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    frame.CopyFrameDataToArray(_data);
                    processed = true;
                }
            }

            if (processed)
            {
                await UpdateAsync(_data);
            }
        }
        public async void Update(InfraredFrame frame)
        {
            if (frame != null)
            {
                frame.CopyFrameDataToArray(_data);
                await UpdateAsync(_data);
            }
        }
        public async Task UpdateAsync(ushort[] data)
        {
            await Task.Run(async () =>
            {
                int colorPixelIndex = 0;
                int dataLen = data.Length;
                float avgSd = _avg * _sd;

                if (_resetAvgSdCounter++ == 15)
                {
                    _avg = data.Average(d => (d / _max));
                    _resetAvgSdCounter = 0;
                }
                for (int i = 0; i < dataLen; ++i)
                {
                    float intensityRatio = (float)data[i] / _max;
                    intensityRatio /= avgSd;
                    intensityRatio = Math.Min(_infraredOutputValueMaximum, intensityRatio);
                    intensityRatio = Math.Max(_infraredOutputValueMinimum, intensityRatio);
                    byte intensity = (byte)(intensityRatio * 255.0f);
                    _bytes[colorPixelIndex++] = intensity;  // B
                    _bytes[colorPixelIndex++] = intensity;  // G
                    _bytes[colorPixelIndex++] = intensity;  // R
                    _bytes[colorPixelIndex++] = 255;        // A
                }
                await _bitmap.Dispatcher.InvokeAsync(() =>
                {
                    _bitmap.FromByteArray(_bytes);
                });

            });
        }

    }
}
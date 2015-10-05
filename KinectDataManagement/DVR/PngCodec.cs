using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectDataManagement.DVR
{
    public class PngCodec : ICodec
    {
        private int _outputWidth = int.MinValue;
        private int _outputHeight = int.MinValue;

        public int ColorCodecId { get { return 2; } }
        public int DepthCodecId { get { return 2; } }
        public int InfraredCodecId { get { return 2; } }

        public int Width { get; set; }
        public int Height { get; set; }
        public int OutputWidth
        {
            get { return _outputWidth == int.MinValue ? Width : _outputWidth; }
            set { _outputWidth = value; }
        }
        public int OutputHeight
        {
            get { return _outputHeight == int.MinValue ? Height : _outputHeight; }
            set { _outputHeight = value; }
        }

        public PixelFormat PixelFormat { get; set; }


        public async Task EncodeColorAsync(byte[] colorData, BinaryWriter writer)
        {
            await Task.Run(() =>
            {
                var format = PixelFormats.Bgra32;
                int stride = (int)this.Width * format.BitsPerPixel / 8;
                var bmp = BitmapSource.Create(
                    this.Width,
                    this.Height,
                    96.0,
                    96.0,
                    format,
                    null,
                    colorData,
                    stride);
                BitmapFrame frame;
                if (this.Width != this.OutputWidth || this.Height != this.OutputHeight)
                {
                    var transform = new ScaleTransform((double)this.OutputHeight / this.Height, (double)this.OutputHeight / this.Height);
                    var scaledbmp = new TransformedBitmap(bmp, transform);
                    frame = BitmapFrame.Create(scaledbmp);
                }
                else
                {
                    frame = BitmapFrame.Create(bmp);
                }

                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(frame);
                using (var pngStream = new MemoryStream())
                {
                    encoder.Save(pngStream);

                    if (writer.BaseStream == null || writer.BaseStream.CanWrite == false)
                        return;

                    // Header
                    writer.Write(this.OutputWidth);
                    writer.Write(this.OutputHeight);
                    writer.Write((int)pngStream.Length);

                    // Data
                    pngStream.Position = 0;
                    pngStream.CopyTo(writer.BaseStream);
                }
            });
        }

        public async Task EncodeDepthAsync(byte[] depthData, BinaryWriter writer)
        {
            await Task.Run(() =>
            {
                var format = PixelFormats.Bgra32;
                int stride = (int)this.Width * format.BitsPerPixel / 8;
                var bmp = BitmapSource.Create(
                    this.Width,
                    this.Height,
                    96.0,
                    96.0,
                    format,
                    null,
                    depthData,
                    stride);
                BitmapFrame frame;

                frame = BitmapFrame.Create(bmp);

                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(frame);
                using (var pngStream = new MemoryStream())
                {
                    encoder.Save(pngStream);

                    if (writer.BaseStream == null || writer.BaseStream.CanWrite == false)
                        return;

                    // Header
                    writer.Write(this.OutputWidth);
                    writer.Write(this.OutputHeight);
                    writer.Write((int)pngStream.Length);

                    // Data
                    pngStream.Position = 0;
                    pngStream.CopyTo(writer.BaseStream);
                }
            });
        }

        public async Task EncodeInfraredAsync(byte[] infraredData, BinaryWriter writer)
        {
            await Task.Run(() =>
            {
                var format = PixelFormats.Bgra32;
                int stride = (int)this.Width * format.BitsPerPixel / 8;
                var bmp = BitmapSource.Create(
                    this.Width,
                    this.Height,
                    96.0,
                    96.0,
                    format,
                    null,
                    infraredData,
                    stride);
                BitmapFrame frame;

                frame = BitmapFrame.Create(bmp);

                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(frame);
                using (var pngStream = new MemoryStream())
                {
                    encoder.Save(pngStream);

                    if (writer.BaseStream == null || writer.BaseStream.CanWrite == false)
                        return;

                    // Header
                    writer.Write(this.OutputWidth);
                    writer.Write(this.OutputHeight);
                    writer.Write((int)pngStream.Length);

                    // Data
                    pngStream.Position = 0;
                    pngStream.CopyTo(writer.BaseStream);
                }
            });
        }
    }
}

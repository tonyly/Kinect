using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectDataManagement.DVR
{
    public class JpegCodec : ICodec
    {
        private int _outputWidth = int.MinValue;
        private int _outputHeight = int.MinValue;
        private int _jpegQuality = 70;

        public int ColorCodecId { get { return 1; } }
        public int DepthCodecId { get { return 1; } }
        public int InfraredCodecId { get { return 1; } }

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

        public int JpegQuality
        {
            get { return _jpegQuality; }
            set { _jpegQuality = value; }
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

                var encoder = new JpegBitmapEncoder()
                {
                    QualityLevel = this.JpegQuality
                };
                encoder.Frames.Add(frame);
                using (var jpegStream = new MemoryStream())
                {
                    encoder.Save(jpegStream);

                    if (writer.BaseStream == null || writer.BaseStream.CanWrite == false)
                        return;

                    // Header
                    writer.Write(this.OutputWidth);
                    writer.Write(this.OutputHeight);
                    writer.Write((int)jpegStream.Length);

                    // Data
                    jpegStream.Position = 0;
                    jpegStream.CopyTo(writer.BaseStream);
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

                var encoder = new JpegBitmapEncoder()
                {
                    QualityLevel = this.JpegQuality
                };
                encoder.Frames.Add(frame);
                using (var jpegStream = new MemoryStream())
                {
                    encoder.Save(jpegStream);

                    if (writer.BaseStream == null || writer.BaseStream.CanWrite == false)
                        return;

                    // Header
                    writer.Write(this.OutputWidth);
                    writer.Write(this.OutputHeight);
                    writer.Write((int)jpegStream.Length);

                    // Data
                    jpegStream.Position = 0;
                    jpegStream.CopyTo(writer.BaseStream);
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

                var encoder = new JpegBitmapEncoder()
                {
                    QualityLevel = this.JpegQuality
                };
                encoder.Frames.Add(frame);
                using (var jpegStream = new MemoryStream())
                {
                    encoder.Save(jpegStream);

                    if (writer.BaseStream == null || writer.BaseStream.CanWrite == false)
                        return;

                    // Header
                    writer.Write(this.OutputWidth);
                    writer.Write(this.OutputHeight);
                    writer.Write((int)jpegStream.Length);

                    // Data
                    jpegStream.Position = 0;
                    jpegStream.CopyTo(writer.BaseStream);
                }
            });
        }
        public void ReadColorHeader(BinaryReader reader, PlayFrame frame)
        {
            var colorFrame = frame as PlayColorFrame;

            if (colorFrame == null)
                throw new InvalidOperationException("Must be a ReplayColorFrame");

            colorFrame.Width = reader.ReadInt32();
            colorFrame.Height = reader.ReadInt32();
            colorFrame.FrameDataSize = reader.ReadInt32();
        }
        public void ReadDepthHeader(BinaryReader reader, PlayFrame frame)
        {
            var depthFrame = frame as PlayDepthFrame;

            if (depthFrame == null)
                throw new InvalidOperationException("Must be a ReplayColorFrame");

            depthFrame.Width = reader.ReadInt32();
            depthFrame.Height = reader.ReadInt32();
            depthFrame.FrameDataSize = reader.ReadInt32();
        }
        public void ReadInfraredHeader(BinaryReader reader, PlayFrame frame)
        {
            var infraredFrame = frame as PlayInfraredFrame;

            if (infraredFrame == null)
                throw new InvalidOperationException("Must be a ReplayColorFrame");

            infraredFrame.Width = reader.ReadInt32();
            infraredFrame.Height = reader.ReadInt32();
            infraredFrame.FrameDataSize = reader.ReadInt32();
        }
        public async Task<byte[]> DecodeAsync(byte[] encodedBytes)
        {
            using (var str = new MemoryStream())
            {
                str.Write(encodedBytes, 0, encodedBytes.Length);
                str.Position = 0;
                var dec = new JpegBitmapDecoder(str, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                var frame = dec.Frames[0];
                this.PixelFormat = frame.Format;
                var bpp = frame.Format.BitsPerPixel / 8;
                var stride = bpp * frame.PixelWidth;
                var size = stride * frame.PixelHeight;
                var output = new byte[size];
                frame.CopyPixels(output, stride, 0);
                return await Task.FromResult(output);
            }
        }
        }
}

using System;
using System.IO;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectDataManagement.DVR
{
    public class RawCodec : ICodec
    {
        private int _outputHeight = int.MinValue;
        private int _outputWidth = int.MinValue;

        public int ColorCodecId { get { return 0; }}
        public int DepthCodecId { get { return 0; } }
        public int InfraredCodecId { get { return 0; } }
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
            if (this.Width == this.OutputWidth && this.Height == this.OutputHeight)
            {
                // Header
                writer.Write(this.Width);
                writer.Write(this.Height);
                writer.Write(colorData.Length);

                // Data
                writer.Write(colorData);
            }
            else
            {
                WriteableBitmap bmp = BitmapFactory.New(this.Width, this.Height);
                int stride = this.Width * 4; // 4 bytes per pixel in BGRA
                var dirtyRect = new Int32Rect(0, 0, this.Width, this.Height);
                bmp.WritePixels(dirtyRect, colorData, stride, 0);
                var newBytes = await Task.FromResult(bmp.Resize(this.OutputWidth, this.OutputHeight, WriteableBitmapExtensions.Interpolation.NearestNeighbor).ToByteArray());

                // Header
                writer.Write(this.OutputWidth);
                writer.Write(this.OutputHeight);
                writer.Write(newBytes.Length);
                writer.Write(newBytes);

            }
        }

        public async Task EncodeDepthAsync(byte[] depthData, BinaryWriter writer)
        {
            await Task.Run(() =>
            {
                // Header
                writer.Write(this.Width);
                writer.Write(this.Height);
                writer.Write(depthData.Length);

                // Data
                writer.Write(depthData);
            });
        }

        public async Task EncodeInfraredAsync(byte[] infraredData, BinaryWriter writer)
        {
            await Task.Run(() =>
            {
                // Header
                writer.Write(this.Width);
                writer.Write(this.Height);
                writer.Write(infraredData.Length);

                // Data
                writer.Write(infraredData);
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

            this.PixelFormat = PixelFormats.Pbgra32;

            return await Task.FromResult(encodedBytes);
        }
    }
}

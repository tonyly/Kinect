using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace KinectDataManagement.DVR
{
    public class PlayInfraredFrame : PlayFrame
    {
        private byte[] _frameData = null;
        internal Stream Stream;
        internal long StreamPosition;
        internal ICodec Codec;

        internal int FrameDataSize { get; set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public byte[] FrameData
        {
            get
            {
                if (_frameData == null)
                {
                    // Assume we must read it from disk
                    return GetFrameDataAsync().Result;
                }
                return _frameData;
            }
        }
        internal PlayInfraredFrame() { }

        internal static PlayInfraredFrame FromReader(BinaryReader reader, ICodec codec)
        {
            var frame = new PlayInfraredFrame();

            frame.FrameType = FrameTypes.Infrared;
            frame.RelativeTime = TimeSpan.FromMilliseconds(reader.ReadDouble());
            frame.FrameSize = reader.ReadInt64();

            long frameStartPos = reader.BaseStream.Position;

            frame.Codec = codec;
            frame.Codec.ReadInfraredHeader(reader, frame);

            frame.Stream = reader.BaseStream;
            frame.StreamPosition = frame.Stream.Position;
            frame.Stream.Position += frame.FrameDataSize;

            // Do Frame Integrity Check
            var isGoodFrame = false;
            try
            {
                if (reader.ReadString() == PlayFrame.EndOfFrameMarker)
                {
                    isGoodFrame = true;
                }
            }
            catch { }

            if (!isGoodFrame)
            {
                System.Diagnostics.Debug.WriteLine("BAD FRAME...RESETTING");
                reader.BaseStream.Position = frameStartPos + frame.FrameSize;

                try
                {
                    if (reader.ReadString() != PlayFrame.EndOfFrameMarker)
                    {
                        throw new IOException("The recording appears to be corrupt.");
                    }
                    return null;
                }
                catch
                {
                    throw new IOException("The recording appears to be corrupt.");
                }

            }

            return frame;
        }
        public Task<byte[]> GetFrameDataAsync()
        {
            return Task<byte[]>.Run(async () =>
            {
                Monitor.Enter(Stream);
                var bytes = new byte[FrameDataSize];

                long savedPosition = Stream.Position;
                Stream.Position = StreamPosition;

                Stream.Read(bytes, 0, FrameDataSize);

                Stream.Position = savedPosition;
                Monitor.Exit(Stream);

                return await Codec.DecodeAsync(bytes);
            });
        }
        public byte[] GetRawFrameData()
        {
            Monitor.Enter(Stream);
            var bytes = new byte[FrameDataSize];

            long savedPosition = Stream.Position;
            Stream.Position = StreamPosition;

            Stream.Read(bytes, 0, FrameDataSize);

            Stream.Position = savedPosition;
            Monitor.Exit(Stream);

            return bytes;
        }
    }
}
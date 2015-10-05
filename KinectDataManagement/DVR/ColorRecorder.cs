using System;
using System.IO;
using System.Threading.Tasks;

namespace KinectDataManagement.DVR
{
    internal class ColorRecorder
    {
        private readonly BinaryWriter _writer;
        private bool _isStarted = false;

        
        private ICodec _codec;


        public ICodec Codec { get { return _codec; } set { _codec = value; } }
        public ColorRecorder(BinaryWriter writer)
        {
            this._writer = writer;
            this._codec = new RawCodec();
        }
        public async Task RecordAsync(RecordColorFrame frame)
        {
            if (_writer.BaseStream == null || _writer.BaseStream.CanWrite == false)
                return;

            _isStarted = true;

            try
            {
                // Header
                _writer.Write((int)frame.FrameType);
                _writer.Write(frame.RelativeTime.TotalMilliseconds);

                // Data
                using (var dataStream = new MemoryStream())
                {
                    using (var dataWriter = new BinaryWriter(dataStream))
                    {
                        _codec.Width = frame.Width;
                        _codec.Height = frame.Height;
                        await _codec.EncodeColorAsync(frame.FrameData, dataWriter);

                        // Reset frame data stream
                        dataWriter.Flush();
                        dataStream.Position = 0;

                        // Write FrameSize
                        _writer.Write(dataStream.Length);

                        // Write actual frame data
                        dataStream.CopyTo(_writer.BaseStream);

                        // Write end of frame marker
                        _writer.Write(RecordFrame.EndOfFrameMarker);
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: Change to log the error
                System.Diagnostics.Debug.WriteLine("Error saving Colorframe RecordAsync" + ex);
            }
        }
    }
}

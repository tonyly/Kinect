using System;
using System.IO;
using System.Threading.Tasks;

namespace KinectDataManagement.DVR
{
    class DepthRecorder
    {
        private readonly BinaryWriter _writer;
        //private byte[] _bytes = null;
        private bool _isStarted = false;
        

        private ICodec _codec;

        public DepthRecorder(BinaryWriter writer)
        {
            
            this._writer = writer;
            this._codec = new RawCodec();
        }

        public ICodec Codec { get { return _codec; } set { _codec = value; } }

        public async Task RecordAsync(RecordDepthFrame frame)
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
                        //dataWriter.Write(frame.DepthMinReliableDistance);
                        //dataWriter.Write(frame.DepthMaxReliableDistance);
                        //dataWriter.Write(frame.BytesPerPixel);

                        _codec.Width = frame.Width;
                        _codec.Height = frame.Height;

                        #region Obsoleto
                        await Task.Run( async() =>
                        {
                            int colorPixelIndex = 0;
                            byte[] _bytes = new byte[frame.FrameData.Length * 4];
                            for (int i = 0; i < frame.FrameData.Length; ++i)
                            {
                                ushort depth = frame.FrameData[i];
                                byte intensity = (byte)(depth >= frame.DepthMinReliableDistance && depth <= frame.DepthMaxReliableDistance ? depth : 0);

                                _bytes[colorPixelIndex++] = intensity; // B
                                _bytes[colorPixelIndex++] = intensity; // G
                                _bytes[colorPixelIndex++] = intensity; // R
                                _bytes[colorPixelIndex++] = intensity; // A
                            }

                            //if (_bytes == null)
                            //   _bytes = new byte[frame.FrameData.Length * 2];

                            //System.Buffer.BlockCopy(frame.FrameData, 0, _bytes, 0, _bytes.Length);
                            await _codec.EncodeDepthAsync(_bytes, dataWriter);

                        });
                        
                        #endregion


                        


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
                System.Diagnostics.Debug.WriteLine("Error saving Dethframe RecordAsync" + ex);
            }
        }

    }
}

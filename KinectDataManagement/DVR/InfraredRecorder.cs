using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;


namespace KinectDataManagement.DVR
{
    internal class InfraredRecorder
    {
        private readonly BinaryWriter _writer;
        private byte[] _bytes = null;

        
        private bool _isStarted = false;
        private float _infraredOutputValueMinimum = 0.01f;
        private float _infraredOutputValueMaximum = 1.0f;
        private int _resetAvgSdCounter = 15;
        private float _avg = 0.08f;
        private float _sd = 3.0f;
        private float _max = (float)ushort.MaxValue;

        private ICodec _codec;

        public InfraredRecorder(BinaryWriter writer)
        {
            this._bytes = new byte[217088 * 4];

            this._writer = writer;
            this._codec = new RawCodec();
        }

        public ICodec Codec { get { return _codec; } set { _codec = value; } }

        public async Task RecordAsync(RecordInfraredFrame frame)
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

                        // ushot frame data to byte array encoding to raw.
                        await Task.Run( () =>
                        {
                            int colorPixelIndex = 0;
                            int dataLen = frame.FrameData.Length;
                            float avgSd = _avg * _sd;
                            //byte[] _bytes = new byte[frame.FrameData.Length * 4];
                            if (_resetAvgSdCounter++ == 15)
                            {
                                _avg = frame.FrameData.Average(d => (d / _max));
                                _resetAvgSdCounter = 0;
                            }
                            for (int i = 0; i < dataLen; ++i)
                            {
                                float intensityRatio = (float)frame.FrameData[i] / _max;
                                intensityRatio /= avgSd;
                                intensityRatio = Math.Min(_infraredOutputValueMaximum, intensityRatio);
                                intensityRatio = Math.Max(_infraredOutputValueMinimum, intensityRatio);
                                byte intensity = (byte)(intensityRatio * 255.0f);

                                _bytes[colorPixelIndex++] = intensity;  // B
                                _bytes[colorPixelIndex++] = intensity;  // G
                                _bytes[colorPixelIndex++] = intensity;  // R
                                _bytes[colorPixelIndex++] = 255;        // A
                            }
                            
                            //if (_bytes == null)
                                //_bytes = new byte[frame.FrameData.Length * 2];

                            //System.Buffer.BlockCopy(frame.FrameData, 0, _bytes, 0, _bytes.Length);
                            
                        });

                        await _codec.EncodeInfraredAsync(_bytes, dataWriter);

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

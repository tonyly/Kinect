using System;
using System.IO;

namespace KinectDataManagement.DVR
{
    internal class PlayInfraredSystem : PlaySystem
    {
        private ICodec _codec;
        public event Action<PlayInfraredFrame> FrameArrived;
        public PlayInfraredSystem(ICodec codec)
        {
            this._codec = codec;
        }
        public void AddFrame(BinaryReader reader)
        {
            var frame = PlayInfraredFrame.FromReader(reader, _codec);
            if (frame != null)
                this.Frames.Add(frame);
        }
        public override void PushCurrentFrame()
        {
            if (this.FrameCount == 0)
                return;

            var frame = (PlayInfraredFrame)this.Frames[CurrentFrame];
            if (FrameArrived != null)
                FrameArrived(frame);
        }
    }
}
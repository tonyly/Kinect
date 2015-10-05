using System;
using System.IO;

namespace KinectDataManagement.DVR
{
    internal class PlayColorSystem : PlaySystem
    {
        private ICodec _codec;
        public event Action<PlayColorFrame> FrameArrived;
        public PlayColorSystem(ICodec codec)
        {
            this._codec = codec;
        }
        public void AddFrame(BinaryReader reader)
        {
            var frame = PlayColorFrame.FromReader(reader, _codec);
            if (frame != null)
                this.Frames.Add(frame);
        }
        public override void PushCurrentFrame()
        {
            if (this.FrameCount == 0)
                return;

            var frame = (PlayColorFrame)this.Frames[CurrentFrame];
            if (FrameArrived != null)
                FrameArrived(frame);
        }
    }
}
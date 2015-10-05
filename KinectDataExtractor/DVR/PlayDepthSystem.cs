﻿using System;
using System.IO;

namespace KinectDataManagement.DVR
{
    internal class PlayDepthSystem : PlaySystem
    {
        private ICodec _codec;
        public event Action<PlayDepthFrame> FrameArrived;
        public PlayDepthSystem(ICodec codec)
        {
            this._codec = codec;
        }
        public void AddFrame(BinaryReader reader)
        {
            var frame = PlayDepthFrame.FromReader(reader, _codec);
            if (frame != null)
                this.Frames.Add(frame);
        }
        public override void PushCurrentFrame()
        {
            if (this.FrameCount == 0)
                return;

            var frame = (PlayDepthFrame)this.Frames[CurrentFrame];
            if (FrameArrived != null)
                FrameArrived(frame);
        }
    }
}
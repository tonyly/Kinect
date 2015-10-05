using Microsoft.Kinect;

namespace KinectDataManagement.DVR
{
    internal class FileMetadata
    {
        public string Version { get; set; }
        public bool HasColor { get; set; }
        public bool HasDepth { get; set; }
        public bool HasInfrared { get; set; }
        public string ColorLocation { get; set; }
        public string DepthLocation { get; set; }
        public string InfraredLocation { get; set; }
        public int ColorCodecId { get; set; }
        public int DepthCodecId { get; set; }
        public int InfraredCodecId { get; set; }
        public int FpsColor { get; set; }
        public int FpsDepth { get; set; }
        public int FpsInfrared { get; set; }
    }
}

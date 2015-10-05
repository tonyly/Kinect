using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectDataManagement.DVR
{
    public interface ICodec
    {
        int ColorCodecId { get; }
        int DepthCodecId { get; }
        int InfraredCodecId { get; }

        int Width { get; set; }
        int Height { get; set; }
        int OutputWidth { get; set; }
        int OutputHeight { get; set; }
        PixelFormat PixelFormat { get; set; }

        Task EncodeColorAsync(byte[] colorData, BinaryWriter writer);
        Task EncodeDepthAsync(byte[] depthData, BinaryWriter writer);
        Task EncodeInfraredAsync(byte[] infraredData, BinaryWriter writer);

    }
}

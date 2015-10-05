using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectDataManagement.DVR
{
    internal static class Codecs
    {
        public static ICodec RawColor { get; set; }
        public static ICodec JpegColor { get; set; }
        public static ICodec RawDepth { get; set; }
        public static ICodec JpegDepth { get; set; }
        public static ICodec PngDepth { get; set; }
        public static ICodec RawInfrared { get; set; }
        public static ICodec JpegInfrared { get; set; }
        public static ICodec PngInfrared { get; set; }

        static Codecs()
        {
            RawColor = new RawCodec();
            JpegColor = new JpegCodec();

            RawDepth = new RawCodec();
            JpegDepth = new JpegCodec();
            PngDepth = new PngCodec();

            RawInfrared = new RawCodec();
            JpegInfrared = new JpegCodec();
            PngInfrared = new PngCodec();
        }
    }
}

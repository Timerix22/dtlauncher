using System;

namespace DTLib
{
    public class Color
    {
        public record RGBA(byte R, byte G, byte B, byte A)
        {
            public RGBA(byte[] arrayRGBA) : this(arrayRGBA[0], arrayRGBA[1], arrayRGBA[2], arrayRGBA[3])
            {
                if (arrayRGBA.Length != 4) throw new Exception("Color.RGBA(byte[] arrayRGBA) error: arrayRGBA.Length != 4\n");
            }
        }

        public record RGB(byte R, byte G, byte B)
        {
            public RGB(byte[] arrayRGB) : this(arrayRGB[0], arrayRGB[1], arrayRGB[2])
            {
                if (arrayRGB.Length != 3) throw new Exception("Color.RGB(byte[] arrayRGB) error: arrayRGB.Length != 3\n");
            }
        }
    }

}

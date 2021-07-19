using System;

namespace DTLib
{
    public class Color
    {
        public class RGBA
        {
            public byte R;
            public byte G;
            public byte B;
            public byte A;

            public RGBA() { }

            public RGBA(byte R, byte G, byte B, byte A)
            {
                this.R = R;
                this.G = G;
                this.B = B;
                this.A = A;
            }

            public RGBA(byte[] arrayRGBA)
            {
                if (arrayRGBA.Length != 4) throw new Exception("Color.RGBA(byte[] arrayRGBA) error: arrayRGBA.Length != 4\n");
                R = arrayRGBA[0];
                G = arrayRGBA[1];
                B = arrayRGBA[2];
                A = arrayRGBA[3];
            }
        }
        public class RGB
        {
            public byte R;
            public byte G;
            public byte B;

            public RGB() { }

            public RGB(byte R, byte G, byte B)
            {
                this.R = R;
                this.G = G;
                this.B = B;
            }

            public RGB(byte[] arrayRGB)
            {
                if (arrayRGB.Length != 4) throw new Exception("Color.RGB(byte[] arrayRGB) error: arrayRGB.Length != 4\n");
                R = arrayRGB[0];
                G = arrayRGB[1];
                B = arrayRGB[2];
            }
        }
    }
}

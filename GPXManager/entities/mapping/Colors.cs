using System;
using System.Drawing;

namespace GPXManager.entities.mapping
{
    public static class Colors
    {
        public static Color FromARGBString(string argb)
        {
            return Color.FromArgb(Convert.ToInt32(argb));
        }

        public static Color UintToColor(uint val)
        {
            int r, g, b;

            GetRGB((int)val, out r, out g, out b);

            return Color.FromArgb(255, r, g, b);
        }

        public static void GetRGB(int Color, out int r, out int g, out int b)
        {
            if (Color < 0)
                Color = 0;

            r = (int)(Color & 0xFF);
            g = (int)(Color & 0xFF00) / 256;	//shift right 8 bits
            b = (int)(Color & 0xFF0000) / 65536; //shift right 16 bits
        }

        public static int ColorToInt(Color c)
        {
            int retval = ((int)c.B) << 16;
            retval += ((int)c.G) << 8;
            return retval + ((int)c.R);
        }

        public static UInt32 ColorToUInteger(Color c)
        {
            int retval = ((int)c.B) << 16;
            retval += ((int)c.G) << 8;
            return Convert.ToUInt32(retval + ((int)c.R));
        }

        public static int ColorToaInt(Color c)
        {
            int retval = ((int)c.B) << 16;
            retval += ((int)c.G) << 8;
            return retval + ((int)c.R);
        }

        public static Color HSLtoColor(float Hue, float Sat, float Lum)
        {
            double r = 0, g = 0, b = 0;

            double temp1, temp2;

            if (Lum == 0)
            {
                r = g = b = 0;
            }
            else
            {
                if (Sat == 0)
                {
                    r = g = b = Lum;
                }
                else
                {
                    temp2 = ((Lum <= 0.5) ? Lum * (1.0 + Sat) : Lum + Sat - (Lum * Sat));

                    temp1 = 2.0 * Lum - temp2;

                    double[] t3 = new double[] { Hue + 1.0 / 3.0, Hue, Hue - 1.0 / 3.0 };

                    double[] clr = new double[] { 0, 0, 0 };

                    for (int i = 0; i < 3; i++)
                    {
                        if (t3[i] < 0)

                            t3[i] += 1.0;

                        if (t3[i] > 1)

                            t3[i] -= 1.0;

                        if (6.0 * t3[i] < 1.0)

                            clr[i] = temp1 + (temp2 - temp1) * t3[i] * 6.0;
                        else if (2.0 * t3[i] < 1.0)

                            clr[i] = temp2;
                        else if (3.0 * t3[i] < 2.0)

                            clr[i] = (temp1 + (temp2 - temp1) * ((2.0 / 3.0) - t3[i]) * 6.0);
                        else

                            clr[i] = temp1;
                    }

                    r = clr[0];

                    g = clr[1];

                    b = clr[2];
                }
            }
            return Color.FromArgb((int)(255 * r), (int)(255 * g), (int)(255 * b));
        }

        public static void GetHSL(Color c, out float Hue, out float Sat, out float Lum)
        {
            Hue = c.GetHue() / 360f;
            Sat = c.GetSaturation();
            Lum = c.GetBrightness();
        }
    }
}
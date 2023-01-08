using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using NRG.Models;
using NRG.MathsHelpers;

namespace NRG.Services
{
    public static class Conversions
    {
        #region Color

        public static int RGBtoInt(int r, int g, int b)
        {
            if (r < 0)
                r = 0;
            else if (r > 255)
                r = 255;

            if (g < 0)
                g = 0;
            else if (g > 255)
                g = 255;

            if (b < 0)
                b = 0;
            else if (b > 255)
                b = 255;

            int rgb = b;
            rgb = (rgb << 8) + g;
            rgb = (rgb << 8) + r;

            return rgb;
        }

        public static void LongIntToRGB(long color, ref int r, ref int g, ref int b)
        {
            b = (int)(color >> 16) & 0xFF;
            g = (int)(color >> 8) & 0xFF;
            r = (int)(color & 0xFF);
        }

        public static string RGBToHex(int r, int g, int b)
        {
            string hexValue = "#";

            var red = ToHex(r);
            var green = ToHex(g);
            var blue = ToHex(b);

            hexValue += (red + green + blue);

            return hexValue;
        }

        public static string RGBToHex(System.Drawing.Color color)
        {
            return RGBToHex(color.R, color.G, color.B);
        }

        public static string ToHex(double rgb)
        {
            var hex = "";
            hex = ((int)rgb).ToString("X").ToUpper();

            if (hex.Length == 1)
                hex = "0" + hex;
            else if (hex.Length == 0)
                hex = "00";

            return hex;
        }

        /// <summary>
        /// Test a colour for its brightness. Bright colours return black, dark colours return white. (Good for getting text colour based on background colour)
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color ContrastColor(Color color)
        {
            int d = 0;

            // Counting the perceptive luminance - human eye favors green color... 
            double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;

            if (luminance > 0.5)
                d = 0; // bright colors - black font
            else
                d = 255; // dark colors - white font

            return Color.FromArgb(d, d, d);
        }

        //Todo - Remove this method and replace with the method below (Optimization)
        public static void GetHeightMapFromPoint(Bounds bounds, double z, ref int r, ref int g, ref int b)
        {
            var val = ((z - bounds.Min.Z) / bounds.Size.Z) * 1020;
            var segment = Math.Floor(val / 255);

            switch (segment)
            {
                case 0:
                    b = 255;
                    g = (int)Math.Floor(val % 255);
                    r = 0;
                    return;
                case 1:
                    b = (int)Math.Floor(255 - (val % 255));
                    g = 255;
                    r = 0;
                    return;
                case 2:
                    b = 0;
                    g = 255;
                    r = (int)Math.Floor(val % 255);
                    return;
                case 3:
                    b = 0;
                    g = (int)Math.Floor(255 - (val % 255));
                    r = 255;
                    return;
                default:
                    b = 0;
                    g = 0;
                    r = 255;
                    return;
            }
        }

        /// <summary>
        /// Generates a rgb color for a given level from Blue (min level) to Red (max level)
        /// </summary>
        /// <param name="minZ">The minimum level of the model</param>
        /// <param name="maxZ">The maximum level of the model</param>
        /// <param name="z">The level of the point</param>
        /// <param name="r">The resulting Red color channel</param>
        /// <param name="g">The resulting Blue color channel</param>
        /// <param name="b">The resulting Green color channel</param>
        public static void GetHeightMapFromPoint(double minZ, double maxZ, double z, ref int r, ref int g, ref int b)
        {
            var size = maxZ - minZ;
            var val = ((z - minZ) / size) * 1020;
            var segment = Math.Floor(val / 255);

            switch(segment)
            {
                case 0:
                    b = 255;
                    g = (int)Math.Floor(val % 255);
                    r = 0;
                    return;
                case 1:
                    b = (int)Math.Floor(255 - (val % 255));
                    g = 255;
                    r = 0;
                    return;
                case 2:
                    b = 0;
                    g = 255;
                    r = (int)Math.Floor(val % 255);
                    return;
                case 3:
                    b = 0;
                    g = (int)Math.Floor(255 - (val % 255));
                    r = 255;
                    return;
                default:
                    b = 0;
                    g = 0;
                    r = 255;
                    return;
            }
        }

        public static int GetGrayScaleFromColor(int r, int g, int b)
        {
            return (int)((r * 0.3) + (g * 0.59) + (b * 0.11));
        }

        public static void GetIntensityFromPoint(float intensity)
        {

        }

        #endregion

        #region Misc

        public static string GetSpacing(int spacing)
        {
            return new string(' ', spacing);
        }

        /// <summary>
        /// Convert a bearing (radians) to a format of choice
        /// </summary>
        /// <param name="format"></param>
        /// <param name="brg"></param>
        /// <returns></returns>
        public static string GetBrgFromFormat(string format, double brg)
        {
            switch (format)
            {
                case "Degrees":
                    return Trig.DegToDMS(Trig.RadToDeg(brg));
                case "Rads":
                    return brg.ToString("0.00");
                case "Grads":
                    return Trig.RadToGrad(brg).ToString("0.00");
                default:
                    return "0.00";
            }
        }

        public static string FormatGrade(double grade)
        {
            if (grade != double.PositiveInfinity)
            {
                return "1:" + grade.ToString("0.000");
            }
            return "1:inf";
        }

        public static string GetFileSizeFromLong(long sizeInBytes)
        {
            if (sizeInBytes < 1024)
                return sizeInBytes.ToString() + "B";
            else if (sizeInBytes < 1048576)
                return ((decimal)sizeInBytes / (decimal)1024).ToString("0.00") + "KB";
            else if (sizeInBytes < 1073741824)
                return ((decimal)sizeInBytes / (decimal)1048576).ToString("0.00") + "MB";
            else if (sizeInBytes < 1099511627776)
                return ((decimal)sizeInBytes / (decimal)1073741824).ToString("0.00") + "GB";
            else
                return ((decimal)sizeInBytes / (decimal)1099511627776).ToString("0.00") + "TB";
        }

        #endregion

        #region Imagery

        public static byte[] GetPixelBitsFromBmp(string filename, ref int width, ref int height)
        {
            using (var image = Image.FromFile(filename))
            {
                using (var bitmap = new Bitmap(image))
                {
                    var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                    var bytesPerPixel = 4;
                    var ptr = bitmapData.Scan0;
                    width = bitmapData.Width;
                    height = bitmapData.Height;
                    var imageSize = bitmapData.Width * bitmapData.Height * bytesPerPixel;
                    var data = new byte[imageSize];
                    for (int x = 0; x < imageSize; x += bytesPerPixel)
                    {
                        for (var y = 0; y < bytesPerPixel; y++)
                        {
                            data[x + y] = Marshal.ReadByte(ptr);
                            ptr += 1;
                        }
                    }

                    bitmap.UnlockBits(bitmapData);
                    return data;
                }
            }
        }

        #endregion
    }

    public class IntensityConverter
    {
        #region Properties

        public int MaxIntensity { get; set; }
        public int MinIntensity { get; set; }
        public List<(Color StartColor, Color EndColor)> IntensityColorList { get; set; }
        public double IntensityFraction { get; set; }
        public int OutOfRangeMode { get; set; }

        #endregion

        #region Setup

        public IntensityConverter()
        {
            MinIntensity = 0;
            MaxIntensity = 65535;
            IntensityColorList = new List<(Color StartColor, Color EndColor)>();
            OutOfRangeMode = 0;
        }

        #endregion

        #region Methods

        public void GetIntensityColor(ushort intensity, ref int r, ref int g, ref int b)
        {
            var fraction = (double)(intensity - MinIntensity) / (double)(MaxIntensity - MinIntensity);
            if(fraction >= 1)
            {
                var color = IntensityColorList.Last().EndColor;
                r = color.R;
                g = color.G;
                b = color.B;
                return;
            }

            var index = (int)(fraction * IntensityColorList.Count);

            var lowerBracket = IntensityFraction * (double)index;
            var upperBracket = IntensityFraction * (double)(index + 1);

            var fractionOfBracket = (double)(fraction - lowerBracket) / (double)(upperBracket - lowerBracket);

            var colorBracket = IntensityColorList[index];
            int sR = colorBracket.StartColor.R, sG = colorBracket.StartColor.G, sB = colorBracket.StartColor.B;
            int eR = colorBracket.EndColor.R, eG = colorBracket.EndColor.G, eB = colorBracket.EndColor.B;

            r = (int)(sR + ((eR - sR) * fractionOfBracket));
            g = (int)(sG + ((eG - sG) * fractionOfBracket));
            b = (int)(sB + ((eB - sB) * fractionOfBracket));
        }

        public List<int> GenerateColorIndices(int numColors)
        {
            var colorIndices = new List<int>();

            switch(numColors)
            {
                default:
                    colorIndices = new List<int> { 0, 1, 2, 3, 4, 5 };
                    break;
                case 2:
                    colorIndices = new List<int> { 0, 5 };
                    break;
                case 3:
                    colorIndices = new List<int> { 0, 3, 5 };
                    break;
                case 4:
                    colorIndices = new List<int> { 0, 2, 3, 5 };
                    break;
                case 5:
                    colorIndices = new List<int> { 0, 1, 2, 3, 5 };
                    break;
            }

            return colorIndices;
        }

        //Almost certainly much more complicated than it needs to be but I am open to suggestions on how to improve it
        public List<(Color StartColor, Color EndColor)> GenerateColorList(List<int> colorIndices, List<Color> intensityColors)
        {
            var colorList = new List<(Color StartGradient, Color EndGradient)>();
            Color startColor, endColor;

            startColor = intensityColors[colorIndices[0]];
            endColor = intensityColors[colorIndices[1]];

            int sR = startColor.R, sG = startColor.G, sB = startColor.B;
            int eR = endColor.R, eG = endColor.G, eB = endColor.B;

            if (colorIndices.Count == 2)
            {
                var middleColor = Color.FromArgb(sR + (int)((eR - sR) * 0.5), sG + (int)((eG - sG) * 0.5), sB + (int)((eB - sB) * 0.5));

                colorList.Add((startColor, middleColor));
                colorList.Add((middleColor, endColor));
                return colorList;
            }
            else if (colorIndices.Count == 3)
            {
                var middleColor = Color.FromArgb(sR + (int)((eR - sR) * 0.75), sG + (int)((eG - sG) * 0.75), sB + (int)((eB - sB) * 0.75));
                colorList.Add((startColor, middleColor));

                //Move to next zone
                startColor = intensityColors[colorIndices[1]];
                endColor = intensityColors[colorIndices[2]];
                sR = startColor.R; sG = startColor.G; sB = startColor.B;
                eR = endColor.R; eG = endColor.G; eB = endColor.B;

                var middleColor2 = Color.FromArgb(sR + (int)((eR - sR) * 0.25), sG + (int)((eG - sG) * 0.25), sB + (int)((eB - sB) * 0.25));

                colorList.Add((middleColor, middleColor2));
                colorList.Add((middleColor2, endColor));
                return colorList;
            }
            else if (colorIndices.Count >= 4 && colorIndices.Count <= 6)
            {
                var middleColor = Color.FromArgb(sR + (int)((eR - sR) * 0.75), sG + (int)((eG - sG) * 0.75), sB + (int)((eB - sB) * 0.75));
                colorList.Add((startColor, middleColor));

                //Move to next zone
                startColor = intensityColors[colorIndices[1]];
                endColor = intensityColors[colorIndices[2]];
                sR = startColor.R; sG = startColor.G; sB = startColor.B;
                eR = endColor.R; eG = endColor.G; eB = endColor.B;

                var middleColor2 = Color.FromArgb(sR + (int)((eR - sR) * 0.50), sG + (int)((eG - sG) * 0.50), sB + (int)((eB - sB) * 0.50));
                colorList.Add((middleColor, middleColor2));

                //Move to next one
                startColor = intensityColors[colorIndices[2]];
                endColor = intensityColors[colorIndices[3]];
                sR = startColor.R; sG = startColor.G; sB = startColor.B;
                eR = endColor.R; eG = endColor.G; eB = endColor.B;

                if (colorIndices.Count == 4)
                {
                    middleColor = Color.FromArgb(sR + (int)((eR - sR) * 0.25), sG + (int)((eG - sG) * 0.25), sB + (int)((eB - sB) * 0.25));
                    colorList.Add((middleColor2, middleColor));
                    colorList.Add((middleColor, endColor));
                    return colorList;
                }

                middleColor = Color.FromArgb(sR + (int)((eR - sR) * 0.50), sG + (int)((eG - sG) * 0.50), sB + (int)((eB - sB) * 0.50));
                colorList.Add((middleColor2, middleColor));

                //Move to next one
                startColor = intensityColors[colorIndices[3]];
                endColor = intensityColors[colorIndices[4]];
                sR = startColor.R; sG = startColor.G; sB = startColor.B;
                eR = endColor.R; eG = endColor.G; eB = endColor.B;

                if (colorIndices.Count == 5)
                {
                    middleColor2 = Color.FromArgb(sR + (int)((eR - sR) * 0.25), sG + (int)((eG - sG) * 0.25), sB + (int)((eB - sB) * 0.25));
                    colorList.Add((middleColor, middleColor2));
                    colorList.Add((middleColor2, endColor));
                    return colorList;
                }

                middleColor2 = Color.FromArgb(sR + (int)((eR - sR) * 0.50), sG + (int)((eG - sG) * 0.50), sB + (int)((eB - sB) * 0.50));
                colorList.Add((middleColor, middleColor2));

                //Move to the next one
                startColor = intensityColors[colorIndices[4]];
                endColor = intensityColors[colorIndices[5]];
                sR = startColor.R; sG = startColor.G; sB = startColor.B;
                eR = endColor.R; eG = endColor.G; eB = endColor.B;

                middleColor = Color.FromArgb(sR + (int)((eR - sR) * 0.25), sG + (int)((eG - sG) * 0.25), sB + (int)((eB - sB) * 0.25));
                colorList.Add((middleColor2, middleColor));
                colorList.Add((middleColor, endColor));
                return colorList;
            }

            return colorList;
        }

        public IntensityConverter Copy()
        {
            return new IntensityConverter
            {
                MaxIntensity = this.MaxIntensity,
                MinIntensity = this.MinIntensity,
                IntensityColorList = this.IntensityColorList.ToList(),
                IntensityFraction = this.IntensityFraction,
                OutOfRangeMode = this.OutOfRangeMode
            };
        }

        public void GenerateIntensityFraction()
        {
            if(IntensityColorList.Count != 0)
                IntensityFraction = 1.0D / (double)IntensityColorList.Count;
        }

        #endregion
    }
}

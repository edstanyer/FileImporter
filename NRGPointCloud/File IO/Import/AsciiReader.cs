using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;

namespace NRG.Import
{
    public class AsciiReader : PointReader
    {
        #region Properties

        private Bounds bounds = new Bounds();
        private StreamReader stream;
        private long pointsRead;
        private long pointCount;
        private CloudPoint point;
        private AsciiFormat format;
        private float colorOffset;
        private float colorScale;
        private float intensityOffset;
        private float intensityScale;
        private int linesSkipped;

        #endregion

        #region Setup

        public AsciiReader(string file, AsciiFormat format, List<double> colorRange, List<double> intensityRange)
        {
            stream = new StreamReader(file);
            this.format = format;
            pointsRead = 0;
            linesSkipped = 0;
            pointCount = 0;
            colorScale = -1;

            if(intensityRange.Count == 2)
            {
                intensityOffset = (float)intensityRange[0];
                intensityScale = (float)intensityRange[1] - (float)intensityRange[0];
            }
            else if(intensityRange.Count == 1)
            {
                intensityOffset = 0.0f;
                intensityScale = (float)intensityRange[0];
            }
            else
            {
                intensityOffset = 0.0f;
                intensityScale = 1.0f;
            }

            if(colorRange.Count == 2)
            {
                colorOffset = (float)colorRange[0];
                colorScale = (float)colorRange[1];
            }
            else if(colorRange.Count == 1)
            {
                colorOffset = 0.0f;
                colorScale = (float)colorRange[0];
            }
            else if(colorRange.Count == 0)
            {
                colorOffset = 0.0f;

                //try to find the color range by evaluating the first x points
                float max = 0;
                int j = 0;
                string line;
                while((line = stream.ReadLine()) != null && j < 1000)
                {
                    if (j < format.SkipLines)
                        continue;

                    //Possibly needs trim here check later
                    List<string> tokens = line.Split(new string[] { format.Delimiter }, StringSplitOptions.None).ToList();

                    if (this.format.Format == "" && tokens.Count >= 3)
                    {
                        var f = new string('s', tokens.Count).ToArray();
                        f[0] = 'x';
                        f[1] = 'y';
                        f[2] = 'z';

                        if(tokens.Count >= 6)
                        {
                            f[3] = 'r';
                            f[4] = 'g';
                            f[5] = 'b';
                        }

                        this.format.Format = f.ToString();
                    }

                    if (tokens.Count < this.format.Format.Count())
                        continue;

                    int i = 0;
                    foreach(var f in format.Format)
                    {
                        string token = tokens[i++];
                        if(f == 'r')
                        {
                            max = Math.Max(max, Convert.ToSingle(token));
                        }
                        else if(f == 'g')
                        {
                            max = Math.Max(max, Convert.ToSingle(token));
                        }
                        else if(f == 'b')
                        {
                            max = Math.Max(max, Convert.ToSingle(token));
                        }
                    }

                    j++;
                }

                if (max <= 1.0f)
                    colorScale = 1.0f;
                else if (max <= 255.0f)
                    colorScale = 255.0f;
                else if (max < Math.Pow(2, 16) - 1)
                    colorScale = (float)Math.Pow(2, 16) - 1;
                else
                    colorScale = (float)max;

                stream.DiscardBufferedData();
                stream.BaseStream.Position = 0;

                //Skip the number of lines required
                while((line = stream.ReadLine()) != null && linesSkipped < format.SkipLines)
                {
                    linesSkipped++;
                }
            }
        }

        public override void Close()
        {
            stream.BaseStream.Flush();
            stream.Close();
            stream = null;
        }

        #endregion

        #region Methods

        public override bool ReadNextPoint(ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity)
        {
            string line;

            while((line = stream.ReadLine()) != null)
            {
                try
                {
                    var tokens = line.Split(new string[] { format.Delimiter }, StringSplitOptions.None).ToList();
                    var count = Math.Min(tokens.Count, format.Format.Count());

                    //Not enough values, read next
                    if (count < format.Format.Length)
                    {
                        //Check if we can skip
                        if (count < format.Format.TrimEnd(new char[] { 's' }).Length)
                            continue;
                    }

                    for(int i = 0; i < count; i++)
                    {
                        string token = tokens[i];
                        char f = format.Format[i];

                        switch(f)
                        {
                            case 'x':
                                x = Math.Round(Convert.ToDouble(token), 3);
                                break;
                            case 'y':
                                y = Math.Round(Convert.ToDouble(token), 3);
                                break;
                            case 'z':
                                z = Math.Round(Convert.ToDouble(token), 3);
                                break;
                            case 'r':
                                r = Convert.ToByte(token);
                                break;
                            case 'g':
                                g = Convert.ToByte(token);
                                break;
                            case 'b':
                                b = Convert.ToByte(token);
                                break;
                            case 'i':
                                try
                                {
                                    intensity = Convert.ToUInt16(65535 * (Convert.ToDouble(token) - intensityOffset) / intensityScale);
                                }
                                catch
                                {
                                    intensity = 0;
                                }
                                break;
                        }
                    }

                    return true;
                }
                catch
                {

                }
            }

            return false;
        }

        public override bool ReadNextPoint(ref CloudPoint point)
        {
            double x = 0, y = 0, z = 0;
            byte r = 0, g = 0, b = 0;
            ushort intensity = 0;

            string line;

            while ((line = stream.ReadLine()) != null)
            {
                try
                {
                    List<string> tokens = line.Split(new string[] { format.Delimiter }, StringSplitOptions.None).ToList();
                    if (tokens.Count != format.Format.Count())
                    {
                        linesSkipped++;
                        continue;
                    }

                    int i = 0;
                    foreach (var f in format.Format)
                    {
                        string token = tokens[i++];
                        if (f == 'x')
                            x = Math.Round(Convert.ToDouble(token), 3);
                        else if (f == 'y')
                            y = Math.Round(Convert.ToDouble(token), 3);
                        else if (f == 'z')
                            z = Math.Round(Convert.ToDouble(token), 3);
                        else if (f == 'r')
                            r = Convert.ToByte(token);
                        else if (f == 'g')
                            g = Convert.ToByte(token);
                        else if (f == 'b')
                            b = Convert.ToByte(token);
                        else if (f == 'i')
                        {
                            try
                            {
                                intensity = Convert.ToUInt16(65535 * (Convert.ToDouble(token) - intensityOffset) / intensityScale);
                            }
                            catch
                            {
                                intensity = 0;
                            }
                        }
                        else if (f == 's')
                        {
                            //skip
                        }
                    }
          
                    point = new CloudPoint();
                    point.X = x;
                    point.Y = y;
                    point.Z = z;
                    point.R = r;
                    point.G = g;
                    point.B = b;
                    point.Intensity = intensity;
                    pointsRead++;
                    return true;
                }
                catch
                {

                }
            }

            return false;
        }

        public override Bounds GetBounds()
        {
            double x = 0, y = 0, z = 0;
            byte r = 0, g = 0, b = 0;
            ushort intensity = 0;

            bounds = new Bounds();

            stream.DiscardBufferedData();
            stream.BaseStream.Position = 0;

            //Read the lines to skip
            for (int i = 0; i < format.SkipLines; i++)
            {
                try
                {
                    stream.ReadLine();
                }
                catch
                {

                }
            }

            //read through once to calculate bounds and number of points
            while (ReadNextPoint(ref x, ref y, ref z, ref r, ref g, ref b, ref intensity))
            {
                bounds.Update(x, y, z);
                pointCount++;
            }

            stream.DiscardBufferedData();
            stream.BaseStream.Position = 0;

            //Read the lines to skip
            for (int i = 0; i < format.SkipLines; i++)
            {
                try
                {
                    stream.ReadLine();
                }
                catch
                {

                }
            }
            return bounds;
        }

        public override ulong NumPoints()
        {
            return (ulong)pointCount;
        }

        #endregion
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using NRG.Services;

namespace NRG.Export
{
    public class GpfPointWriter : PointWriter
    {
        #region Properties

        public StreamWriter writer;

        #endregion

        #region Setup

        public GpfPointWriter(string file)
        {
            this.file = file;
            numPoints = 0;
            writer = new StreamWriter(file);
            writer.WriteLine(";NRG GROUND PLOT FILE VERSION 2");
        }

        public override void CloseWriter()
        {
            writer.Flush();
            writer.Close();
            writer = null;
            Close();
        }

        public override void Close()
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
                writer = null;
            }
        }

        #endregion

        #region Methods

        public override void Write(CloudPoint point)
        {
            string pointString = "";
            pointString += "," + "/RGB" + Conversions.RGBtoInt((int)point.R, (int)point.G, (int)point.B);
            pointString += "," + point.X;
            pointString += "," + point.Y;
            pointString += "," + point.Z;
            writer.WriteLine(pointString);
            numPoints++;
        }

        public override void WriteLinePoint(double x, double y, double z, string lineString)
        {
            string pointString = "";
            pointString += "," + lineString;
            pointString += "," + x;
            pointString += "," + y;
            pointString += "," + z;
            writer.WriteLine(pointString);
            numPoints++;
        }

        public override void WriteAllPoints(List<CloudPoint> points)
        {
            foreach (var point in points)
            {
                var pointString = "";
                pointString += "," + "/RGB" + Conversions.RGBtoInt((int)point.R, (int)point.G, (int)point.B);
                pointString += "," + point.X;
                pointString += "," + point.Y;
                pointString += "," + point.Z;
                writer.WriteLine(pointString);
                numPoints++;
            }
        }

        public override void WriteAllPoints(List<CodedPoint> points)
        {
            foreach (var point in points)
            {
                var pointString = "";
                pointString += "," + point.Code;
                pointString += "," + point.X;
                pointString += "," + point.Y;
                pointString += "," + point.Z;
                writer.WriteLine(pointString);
                numPoints++;
            }
        }

        public override void WriteAllPoints(List<CloudPoint> points, Bounds modelBounds, ColorType colorType, IntensityConverter intensityConverter)
        {
            var pointString = "";
            int cR = 0, cG = 0, cB = 0;

            //Different export logic depending on the point - Note we put Clearance and RGB together as clearance should not be shown on the selected window
            switch(colorType)
            {
                case ColorType.RGB: case ColorType.Clearance: default:
                    foreach(var point in points)
                    {
                        pointString = ",/RGB" + Conversions.RGBtoInt((int)point.R, (int)point.G, (int)point.B);
                        pointString += "," + point.X + "," + point.Y + "," + point.Z;
                        writer.WriteLine(pointString);
                        numPoints++;
                    }
                    break;
                case ColorType.HeightMap:
                    var minZ = modelBounds.Min.Z;
                    var maxZ = modelBounds.Max.Z;
                    foreach(var point in points)
                    {
                        Conversions.GetHeightMapFromPoint(minZ, maxZ, point.Z, ref cR, ref cG, ref cB);
                        pointString = ",/RGB" + Conversions.RGBtoInt(cR, cG, cB);
                        pointString += "," + point.X + "," + point.Y + "," + point.Z;
                        writer.WriteLine(pointString);
                        numPoints++;
                    }
                    break;
                case ColorType.Intensity:
                    ushort intensity = 0;
                    int minIntensity = intensityConverter.MinIntensity, maxIntensity = intensityConverter.MaxIntensity;
                    int outOfRangeMode = intensityConverter.OutOfRangeMode;
                    var firstColor = intensityConverter.IntensityColorList.FirstOrDefault().StartColor;
                    var lastColor = intensityConverter.IntensityColorList.LastOrDefault().EndColor;
                    foreach(var point in points)
                    {
                        //Get intensity and check that it is within range
                        intensity = point.Intensity;

                        if(intensity < minIntensity || intensity > maxIntensity)
                        {
                            if(outOfRangeMode == 0)
                            {
                                //Show as RGB
                                pointString = ",/RGB" + Conversions.RGBtoInt(point.R, point.G, point.B);
                                pointString += "," + point.X + "," + point.Y + "," + point.Z;
                                writer.WriteLine(pointString);
                                numPoints++;
                            }
                            else if(outOfRangeMode == 1)
                            {
                                //Show as Min / Max intensity
                                if (intensity < minIntensity)
                                    pointString = ",/RGB" + Conversions.RGBtoInt(firstColor.R, firstColor.G, firstColor.B);
                                else
                                    pointString = ",/RGB" + Conversions.RGBtoInt(lastColor.R, lastColor.G, lastColor.B);

                                pointString += "," + point.X + "," + point.Y + "," + point.Z;
                                writer.WriteLine(pointString);
                                numPoints++;
                            }
                        }
                        else
                        {
                            //Display the point with intensity
                            intensityConverter.GetIntensityColor(intensity, ref cR, ref cG, ref cB);
                            pointString = ",/RGB" + Conversions.RGBtoInt(cR, cG, cB);
                            pointString += "," + point.X + "," + point.Y + "," + point.Z;
                            writer.WriteLine(pointString);
                            numPoints++;
                        }
                    }
                    break;
                case ColorType.GrayScale:
                    foreach(var point in points)
                    {
                        var gray = Conversions.GetGrayScaleFromColor(point.R, point.G, point.B);
                        pointString = ",/RGB" + Conversions.RGBtoInt(gray, gray, gray);
                        pointString += "," + point.X + "," + point.Y + "," + point.Z;
                        writer.WriteLine(pointString);
                        numPoints++;
                    }
                    break;
            }
        }

        #endregion
    }
}

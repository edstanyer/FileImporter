using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;

namespace NRG.Export
{
    public class BinWriter
    {
        #region Properties

        private BinaryWriter writer;

        #endregion

        #region Setup

        public BinWriter(string filename, string pointCloudFile)
        {
            
            writer = new BinaryWriter(File.Open(filename, FileMode.Create));
            writer.Write("NRG User Data File Version 1.1");
            writer.Write(pointCloudFile);
        }

        public void Close()
        {
            if (writer != null)
                writer.Close();
            writer = null;
        }

        #endregion

        #region Methods

        public void WriteLines(List<Line> lines)
        {
            if (writer == null)
                return;

            //Write the data type
            writer.Write((int)DataTypes.Line);

            //Write the number of lines
            if (lines != null)
            {
                writer.Write(lines.Count);
                foreach (var line in lines)
                {
                    writer.Write(line.Code);
                    writer.Write(line.IsPoly);
                    writer.Write(line.Color.R);
                    writer.Write(line.Color.G);
                    writer.Write(line.Color.B);
                    writer.Write(line.Thickness);
                    if (line.Points != null)
                    {
                        writer.Write(line.PointCount);
                        foreach (var point in line.Points)
                        {
                            writer.Write(point.X);
                            writer.Write(point.Y);
                            writer.Write(point.Z);
                        }
                    }
                    else
                        writer.Write(0);
                }
            }
            else
                writer.Write(0);
        }

        public void WritePoints(List<CodedPoint> points)
        {
            if (writer == null)
                return;

            //Write the data type
            writer.Write((int)DataTypes.CodedPoint);

            //Write the number of points
            if (points != null)
            {
                writer.Write(points.Count);
                foreach (var point in points)
                {
                    writer.Write(point.Code);
                    writer.Write(point.Color.R);
                    writer.Write(point.Color.G);
                    writer.Write(point.Color.B);
                    writer.Write(point.Thickness);
                    writer.Write(point.X);
                    writer.Write(point.Y);
                    writer.Write(point.Z);
                }
            }
            else
                writer.Write(0);
        }

        public void WriteZones(List<ZonePolygon> zones, bool isSelection)
        {
            if (writer == null)
                return;

            //Write the data type
            writer.Write(isSelection ? (int)DataTypes.SelectionZone : (int)DataTypes.DeselectionZone);

            //Write the number of points
            if (zones != null)
            {
                writer.Write(zones.Count);
                foreach (var poly in zones)
                {
                    writer.Write(poly.IsDeselectionZone);
                    writer.Write(poly.Filtered);
                    if (poly.Points != null)
                    {
                        writer.Write(poly.PointCount);
                        foreach (var point in poly.Points)
                        {
                            writer.Write(point.X);
                            writer.Write(point.Y);
                        }
                    }
                    else
                        writer.Write(0);

                    writer.Write(poly.Bounds.Min.Z);
                    writer.Write(poly.Bounds.Max.Z);
                }
            }
            else
                writer.Write(0);
        }

        public void WriteFilteredPoints(List<CloudPoint> points)
        {
            if (writer == null)
                return;

            //Write the data type
            writer.Write((int)DataTypes.FilteredPoints);

            //Write the number of points
            if (points != null)
            {
                writer.Write(points.Count);
                foreach (var point in points)
                {
                    writer.Write(point.X);
                    writer.Write(point.Y);
                    writer.Write(point.Z);
                    writer.Write(point.R);
                    writer.Write(point.G);
                    writer.Write(point.B);
                    writer.Write(point.Intensity);
                }
            }
            else
                writer.Write(0);
        }

        #endregion
    }
}

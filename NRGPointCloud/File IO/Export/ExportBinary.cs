using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;

namespace NRG.Export
{
    public static class ExportBinary
    {
        #region Methods

        public static bool ExportUserData(List<Line> lines, List<CodedPoint> points, List<ZonePolygon> selectionZones, List<ZonePolygon> deselectionZones, string filename)
        {
            try
            {
                var writer = new BinaryWriter(File.Open(filename, FileMode.Create));

                //Writer number of lines
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
                        writer.Write(line.PointCount);
                        if (line.Points != null)
                        {
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

                //Write points
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

                //Write selectionZones
                if (selectionZones != null)
                {
                    writer.Write(selectionZones.Count);
                    foreach (var poly in selectionZones)
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
                                writer.Write(0D);
                            }
                        }
                        else
                            writer.Write(0);
                    }
                }
                else
                    writer.Write(0);

                //Write deselectionZones
                if (deselectionZones != null)
                {
                    writer.Write(deselectionZones.Count);
                    foreach (var poly in deselectionZones)
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
                                writer.Write(0D);
                            }
                        }
                        else
                            writer.Write(0);
                    }
                }
                else
                    writer.Write(0);

                return true;
            }
            catch
            {

                return false;
            }
        }

        #endregion
    }
}

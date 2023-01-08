using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using NRG.Export;
using System.Runtime.Serialization.Formatters.Binary;

namespace NRG.Export
{
    //Repupose this class into writing points without an offset
    public class BinPointWriter : PointWriter
    {
        #region Properties

        public Stream sWriter;
        public BinaryWriter writer;

        #endregion

        #region Setup

        public BinPointWriter(string file)
        {
            this.file = file;
            numPoints = 0;

            writer = new BinaryWriter(File.Open(file, FileMode.Create));
        }

        public void CloseBinaryWriter()
        {
            writer = null;
            sWriter = null;
            Close();
        }

        public override void Close()
        {
            if (writer != null)
                writer.Close();
            writer = null;
        }

        #endregion

        #region Methods

        public override void WriteAllPoints(List<CloudPoint> points)
        {
            try
            {
                foreach (var p in points)
                {
                    writer.Write(p.X);
                    writer.Write(p.Y);
                    writer.Write(p.Z);
                    byte[] rgba = new byte[4] { p.R, p.G, p.B, 255 };
                    writer.Write(rgba);
                    writer.Write(p.Intensity);
                    numPoints++;
                }

                Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public override void Write(CloudPoint point)
        {
            try
            {
                writer.Write(point.X);
                writer.Write(point.Y);
                writer.Write(point.Z);
                byte[] rgba = new byte[4] { point.R, point.G, point.B, 255 };
                writer.Write(rgba);
                writer.Write(point.Intensity);
                numPoints++;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public override void Write(double x, double y, double z, byte r, byte g, byte b, ushort intensity)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
            byte[] rgba = new byte[4] { r, g, b, 255 };
            writer.Write(rgba);
            writer.Write(intensity);
            numPoints++;
        }

        public override void WriteBounds(Bounds bounds)
        {
            writer.Write(bounds.Min.X);
            writer.Write(bounds.Min.Y);
            writer.Write(bounds.Min.Z);
            writer.Write(bounds.Max.X);
            writer.Write(bounds.Max.Y);
            writer.Write(bounds.Max.Z);
        }

        #endregion
    }
}

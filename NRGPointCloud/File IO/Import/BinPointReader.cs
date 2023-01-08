using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using NRG.MathsHelpers;
using NRG.Services;
using NRG.Import;
using System.Runtime.Serialization.Formatters.Binary;

namespace NRG.Import
{
    //Repurpose this class into writing points without an offset and instead made of 3 doubles, 3 bytes and a ushort
    public class BinPointReader : PointReader
    {
        #region Properties

        private Bounds bounds;
        private string path;
        private BinaryReader reader;
        private uint numberOfPoints;
        private uint numberOfReadPoints;

        #endregion

        #region Setup

        public BinPointReader(string path)
        {
            this.path = path;

            try
            {
                reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

                //Read the bounds from the end of the stream 
                reader.BaseStream.Seek(-48, SeekOrigin.End);
                this.bounds = new Bounds(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                numberOfPoints = (uint)(reader.BaseStream.Length - 48) / 30;
                numberOfReadPoints = 0;
            }
            catch
            {

            }
        }

        public override void Close()
        {
            if (reader != null)
                reader.Close();

            reader = null;
        }

        #endregion

        #region Methods

        public override bool ReadNextPoint(ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity)
        {
            if (reader == null)
                return false;

            if (numberOfReadPoints >= numberOfPoints)
                return false;

            try
            {
                x = reader.ReadDouble();
                y = reader.ReadDouble();
                z = reader.ReadDouble();

                byte[] rgba = reader.ReadBytes(4);
                r = rgba[0];
                g = rgba[1];
                b = rgba[2];

                intensity = reader.ReadUInt16();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool ReadNextPoint(ref CloudPoint point)
        {
            if (reader == null)
                return false;

            if (numberOfReadPoints >= numberOfPoints)
                return false;

            try
            {
                point = new CloudPoint();
                point.X = reader.ReadDouble();
                point.Y = reader.ReadDouble();
                point.Z = reader.ReadDouble();

                byte[] rgba = reader.ReadBytes(4);
                point.R = rgba[0];
                point.G = rgba[1];
                point.B = rgba[2];
                point.Intensity = reader.ReadUInt16();

                numberOfReadPoints++;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override Bounds GetBounds()
        {
            return bounds;
        }

        public override ulong NumPoints()
        {
            return numberOfPoints;
        }

        #endregion
    }
}

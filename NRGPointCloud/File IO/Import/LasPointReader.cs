using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using laszip.net;

namespace NRG.Import
{
    public class ImportLas
    {
        #region Properties

        double[] tr = new double[16];
        private bool HasTransform;

        public laszip_dll LaszipMain;
        public laszip_header Header;
        public laszip_point Point;
        int ColorScale;
        double[] Coordinates = new double[3];
        ulong PointsRead = 0;

        #endregion

        #region Setup

        public ImportLas(string path)
        {
            LaszipMain = laszip_dll.laszip_create();
            bool compressed = false;
            LaszipMain.laszip_open_reader(path, ref compressed);
            Header = LaszipMain.header;
            ulong numPoints = Header.number_of_point_records;

            Point = LaszipMain.point;

            ColorScale = 1;
            for (ulong i = 0; i < 100000 && i < numPoints; i++)
            {
                LaszipMain.laszip_read_point();
                var r = Point.rgb[0];
                var g = Point.rgb[1];
                var b = Point.rgb[2];

                if (r > 255 || g > 255 || b > 255)
                {
                    ColorScale = 256;
                    break;
                }
            }

            LaszipMain.laszip_seek_point(0);
        }

        public void CloseReader()
        {
            LaszipMain.laszip_close_reader();
            LaszipMain = null;
        }

        public void Close()
        {
            LaszipMain.laszip_close_reader();
        }

        #endregion

        #region Methods

        private CloudPoint Transform(double x, double y, double z)
        {
            var p = new CloudPoint();
            if (HasTransform)
            {
                p.X = (tr[0] * x + tr[4] * y + tr[8] * z + tr[12]);
                p.Y = (tr[1] * x + tr[5] * y + tr[9] * z + tr[13]);
                p.Z = (tr[2] * x + tr[6] * y + tr[10] * z + tr[14]);
            }
            else
            {
                p.X = x;
                p.Y = y;
                p.Z = z;
            }
            return p;
        }

        public ulong NumPoints()
        {
            if (Header.version_major >= 1 && Header.version_minor >= 4)
            {
                return Header.extended_number_of_point_records;
            }
            else
                return Header.number_of_point_records;
        }

        public bool ReadPoint()
        {
            if (PointsRead < NumPoints())
            {
                LaszipMain.laszip_read_point();
                PointsRead++;
                return true;
            }
            else
                return false;
        }

        public CloudPoint GetPoint()
        {
            LaszipMain.laszip_get_coordinates(Coordinates);
            CloudPoint p = new CloudPoint(Coordinates[0], Coordinates[1], Coordinates[2]);
            var test1 = LaszipMain.point.X;
            p.Intensity = Point.intensity;

            p.R = (byte)(Point.rgb[0] / ColorScale);
            p.G = (byte)(Point.rgb[1] / ColorScale);
            p.B = (byte)(Point.rgb[2] / ColorScale);

            return p;
        }

        public void GetPoint(ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity)
        {
            LaszipMain.laszip_get_coordinates(Coordinates);
            x = Math.Round(Coordinates[0], 3);
            y = Math.Round(Coordinates[1], 3);
            z = Math.Round(Coordinates[2], 3);
            //x = Coordinates[0];
            //y = Coordinates[1];
            //z = Coordinates[2];

            r = (byte)(Point.rgb[0] / ColorScale);
            g = (byte)(Point.rgb[1] / ColorScale);
            b = (byte)(Point.rgb[2] / ColorScale);

            intensity = Point.intensity;
        }

        public void GetPoint(ref CloudPoint point)
        {
            point = new CloudPoint();
            LaszipMain.laszip_get_coordinates(Coordinates);
            point.X = Math.Round(Coordinates[0], 3);
            point.Y = Math.Round(Coordinates[1], 3);
            point.Z = Math.Round(Coordinates[2], 3);

            //point.X = Coordinates[0];
            //point.Y = Coordinates[1];
            //point.Z = Coordinates[2];

            point.R = (byte)(Point.rgb[0] / ColorScale);
            point.G = (byte)(Point.rgb[1] / ColorScale);
            point.B = (byte)(Point.rgb[2] / ColorScale);

            point.Intensity = Point.intensity;
        }

        public Bounds GetBounds()
        {
            Bounds bounds = new Bounds();
            Point3D minP = new Point3D(Header.min_x, Header.min_y, Header.min_z);
            Point3D maxP = new Point3D(Header.max_x, Header.max_y, Header.max_z);
            //bounds.Update(minP);
            //bounds.Update(maxP);
            bounds = new Bounds(minP, maxP);

            return bounds;
        }

        #endregion
    }

    public class LASPointReader : PointReader
    {
        #region Properties

        private Bounds bounds;
        private ImportLas reader;
        private List<string> files;
        private string currentFile;

        #endregion

        #region Setup

        public LASPointReader(string path)
        {
            bounds = new Bounds();
            files = new List<string>();
            //Its a directory
            if(Directory.Exists(path))
            {
                foreach(var file in Directory.GetFiles(path))
                {
                    if(file.ToUpper().EndsWith(".LAS") || file.ToUpper().EndsWith(".LAZ"))
                    {
                        files.Add(file);
                    }
                }
            }
            else if(File.Exists(path))
            {
                files.Add(path);
            }

            //Read bounds
            foreach(var file in files)
            {
                var boundryReader = new ImportLas(file);
                Bounds bounds1 = boundryReader.GetBounds();

                bounds.Update(bounds1.Min);
                bounds.Update(bounds1.Max);

                boundryReader.Close();
            }

            //Open first file
            currentFile = files.First();
            reader = new ImportLas(currentFile);
        }

        public void CloseLASPointReader()
        {
            Close();
        }

        public override void Close()
        {
            if (reader != null)
                reader.Close();

            reader = null;
            return;
        }

        #endregion

        #region Methods

        public override bool ReadNextPoint(ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity)
        {
            bool hasPoints = reader.ReadPoint();

            if (!hasPoints)
            {
                //Try to open the next file if available
                reader.Close();
                reader = null;

                if (files.Count > 1)
                {
                    files.RemoveAt(0);
                    currentFile = files.First();
                    reader = new ImportLas(currentFile);
                    hasPoints = reader.ReadPoint();
                }
            }

            if (hasPoints)
            {
                reader.GetPoint(ref x, ref y, ref z, ref r, ref g, ref b, ref intensity);
            }

            return hasPoints;
        }

        public override bool ReadNextPoint(ref CloudPoint point)
        {
            bool hasPoints = reader.ReadPoint();

            if (!hasPoints)
            {
                //Try to open the next file if available
                reader.Close();
                reader = null;

                if (files.Count > 1)
                {
                    files.RemoveAt(0);
                    currentFile = files.First();
                    reader = new ImportLas(currentFile);
                    hasPoints = reader.ReadPoint();
                }
            }

            if (hasPoints)
            {
                reader.GetPoint(ref point);
            }
            return hasPoints;
        }

        public override Bounds GetBounds()
        {
            return bounds;
        }

        public override ulong NumPoints()
        {
            return (reader.Header.version_major >= 1 && reader.Header.version_minor >= 4) ? reader.Header.extended_number_of_point_records : reader.Header.number_of_point_records;
        }

        public override Point3D GetScale()
        {
            Point3D scale = new Point3D(reader.Header.x_scale_factor, reader.Header.y_scale_factor, reader.Header.z_scale_factor);
            return scale;
        }

        #endregion
    }
}

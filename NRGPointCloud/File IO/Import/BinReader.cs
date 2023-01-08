using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;

namespace NRG.Import
{
    public class BinReader
    {
        #region Properties

        private BinaryReader reader;
        public List<Line> Lines { get; private set; }
        public List<CodedPoint> Points { get; private set; }
        public List<ZonePolygon> SelectionZones { get; private set; }
        public List<ZonePolygon> DeselectionZones { get; private set; }
        public List<CloudPoint> FilteredPoints { get; private set; }
        public string PointCloudFile { get; private set; }

        public UserDataFileVersion FileVersion;

        #endregion

        #region Setup

        public BinReader(string filename)
        {
            reader = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read));
            Lines = new List<Line>();
            Points = new List<CodedPoint>();
            SelectionZones = new List<ZonePolygon>();
            DeselectionZones = new List<ZonePolygon>();
            FilteredPoints = new List<CloudPoint>();
            PointCloudFile = "";
        }

        #endregion

        #region Methods

        public bool ReadFile()
        {
            try
            {
                //Read the first string to ensure its a nrg file
                var checkString = reader.ReadString();

                switch(checkString)
                {
                    case "NRG User Data File Version 1.0":
                        FileVersion = UserDataFileVersion.FileVersion1;
                        break;
                    case "NRG User Data File Version 1.1":
                        FileVersion = UserDataFileVersion.FileVersion1x1;
                        break;
                    default:
                        return false;
                }

                PointCloudFile = reader.ReadString();

                var finalPosition = reader.BaseStream.Length;

                while (reader.BaseStream.Position < finalPosition)
                {
                    var dataType = reader.ReadInt32();
                    switch ((DataTypes)dataType)
                    {
                        case DataTypes.Line:
                            ReadLines();
                            break;
                        case DataTypes.CodedPoint:
                            ReadPoints();
                            break;
                        case DataTypes.SelectionZone:
                            SelectionZones = ReadZones();
                            break;
                        case DataTypes.DeselectionZone:
                            DeselectionZones = ReadZones();
                            break;
                        case DataTypes.FilteredPoints:
                            ReadFilteredPoints();
                            break;
                    }
                }

                reader.Close();
                reader = null;

                return true;
            }
            catch
            {
                if (reader != null)
                    reader.Close();
                reader = null;

                return false;
            }
        }

        private void ReadLines()
        {
            var count = reader.ReadInt32();
            Lines = new List<Line>(count);

            for (int i = 0; i < count; i++)
            {
                var line = new Line();
                line.Code = reader.ReadString();
                line.IsPoly = reader.ReadBoolean();
                line.Color = System.Drawing.Color.FromArgb(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                line.Thickness = reader.ReadDouble();

                var pointCount = reader.ReadInt32();
                line.Points = new List<Point3D>(pointCount);

                for (int j = 0; j < pointCount; j++)
                {
                    var point = new Point3D();
                    point.X = reader.ReadDouble();
                    point.Y = reader.ReadDouble();
                    point.Z = reader.ReadDouble();
                    line.Points.Add(point);
                }

                Lines.Add(line);
            }
        }

        private void ReadPoints()
        {
            var count = reader.ReadInt32();
            Points = new List<CodedPoint>(count);

            for (int i = 0; i < count; i++)
            {
                var point = new CodedPoint();
                point.Code = reader.ReadString();
                point.Color = System.Drawing.Color.FromArgb(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                point.Thickness = reader.ReadDouble();
                point.X = reader.ReadDouble();
                point.Y = reader.ReadDouble();
                point.Z = reader.ReadDouble();
                Points.Add(point);
            }
        }

        private List<ZonePolygon> ReadZones()
        {
            int count = 0;
            List<ZonePolygon> zones;

            switch(FileVersion)
            {
                case UserDataFileVersion.FileVersion1x1:
                    count = reader.ReadInt32();
                    zones = new List<ZonePolygon>(count);

                    for(int i = 0; i < count; i++)
                    {
                        var zone = new ZonePolygon();
                        zone.IsDeselectionZone = reader.ReadBoolean();
                        zone.Filtered = reader.ReadBoolean();

                        var pointCount = reader.ReadInt32();
                        zone.Points = new List<Point3D>(pointCount);

                        for(int j = 0; j < pointCount; j++)
                        {
                            var point = new Point3D(reader.ReadDouble(), reader.ReadDouble());
                            zone.Points.Add(point);
                        }

                        zone.CalculateBounds(reader.ReadDouble(), reader.ReadDouble());
                        zones.Add(zone);
                    }
                    break;
                default:
                    count = reader.ReadInt32();
                    zones = new List<ZonePolygon>(count);

                    for(int i = 0; i < count; i++)
                    {
                        var zone = new ZonePolygon();
                        zone.IsDeselectionZone = reader.ReadBoolean();
                        zone.Filtered = reader.ReadBoolean();

                        var pointCount = reader.ReadInt32();
                        zone.Points = new List<Point3D>(pointCount);

                        for(int j = 0; j < pointCount; j++)
                        {
                            var point = new Point3D(reader.ReadDouble(), reader.ReadDouble());
                            zone.Points.Add(point);
                            reader.ReadDouble();    //We read this line to honor the time when polygons had a point3D
                        }
                        zones.Add(zone);
                    }
                    break;
            }

            return zones;
        }

        private void ReadFilteredPoints()
        {
            var count = reader.ReadInt32();
            FilteredPoints = new List<CloudPoint>(count);

            for (int i = 0; i < count; i++)
            {
                var point = new CloudPoint();
                point.X = reader.ReadDouble();
                point.Y = reader.ReadDouble();
                point.Z = reader.ReadDouble();
                point.R = reader.ReadByte();
                point.G = reader.ReadByte();
                point.B = reader.ReadByte();
                point.Intensity = reader.ReadUInt16();
                FilteredPoints.Add(point);
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using NRG.MathsHelpers;

namespace NRG.Services
{
    public static class OctreeInterop
    {
        #region General

        public static Bounds ChildBounds(Bounds bounds, int index)
        {
            var min = bounds.Min.Copy();
            var max = bounds.Max.Copy();

            if ((index & 0b0001) > 0)
                min.Z += bounds.Size.Z / 2;
            else
                max.Z -= bounds.Size.Z / 2;

            if ((index & 0b0010) > 0)
                min.Y += bounds.Size.Y / 2;
            else
                max.Y -= bounds.Size.Y / 2;

            if ((index & 0b0100) > 0)
                min.X += bounds.Size.X / 2;
            else
                max.X -= bounds.Size.X / 2;

            return new Bounds(min, max);
        }

        public static int NodeIndex(Bounds bounds, CloudPoint point)
        {
            int nx = (int)(2.0 * (point.X - bounds.Min.X) / bounds.Size.X);
            int ny = (int)(2.0 * (point.Y - bounds.Min.Y) / bounds.Size.Y);
            int nz = (int)(2.0 * (point.Z - bounds.Min.Z) / bounds.Size.Z);

            nx = Math.Min(nx, 1);
            ny = Math.Min(ny, 1);
            nz = Math.Min(nz, 1);

            return (nx << 2) | (ny << 1) | nz;
        }

        public static int NodeIndex(Bounds bounds, double x, double y, double z)
        {
            int nx = (int)(2.0 * (x - bounds.Min.X) / bounds.Size.X);
            int ny = (int)(2.0 * (y - bounds.Min.Y) / bounds.Size.Y);
            int nz = (int)(2.0 * (z - bounds.Min.Z) / bounds.Size.Z);

            nx = Math.Min(nx, 1);
            ny = Math.Min(ny, 1);
            nz = Math.Min(nz, 1);

            return (nx << 2) | (ny << 1) | nz;
        }

        public static string GetOutputFilter(OutPutFormat format)
        {
            string filter = "";
            switch (format)
            {
                case OutPutFormat.GPF:
                    filter = "Ground Plot Files (*.gpf) | *.gpf";
                    break;
                case OutPutFormat.LAS:
                    filter = "LIDAR Data File (*.las) | *.las";
                    break;
                case OutPutFormat.LAZ:
                    filter = "Compressed LIDAR Data File (*.laz) | *.laz";
                    break;
                case OutPutFormat.XYZ:
                    filter = "XYZ / ASCII File (*.xyz; *.txt) | *.xyz; *.txt";
                    break;
                case OutPutFormat.BINARY:
                    filter = "Binary File (*.bin) | *.bin";
                    break;
                case OutPutFormat.DXF:
                    filter = "Data Exchange Format (*.dxf) | *.dxf";
                    break;
            }
            return filter;
        }

        #endregion

        #region Data Queries

        public static bool FindClosestPoint(NRG.Models.Ray ray, Octree writer, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity)
        {
            try
            {
                double distance = double.MaxValue;
                if (writer.Root.GetClosest(ray, ref x, ref y, ref z, ref r, ref g, ref b, ref intensity, ref distance))
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool FindClosestPointUsingOffsetRange(NRG.Models.Ray ray, Octree writer, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity)
        {
            try
            {
                List<CloudPoint> pointsInOffset = new List<CloudPoint>();
                double offSetMaxRange = 0.05; //Controls the precision of point selection from the ray thats cast. Higher=less less point precision/increased chance of getting any closest point
                double distance = Double.MaxValue;
                if (writer.Root.GetClosestInOffsetRange(ray, ref x, ref y, ref z, ref r, ref g, ref b, ref intensity, ref distance, offSetMaxRange, pointsInOffset))
                {

                    double newDistFromCam = -999;
                    double lastDistFromCam = double.MaxValue;
                    
                    //Cycle through list of points within the offset-range and find closest one to the camera/ray origin
                    foreach (var p in pointsInOffset)
                    {

                        newDistFromCam = NRG.MathsHelpers.Vector.Fn3DDistance(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, p.X, p.Y, p.Z);

                        //Compare point distances from camera
                        if (newDistFromCam < lastDistFromCam)
                        {
                            x = p.X;
                            y = p.Y;
                            z = p.Z;
                            r = p.R;
                            g = p.G;
                            b = p.B;
                            intensity = p.Intensity;
                            lastDistFromCam = newDistFromCam;
                        }
                    }

                    pointsInOffset.Clear();



                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool FindClosestPoint(Ray ray, Octree writer, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity, ref BackgroundWorker worker, DoWorkEventArgs e)
        {
            try
            {
                double distance = double.MaxValue;
                if (writer.Root.GetClosest(ray, ref x, ref y, ref z, ref r, ref g, ref b, ref intensity, ref distance, ref worker, e))
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool FindClosePointFromSelection(Ray ray, List<CloudPoint> filteredPoints, List<CloudPoint> selectedPoints, List<ZonePolygon> selectionZones, List<ZonePolygon> deselectionZones, Octree writer, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity)
        {
            try
            {
                double distance = double.MaxValue;
                var unfilteredZones = new List<ZonePolygon>();
                foreach (var zone in selectionZones)
                {
                    if (!zone.Filtered)
                        unfilteredZones.Add(zone);
                }

                //Check selected points
                if(selectedPoints != null)
                {
                    foreach(var p in selectedPoints)
                    {
                        //Check distance
                        var newDist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);
                        if(newDist < distance)
                        {
                            x = p.X;
                            y = p.Y;
                            z = p.Z;
                            r = p.R;
                            g = p.G;
                            b = p.B;
                            intensity = p.Intensity;
                            distance = newDist;
                        }
                    }
                }

                //Check all filtered points
                if (filteredPoints != null)
                {
                    foreach (var p in filteredPoints)
                    {
                        //Check distance
                        var newDist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);
                        if (newDist < distance)
                        {
                            x = p.X;
                            y = p.Y;
                            z = p.Z;
                            r = p.R;
                            g = p.G;
                            b = p.B;
                            intensity = p.Intensity;
                            distance = newDist;
                        }
                    }
                }

                //Now check the model
                if (unfilteredZones.Count <= 0)
                {
                    if (distance == double.MaxValue)
                        return false;
                    else
                        return true;
                }

                if (writer.Root.GetClosestPointFromSelection(ray, unfilteredZones, deselectionZones, ref x, ref y, ref z, ref r, ref g, ref b, ref intensity, ref distance))
                {
                    if (distance == float.PositiveInfinity)
                        return false;
                    else
                        return true;
                }
                else
                {
                    if (distance == float.PositiveInfinity)
                        return false;
                    else
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool FindClosePointFromSelection(Ray ray, List<CloudPoint> filteredPoints, List<ZonePolygon> selectionZones, List<ZonePolygon> deselectionZones, Octree writer, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity, ref BackgroundWorker worker, DoWorkEventArgs e)
        {
            try
            {
                double distance = double.MaxValue;
                var unfilteredZones = new List<ZonePolygon>();
                foreach(var zone in selectionZones)
                {
                    if (!zone.Filtered)
                        unfilteredZones.Add(zone);
                }

                //Check all filtered points
                if(filteredPoints != null)
                {
                    foreach(var p in filteredPoints)
                    {
                        //Check distance
                        var newDist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);
                        if(newDist < distance)
                        {
                            x = p.X;
                            y = p.Y;
                            z = p.Z;
                            r = p.R;
                            g = p.G;
                            b = p.B;
                            intensity = p.Intensity;
                            distance = newDist;
                        }
                    }
                }

                //Now check the model
                if(unfilteredZones.Count <= 0)
                {
                    if (distance == double.MaxValue)
                        return false;
                    else
                        return true;
                }

                if(writer.Root.GetClosestPointFromSelection(ray, unfilteredZones, deselectionZones, ref x, ref y, ref z, ref r, ref g, ref b, ref intensity, ref distance, ref worker, e))
                {
                    if (distance == float.PositiveInfinity)
                        return false;
                    else
                        return true;
                        
                }
                else
                {
                    if (distance == float.PositiveInfinity)
                        return false;
                    else
                        return true;
                }

            }
            catch
            {
                return false;
            }
        }

        public static void GetPointsFromFilter(List<CloudPoint> filteredList, ref CrossSection section)
        {
            foreach (var p in filteredList)
            {
                if (section.ClippingBox.IsInside3D(p.X, p.Y, p.Z))
                {
                    //Add to the points list
                    double newX = 0, newY = 0;
                    Trig.DistanceAndOffsetFromLine(section.SectionCenter.X, section.SectionCenter.Y, section.Bearing, p.X, p.Y, ref newX, ref newY);
                    section.OriginalPoints.Add(new SectionPoint(newX, p.Z, newY, p.R, p.G, p.B));
                    section.SectionBounds.Update(newX, p.Z, newY);
                }
            }
        }

        #endregion
    }
}

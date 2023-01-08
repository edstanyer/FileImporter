using System;
using System.Drawing.Design;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using NRG.Services;
using NRG.MathsHelpers;
using NRG.Models.Rendering;
using TriangleNet.Geometry;
using System.Security.Policy;
using TriangleNet;
using TriangleNet.Meshing.Data;
using TriangleNet.Tools;
using TriangleNet.Topology;
using System.IO;
using netDxf;
using System.Drawing;
using System.Windows.Forms;
using NRG.Settings;
using System.Diagnostics;


using TriangleNet.Geometry;
using TriangleNet.Meshing.Algorithm;
using TriangleNet.Rendering.Text;


//Add some comment

namespace NRG.Models
{
    [TypeConverter(typeof(LineConverter))]
    public class Line
    {
        #region Properties

        private int id;

        [Category("Line"), Description("The index ID for the line")]
        public int ID
        {
            get { return id; }
        }

        [Category("Line"), Description("The line code used for the line")]
        public string Code { get; set; }

        public string startLabel = "";

        public string StartLineCode
        {
            get
            {
                if (startLabel == "")
                    return Code;
                else
                    return startLabel + " " + Code;
            }
        }

        [Category("Line"), Description("Whether the line will close")]
        public bool IsPoly { get; set; }

        [Category("Coordinates"), Description("A list of coordinate points for the line")]
        public List<Point3D> Points { get; set; }

        [Category("Coordinates"), Description("The number of coordinate points for the line")]
        public int PointCount
        {
            get
            {
                if (Points == null)
                    return 0;
                else
                    return Points.Count;
            }
        }

        public System.Drawing.Color Color = System.Drawing.Color.Black;

        [Category("Color"), Description("The color of the line")]
        public System.Drawing.Color LineColor
        {
            get { return Color; }
        }

        [Category("Line"), Description("The thickness of the line")]
        public double Thickness { get; set; }

        #endregion

        #region Setup

        public Line()
        {
            this.id = 0;
            this.IsPoly = false;
            this.Code = "";
            this.Points = new List<Point3D>();
            this.Thickness = 0.2;
        }

        public Line(string code, int id, bool isPoly = false, int r = 0, int g = 0, int b = 0)
        {
            this.Code = code;
            this.id = id;
            this.IsPoly = isPoly;
            this.Points = new List<Point3D>();
            this.Color = System.Drawing.Color.FromArgb(r, g, b);
            this.Thickness = 0.2;
        }

        public Line(int id)
        {
            this.id = id;
            this.IsPoly = false;
            this.Code = "";
            this.Points = new List<Point3D>();
            this.Thickness = 0.2;
        }

        #endregion

        #region Methods

        public Line Copy()
        {
            var newLine = new Line(this.ID)
            {
                Code = this.Code,
                IsPoly = this.IsPoly,
                Color = this.Color,
                startLabel = this.startLabel,
                Thickness = this.Thickness,
                id = this.id,
                Points = new List<Point3D>()
            };

            foreach (var point in this.Points)
            {
                newLine.Points.Add(point.Copy());
            }
            return newLine;
        }

        public void SetID(int newId)
        {
            this.id = newId;
        }

        #endregion
    }

    public class Polygon
    {
        //A raw polygon object is essentially a method store for children of the object to use
        #region Properties

        #endregion

        #region Setup

        /// <summary>
        /// Generates a new <see cref="Polygon"/> object
        /// </summary>
        public Polygon()
        {

        }

        #endregion

        #region Methods

        private List<Point3D> Poly2Dto3DConverter(List<Point2D> Poly2DPoints)
        {
            List<Point3D> Poly3DPoints = new List<Point3D>();
            
            foreach (Point2D p2 in Poly2DPoints) 
            {
                Point3D pt3d = new Point3D(p2.X, p2.Y);
                Poly3DPoints.Add(pt3d); 
            }
            return Poly3DPoints;
        }

        /// <summary>
        /// Determines if a given point falls within the 2D footprint of the polygon
        /// </summary>
        /// <param name="point">The <see cref="Point2D"/> to check</param>
        /// <returns>Returns true if the point is within the polygon</returns>
        public bool InPoly2D(Point2D point, List<Point3D> PolyPoints)
        {
            return InPoly2D(point.X, point.Y, PolyPoints);
        }

        /// <summary>
        /// Determines if a given point falls within the 2D footprint of the polygon
        /// </summary>
        /// <param name="point">The <see cref="Point2D"/> to check</param>
        /// <returns>Returns true if the point is within the polygon</returns>
        public bool InPoly2D(Point2D point, List<Point2D> PolyPoints)
        {
            return InPoly2D(point.X, point.Y, Poly2Dto3DConverter(PolyPoints));
        }

        /// <summary>
        /// Determines if a given point falls within the 2D footprint of the polygon
        /// </summary>
        /// <param name="x">A <see cref="double"/> representing the X value of the point to check</param>
        /// <param name="y">A <see cref="double"/> representing the Y value of the point to check</param>
        /// <returns>Returns true if the point is within the polygon</returns>
        public bool InPoly2D(double x, double y, List<Point3D> PolyPoints)
        {
            bool isInPoly = false;
            int il = 0;
            int i = 0;
            //start and endpoints of a line seg;
            double sx = 0;
            double sy = 0;
            double ex = 0;
            double ey = 0;
            int crossings = 0;

            int numLasso = PolyPoints.Count;

            //if we have less than two points we don't have a polygon
            if (numLasso < 2)
                return false;

            crossings = 0;
            //go through each line segment in the polygon
            for (i = 0; i < numLasso; i++)
            {
                //get the start and end points of the line relative to origin x,y
                il = i - 1;
                if (il == -1)
                    il = numLasso - 1;
                sx = PolyPoints[il].X - x;
                sy = PolyPoints[il].Y - y;
                ex = PolyPoints[i].X - x;
                ey = PolyPoints[i].Y - y;
                if (sx == 0 && sy == 0)
                    return true;

                if (ex == 0 && ey == 0)
                    return true;

                //check if the line segment sx, sy - ex, ey straddles the x axis
                if (ey > 0 && sy <= 0)
                {
                    //calculate the intersetion with x axis
                    double ix = (ex * sy - sx * ey) / (double)(sy - ey);
                    //if this intersection is strictly +ve then we have a crossing
                    if (ix > 0)
                        crossings += 1;
                }

                if (sy > 0 && ey <= 0)
                {
                    //calculate the intersection with the x axis
                    double ix = (ex * sy - sx * ey) / (double)(sy - ey);
                    //if this intersection is strictly +ve then we have a crossing
                    if (ix > 0)
                        crossings += 1;
                }
            }

            //if there is an odd number of crossings then the point is in the poly
            if ((crossings % 2) == 1)
                isInPoly = true;
            else
                isInPoly = false;

            return isInPoly;
        }

        /// <summary>
        /// Determines if a given point falls within the 2D footprint of the polygon
        /// </summary>
        /// <param name="x">A <see cref="double"/> representing the X value of the point to check</param>
        /// <param name="y">A <see cref="double"/> representing the Y value of the point to check</param>
        /// <returns>Returns true if the point is within the polygon</returns>
        public bool InPoly2D(double x, double y, List<Point2D> PolyPoints)
        {
            return InPoly2D(x, y, Poly2Dto3DConverter(PolyPoints));
        }

        /// <summary>
        /// Calculates the area of the <see cref="Polygon"/>
        /// </summary>
        /// <returns>Returns a <see cref="double"/> representing the area of the <see cref="Polygon"/></returns>
        public double CalcArea(List<Point3D> PolyPoints)
        {
            double area = 0;
            Point2D segStart, segEnd;

            //If there are less than 3 points we don't have a valid polygon
            if (PolyPoints.Count < 3)
                return 0;

            //Add the area from each line segment
            for (int i = 0; i < PolyPoints.Count - 1; i++)
            {
                segStart = PolyPoints[i];
                segEnd = PolyPoints[i + 1];

                area += (segEnd.X - segStart.X) * (segEnd.Y - segStart.Y);
            }

            //Add the last segment
            segStart = PolyPoints.Last();
            segEnd = PolyPoints.First();
            area += (segEnd.X - segStart.X) * (segEnd.Y - segStart.Y);

            return area;
        }

        /// <summary>
        /// Calculates the area of the <see cref="Polygon"/>
        /// </summary>
        /// <returns>Returns a <see cref="double"/> representing the area of the <see cref="Polygon"/></returns>
        public double CalcArea(List<Point2D> PolyPoints)
        {
            return CalcArea(Poly2Dto3DConverter(PolyPoints));
        }


        /// <summary>
        /// Calculates the resulting intersection lines between a <see cref="Polygon"/> and a list of lines
        /// </summary>
        /// <param name="originalLines">A List of <see cref="Point2D"/> object representing lines as pairs of 2 points</param>
        /// <returns>Returns a list of <see cref="Point2D"/> object representing the resulting intersection lines as pairs of 2 points</returns>
        public List<Point2D> CalcIntersectLines(List<Point2D> originalLines, List<Point3D> PolyPoints)
        {
            var intersectedLines = new List<Point2D>();

            //Check for intersections for each line between each line segment of the polygon

            //Where an intersection is found add it as the start line , and find the next intersection to add as the end line
            for (int i = 0; i < originalLines.Count - 1; i += 2)
            {
                var lineStart = originalLines[i];
                var lineEnd = originalLines[i + 1];
                double ipX = 0, ipY = 0;

                var intersections = new List<Point2D>();

                //Check for intersections to next point
                for (int j = 0; j < PolyPoints.Count - 1; j++)
                {
                    var segStart = PolyPoints[j];
                    var segEnd = PolyPoints[j + 1];

                    if (Intersection.IntersectLines(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, segStart.X, segStart.Y, segEnd.X, segEnd.Y, ref ipX, ref ipY))
                        intersections.Add(new Point2D(ipX, ipY));
                }

                if (intersections.Count % 2 != 0)
                    continue;

                for (int j = 0; j < intersections.Count;)
                {
                    int closestIndex = 0;
                    double distanceFrom = double.MaxValue;

                    for (int k = 0; k < intersections.Count; k++)
                    {
                        var d = Vector.FnDistance(lineStart.X, lineStart.Y, intersections[k].X, intersections[k].Y);
                        if (d < distanceFrom)
                        {
                            distanceFrom = d;
                            closestIndex = k;
                        }
                    }

                    intersectedLines.Add(intersections[closestIndex]);

                    //Make sure we remove this one first or else index will change
                    intersections.RemoveAt(closestIndex);
                }
            }

            return intersectedLines;
        }

        /// <summary>
        /// Calculates the resulting intersection lines between a <see cref="Polygon"/> and a list of lines
        /// </summary>
        /// <param name="originalLines">A List of <see cref="Point2D"/> object representing lines as pairs of 2 points</param>
        /// <returns>Returns a list of <see cref="Point2D"/> object representing the resulting intersection lines as pairs of 2 points</returns>
        public List<Point2D> CalcIntersectLines(List<Point2D> originalLines, List<Point2D> PolyPoints)
        {
            return CalcIntersectLines(originalLines, Poly2Dto3DConverter(PolyPoints));
        }


        /// <summary>
        /// Generates a list of <see cref="Point3D"/> objects representing triangle vertices for the <see cref="Polygon"/>
        /// </summary>
        /// <returns>Returns a list of triangle vertices as <see cref="Point2D"/> objects</returns>
        public List<Point2D> CalcSolidFillTriangles(List<Point3D> PolyPoints)
        {
            var trianglePoints = new List<Point2D>();

            if (PolyPoints.Count <= 2)
                return trianglePoints;

            if (PolyPoints.Count == 3)
            {
                trianglePoints.Add(PolyPoints[0]);
                trianglePoints.Add(PolyPoints[1]);
                trianglePoints.Add(PolyPoints[2]);
                return trianglePoints;
            }

            var poly = new TriangleNet.Geometry.Polygon(PolyPoints.Count);

            foreach (var point in PolyPoints)
                poly.Add(new TriangleNet.Geometry.Vertex(point.X, point.Y));

            var constraintOptions = new TriangleNet.Meshing.ConstraintOptions()
            {
                ConformingDelaunay = true,
                Convex = false,
            };

            var qualityOptions = new TriangleNet.Meshing.QualityOptions()
            {
                MaximumAngle = 360,
                MinimumAngle = 0,
            };

            var mesh = poly.Triangulate(constraintOptions, qualityOptions);
            foreach (var triangle in mesh.Triangles)
            {
                var point1 = PolyPoints[triangle.GetVertexID(0)];
                if (!InPoly2D(point1.X, point1.Y, PolyPoints))
                    continue;

                var point2 = PolyPoints[triangle.GetVertexID(1)];
                if (!InPoly2D(point2.X, point2.Y, PolyPoints))
                    continue;

                var point3 = PolyPoints[triangle.GetVertexID(2)];
                if (!InPoly2D(point3.X, point3.Y, PolyPoints))
                    continue;

                //If the center of the triange is not in the polygon its an invalid triangle so go to next
                var centroid = new Point2D((point1.X + point2.X + point3.X) / 3, (point1.Y + point2.Y + point3.Y) / 3);
                if (!InPoly2D(centroid.X, centroid.Y, PolyPoints))
                    continue;

                trianglePoints.Add(point1);
                trianglePoints.Add(point2);
                trianglePoints.Add(point3);
            }

            return trianglePoints;
        }

        /// <summary>
        /// Generates a list of <see cref="Point3D"/> objects representing triangle vertices for the <see cref="Polygon"/>
        /// </summary>
        /// <returns>Returns a list of triangle vertices as <see cref="Point2D"/> objects</returns>
        public List<Point2D> CalcSolidFillTriangles(List<Point2D> PolyPoints)
        {
            return CalcSolidFillTriangles(Poly2Dto3DConverter(PolyPoints));
        }

        #endregion
    }

    public class Basic2DPolygon : Polygon
    {
        /// <summary>
        /// A List of <see cref="Point3D"/> objects representing the vertices of the <see cref="Polygon"/>
        /// </summary>
        [Category("Coordinates"), Description("A list of coordinate points for the polygon")]
        public List<Point2D> Points { get; set; }

        public Basic2DPolygon()
        {
            Points = new List<Point2D>();
        }

    }
    [TypeConverter(typeof(ZonePolygon))]
    public class ZonePolygon : Polygon
    {
        #region Properties

        /// <summary>
        /// A List of <see cref="Point3D"/> objects representing the vertices of the <see cref="Polygon"/>
        /// </summary>
        [Category("Coordinates"), Description("A list of coordinate points for the polygon")]
        public List<Point3D> Points { get; set; }



        /// <summary>
        /// Represents the ID of the <see cref="ZonePolygon"/> in its associated list
        /// </summary>
        [Category("Polygon"), Description("The index ID for the polygon")]
        public int ID { get; private set; }

        /// <summary>
        /// Represents whether the <see cref="ZonePolygon"/> is a selection or deselection zone
        /// </summary>
        [Category("Polgon"), Description("Whether the polygon is a deselection zone or not")]
        public bool IsDeselectionZone { get; set; }

        /// <summary>
        /// Represents the number of <see cref="Point2D"/> objects that make up the <see cref="ZonePolygon"/>
        /// </summary>
        [Category("Coordinates"), Description("The number of coordinate points for the polygon")]
        public int PointCount
        {
            get
            {
                if (Points == null)
                    return 0;

                return Points.Count;
            }
        }

        /// <summary>
        /// Represents whether the duplicated points have been removed from the <see cref="ZonePolygon"/>
        /// </summary>
        public bool RemovedDuplicated { get; set; }

        /// <summary>
        /// Represents whether the <see cref="ZonePolygon"/> has been filtered or not
        /// </summary>
        public bool Filtered { get; set; }

        /// <summary>
        /// A <see cref="NRG.Models.Bounds"/> object representing the 3D extents of the <see cref="ZonePolygon"/>
        /// </summary>
        public Bounds Bounds { get; set; }

        #endregion

        #region Setup

        /// <summary>
        /// Generates a new <see cref="ZonePolygon"/> object
        /// </summary>
        public ZonePolygon()
        {
            ID = 0;
            IsDeselectionZone = false;
            Points = new List<Point3D>();
            RemovedDuplicated = false;
            Filtered = false;
            Bounds = new Bounds();
        }

        /// <summary>
        /// Generates a new <see cref="ZonePolygon"/> with a given id
        /// </summary>
        /// <param name="id">An <see cref="int"/> to represent the ID of the <see cref="ZonePolygon"/></param>
        /// <param name="isDeselectionZone">A <see cref="bool"/> to represent whether or not the <see cref="ZonePolygon"/> is a deselection zone</param>
        public ZonePolygon(int id, bool isDeselectionZone = false)
        {
            ID = id;
            IsDeselectionZone = isDeselectionZone;
            Points = new List<Point3D>();
            RemovedDuplicated = false;
            Filtered = false;
            Bounds = new Bounds();
        }

        /// <summary>
        /// Copies the current <see cref="ZonePolygon"/> into a new object
        /// </summary>
        /// <returns>Returns a new <see cref="ZonePolygon"/> containing a dereferenced copy of its properties</returns>
        public new ZonePolygon Copy()
        {
            return new ZonePolygon()
            {
                Points = this.Points.ToList(),
                ID = this.ID,
                IsDeselectionZone = this.IsDeselectionZone,
                RemovedDuplicated = this.RemovedDuplicated,
                Filtered = this.Filtered,
                Bounds = this.Bounds.Copy()
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the ID of the <see cref="ZonePolygon"/>
        /// </summary>
        /// <param name="newID">An <see cref="int"/> representing the ID to set</param>
        public void SetID(int newID)
        {
            ID = newID;
        }

        /// <summary>
        /// Creates a set of <see cref="NRG.Models.Bounds"/> representing the <see cref="ZonePolygon"/>
        /// </summary>
        /// <param name="modelBounds">A <see cref="NRG.Models.Bounds"/> object representing the Min and Max Z value to use for the <see cref="ZonePolygon"/></param>
        public void CalculateBounds(Bounds modelBounds)
        {
            CalculateBounds(modelBounds.Min.Z, modelBounds.Max.Z);
        }

        /// <summary>
        /// Creates a set of <see cref="NRG.Models.Bounds"/> representing the <see cref="ZonePolygon"/>
        /// </summary>
        /// <param name="minZ">A <see cref="double"/> representing the minimum Z value to use for the <see cref="ZonePolygon"/></param>
        /// <param name="maxZ">A <see cref="double"/> representing the maximum Z value to use for the <see cref="ZonePolygon"/></param>
        public void CalculateBounds(double minZ, double maxZ)
        {
            Bounds = new Bounds();

            //If there are less than 3 points the polygon is invalid and the bounds cannot exist
            if (Points.Count <= 2)
                return;

            //Search through all points and remove duplicate (sequential) points
            for (int i = 1; i < Points.Count;)
            {
                var curPoint = Points[i];
                var lastPoint = Points[i - 1];

                if (curPoint.X == lastPoint.X && curPoint.Y == lastPoint.Y)
                    Points.Remove(curPoint);
                else
                    i++;
            }

            foreach (var point in Points)
                Bounds.Update(point);

            //Set the min and max Z value of the bounds
            RemovedDuplicated = true;
            Bounds.Min.Z = minZ;
            Bounds.Max.Z = maxZ;
        }

        public bool IsInside(double x, double y, double z)
        {
            if (Bounds?.IsInside(x, y, z) ?? false && InPoly2D(x, y, Points))
            {
                return true;
            }

            return false;
        }

        public bool SaveToGPF(string filename)
        {
            if (filename == "" || filename == null)
            {
                return false;
            }


            DTM mod = new DTM();

            //mod.

            return true;
        }

        #endregion
    }

    public class ClearancePolygon : Polygon
    {
        #region Properties

        /// <summary>
        /// A List of <see cref="Point2D"/> objects representing the vertices of the <see cref="Polygon"/>
        /// </summary>
        [Category("Coordinates"), Description("A list of coordinate points for the polygon")]
        public List<Point2D> Points { get; set; }


        /// <summary>
        /// Represents whether or not the <see cref="ClearancePolygon"/> is in Overbreak or Underbreak
        /// </summary>
        public bool IsOverBreak { get; set; }

        #endregion

        #region Setup

        /// <summary>
        /// Generates a new <see cref="ClearancePolygon"/> object
        /// </summary>
        /// <param name="isOverBreak">A <see cref="bool"/> representing whether or not the <see cref="ClearancePolygon"/> is in Overbreak or Underbreak</param>
        public ClearancePolygon(bool isOverBreak)
        {
            IsOverBreak = isOverBreak;
            Points = new List<Point2D>();
        }

        /// <summary>
        /// Copies the current <see cref="ClearancePolygon"/> into a new object
        /// </summary>
        /// <returns>Returns a ne w<see cref="ClearancePolygon"/> containing a dereferenced copy of its properties</returns>
        public new ClearancePolygon Copy()
        {
            return new ClearancePolygon(IsOverBreak)
            {
                Points = this.Points.ToList()
            };
        }

        #endregion
    }

    [TypeConverter(typeof(Point3DConverter))]
    [Serializable]
    public class Point3D : Point2D
    {
        #region Properties

        [Category("Point"), Description("The z value of the point")]
        private double _z { get; set; }
        public double Z { get { return _z; } set { _z = value; Moved = true; } }

        #endregion

        #region Setup

        public Point3D()
        {

        }

        public Point3D(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Point3D(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public Point3D(Point2D point, double z)
        {
            this.X = point.X;
            this.Y = point.Y;
            this.Z = z;
        }

        public Point3D(Point3D point, string label)
        {
            this.X = point.X;
            this.Y = point.Y;
            this.Z = point.Z;
            this.SectionLabel = label;
        }

        #endregion

        #region Operators

        public static Point3D operator -(Point3D left, Point3D right)
        {
            return new Point3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static Point3D operator -(Point3D left, double right)
        {
            return new Point3D(left.X - right, left.Y - right, left.Z - right);
        }

        public static Point3D operator +(Point3D left, Point3D right)
        {
            return new Point3D(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static Point3D operator +(Point3D left, double right)
        {
            return new Point3D(left.X + right, left.Y + right, left.Z + right);
        }

        public static Point3D operator *(Point3D left, double right)
        {
            return new Point3D(left.X * right, left.Y * right, left.Z * right);
        }

        public static Point3D operator *(double left, Point3D right)
        {
            return new Point3D(left * right.X, left * right.Y, left * right.Z);
        }

        public static Point3D operator /(Point3D left, double right)
        {
            return new Point3D(left.X / right, left.Y / right, left.Z / right);
        }

        #endregion

        #region Methods


        public DTMPoint ToDTMPoint()
        {
            DTMPoint pt = new DTMPoint();
            pt.X = this.X;
            pt.Y = this.Y;
            pt.Z = this.Z;

            return pt;
        }

        public override double Length()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public override double SquaredLength()
        {
            return X * X + Y * Y + Z * Z;
        }

        public double DistanceTo(Point3D point)
        {
            //return (this - point).Length();
            return NRG.MathsHelpers.Vector.FnDistance(this, point);
        }

        public double Distance3DTo(Point3D point)
        {
            return NRG.MathsHelpers.Vector.Fn3DDistance(this.X, this.Y, this.Z, point.X, point.Y, point.Z);
        }

        public double SquaredDistanceTo(Point3D point)
        {
            return (this - point).SquaredLength();
        }

        public override double MaxValue()
        {
            return Math.Max(X, Math.Max(Y, Z));
        }

        public override double MinValue()
        {
            return Math.Min(X, Math.Min(Y, Z));
        }

        public Point2D ToPoint2D()
        {
            return new Point2D()
            {
                X = this.X,
                Y = this.Y
            };
        }

        public new Point3D Copy()
        {
            return new Point3D()
            {
                X = this.X,
                Y = this.Y,
                Z = this.Z

            };
        }

        public void ShiftBy(double x = 0, double y = 0, double z = 0)
        {
            X += x;
            Y += y;
            Z += z;
        }

        #endregion
    }

    [Serializable]
    [TypeConverter(typeof(Point2DConverter))]
    public class Point2D
    {
        #region Properties
        public bool Moved = true;

        [Category("Point"), Description("The x value of the point")]
        private double _x { get; set; }
        public double X { get { return _x; } set { _x = value; Moved = true; } }

        [Category("Point"), Description("The y value of the point")]
        private double _y { get; set; }
        public double Y { get { return _y; } set { _y = value; Moved = true; } }


        [Category("Point"), Description("The r value of the point colour")]
        public byte R { get; set; }

        [Category("Point"), Description("The g value of the point colour")]
        public byte G { get; set; }

        [Category("Point"), Description("The b value of the point colour")]
        public byte B { get; set; }

        [Category("Point"), Description("The label value of the point")]
        public string SectionLabel { get; set; }

        #endregion

        #region Setup

        public Point2D()
        {

        }

        public Point2D(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public Point2D(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            //this.Z = z; //Added by TN: 14.04.2021 removed by TN 16.06.22
        }

        #endregion

        #region Operators

        public static Point2D operator -(Point2D left, Point2D right)
        {
            return new Point2D(left.X - right.X, left.Y - right.Y);
        }

        public static Point2D operator -(Point2D left, double right)
        {
            return new Point2D(left.X - right, left.Y - right);
        }

        public static Point2D operator +(Point2D left, Point2D right)
        {
            return new Point2D(left.X + right.X, left.Y + right.Y);
        }

        public static Point2D operator +(Point2D left, double right)
        {
            return new Point2D(left.X + right, left.Y + right);
        }

        public static Point2D operator *(Point2D left, double right)
        {
            return new Point2D(left.X * right, left.Y * right);
        }

        public static Point2D operator *(double left, Point2D right)
        {
            return new Point2D(left * right.X, left * right.Y);
        }

        public static Point2D operator /(Point2D left, double right)
        {
            return new Point2D(left.X / right, left.Y / right);
        }

        #endregion

        #region Methods

        public virtual double Length()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        public virtual double SquaredLength()
        {
            return X * X + Y * Y;
        }

        public double DistanceTo(Point2D point)
        {
            //return (this - point).Length();

            return MathsHelpers.Vector.FnDistance(this, point);
        }

        public double DistanceTo(double x, double y)
        {
            return MathsHelpers.Vector.FnDistance(this.X, this.Y, x, y);
        }

        public double SquaredDistanceTo(Point2D point)
        {
            return (this - point).SquaredLength();
        }

        public virtual double MaxValue()
        {
            return Math.Max(X, Y);
        }

        public virtual double MinValue()
        {
            return Math.Min(X, Y);
        }

        public Point2D Copy()
        {
            return new Point2D()
            {
                X = this.X,
                Y = this.Y
            };
        }

        #endregion
    }

    [TypeConverter(typeof(CodedPointConverter))]
    public class CodedPoint : Point3D
    {
        #region Properties

        [Category("Style"), Description("The thickness of the point")]
        public double Thickness { get; set; }

        public System.Drawing.Color Color = System.Drawing.Color.Black;

        [Category("Color"), Description("The color of the point")]
        public System.Drawing.Color PointColor
        {
            get { return Color; }
        }

        [Category("Code"), Description("The assigned point code")]
        public string Code { get; set; }

        #endregion

        #region Setup

        public CodedPoint()
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
            this.Code = "";
            this.Thickness = 0.2f;
        }

        public CodedPoint(double x, double y, double z, byte r = 0, byte g = 0, byte b = 0, string code = "", float thickness = 0.2f)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Code = code;
            this.Thickness = thickness;
        }

        #endregion

        #region Methods

        public new CodedPoint Copy()
        {
            return new CodedPoint()
            {
                X = this.X,
                Y = this.Y,
                Z = this.Z,
                Color = this.Color,
                Code = this.Code,
                Thickness = this.Thickness,
                id = this.id
            };
        }

        public int id;

        public int ID
        {
            get { return id; }
        }

        public void SetID(int id)
        {
            this.id = id;
        }

        #endregion
    }

    public class SectionPoint : Point3D
    {
        #region Properties

        public double Brg;
        public ushort intensity;

        #endregion

        #region Setup

        public SectionPoint()
        {
            X = 0;
            Y = 0;
            Z = 0;
            Brg = 0;
            R = 0;
            G = 0;
            B = 0;
        }

        public SectionPoint(double x, double y, double z, byte r, byte g, byte b)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            Brg = 0;
            this.R = r;
            this.G = g;
            this.B = b;
            this.intensity = 0;
        }

        public SectionPoint(double x, double y, double z, byte r, byte g, byte b, ushort intensity)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            Brg = 0;
            this.R = r;
            this.G = g;
            this.B = b;
            this.intensity = intensity;
        }

        #endregion

        #region Methods

        public new SectionPoint Copy()
        {
            return new SectionPoint()
            {
                X = this.X,
                Y = this.Y,
                Z = this.Z,
                Brg = this.Brg,
                R = this.R,
                G = this.G,
                B = this.B
            };
        }

        #endregion
    }

    public class Ray
    {
        #region Properties

        public Point3D Origin = new Point3D();
        public Point3D End = new Point3D();
        public Point3D Direction = new Point3D();

        #endregion

        #region Setup

        public Ray(Point3D origin, Point3D direction, Point3D end)
        {
            this.Origin = origin;
            this.Direction = direction;
            this.End = end;
        }

        public Ray()
        {

        }

        #endregion
    }

    [Serializable]
    public class CloudPoint : Point3D
    {
        #region Properties

        //public byte R;
        //public byte G;  Removed by TN: 02.08.21. RGB values are now in Poind2D so they can be used in any point type.
        //public byte B;
        public ushort Intensity = 0;

        #endregion

        #region Setup

        public CloudPoint()
        {

        }

        public CloudPoint(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public CloudPoint(double x, double y, double z, byte r, byte g, byte b)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public CloudPoint(double x, double y, double z, byte r, byte g, byte b, ushort intensity)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.R = r;
            this.G = g;
            this.B = b;
            this.Intensity = intensity;
        }

        #endregion

        #region Methods

        public new CloudPoint Copy()
        {
            return new CloudPoint()
            {
                X = this.X,
                Y = this.Y,
                Z = this.Z,
                R = this.R,
                G = this.G,
                B = this.B,
                Intensity = this.Intensity
            };

        }

        #endregion
    }

    [Serializable]
    public class Bounds
    {
        #region Properties

        public Point3D Min;
        public Point3D Max;
        public Point3D Size
        {
            get { return Max - Min; }
        }

        public Point3D Center
        {
            get { return (Max + Min) / 2; }
        }

        public bool IsValid
        {
            get
            {
                if (Min == null || Max == null)
                    return false;

                if (Min.X == double.MaxValue || Min.Y == double.MaxValue)
                    return false;

                if (Max.X == double.MinValue || Max.Y == double.MaxValue)
                    return false;

                return true;
            }
        }

        #endregion

        #region Setup

        public Bounds()
        {
            Min = new Point3D(double.MaxValue, double.MaxValue, double.MaxValue);
            Max = new Point3D(double.MinValue, double.MinValue, double.MinValue);
        }

        public Bounds(Point3D min, Point3D max)
        {
            Min = min.Copy();
            Max = max.Copy();
        }

        public Bounds(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            Min = new Point3D(minX, minY, minZ);
            Max = new Point3D(maxX, maxY, maxZ);
        }

        #endregion

        #region Methods

        public Bounds Copy()
        {
            var newBounds = new Bounds();

            if (this.Min != null)
                newBounds.Min = this.Min.Copy();
            if (this.Max != null)
                newBounds.Max = this.Max.Copy();

            return newBounds;
        }

        public bool IsInside(Point3D point)
        {
            if (Min.X <= point.X && point.X <= Max.X)
                if (Min.Y <= point.Y && point.Y <= Max.Y)
                    if (Min.Z <= point.Z && point.Z <= Max.Z)
                        return true;

            return false;
        }

        public bool IsInside(double x, double y, double z)
        {
            if (Min.X <= x && x <= Max.X)
                if (Min.Y <= y && y <= Max.Y)
                    if (Min.Z <= z && z <= Max.Z)
                        return true;
            return false;
        }

        public bool IsInside2D(double x, double y, double z)
        {
            if (Min.X <= x && x <= Max.X)
                if (Min.Y <= y && y <= Max.Y)
                    return true;

            return false;
        }

        public bool IsInside2D(Point2D point)
        {
            if (Min.X <= point.X && point.X <= Max.X)
                if (Min.Y <= point.Y && point.Y <= Max.Y)
                    return true;

            return false;
        }

        public bool IsInside2D(Point3D point)
        {
            if (Min.X <= point.X && point.X <= Max.X)
                if (Min.Y <= point.Y && point.Y <= Max.Y)
                    return true;

            return false;
        }

        public bool Intersects2D(Bounds b)
        {
            return (this.Min.X <= b.Max.X && this.Max.X >= b.Min.X) && (this.Min.Y <= b.Max.Y && this.Max.Y >= b.Min.Y);
        }

        public bool Intersects3D(Bounds b)
        {
            return (this.Min.X <= b.Max.X && this.Max.X >= b.Min.X) && (this.Min.Y <= b.Max.Y && this.Max.Y >= b.Min.Y) && (this.Min.Z <= b.Max.Z && this.Max.Z >= b.Min.Z);
        }

        public bool Intersects3D(Point3D min, Point3D max)
        {
            return (this.Min.X <= max.X && this.Max.X >= min.X) && (this.Min.Y <= max.Y && this.Max.Y >= min.Y) && (this.Min.Z <= max.Z && this.Max.Z >= min.Z);
        }

        public bool Update(Point3D point)
        {
            bool updated = false;

            if (point.X != double.MaxValue && point.X != double.MinValue)
            {
                if (point.X < Min.X)
                {
                    Min.X = point.X;
                    updated = true;
                }
                if (point.X > Max.X)
                {
                    Max.X = point.X;
                    updated = true;
                }
            }

            if (point.Y != double.MaxValue && point.Y != double.MinValue)
            {
                if (point.Y < Min.Y)
                {
                    Min.Y = point.Y;
                    updated = true;
                }
                if (point.Y > Max.Y)
                {
                    Max.Y = point.Y;
                    updated = true;
                }
            }

            if (point.Z != double.MaxValue && point.Z != double.MinValue)
            {
                if (point.Z != -999)
                {
                    if (point.Z < Min.Z)
                    {
                        Min.Z = point.Z;
                        updated = true;
                    }
                    if (point.Z > Max.Z)
                    {
                        Max.Z = point.Z;
                        updated = true;
                    }
                }
            }

            return updated;
        }

        public void Update(Point2D point)
        {
            Min.X = Math.Min(Min.X, point.X);
            Min.Y = Math.Min(Min.Y, point.Y);

            Max.X = Math.Max(Max.X, point.X);
            Max.Y = Math.Max(Max.Y, point.Y);
        }

        public void Update(double x, double y)
        {
            Min.X = Math.Min(Min.X, x);
            Min.Y = Math.Min(Min.Y, y);

            Max.X = Math.Max(Max.X, x);
            Max.Y = Math.Max(Max.Y, y);
        }

        public void Update(double x, double y, double z)
        {
            Min.X = Math.Min(Min.X, x);
            Min.Y = Math.Min(Min.Y, y);
            Min.Z = Math.Min(Min.Z, z);

            Max.X = Math.Max(Max.X, x);
            Max.Y = Math.Max(Max.Y, y);
            Max.Z = Math.Max(Max.Z, z);
        }

        public void Update(Bounds bounds)
        {
            Update(bounds.Min);
            Update(bounds.Max);
        }

        public void MakeCubic()
        {
            Max = Min + Size.MaxValue();
        }

        public void SetBoundLines(ref float[] vertices, ref float[] colors, ref int count, double offsetX, double offsetY, double offsetZ)
        {
            count = 24;
            vertices = new float[72];
            colors = new float[72];
            int index = 0;

            //Back panel
            AddTopLeftDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddTopLeftUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddTopLeftDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddTopRightDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddTopLeftUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddTopRightUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddTopRightDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddTopRightUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);

            //Front panel
            AddBotLeftDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddBotLeftUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddBotLeftDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddBotRightDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddBotLeftUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddBotRightUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddBotRightDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddBotRightUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);

            //Left Side
            AddBotLeftDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddTopLeftDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddBotLeftUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddTopLeftUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);

            //Right side
            AddBotRightDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddTopRightDown(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddBotRightUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
            AddTopRightUp(ref vertices, ref colors, ref index, offsetX, offsetY, offsetZ);
        }

        private void AddTopLeftUp(ref float[] vertices, ref float[] colors, ref int count, double offsetX, double offsetY, double offsetZ)
        {
            vertices[count] = (float)(Min.X - offsetX);
            vertices[count + 1] = (float)(Max.Y - offsetY);
            vertices[count + 2] = (float)(Max.Z - offsetZ);
            colors[count] = 1.0f;
            colors[count + 1] = 0.953f;
            colors[count + 2] = 0.0588f;
            count += 3;
        }

        private void AddTopLeftDown(ref float[] vertices, ref float[] colors, ref int count, double offsetX, double offsetY, double offsetZ)
        {
            vertices[count] = (float)(Min.X - offsetX);
            vertices[count + 1] = (float)(Max.Y - offsetY);
            vertices[count + 2] = (float)(Min.Z - offsetZ);
            colors[count] = 1.0f;
            colors[count + 1] = 0.953f;
            colors[count + 2] = 0.0588f;
            count += 3;
        }

        private void AddBotLeftUp(ref float[] vertices, ref float[] colors, ref int count, double offsetX, double offsetY, double offsetZ)
        {
            vertices[count] = (float)(Min.X - offsetX);
            vertices[count + 1] = (float)(Min.Y - offsetY);
            vertices[count + 2] = (float)(Max.Z - offsetZ);
            colors[count] = 1.0f;
            colors[count + 1] = 0.953f;
            colors[count + 2] = 0.0588f;
            count += 3;
        }

        private void AddBotLeftDown(ref float[] vertices, ref float[] colors, ref int count, double offsetX, double offsetY, double offsetZ)
        {
            vertices[count] = (float)(Min.X - offsetX);
            vertices[count + 1] = (float)(Min.Y - offsetY);
            vertices[count + 2] = (float)(Min.Z - offsetZ);
            colors[count] = 1.0f;
            colors[count + 1] = 0.953f;
            colors[count + 2] = 0.0588f;
            count += 3;
        }

        private void AddTopRightUp(ref float[] vertices, ref float[] colors, ref int count, double offsetX, double offsetY, double offsetZ)
        {
            vertices[count] = (float)(Max.X - offsetX);
            vertices[count + 1] = (float)(Max.Y - offsetY);
            vertices[count + 2] = (float)(Max.Z - offsetZ);
            colors[count] = 1.0f;
            colors[count + 1] = 0.953f;
            colors[count + 2] = 0.0588f;
            count += 3;
        }

        private void AddTopRightDown(ref float[] vertices, ref float[] colors, ref int count, double offsetX, double offsetY, double offsetZ)
        {
            vertices[count] = (float)(Max.X - offsetX);
            vertices[count + 1] = (float)(Max.Y - offsetY);
            vertices[count + 2] = (float)(Min.Z - offsetZ);
            colors[count] = 1.0f;
            colors[count + 1] = 0.953f;
            colors[count + 2] = 0.0588f;
            count += 3;
        }

        private void AddBotRightUp(ref float[] vertices, ref float[] colors, ref int count, double offsetX, double offsetY, double offsetZ)
        {
            vertices[count] = (float)(Max.X - offsetX);
            vertices[count + 1] = (float)(Min.Y - offsetY);
            vertices[count + 2] = (float)(Max.Z - offsetZ);
            colors[count] = 1.0f;
            colors[count + 1] = 0.953f;
            colors[count + 2] = 0.0588f;
            count += 3;
        }

        private void AddBotRightDown(ref float[] vertices, ref float[] colors, ref int count, double offsetX, double offsetY, double offsetZ)
        {
            vertices[count] = (float)(Max.X - offsetX);
            vertices[count + 1] = (float)(Min.Y - offsetY);
            vertices[count + 2] = (float)(Min.Z - offsetZ);
            colors[count] = 1.0f;
            colors[count + 1] = 0.953f;
            colors[count + 2] = 0.0588f;
            count += 3;
        }

        public bool IntersectsWithRay(Models.Ray ray)
        {
            //Check x plane
            double tNear = double.NegativeInfinity, tFar = double.PositiveInfinity;
            if (ray.Direction.X == 0)
            {
                //Ray is parallel to x plane and must be between bounds
                if (ray.Origin.X < Min.X || ray.Origin.X > Max.X)
                    return false;
            }
            else
            {
                var t1 = (Min.X - ray.Origin.X) / ray.Direction.X;
                var t2 = (Max.X - ray.Origin.X) / ray.Direction.X;
                if (t1 > t2)
                    (t1, t2) = (t2, t1);
                if (t1 > tNear)
                    tNear = t1;
                if (t2 < tFar)
                    tFar = t2;
                if (tNear > tFar)
                    return false;
                if (tFar < 0)
                    return false;
            }

            //Check y plane
            if (ray.Direction.Y == 0)
            {
                //Ray is parallel to y plane and must be between bounds
                if (ray.Origin.Y < Min.Y || ray.Origin.Y > Max.Y)
                    return false;
            }
            else
            {
                var t1 = (Min.Y - ray.Origin.Y) / ray.Direction.Y;
                var t2 = (Max.Y - ray.Origin.Y) / ray.Direction.Y;
                if (t1 > t2)
                    (t1, t2) = (t2, t1);
                if (t1 > tNear)
                    tNear = t1;
                if (t2 < tFar)
                    tFar = t2;
                if (tNear > tFar)
                    return false;
                if (tFar < 0)
                    return false;
            }

            //Check z plane
            if (ray.Direction.Z == 0)
            {
                //Ray is parallel to z plane and must be between bounds
                if (ray.Origin.Z < Min.Z || ray.Origin.Z > Max.Z)
                    return false;
            }
            else
            {
                var t1 = (Min.Z - ray.Origin.Z) / ray.Direction.Z;
                var t2 = (Max.Z - ray.Origin.Z) / ray.Direction.Z;
                if (t1 > t2)
                    (t1, t2) = (t2, t1);
                if (t1 > tNear)
                    tNear = t1;
                if (t2 < tFar)
                    tFar = t2;
                if (tNear > tFar)
                    return false;
                if (tFar < 0)
                    return false;
            }

            return true;
        }

        public void GenerateLocalCorners(ref Point3D topLeft, ref Point3D topRight, ref Point3D botRight, ref Point3D botLeft, double zValue)
        {
            topLeft.X = Min.X - Center.X;
            topLeft.Y = Min.Y - Center.Y;
            topLeft.Z = zValue;

            topRight.X = Max.X - Center.X;
            topRight.Y = Min.Y - Center.Y;
            topRight.Z = zValue;

            botRight.X = Max.X - Center.X;
            botRight.Y = Max.Y - Center.Y;
            botRight.Z = zValue;

            botLeft.X = Min.Y - Center.X;
            botLeft.Y = Max.Y - Center.Y;
            botLeft.Z = zValue;
        }

        /// <summary>
        /// Generates a 2D array of corners coordinates in the order topLeft, topRight, botRight, botLeft
        /// </summary>
        /// <param name="zValue"></param>
        /// <returns></returns>
        public float[,] GenerateLocalCorners(float zValue)
        {
            var corners = new float[4, 3];
            corners[0, 0] = (float)(Min.X - Center.X);
            corners[0, 1] = (float)(Min.Y - Center.Y);
            corners[0, 2] = zValue;

            corners[1, 0] = (float)(Max.X - Center.X);
            corners[1, 1] = (float)(Min.Y - Center.Y);
            corners[1, 2] = zValue;

            corners[2, 0] = (float)(Max.X - Center.X);
            corners[2, 1] = (float)(Max.Y - Center.Y);
            corners[2, 2] = zValue;

            corners[3, 0] = (float)(Min.X - Center.X);
            corners[3, 1] = (float)(Max.Y - Center.Y);
            corners[3, 2] = zValue;

            return corners;
        }

        public double CalcSpacingFromDistance(double originalDist, int originalWidth, Point3D cameraPos)
        {
            var cX = Min.X + (Size.X / 2);
            var cY = Min.Y + (Size.Y / 2);
            var cZ = Min.Z + (Size.Z / 2);

            var distToCenter = Vector.FnDistance(cameraPos.X, cameraPos.Y, cX, cY);

            var newDist = originalDist + (distToCenter);
            return newDist / originalWidth;
        }

        public bool FindClosestIntersection2D(Point3D lineStart, Point3D lineEnd, ref Point3D intersectPoint)
        {
            var boundPoints = new List<Point3D> { new Point3D(Min.X, Min.Y, 0), new Point3D(Min.X, Max.Y, 0), new Point3D(Max.X, Max.Y, 0), new Point3D(Max.X, Min.Y, 0), new Point3D(Min.X, Min.Y, 0) };

            double dist = double.MaxValue;
            bool doesIntersect = false;




            for (int i = 0; i < boundPoints.Count - 1; i++)
            {
                var segStart = boundPoints[i];
                var segEnd = boundPoints[i + 1];


                double ipX = 0, ipY = 0;
                if (Intersection.IntersectLines(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, segStart.X, segStart.Y, segEnd.X, segEnd.Y, ref ipX, ref ipY))
                {
                    //Intersection is found so test it is valid
                    if (Intersection.FnInBox1(segStart.X, segStart.Y, segEnd.X, segEnd.Y, ipX, ipY))
                    {
                        var distToIntersection = Vector.FnDistance(lineStart.X, lineStart.Y, ipX, ipY);
                        if (distToIntersection < dist && distToIntersection > 0.0001)
                        {
                            doesIntersect = true;
                            dist = distToIntersection;
                            intersectPoint = new Point3D(ipX, ipY, 0);
                        }
                    }
                }
            }

            return doesIntersect;
        }

        #endregion
    }

    public class ClippingBox : Polygon
    {//
        #region Info

        //  Poly points are in the order from a 2D perspective
        //
        //          2  ------  1
        //            |      |
        //            |      |
        //          3  ------  0
        //
        //  Index IDs for the cube where 1 represents the top of the cube and 6 represents the bottom of the cube
        //
        //             -------
        //            |       |
        //            |   3   |
        //     ------- ------- ------- -------
        //    |       |       |       |       |
        //    |   4   |   1   |   5   |   6   |
        //     ------- ------- ------- -------
        //            |       |
        //            |   2   |
        //             -------
        //
        //  An ID of 0 represents no intersection

        #endregion

        #region Properties

        /// <summary>
        /// A List of <see cref="Point2D"/> objects representing the vertices of the <see cref="Polygon"/>
        /// </summary>
        [Category("Coordinates"), Description("A list of coordinate points for the polygon")]
        public List<Point2D> Points { get; set; }


        /// <summary>
        /// Represents the <see cref="NRG.Models.Bounds"/> of the <see cref="ClippingBox"/>
        /// </summary>
        public Bounds Bounds { get; set; }

        /// <summary>
        /// Determines if the <see cref="ClippingBox"/> is comprised of atleast 4 <see cref="Point2D"/>
        /// </summary>
        public bool IsValid { get { return Points?.Count >= 4; } }
        private List<(Point3D PanelCenter, double PanelSize)> PanelInfo { get; set; }

        #endregion

        #region Setup

        /// <summary>
        /// Generates a new <see cref="ClippingBox"/> object
        /// </summary>
        public ClippingBox()
        {
            Points = new List<Point2D>();
            Bounds = new Bounds();
            PanelInfo = new List<(Point3D PanelCenter, double PanelSize)>();
        }

        /// <summary>
        /// Copies the current <see cref="ClippingBox"/> into a new object
        /// </summary>
        /// <returns>Returns a new <see cref="ClippingBox"/> copy of the object</returns>
        public new ClippingBox Copy()
        {
            return new ClippingBox
            {
                Points = this.Points.ToList(),
                Bounds = this.Bounds.Copy(),
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Calculates the intersection index and point of a <see cref="Ray"/> with the <see cref="ClippingBox"/>
        /// </summary>
        /// <param name="ray">A <see cref="Ray"/> to intersect with the <see cref="ClippingBox"/></param>
        /// <param name="intersectionPoint">A <see cref="Point3D"/> to store the intersection point of the <see cref="Ray"/>. Will be null if there is no intersection</param>
        /// <returns>Returns an <see cref="int"/> representing the index of the intersected panel</returns>
        public int PlaneIntersection(Ray ray, ref Point3D intersectionPoint)
        {
            int curIndex = 0;
            intersectionPoint = null;
            var intersections = new List<Point3D>();
            double distance = double.MaxValue, dist = 0;

            //If there are not enough polygon points to make a valid clipping box we must return null
            if (!IsValid)
                return 0;

            try
            {
                //Generate a list of intersection from the 4 corners of each plane
                //Order is 1 - 2 - 3 - 4 - 5 - 6
                intersections.Add(Intersection.IntersectRayOnPlane(new Point3D(Points[0], Bounds.Max.Z), new Point3D(Points[1], Bounds.Max.Z), new Point3D(Points[2], Bounds.Max.Z), new Point3D(Points[3], Bounds.Max.Z), ray));
                intersections.Add(Intersection.IntersectRayOnPlane(new Point3D(Points[0], Bounds.Min.Z), new Point3D(Points[0], Bounds.Max.Z), new Point3D(Points[3], Bounds.Max.Z), new Point3D(Points[3], Bounds.Min.Z), ray));
                intersections.Add(Intersection.IntersectRayOnPlane(new Point3D(Points[1], Bounds.Min.Z), new Point3D(Points[1], Bounds.Max.Z), new Point3D(Points[2], Bounds.Max.Z), new Point3D(Points[2], Bounds.Min.Z), ray));
                intersections.Add(Intersection.IntersectRayOnPlane(new Point3D(Points[3], Bounds.Min.Z), new Point3D(Points[3], Bounds.Max.Z), new Point3D(Points[2], Bounds.Max.Z), new Point3D(Points[2], Bounds.Min.Z), ray));
                intersections.Add(Intersection.IntersectRayOnPlane(new Point3D(Points[0], Bounds.Min.Z), new Point3D(Points[0], Bounds.Max.Z), new Point3D(Points[1], Bounds.Max.Z), new Point3D(Points[1], Bounds.Min.Z), ray));
                intersections.Add(Intersection.IntersectRayOnPlane(new Point3D(Points[0], Bounds.Min.Z), new Point3D(Points[1], Bounds.Min.Z), new Point3D(Points[2], Bounds.Min.Z), new Point3D(Points[3], Bounds.Min.Z), ray));

                //Find the closest non null intersection
                for (int i = 0; i < 6; i++)
                {
                    if (intersections[i] == null)
                        continue;

                    var intersection = intersections[i];
                    dist = ray.Origin.DistanceTo(intersection);

                    if (dist < distance)
                    {
                        curIndex = i + 1;
                        distance = dist;
                        intersectionPoint = intersection.Copy();
                    }
                }

                //Determine if there are any hooks within range of the ray
                if (PanelInfo.Count < 6)
                    GeneratePanelInfo();

                for (int i = 0; i < 6; i++)
                {
                    var panel = PanelInfo[i];
                    var intersection = intersections[i];
                    if (intersection == null)
                        continue;

                    //Determine if the intersection point is within the radius of the hook
                    dist = Vector.Fn3DDistance(panel.PanelCenter.X, panel.PanelCenter.Y, panel.PanelCenter.Z, intersection.X, intersection.Y, intersection.Z);
                    if (dist <= panel.PanelSize)
                    {
                        //Determine if the intersection point is the closest to the hook so far
                        if (dist < distance)
                        {
                            distance = dist;
                            curIndex = i + 1;
                            intersectionPoint = intersection.Copy();
                        }
                    }
                }

                //Determine if one of the center bars should be selected
                var centerZ = (Bounds.Max.Z + Bounds.Min.Z) / 2;
                Point3D lineStart, lineEnd, shortestLineStart = new Point3D(), shortestLineEnd = new Point3D();

                //7 - X center bar
                lineStart = new Point3D((Points[3] + Points[2]) / 2, centerZ);
                lineEnd = new Point3D((Points[0] + Points[1]) / 2, centerZ);
                Vector.ShortestLineBetweenTwoLines(lineStart, lineEnd, ray.Origin, ray.End, ref shortestLineStart, ref shortestLineEnd);
                if (IsInside3D(shortestLineStart))
                {
                    dist = Vector.Fn3DDistance(shortestLineStart.X, shortestLineStart.Y, shortestLineStart.Z, shortestLineEnd.X, shortestLineEnd.Y, shortestLineEnd.Z);
                    if (dist <= 0.1 && dist < distance)
                    {
                        curIndex = 7;
                        distance = dist;
                    }
                }

                //8 - Y center bar
                lineStart = new Point3D((Points[3] + Points[0]) / 2, centerZ);
                lineEnd = new Point3D((Points[2] + Points[1]) / 2, centerZ);
                Vector.ShortestLineBetweenTwoLines(lineStart, lineEnd, ray.Origin, ray.End, ref shortestLineStart, ref shortestLineEnd);
                if (IsInside3D(shortestLineStart))
                {
                    dist = Vector.Fn3DDistance(shortestLineStart.X, shortestLineStart.Y, shortestLineStart.Z, shortestLineEnd.X, shortestLineEnd.Y, shortestLineEnd.Z);
                    if (dist <= 0.1 && dist < distance)
                    {
                        curIndex = 8;
                        distance = dist;
                    }
                }

                //9 - Z center bar
                lineStart = new Point3D(PanelInfo[0].PanelCenter.X, PanelInfo[0].PanelCenter.Y, Bounds.Min.Z);
                lineEnd = new Point3D(PanelInfo[0].PanelCenter.X, PanelInfo[0].PanelCenter.Y, Bounds.Max.Z);
                Vector.ShortestLineBetweenTwoLines(lineStart, lineEnd, ray.Origin, ray.End, ref shortestLineStart, ref shortestLineEnd);
                if (IsInside3D(shortestLineStart))
                {
                    dist = Vector.Fn3DDistance(shortestLineStart.X, shortestLineStart.Y, shortestLineStart.Z, shortestLineEnd.X, shortestLineEnd.Y, shortestLineEnd.Z);
                    if (dist <= 0.1 && dist < distance)
                    {
                        curIndex = 9;
                        distance = dist;
                    }
                }

                //If we already have a current index at this point then return
                if (curIndex > 0 && curIndex <= 6)
                    return curIndex;

                return curIndex;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Determines if a <see cref="Point2D"/> falls within the 2D footprint of the <see cref="ClippingBox"/>
        /// </summary>
        /// <param name="point">The <see cref="Point2D"/> to test</param>
        /// <returns>Returns true if the point is within the 2D footprint of the <see cref="ClippingBox"/></returns>
        public bool IsInside2D(Point2D point)
        {
            return InPoly2D(point, Points);
        }

        /// <summary>
        /// Determines if a point falls within the 2D footprint of the <see cref="ClippingBox"/>
        /// </summary>
        /// <param name="x">The X value of the point to test</param>
        /// <param name="y">The Y value of the point to test</param>
        /// <returns>Returns true if the point is within the 2D footprint of the <see cref="ClippingBox"/></returns>
        public bool IsInside2D(double x, double y)
        {
            return InPoly2D(x, y, Points);
        }

        /// <summary>
        /// Determines if a point falls within the <see cref="ClippingBox"/>
        /// </summary>
        /// <param name="point">The <see cref="Point3D"/> to test</param>
        /// <returns>Returns true if the point is within the <see cref="ClippingBox"/></returns>
        public bool IsInside3D(Point3D point)
        {
            return point.X >= Bounds.Min.X && point.X <= Bounds.Max.X && point.Y >= Bounds.Min.Y && point.Y <= Bounds.Max.Y && point.Z >= Bounds.Min.Z && point.Z <= Bounds.Max.Z && InPoly2D(point.X, point.Y, Points);
        }

        /// <summary>
        /// Determines if a point falls within the <see cref="ClippingBox"/>
        /// </summary>
        /// <param name="x">The X value of the point to test</param>
        /// <param name="y">The Y value of the point to test</param>
        /// <param name="z">The Z value of the point to test</param>
        /// <returns>Returns true if the point is within the <see cref="ClippingBox"/></returns>
        public bool IsInside3D(double x, double y, double z)
        {
            return x >= Bounds.Min.X && x <= Bounds.Max.X && y >= Bounds.Min.Y && y <= Bounds.Max.Y && z >= Bounds.Min.Z && z <= Bounds.Max.Z && InPoly2D(x, y, Points);
        }

        /// <summary>
        /// Attempts to extrude one of the 6 panels of the <see cref="ClippingBox"/>
        /// </summary>
        /// <param name="moveX">The number of pixels to move in the X direction</param>
        /// <param name="moveY">The number of pixels to move in the Y direction</param>
        /// <param name="lastCameraPos">A <see cref="Point3D"/> containing the last position of the camera</param>
        /// <param name="lastIntersectionPoint">A <see cref="Point3D"/> containing the last positon of the <see cref="Ray"/> intersection with the <see cref="ClippingBox"/></param>
        /// <param name="ray">The <see cref="Ray"/> from the camera position to the cursor</param>
        /// <param name="panelIndex">A <see cref="int"/> representing the <see cref="ClippingBox"/> panel to extrude</param>
        /// <returns>Returns true if the extrusion has been applied and the model should be updated</returns>
        public bool ExtrudeClippingBox(double moveX, double moveY, ref Point3D lastCameraPos, ref Point3D lastIntersectionPoint, Ray ray, int panelIndex)
        {
            try
            {
                //If the panel index is invalid or we do not have enough 2D polygon points then return false
                if (!IsValid)
                    return false;

                bool extrudeComplete = false;
                double brg = 0, dist = 0, newX = 0, newY = 0, newZ = 0, offset = 0;

                switch (panelIndex)
                {
                    case 1:
                        extrudeComplete = ExtrudeClippingBoxTop(ref lastCameraPos, ref lastIntersectionPoint, ray);
                        break;
                    case 2:
                        extrudeComplete = ExtrudeClippingBoxSide(ref lastCameraPos, ref lastIntersectionPoint, ray, Points[0], Points[3], Points[1], Points[2]);
                        break;
                    case 3:
                        extrudeComplete = ExtrudeClippingBoxSide(ref lastCameraPos, ref lastIntersectionPoint, ray, Points[2], Points[1], Points[3], Points[0]);
                        break;
                    case 4:
                        extrudeComplete = ExtrudeClippingBoxSide(ref lastCameraPos, ref lastIntersectionPoint, ray, Points[3], Points[2], Points[0], Points[1]);
                        break;
                    case 5:
                        extrudeComplete = ExtrudeClippingBoxSide(ref lastCameraPos, ref lastIntersectionPoint, ray, Points[1], Points[0], Points[2], Points[3]);
                        break;
                    case 6:
                        extrudeComplete = ExtrudeClippingBoxBottom(ref lastCameraPos, ref lastIntersectionPoint, ray);
                        break;
                    case 7:
                        //Move Clipping Box Left / Right
                        Trig.RPC(lastCameraPos.X, lastCameraPos.Y, lastIntersectionPoint.X, lastIntersectionPoint.Y, ref brg, ref dist);
                        brg = Trig.RPC(ray.Origin.X, ray.Origin.Y, ray.End.X, ray.End.Y);
                        Trig.PRC(ray.Origin.X, ray.Origin.Y, brg, dist, ref newX, ref newY);

                        //If there is no movement we don't need to update
                        if (Math.Abs(newX - lastIntersectionPoint.X) <= 0.0000001 && Math.Abs(newY - lastIntersectionPoint.Y) <= 0.0000001)
                            return false;

                        //Calculate the bearing of the front line in order to move the line in a perpendicular direction
                        brg = Trig.RPC(Points[3].X, Points[3].Y, Points[2].X, Points[2].Y);

                        //Calculate the offset of the new intersection from the front line
                        Trig.DistanceAndOffsetFromLine(lastIntersectionPoint.X, lastIntersectionPoint.Y, brg, newX, newY, ref dist, ref offset);

                        //Move perpendicular to the front line
                        if (offset == 0)
                            return false;
                        else if (offset < 0)
                            brg -= (Math.PI / 2);
                        else
                            brg += (Math.PI / 2);

                        //Get the distance to move (dist from last intersection and the new intersection)
                        dist = Vector.FnDistance(lastIntersectionPoint.X, lastIntersectionPoint.Y, newX, newY);

                        //Calculate the new front line
                        Trig.PRC(Points[0].X, Points[0].Y, brg, dist, ref newX, ref newY);
                        Points[0] = new Point2D(newX, newY);

                        Trig.PRC(Points[1].X, Points[1].Y, brg, dist, ref newX, ref newY);
                        Points[1] = new Point2D(newX, newY);

                        Trig.PRC(Points[2].X, Points[2].Y, brg, dist, ref newX, ref newY);
                        Points[2] = new Point2D(newX, newY);

                        Trig.PRC(Points[3].X, Points[3].Y, brg, dist, ref newX, ref newY);
                        Points[3] = new Point2D(newX, newY);

                        //Calculate the new intersection point
                        Trig.PRC(lastIntersectionPoint.X, lastIntersectionPoint.Y, brg, dist, ref newX, ref newY);

                        //Set the new intersection point and camera position
                        lastIntersectionPoint.X = newX;
                        lastIntersectionPoint.Y = newY;
                        lastCameraPos = ray.Origin;
                        extrudeComplete = true;
                        break;
                    case 8:
                        //Move Clipping Box Forward / Backwards
                        Trig.RPC(lastCameraPos.X, lastCameraPos.Y, lastIntersectionPoint.X, lastIntersectionPoint.Y, ref brg, ref dist);
                        brg = Trig.RPC(ray.Origin.X, ray.Origin.Y, ray.End.X, ray.End.Y);
                        Trig.PRC(ray.Origin.X, ray.Origin.Y, brg, dist, ref newX, ref newY);

                        //If there is no movement we don't need to update
                        if (Math.Abs(newX - lastIntersectionPoint.X) <= 0.0000001 && Math.Abs(newY - lastIntersectionPoint.Y) <= 0.0000001)
                            return false;

                        //Calculate the bearing of the front line in order to move the line in a perpendicular direction
                        brg = Trig.RPC(Points[3].X, Points[3].Y, Points[0].X, Points[0].Y);

                        //Calculate the offset of the new intersection from the front line
                        Trig.DistanceAndOffsetFromLine(lastIntersectionPoint.X, lastIntersectionPoint.Y, brg, newX, newY, ref dist, ref offset);

                        //Move perpendicular to the front line
                        if (offset == 0)
                            return false;
                        else if (offset < 0)
                            brg -= (Math.PI / 2);
                        else
                            brg += (Math.PI / 2);

                        //Get the distance to move (dist from last intersection and the new intersection)
                        dist = Vector.FnDistance(lastIntersectionPoint.X, lastIntersectionPoint.Y, newX, newY);

                        //Calculate the new front line
                        Trig.PRC(Points[0].X, Points[0].Y, brg, dist, ref newX, ref newY);
                        Points[0] = new Point3D(newX, newY);

                        Trig.PRC(Points[1].X, Points[1].Y, brg, dist, ref newX, ref newY);
                        Points[1] = new Point3D(newX, newY);

                        Trig.PRC(Points[2].X, Points[2].Y, brg, dist, ref newX, ref newY);
                        Points[2] = new Point3D(newX, newY);

                        Trig.PRC(Points[3].X, Points[3].Y, brg, dist, ref newX, ref newY);
                        Points[3] = new Point3D(newX, newY);

                        //Calculate the new intersection point
                        Trig.PRC(lastIntersectionPoint.X, lastIntersectionPoint.Y, brg, dist, ref newX, ref newY);

                        //Set the new intersection point and camera position
                        lastIntersectionPoint.X = newX;
                        lastIntersectionPoint.Y = newY;
                        lastCameraPos = ray.Origin;
                        extrudeComplete = true;
                        break;
                    case 9:
                        //Move Clipping Box Up / Down
                        Trig.RPC(lastCameraPos.Y, lastCameraPos.Z, lastIntersectionPoint.Y, lastIntersectionPoint.Z, ref brg, ref dist);
                        brg = Trig.RPC(ray.Origin.Y, ray.Origin.Z, ray.End.Y, ray.End.Z);
                        Trig.PRC(ray.Origin.Y, ray.Origin.Z, brg, dist, ref newY, ref newZ);
                        dist = newZ - lastIntersectionPoint.Z;

                        //If there is no movement we don't need to update
                        if (Math.Abs(dist) <= 0.0000001)
                            break;

                        lastIntersectionPoint.Z = newZ;
                        Bounds.Max.Z += dist;
                        Bounds.Min.Z += dist;
                        extrudeComplete = true;
                        break;
                }

                GeneratePanelInfo();
                return extrudeComplete;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to extrude one of the sides of the <see cref="ClippingBox"/>
        /// </summary>
        /// <param name="lastCameraPos">A <see cref="Point3D"/> containing the last position of the camera</param>
        /// <param name="lastIntersectionPoint">A <see cref="Point3D"/> containing the last position of the <see cref="Ray"/> intersection with the <see cref="ClippingBox"/></param>
        /// <param name="ray">The <see cref="Ray"/> from the camera positon to the cursor</param>
        /// <param name="front1">A <see cref="Point2D"/> representing the start of the line to extrude</param>
        /// <param name="front2">A <see cref="Point2D"/> representing the end of the line to extrude</param>
        /// <param name="back1">A <see cref="Point2D"/> representing the start of the back line to check against</param>
        /// <param name="back2">A <see cref="Point2D"/> representing the end of the back line to check against</param>
        /// <returns>Returns true if the extrusion has been applied and the model should be updated</returns>
        private bool ExtrudeClippingBoxSide(ref Point3D lastCameraPos, ref Point3D lastIntersectionPoint, Ray ray, Point2D front1, Point2D front2, Point2D back1, Point2D back2)
        {
            //Extrude the side of the clipping box
            double brg = 0, dist = 0, newX = 0, newY = 0, offset = 0;

            //Calculate the distance to the old intersection then generate a new intersection point at this distance along the ray
            Trig.RPC(lastCameraPos.X, lastCameraPos.Y, lastIntersectionPoint.X, lastIntersectionPoint.Y, ref brg, ref dist);
            brg = Trig.RPC(ray.Origin.X, ray.Origin.Y, ray.End.X, ray.End.Y);
            Trig.PRC(ray.Origin.X, ray.Origin.Y, brg, dist, ref newX, ref newY);

            //Check if there is an update
            if (Math.Abs(lastIntersectionPoint.X - newX) <= 0.0000001 && Math.Abs(lastIntersectionPoint.Y - newY) <= 0.0000001)
                return false;

            //Get the offset of the new intersection point from the back line
            Trig.DistanceAndOffsetFromLine(back1.X, back1.Y, back2.X, back2.Y, newX, newY, ref dist, ref offset);

            //Check that the section is still a positive size (If the offset is positive it means the intersection has gone past the back line
            if (offset >= 0)
            {
                //Move the front line to be just inside the back border
                brg = Trig.RPC(back1.X, back1.Y, back2.X, back2.Y);
                brg -= (Math.PI / 2);

                Trig.PRC(back1.X, back1.Y, brg, 0.0001, ref newX, ref newY);
                front1.X = newX;
                front1.Y = newY;

                Trig.PRC(back2.X, back2.Y, brg, 0.0001, ref newX, ref newY);
                front2.X = newX;
                front2.Y = newY;
            }
            else
            {
                //Calculate the bearing of the front line in order to move the line in a perpendicular direction
                brg = Trig.RPC(front1.X, front1.Y, front2.X, front2.Y);

                //Calculate the offset of the new intersection from the front line
                Trig.DistanceAndOffsetFromLine(front1.X, front1.Y, brg, newX, newY, ref dist, ref offset);

                //Move perpendicular to the front line
                if (offset == 0)
                    return false;
                else if (offset < 0)
                    brg -= (Math.PI / 2);
                else
                    brg += (Math.PI / 2);

                //Get the distance to move (dist from last intersection and the new intersection)
                dist = Vector.FnDistance(lastIntersectionPoint.X, lastIntersectionPoint.Y, newX, newY);

                //Calculate the new front line
                Trig.PRC(front1.X, front1.Y, brg, dist, ref newX, ref newY);
                front1.X = newX;
                front1.Y = newY;

                Trig.PRC(front2.X, front2.Y, brg, dist, ref newX, ref newY);
                front2.X = newX;
                front2.Y = newY;

                //Calculate the new intersection point
                Trig.PRC(lastIntersectionPoint.X, lastIntersectionPoint.Y, brg, dist, ref newX, ref newY);

                //Set the new intersection point and camera position
                lastIntersectionPoint.X = newX;
                lastIntersectionPoint.Y = newY;
                lastCameraPos = ray.Origin;
            }

            //If we have made it this far the extrusion should have been applied
            return true;
        }

        /// <summary>
        /// Attempts to extrude the top of the <see cref="ClippingBox"/>
        /// </summary>
        /// <param name="lastCameraPos">A <see cref="Point3D"/> containing the last position of the camera</param>
        /// <param name="lastIntersectionPoint">A <see cref="Point3D"/> containing the last position of the <see cref="Ray"/> intersection with the <see cref="ClippingBox"/></param>
        /// <param name="ray">The <see cref="Ray"/> from the camera position to the cursor</param>
        /// <returns>Returns true if the extrusion has been applied and the model should be updated</returns>
        private bool ExtrudeClippingBoxTop(ref Point3D lastCameraPos, ref Point3D lastIntersectionPoint, Ray ray)
        {
            //Extrude the top of the clipping box - since we are dealing with a flat Z plane we will be working with the Y and Z coordinates
            double brg = 0, dist = 0, newY = 0, newZ = 0;

            //Calculate the distance to the old intersection then generate a new intersection point at this distance along the ray
            Trig.RPC(lastCameraPos.Y, lastCameraPos.Z, lastIntersectionPoint.Y, lastIntersectionPoint.Z, ref brg, ref dist);
            brg = Trig.RPC(ray.Origin.Y, ray.Origin.Z, ray.End.Y, ray.End.Z);
            Trig.PRC(ray.Origin.Y, ray.Origin.Z, brg, dist, ref newY, ref newZ);

            //Check there is an update
            if (Math.Abs(Bounds.Max.Z - newZ) <= 0.0000001)
                return false;

            //Check that the new level is not below the floor of the clipping box
            if (newZ <= Bounds.Min.Z)
                Bounds.Max.Z = Bounds.Min.Z + 0.0001;
            else
                Bounds.Max.Z = newZ;

            //If we have made it this far the extrusion should have been applied
            return true;
        }

        /// <summary>
        /// Attempts to extrude the top of the <see cref="ClippingBox"/>
        /// </summary>
        /// <param name="lastCameraPos">A <see cref="Point3D"/> containing the last position of the camera</param>
        /// <param name="lastIntersectionPoint">A <see cref="Point3D"/> containing the last position of the <see cref="Ray"/> intersection with the <see cref="ClippingBox"/></param>
        /// <param name="ray">The <see cref="Ray"/> from the camera positon to the cursor</param>
        /// <returns>Returns true if the extrusion has been applied and the model should be updated</returns>
        private bool ExtrudeClippingBoxBottom(ref Point3D lastCameraPos, ref Point3D lastIntersectionPoint, Ray ray)
        {
            //Extrude the bottom of the clipping box - since we are dealing with a flat Z plane we will be working with the Y and Z coordinates
            double brg = 0, dist = 0, newY = 0, newZ = 0;

            //Calculate the distance to the old intersection then generate a new intersection point at this distance along the ray
            Trig.RPC(lastCameraPos.Y, lastCameraPos.Z, lastIntersectionPoint.Y, lastIntersectionPoint.Z, ref brg, ref dist);
            brg = Trig.RPC(ray.Origin.Y, ray.Origin.Z, ray.End.Y, ray.End.Z);
            Trig.PRC(ray.Origin.Y, ray.Origin.Z, brg, dist, ref newY, ref newZ);

            //Check there is an update
            if (Math.Abs(Bounds.Min.Z - newZ) <= 0.0000001)
                return false;

            //Check that the new level is not above the roof of the clipping box
            if (newZ >= Bounds.Max.Z)
                Bounds.Min.Z = Bounds.Max.Z - 0.0001;
            else
                Bounds.Min.Z = newZ;

            //If we have made it this far the extrusion should have been applied
            return true;
        }

        /// <summary>
        /// Updates the <see cref="NRG.Models.Bounds"/> of the <see cref="ClippingBox"/>
        /// </summary>
        /// <param name="modelBounds">The <see cref="NRG.Models.Bounds"/> to get the vertical extents from</param>
        public void UpdateBounds(Bounds modelBounds)
        {
            Bounds = new Bounds();
            foreach (var point in Points)
                Bounds.Update(point);

            Bounds.Min.Z = modelBounds.Min.Z;
            Bounds.Max.Z = modelBounds.Max.Z;
        }

        /// <summary>
        /// Updates the <see cref="NRG.Models.Bounds"/> of the <see cref="ClippingBox"/>
        /// </summary>
        /// <param name="minZ">The minimum Z value of the <see cref="ClippingBox"/></param>
        /// <param name="maxZ">The maximum Z value of the <see cref="ClippingBox"/></param>
        public void UpdateBounds(double minZ, double maxZ)
        {
            Bounds = new Bounds();
            foreach (var point in Points)
                Bounds.Update(point);

            Bounds.Min.Z = minZ;
            Bounds.Max.Z = maxZ;
        }

        #region Render Methods

        /// <summary>
        /// Populates a <see cref="float"/> array of vertices and colors representing the semi transparent <see cref="ClippingBox"/> panels to be rendered
        /// </summary>
        /// <param name="vertices">A <see cref="float"/> array to store the panel vertices as XYZ</param>
        /// <param name="colors">A <see cref="float"/> array to store the panel colors as RGBA</param>
        /// <param name="offsetX">The X offset to apply to each vertex</param>
        /// <param name="offsetY">The Y offset to apply to each vertex</param>
        /// <param name="offsetZ">The Z offset to apply to each vertex</param>
        /// <param name="highlightedFace">An <see cref="int"/> representing the currently highlighted face. A value of 0 will not highlight any face</param>
        /// <param name="r">A <see cref="float"/> representing the Red color channel as a factor of 255 </param>
        /// <param name="g">A <see cref="float"/> representing the Green color channel as a factor of 255</param>
        /// <param name="b">A <see cref="float"/> representing the Blue color channel as a factor of 255</param>
        public void SetTransparentBoundPanels(ref float[] vertices, ref float[] colors, double offsetX, double offsetY, double offsetZ, int highlightedFace = 0, float r = 1.0f, float g = 0.953f, float b = 0.0588f)
        {
            vertices = new float[0];
            colors = new float[0];

            //If there are not enough polygon points to make a valid clipping box we must return
            if (!IsValid)
                return;

            try
            {
                var minZ = (float)(Bounds.Min.Z - offsetZ);
                var maxZ = (float)(Bounds.Max.Z - offsetZ);
                var transparency = 0.25f;

                float[,] points = new float[,]
                {
                    { (float)(Points[0].X - offsetX), (float)(Points[0].Y - offsetY) },
                    { (float)(Points[1].X - offsetX), (float)(Points[1].Y - offsetY) },
                    { (float)(Points[2].X - offsetX), (float)(Points[2].Y - offsetY) },
                    { (float)(Points[3].X - offsetX), (float)(Points[3].Y - offsetY) }
                };

                //Set the vertices as a series of triangle elements in the order 0, 1, 2 - 2, 3, 1
                vertices = new float[]
                {
                    //1
                    points[0, 0], points[0, 1], maxZ, points[1, 0], points[1, 1], maxZ, points[2, 0], points[2, 1], maxZ,
                    points[2, 0], points[2, 1], maxZ, points[3, 0], points[3, 1], maxZ, points[0, 0], points[0, 1], maxZ,
                    
                    //2
                    points[0, 0], points[0, 1], minZ, points[0, 0], points[0, 1], maxZ, points[3, 0], points[3, 1], maxZ,
                    points[3, 0], points[3, 1], maxZ, points[3, 0], points[3, 1], minZ, points[0, 0], points[0, 1], minZ,

                    //3
                    points[1, 0], points[1, 1], minZ, points[1, 0], points[1, 1], maxZ, points[2, 0], points[2, 1], maxZ,
                    points[2, 0], points[2, 1], maxZ, points[2, 0], points[2, 1], minZ, points[1, 0], points[1, 1], minZ,

                    //4
                    points[3, 0], points[3, 1], minZ, points[3, 0], points[3, 1], maxZ, points[2, 0], points[2, 1], maxZ,
                    points[2, 0], points[2, 1], maxZ, points[2, 0], points[2, 1], minZ, points[3, 0], points[3, 1], minZ,

                    //5
                    points[0, 0], points[0, 1], minZ, points[0, 0], points[0, 1], maxZ, points[1, 0], points[1, 1], maxZ,
                    points[1, 0], points[1, 1], maxZ, points[1, 0], points[1, 1], minZ, points[0, 0], points[0, 1], minZ,

                    //6
                    points[0, 0], points[0, 1], minZ, points[1, 0], points[1, 1], minZ, points[2, 0], points[2, 1], minZ,
                    points[2, 0], points[2, 1], minZ, points[3, 0], points[3, 1], minZ, points[0, 0], points[0, 1], minZ
                };

                int colIndex = 0;
                colors = new float[144];

                //For each panel
                for (int i = 0; i < 6; i++)
                {
                    if (highlightedFace == i + 1)
                        transparency = 0.5f;
                    else
                        transparency = 0.25f;

                    //Foreach vertex within the panel
                    for (int j = 0; j < 6; j++)
                    {
                        colors[colIndex++] = r;
                        colors[colIndex++] = g;
                        colors[colIndex++] = b;
                        colors[colIndex++] = transparency;
                    }
                }
            }
            catch
            {
                //Empty the arrays to avoid loading potentially corrupt data
                vertices = new float[0];
                colors = new float[0];
            }
        }

        /// <summary>
        /// Populates a <see cref="float"/> array of vertices and colors representing the <see cref="ClippingBox"/> borders to be rendered
        /// </summary>
        /// <param name="vertices">A <see cref="float"/> array to store the border vertices as XYZ</param>
        /// <param name="colors">A <see cref="float"/> array to store the border colors as RGB</param>
        /// <param name="offsetX">The X offset to apply to each vertex</param>
        /// <param name="offsetY">The Y offset to apply to each vertex</param>
        /// <param name="offsetZ">The Z offset to apply to each vertex</param>
        /// <param name="r">A <see cref="float"/> representing the Red color channel as a factor of 255</param>
        /// <param name="g">A <see cref="float"/> representing the Green color channel as a factor of 255</param>
        /// <param name="b">A <see cref="float"/> representing the Blue color channel as a factor of 255</param>
        public void SetBoundsBorders(ref float[] vertices, ref float[] colors, double offsetX, double offsetY, double offsetZ, float r = 1.0f, float g = 0.953f, float b = 0.0588f)
        {
            vertices = new float[0];
            colors = new float[0];

            //If there are not enough polygon points to make a valid clipping box we must return
            if (!IsValid)
                return;

            try
            {
                var minZ = (float)(Bounds.Min.Z - offsetZ);
                var maxZ = (float)(Bounds.Max.Z - offsetZ);

                float[,] points = new float[,]
                {
                    { (float)(Points[0].X - offsetX), (float)(Points[0].Y - offsetY) },
                    { (float)(Points[1].X - offsetX), (float)(Points[1].Y - offsetY) },
                    { (float)(Points[2].X - offsetX), (float)(Points[2].Y - offsetY) },
                    { (float)(Points[3].X - offsetX), (float)(Points[3].Y - offsetY) }
                };

                //Set the vertices as a series of line segments
                vertices = new float[]
                {
                    //1 - 2, 2 - 3, 3 - 4, 4 - 1 Top
                    points[0, 0], points[0, 1], maxZ, points[1, 0], points[1, 1], maxZ,
                    points[1, 0], points[1, 1], maxZ, points[2, 0], points[2, 1], maxZ,
                    points[2, 0], points[2, 1], maxZ, points[3, 0], points[3, 1], maxZ,
                    points[3, 0], points[3, 1], maxZ, points[0, 0], points[0, 1], maxZ,

                    //1 - 2, 2 - 3, 3 - 4, 4 - 1 Bot
                    points[0, 0], points[0, 1], minZ, points[1, 0], points[1, 1], minZ,
                    points[1, 0], points[1, 1], minZ, points[2, 0], points[2, 1], minZ,
                    points[2, 0], points[2, 1], minZ, points[3, 0], points[3, 1], minZ,
                    points[3, 0], points[3, 1], minZ, points[0, 0], points[0, 1], minZ,

                    //1 - 1, 2 - 2, 3 - 3, 4 - 4 Top - Bot
                    points[0, 0], points[0, 1], maxZ, points[0, 0], points[0, 1], minZ,
                    points[1, 0], points[1, 1], maxZ, points[1, 0], points[1, 1], minZ,
                    points[2, 0], points[2, 1], maxZ, points[2, 0], points[2, 1], minZ,
                    points[3, 0], points[3, 1], maxZ, points[3, 0], points[3, 1], minZ
                };

                int colIndex = 0;
                colors = new float[72];

                //For every pair of points
                for (int i = 0; i < 24; i++)
                {
                    colors[colIndex++] = r;
                    colors[colIndex++] = g;
                    colors[colIndex++] = b;
                }
            }
            catch
            {
                //Empty the arrays to avoid loading potentially corrupt data
                vertices = new float[0];
                colors = new float[0];
            }
        }

        /// <summary>
        /// Populates a <see cref="float"/> array of vertices and colors representing the interaction hooks for the <see cref="ClippingBox"/> panels
        /// </summary>
        /// <param name="vertices">A <see cref="float"/> array to store the hook vertices as XYZ</param>
        /// <param name="colors">A <see cref="float"/> array to store the border colors as RGB</param>
        /// <param name="offsetX">The X offset to apply to each vertex</param>
        /// <param name="offsetY">The Y offset to apply to each vertex</param>
        /// <param name="offsetZ">The Z offset to apply to each vertex</param>
        /// <param name="highlightedFace">An <see cref="int"/> representing the currently highlighted face. A value of 0 will not highlight any hook</param>
        public void SetPanelHooks(ref float[] vertices, ref float[] colors, double offsetX, double offsetY, double offsetZ, int highlightedFace = 0)
        {
            vertices = new float[0];
            colors = new float[0];
            var verts = new List<float>();
            var cols = new List<float>();

            double distance = 0, brg = 0, newX = 0, newY = 0;

            //If there are not enough polygon points to make a valid clipping box we must return
            if (!IsValid)
                return;

            try
            {
                var minZ = (float)(Bounds.Min.Z - offsetZ);
                var maxZ = (float)(Bounds.Max.Z - offsetZ);
                var centerZ = (minZ + maxZ) / 2;

                float[,] points = new float[,]
                {
                    { (float)(Points[0].X - offsetX), (float)(Points[0].Y - offsetY) },
                    { (float)(Points[1].X - offsetX), (float)(Points[1].Y - offsetY) },
                    { (float)(Points[2].X - offsetX), (float)(Points[2].Y - offsetY) },
                    { (float)(Points[3].X - offsetX), (float)(Points[3].Y - offsetY) }
                };

                //If there is no panel info generate it
                if (PanelInfo.Count < 6)
                    GeneratePanelInfo();

                //Generate centerPoints
                var center1 = new Point3D(PanelInfo[0].PanelCenter.X - offsetX, PanelInfo[0].PanelCenter.Y - offsetY, PanelInfo[0].PanelCenter.Z - offsetZ);
                var center2 = new Point3D(PanelInfo[1].PanelCenter.X - offsetX, PanelInfo[1].PanelCenter.Y - offsetY, PanelInfo[1].PanelCenter.Z - offsetZ);
                var center3 = new Point3D(PanelInfo[2].PanelCenter.X - offsetX, PanelInfo[2].PanelCenter.Y - offsetY, PanelInfo[2].PanelCenter.Z - offsetZ);
                var center4 = new Point3D(PanelInfo[3].PanelCenter.X - offsetX, PanelInfo[3].PanelCenter.Y - offsetY, PanelInfo[3].PanelCenter.Z - offsetZ);
                var center5 = new Point3D(PanelInfo[4].PanelCenter.X - offsetX, PanelInfo[4].PanelCenter.Y - offsetY, PanelInfo[4].PanelCenter.Z - offsetZ);
                var center6 = new Point3D(PanelInfo[5].PanelCenter.X - offsetX, PanelInfo[5].PanelCenter.Y - offsetY, PanelInfo[5].PanelCenter.Z - offsetZ);

                //Foreach, generate the 4 points then add to the list of verts with color, if highlighted add a circle and highlighted color

                //1
                distance = PanelInfo[0].PanelSize;

                //Horizontal line
                brg = Trig.RPC(points[0, 0], points[0, 1], points[1, 0], points[1, 1]);
                Trig.PRC(center1.X, center1.Y, brg, distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add(maxZ);
                Trig.PRC(center1.X, center1.Y, brg, -distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add(maxZ);

                //Vertical line
                brg = Trig.RPC(points[0, 0], points[0, 1], points[3, 0], points[3, 1]);
                Trig.PRC(center1.X, center1.Y, brg, distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add(maxZ);
                Trig.PRC(center1.X, center1.Y, brg, -distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add(maxZ);

                if (highlightedFace == 1)
                {
                    //Draw blue square 
                    Geometry.GenerateSquareLineStripsFromLine(ref vertices, ref colors, distance, verts[verts.Count - 6], verts[verts.Count - 5], verts[verts.Count - 3], verts[verts.Count - 2], 0, maxZ, 0, 0, 1);
                    verts.AddRange(vertices);
                    cols.AddRange(colors);
                }

                //Color as blue
                cols.AddRange(new float[] { 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1 });

                //2
                distance = PanelInfo[1].PanelSize;

                //Horizontal line
                brg = Trig.RPC(points[0, 0], points[0, 1], points[3, 0], points[3, 1]);
                Trig.PRC(center2.X, center2.Y, brg, distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add((float)centerZ);
                Trig.PRC(center2.X, center2.Y, brg, -distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add((float)centerZ);

                //Vertical line
                verts.Add((float)center2.X); verts.Add((float)center2.Y); verts.Add((float)(centerZ - distance));
                verts.Add((float)center2.X); verts.Add((float)center2.Y); verts.Add((float)(centerZ + distance));

                if (highlightedFace == 2)
                {
                    //Draw green square
                    Geometry.GenerateSquareLineStripsFromLine(ref vertices, ref colors, distance, verts[verts.Count - 12], verts[verts.Count - 11], verts[verts.Count - 9], verts[verts.Count - 8], 1, centerZ, 0, 1, 0);
                    verts.AddRange(vertices);
                    cols.AddRange(colors);
                }

                //Color as green
                cols.AddRange(new float[] { 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0 });

                //3
                distance = PanelInfo[2].PanelSize;

                //Horizontal line
                brg = Trig.RPC(points[1, 0], points[1, 1], points[2, 0], points[2, 1]);
                Trig.PRC(center3.X, center3.Y, brg, distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add((float)centerZ);
                Trig.PRC(center3.X, center3.Y, brg, -distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add((float)centerZ);

                //Vertical line
                verts.Add((float)center3.X); verts.Add((float)center3.Y); verts.Add((float)(centerZ - distance));
                verts.Add((float)center3.X); verts.Add((float)center3.Y); verts.Add((float)(centerZ + distance));

                if (highlightedFace == 3)
                {
                    //Draw green square
                    Geometry.GenerateSquareLineStripsFromLine(ref vertices, ref colors, distance, verts[verts.Count - 12], verts[verts.Count - 11], verts[verts.Count - 9], verts[verts.Count - 8], 1, centerZ, 0, 1, 0);
                    verts.AddRange(vertices);
                    cols.AddRange(colors);
                }

                //Color as green
                cols.AddRange(new float[] { 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0 });

                //4
                distance = PanelInfo[3].PanelSize;

                //Horizontal line
                brg = Trig.RPC(points[3, 0], points[3, 1], points[2, 0], points[2, 1]);
                Trig.PRC(center4.X, center4.Y, brg, distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add((float)centerZ);
                Trig.PRC(center4.X, center4.Y, brg, -distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add((float)centerZ);

                //Vertical line
                verts.Add((float)center4.X); verts.Add((float)center4.Y); verts.Add((float)(centerZ - distance));
                verts.Add((float)center4.X); verts.Add((float)center4.Y); verts.Add((float)(centerZ + distance));

                if (highlightedFace == 4)
                {
                    //Draw red square
                    Geometry.GenerateSquareLineStripsFromLine(ref vertices, ref colors, distance, verts[verts.Count - 12], verts[verts.Count - 11], verts[verts.Count - 9], verts[verts.Count - 8], 1, centerZ, 1, 0, 0);
                    verts.AddRange(vertices);
                    cols.AddRange(colors);
                }

                //Color as red
                cols.AddRange(new float[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0 });

                //5
                distance = PanelInfo[4].PanelSize;

                //Horizontal Lines
                brg = Trig.RPC(points[0, 0], points[0, 1], points[1, 0], points[1, 1]);
                Trig.PRC(center5.X, center5.Y, brg, distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add((float)centerZ);
                Trig.PRC(center5.X, center5.Y, brg, -distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add((float)centerZ);

                //Vertical lines
                verts.Add((float)center5.X); verts.Add((float)center5.Y); verts.Add((float)(centerZ - distance));
                verts.Add((float)center5.X); verts.Add((float)center5.Y); verts.Add((float)(centerZ + distance));

                if (highlightedFace == 5)
                {
                    //Draw red square
                    Geometry.GenerateSquareLineStripsFromLine(ref vertices, ref colors, distance, verts[verts.Count - 12], verts[verts.Count - 11], verts[verts.Count - 9], verts[verts.Count - 8], 1, centerZ, 1, 0, 0);
                    verts.AddRange(vertices);
                    cols.AddRange(colors);
                }

                //Color as red
                cols.AddRange(new float[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0 });

                //6
                distance = PanelInfo[5].PanelSize;

                //Horizontal lines
                brg = Trig.RPC(points[0, 0], points[0, 1], points[3, 0], points[3, 1]);
                Trig.PRC(center6.X, center6.Y, brg, distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add(minZ);
                Trig.PRC(center6.X, center6.Y, brg, -distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add(minZ);

                //Vertical Lines
                brg = Trig.RPC(points[0, 0], points[0, 1], points[1, 0], points[1, 1]);
                Trig.PRC(center6.X, center6.Y, brg, distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add(minZ);
                Trig.PRC(center6.X, center6.Y, brg, -distance, ref newX, ref newY);
                verts.Add((float)newX); verts.Add((float)newY); verts.Add(minZ);

                if (highlightedFace == 6)
                {
                    //Draw blue square
                    Geometry.GenerateSquareLineStripsFromLine(ref vertices, ref colors, distance, verts[verts.Count - 6], verts[verts.Count - 5], verts[verts.Count - 3], verts[verts.Count - 2], 0, minZ, 0, 0, 1);
                    verts.AddRange(vertices);
                    cols.AddRange(colors);
                }

                //Color as blue
                cols.AddRange(new float[] { 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1 });

                //7 X center bar
                verts.Add((points[3, 0] + points[2, 0]) / 2); verts.Add((points[3, 1] + points[2, 1]) / 2); verts.Add((float)centerZ);
                verts.Add((points[0, 0] + points[1, 0]) / 2); verts.Add((points[0, 1] + points[1, 1]) / 2); verts.Add((float)centerZ);

                //Color Red
                cols.AddRange(new float[] { 1, 0, 0, 1, 0, 0 });

                //8 Y center bar
                verts.Add((points[3, 0] + points[0, 0]) / 2); verts.Add((points[3, 1] + points[0, 1]) / 2); verts.Add((float)centerZ);
                verts.Add((points[2, 0] + points[1, 0]) / 2); verts.Add((points[2, 1] + points[1, 1]) / 2); verts.Add((float)centerZ);

                //Color green
                cols.AddRange(new float[] { 0, 1, 0, 0, 1, 0 });

                //9 Z center bar
                verts.Add((float)center1.X); verts.Add((float)center1.Y); verts.Add(minZ);
                verts.Add((float)center1.X); verts.Add((float)center1.Y); verts.Add(maxZ);

                //Color blue
                cols.AddRange(new float[] { 0, 0, 1, 0, 0, 1 });

                vertices = verts.ToArray();
                colors = cols.ToArray();
            }
            catch
            {
                vertices = new float[0];
                colors = new float[0];
            }
        }

        /// <summary>
        /// Populates the <see cref="ClippingBox.PanelInfo"/> list based on the current dimensions of the <see cref="ClippingBox"/>
        /// </summary>
        public void GeneratePanelInfo()
        {
            PanelInfo = new List<(Point3D PanelCenter, double PanelSize)>();

            if (!IsValid)
                return;

            //Update the min and max values since the clipping box may have moved.
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var point in Points)
            {
                if (point.X < minX)
                    minX = point.X;
                if (point.X > maxX)
                    maxX = point.X;

                if (point.Y < minY)
                    minY = point.Y;
                if (point.Y > maxY)
                    maxY = point.Y;
            }

            Bounds.Min.X = minX;
            Bounds.Min.Y = minY;
            Bounds.Max.X = maxX;
            Bounds.Max.Y = maxY;

            Point3D center;
            double panelSize;

            var centerZ = (Bounds.Min.Z + Bounds.Max.Z) / 2;

            //1
            center = new Point3D((Points[0] + Points[1] + Points[2] + Points[3]) / 4, Bounds.Max.Z);
            panelSize = Math.Min(0.5, Vector.FnDistance(center.X, center.Y, (Points[0].X + Points[1].X) / 2, (Points[0].Y + Points[1].Y) / 2));
            panelSize = Math.Min(panelSize, Vector.FnDistance(center.X, center.Y, (Points[1].X + Points[2].X) / 2, (Points[1].Y + Points[2].Y) / 2));
            PanelInfo.Add((center, panelSize));

            //2
            center = new Point3D((Points[0] + Points[3]) / 2, centerZ);
            panelSize = Math.Min(0.5, Vector.FnDistance(center.X, center.Y, Points[0].X, Points[0].Y));
            panelSize = Math.Min(panelSize, Bounds.Max.Z - centerZ);
            PanelInfo.Add((center, panelSize));

            //3
            center = new Point3D((Points[1] + Points[2]) / 2, centerZ);
            panelSize = Math.Min(0.5, Vector.FnDistance(center.X, center.Y, Points[1].X, Points[1].Y));
            panelSize = Math.Min(panelSize, Bounds.Max.Z - centerZ);
            PanelInfo.Add((center, panelSize));

            //4
            center = new Point3D((Points[2] + Points[3]) / 2, centerZ);
            panelSize = Math.Min(0.5, Vector.FnDistance(center.X, center.Y, Points[2].X, Points[2].Y));
            panelSize = Math.Min(panelSize, Bounds.Max.Z - centerZ);
            PanelInfo.Add((center, panelSize));

            //5
            center = new Point3D((Points[0] + Points[1]) / 2, centerZ);
            panelSize = Math.Min(0.5, Vector.FnDistance(center.X, center.Y, Points[0].X, Points[0].Y));
            panelSize = Math.Min(panelSize, Bounds.Max.Z - centerZ);
            PanelInfo.Add((center, panelSize));

            //6
            center = new Point3D((Points[0] + Points[1] + Points[2] + Points[3]) / 4, Bounds.Min.Z);
            panelSize = Math.Min(0.5, Vector.FnDistance(center.X, center.Y, (Points[0].X + Points[1].X) / 2, (Points[0].Y + Points[1].Y) / 2));
            panelSize = Math.Min(panelSize, Vector.FnDistance(center.X, center.Y, (Points[1].X + Points[2].X) / 2, (Points[1].Y + Points[2].Y) / 2));
            PanelInfo.Add((center, panelSize));
        }

        #endregion

        #endregion
    }



    public class DrawingLayer//same as CAD layers, ish
    {
        public string Name = "Default";
        public Color Colour = Color.Black;
        public bool Triangulate = true;
        public bool Draw = true;
        public int Lineweight = 1;

    }

    public class ShapeVector
    {

        #region properties
        public Point2D StartPoint;
        public Point2D EndPoint;
        public double Radius;
        #endregion region properties


        #region Instantiate
        public ShapeVector()
        {

        }
        public ShapeVector(Point2D StartPoint, Point2D EndPoint, double Radius)
        {

        }
        #endregion Instantiate


        #region methods

        //need reverse element here

        public void Reverse()
        {
            //Point2D  tmp
        }

        //
        //Need arc centre calc in case of element reversal
        //also need to calc element length for straights and and arc - for chainage calcs
        #endregion methods

    }

    #region Standard & PolyLines
    public class PolyLine
    {
        #region Properties

        public BindingList<DTMPoint> Nodes = new BindingList<DTMPoint>();
        public bool IsClosed = false;

        /// <summary>
        /// Use wrapped methods for layer interaction where possible
        /// </summary>
        public string OriginalEntityName = "";
        public string Layer = "Default";
        public Color Colour = Color.Black;
        //public SharedRenderItem RenderItem { get; set; }//Rather than individual lines having a render item, the wrapper "CollectionOf[Lines]" contains the render item for all the lines 23.06.22 TN
        public int Lineweight = 1;
        public bool LwByLayer = false;
        public int ID = 0; //Added 16.08.22 by TN. Use freely for indexing/matching entities/debugging etc

        #endregion


        #region Methods

        public bool AddToLayer(DTM dtm, string layerString = null)
        {
            //If the layer passed isn't valid, add object to the Default layer
            if (layerString == null || string.IsNullOrWhiteSpace(layerString))
                layerString = "Default";

            //If layer exists, it'll be set. If not, a new layer is made and then set.
            if (dtm.CheckIfLayerExists(layerString))
            {
                this.Layer = layerString;
                return true;
            }
            else
            {
                if (dtm.AddLayer(layerString))
                {
                    this.Layer = layerString;
                    return true;
                }

            }
            //If this returns false, something is really wrong.
            return false;
        }

        #endregion
    }


    public class CollectionOfAllPolyLines
    {
        public List<PolyLine> PolyLines { get; set; }
        public SharedRenderItem RenderItem { get; set; }
    }

    public class StandardLine
    {
        #region Properties

        public DTMPoint StartPoint = new DTMPoint();
        public DTMPoint EndPoint = new DTMPoint();
        /// <summary>
        /// Use wrapped methods for layer interaction where possible
        /// </summary>
        public string OriginalEntityName = "";

        public double Bearing { get { return GetBearing();}} //added by ES:07.07.22
        public double Length { get { return GetLength();}} //Added by ES:07.07.22
        public int ID { get; set; } //Added by TN 11.08.22, simple ID you can use freely as and when needed

        public string Layer { get; set; }
        public Color Colour = Color.Black;
        //public SharedRenderItem RenderItem { get; set; } //Rather than individual lines having a render item, the wrapper "CollectionOf[Lines]" contains the render item for all the lines 23.06.22 TN
        public int Lineweight = 1;
        public bool LwByLayer = false;

        #endregion

        #region Setup
        public StandardLine()
        {

        }

        public StandardLine(DTMPoint startPoint, DTMPoint endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }
        #endregion

        #region Methods

        public StandardLine ReverseClone()
        {
            //Swap the start/end points
            StandardLine ra = new StandardLine(this.EndPoint, this.StartPoint);

            //the rest of the cosmetic stuff for an arc
            ra.Colour = this.Colour;
            ra.Layer = this.Layer;
            ra.ID = this.ID;

            return ra;
        }


        //Added by ES:07.07.22 - returns the WCB from start point to end point
        private double GetBearing()
        {
            return MathsHelpers.Trig.RPC(StartPoint.X, StartPoint.Y, EndPoint.X, EndPoint.Y);
        }

        //Added by ES:07.07.22 - returns the length from the start point to end point
        private double GetLength()
        {
            return MathsHelpers.Vector.FnDistance(StartPoint, EndPoint);
        }

        public bool AddToLayer(DTM dtm, string layerString = null)
        {
            //If the layer passed isn't valid, add object to the Default layer
            if (layerString == null || string.IsNullOrWhiteSpace(layerString))
                layerString = "Default";

            //If layer exists, it'll be set. If not, a new layer is made and then set.
            if (dtm.CheckIfLayerExists(layerString))
            {
                this.Layer = layerString;
                return true;
            }
            else
            {
                if (dtm.AddLayer(layerString))
                {
                    this.Layer = layerString;
                    return true;
                }

            }
            //If this returns false, something is really wrong.
            return false;
        }

        public DistanceAndOffset GetDistanceAndOffset(Point2D pt)
        {
            double ch = 0, off = 0;

            MathsHelpers.Trig.DistanceAndOffsetFromLine(this.StartPoint.X, this.StartPoint.Y, this.EndPoint.X, this.EndPoint.Y, pt.X, pt.Y, ref ch, ref off);
            return new DistanceAndOffset(ch, off);
        }


        #endregion
    }

    public class CollectionOfAllStandardLines
    {
        public List<StandardLine> StandardLines { get; set; }
        public SharedRenderItem RenderItem { get; set; }
    }
    #endregion

    #region Arcs
    public class Arc : StandardLine
    {
        #region Properties

        //StartPoint and EndPoint inherited from StandardLine. These are the main controllers for the arc along with Radius & CentrePointSide


        public Point3D CentrePoint { get { return GetCentrePoint(); } }
        public Point3D MidPoint { get { return GetMidPoint(); } }
        public double ArcLength { get { return GetArcLength(); } }


        //Start and End Bearings & CentrePoint & MidPoint are only Gets. They are detirmined by the Start and End Point positions & Radius
        public double StartBearing { get { return GetStartBearing(); } }
        public double EndBearing { get { return GetEndBearing(); } }

        public double EntryBearing { get { return GetEntryBearing(); } }
        public double ExitBearing { get { return GetExitBearing(); } }

        public bool Clockwise { get { return GetClockwise(); } }
        public double Radius;
        public bool CentrePointPosition = true;//true left, false right //A switch to decide which side the centre point lays (left or right of start-end point)
        //^^ change to index 0,1,2 if we need an "On the line" setting

        #endregion

        #region Setup

        public Arc(DTMPoint startPoint, DTMPoint endPoint, double radius, bool centrePointSide)
        {
            Radius = radius;
            StartPoint = startPoint;
            EndPoint = endPoint;
            CentrePointPosition = centrePointSide;
        }

        #endregion

        #region Methods
        /// <summary>
        /// ReverseClone - added by ES:11.07.22 shite name, but reurns a cloned version of this object in reverse ie start is end and vice versa
        /// edited by TN 13.07.22
        /// </summary>
        /// <returns></returns>
        public new Arc ReverseClone()
        {
            //Swap the start/end points
            Arc ra = new Arc(this.EndPoint, this.StartPoint, -this.Radius, !this.CentrePointPosition);

            //the rest of the cosmetic stuff for an arc
            ra.Colour = this.Colour;
            ra.Layer = this.Layer;
            ra.ID = this.ID;

            return ra;  
        }


        //Calculate start and end points
        //Point3D arcStart3D = new Point3D(MathsHelpers.Trig.PRC(cadArc.Center.X, cadArc.Center.Y, MathsHelpers.Trig.FNBearingToAngle(cadArc.StartAngle), cadArc.Radius), cadArc.Center.Z);
        //Point3D arcEnd3D = new Point3D(MathsHelpers.Trig.PRC(cadArc.Center.X, cadArc.Center.Y, MathsHelpers.Trig.FNBearingToAngle(cadArc.EndAngle), cadArc.Radius), cadArc.Center.Z);

        public bool GetClockwise()
        {
            if (Radius > 0) { return true; }
            else { return false; }
        }

        private double GetStartBearing()
        {
            Point3D cp = CentrePoint;
            return NRG.MathsHelpers.Trig.RPC(cp.X, cp.Y, StartPoint.X, StartPoint.Y);
        }

        private double GetEndBearing()
        {
            Point3D cp = CentrePoint;
            return NRG.MathsHelpers.Trig.RPC(cp.X, cp.Y, EndPoint.X, EndPoint.Y);
        }

        private Point3D GetCentrePoint()
        {
            double cX = 0; double cY = 0;
            NRG.MathsHelpers.Trig.CalcCenterOfArc(StartPoint.X, StartPoint.Y, EndPoint.X, EndPoint.Y, Radius, ref cX, ref cY, CentrePointPosition);
            double cZ = (StartPoint.Z + EndPoint.Z) / 2;
            return new Point3D(cX, cY, cZ);
        }


        private double GetEntryBearing()
        {

            double eb = 0;
            double exb = 0;
            double len = 0;

            Point3D cp = CentrePoint;
            NRG.MathsHelpers.Trig.CalcEntryAndExitBrg(StartPoint.X, StartPoint.Y, EndPoint.X, EndPoint.Y, cp.X, cp.Y , Radius, ref eb, ref exb, ref len);

            return eb;

            

        }
        private double GetExitBearing()
        {
            double eb = 0;
            double exb = 0;
            double len = 0;

            Point3D cp = CentrePoint;
            NRG.MathsHelpers.Trig.CalcEntryAndExitBrg(StartPoint.X, StartPoint.Y, EndPoint.X, EndPoint.Y, cp.X, cp.Y, Radius, ref eb, ref exb, ref len) ;

            return exb;
        }
        /// <summary>.
        /// Mid point of arc is the point on the arc halfway between the start and end of the element
        /// </summary>
        /// <returns></returns>
        private Point3D GetMidPoint()
        {
            if (MathsHelpers.Vector.FnDistance(StartPoint, EndPoint) <= 0.001)
            {
                //not really an arc so return the start - no one will every know. Well they will because the drawing will look shite, but it's not really my fault
                return StartPoint;
            }

            Point3D cp = CentrePoint;
            Point3D coordMid = (StartPoint + EndPoint) / 2;

            double brg = MathsHelpers.Trig.RPC(cp.X, cp.Y, coordMid.X, coordMid.Y);
            double r = Math.Abs(Radius);
            if (!CentrePointPosition)
            {
                r = -r;
            }
            Point2D mp = MathsHelpers.Trig.PRC(cp.X, cp.Y, brg, r);

            return new Point3D(mp.X, mp.Y);
        }

        private double GetArcLength()
        {
            Point3D cp = CentrePoint;

            if (MathsHelpers.Vector.FnDistance(StartPoint, EndPoint) == 0)
            {
                return 0;
            }
            return MathsHelpers.Geometry.ArcLength(StartBearing, EndBearing, Radius);
        }


        public new DistanceAndOffset GetDistanceAndOffset(Point2D pt)
        { 

            DistanceAndOffset dao = new DistanceAndOffset();
            Point2D cp = CentrePoint;

            double testAngle = 0;
            double dist = 0;
            MathsHelpers.Trig.RPC(cp.X, cp.Y, pt.X, pt.Y ,ref testAngle, ref dist);
            bool pointInArcWedge = false;

            if (this.Clockwise)
            {
                if (AngleInSplay(this.StartBearing, this.EndBearing, testAngle))
                {
                    pointInArcWedge = true;
                    dao.Distance = MathsHelpers.Geometry.ArcLength(StartBearing, testAngle, Radius);
                }
            }
            else
            {
                if (AngleInSplay(this.EndBearing, this.StartBearing, testAngle))
                {
                    pointInArcWedge = true;
                    dao.Distance =  MathsHelpers.Geometry.ArcLength(EndBearing, testAngle, Radius);

                }

            }

            if (pointInArcWedge)
            {
                dao.Offset = Math.Abs(Radius) - dist;
            }
          
            return dao;
        }

        private bool AngleInSplay(double startAngle, double endAngle, double testAngle)
        {
            return  MathsHelpers.Trig.FnAngle(startAngle, endAngle) < MathsHelpers.Trig.FnAngle(startAngle, testAngle);
        }



        #endregion
    }

    


    public class CollectionOfAllArcs
    {
        public List<Arc> Arcs { get; set; }
        public SharedRenderItem RenderItem { get; set; }
    }

    #endregion

    #region Spiral - Prototype

    public class Spiral : StandardLine
    {
        #region Properties
        public object StartEntity { get; set; }
        public object EndEntity { get; set; }

        #endregion
        
        #region Setup
        public Spiral(object startEntity, object endEntity, DTMPoint startPoint, DTMPoint endPoint)
        {
            StartEntity = startEntity;
            EndEntity = endEntity;
            StartPoint = startPoint;
            EndPoint = endPoint;
        }
        #endregion

        #region Methods

        public new Spiral ReverseClone()
        {
            //Swap the start/end points
            Spiral ra = new Spiral(this.EndEntity, this.StartEntity, this.EndPoint, this.StartPoint);

            //the rest of the cosmetic stuff for an arc
            ra.Colour = this.Colour;
            ra.Layer = this.Layer;
            ra.ID = this.ID;
            return ra;
        }
        #endregion
    }

    #endregion

    public class DistanceAndOffset
    {
        public double Distance = double.NegativeInfinity;
        public double Offset = double.NegativeInfinity;

        public DistanceAndOffset()
        {

        }

        public DistanceAndOffset(double dist, double off)
        {
            Distance = dist;
            Offset = off;
        }
        public override string ToString()
        {
            if (Distance > double.NegativeInfinity && Offset > double.NegativeInfinity)
            {
                return "Distance: " + Distance.ToString("0.000") + " Offset: " + Offset.ToString("0.000");
            }
            else
            {
                return "NA";
            }
        }

    }



    #region Blocks

    public class BlockInsert
	{
		#region Properties
        public string BlockDefinitionHandle { get; set; }

        public Point3D InsertionOffset { get; set; }
        public Point3D Scale { get; set; }

        public string Layer { get; set; }
        public double Rotation { get; set; }

        public Color Colour = Color.Black;
		#endregion

		#region Setup
        public BlockInsert()
		{
            BlockDefinitionHandle = "";
            InsertionOffset = new Point3D();
            Layer = "Default";
            Scale = new Point3D();
            Rotation = 0;
		}
        #endregion

        #region Methods

        public bool AddToLayer(DTM dtm, string layerString = null)
        {
            //If the layer passed isn't valid, add object to the Default layer
            if (layerString == null || string.IsNullOrWhiteSpace(layerString))
                layerString = "Default";

            layerString = layerString.Replace(" ", null);

            //If layer exists, it'll be set. If not, a new layer is made and then set.
            if (dtm.CheckIfLayerExists(layerString))
            {
                this.Layer = layerString;
                return true;
            }
            else
            {
                if (dtm.AddLayer(layerString))
                {
                    this.Layer = layerString;
                    return true;
                }

            }
            //If this returns false, something is really wrong.
            return false;
        }

        #endregion
    }

	public class BlockLine
    {
        #region Properties

        public Point3D StartPoint { get; set; }
        public Point3D EndPoint { get; set; }
        public double Radius { get; set; }

		#endregion

		#region Setup

        public BlockLine() //These are essentially our Arc objects. could probably merge them at some pint
		{
            StartPoint = new Point3D();
            EndPoint = new Point3D();
		}

        public BlockLine(Point3D startPoint, Point3D endPoint)
		{
            StartPoint = startPoint;
            EndPoint = endPoint;
		}
		#endregion

		#region Methods

		#endregion
	}

	public class BlockDefinition
    {
        #region Properties
        public string Handle { get; set; }
        public string Name {  get; set; }
        public List<BlockLine> BlockLines { get; set; }
        #endregion

        #region Setup
        public BlockDefinition()
		{
            Handle = "";
            Name = "";
            BlockLines = new List<BlockLine>();
		}
        #endregion

        #region Methods

        /// <summary>
        /// Turns PolyLines into BlockLines for a BlockDefinition (careful of the PolyLine node coordinates, they may need to be adjusted beforehand)
        /// </summary>
        /// <param name="polyline"></param>
        public void AddBlockLines(PolyLine polyline)
        {
            bool startFound = false;
            var startPt = new Point3D();
            var endPt = new Point3D();

            foreach (var node in polyline.Nodes)
            {

                if (!startFound)
                {

                    startPt.X = node.X;
                    startPt.Y = node.Y;
                    startPt.Z = node.Z;
                    startFound = true;
                }
                else
                {
                    endPt.X = node.X;
                    endPt.Y = node.Y;
                    endPt.Z = node.Z;

                    BlockLine newline = new BlockLine();
                    newline.StartPoint = startPt;
                    newline.EndPoint = endPt;
                    startPt = endPt;
                    endPt = new Point3D();

                    this.BlockLines.Add(newline);
                }
            }

            if (polyline.IsClosed && polyline.Nodes.Count > 2)
            {
                BlockLine closingLine = new BlockLine();
                closingLine.StartPoint.X = polyline.Nodes[polyline.Nodes.Count - 1].X;
                closingLine.StartPoint.Y = polyline.Nodes[polyline.Nodes.Count - 1].Y;
                closingLine.StartPoint.Z = polyline.Nodes[polyline.Nodes.Count - 1].Z;
                closingLine.EndPoint.X = polyline.Nodes[0].X;
                closingLine.EndPoint.Y = polyline.Nodes[0].Y;
                closingLine.EndPoint.Z = polyline.Nodes[0].Z;
                this.BlockLines.Add(closingLine);
            }
        }

        public void AddBlockLines(StandardLine standardLine)
        {
            BlockLine bl = new BlockLine(standardLine.StartPoint, standardLine.EndPoint);
            BlockLines.Add(bl);
        }

        public void AddBlockLines(Arc arcLine)
        {
            //Calc arc nodes
            var arcNodes = NRG.MathsHelpers.Trig.CalcVerticesOfArc(arcLine.CentrePoint.X, arcLine.CentrePoint.Y, arcLine.StartBearing, arcLine.EndBearing, arcLine.Radius);
            //Create individual blocklines
            for(int i = 0; i < arcNodes.Count - 1; i++) 
            {
                BlockLine bl = new BlockLine(new Point3D(arcNodes[i].X, arcNodes[i].Y, arcLine.CentrePoint.Z), new Point3D(arcNodes[i+1].X, arcNodes[i+1].Y, arcLine.CentrePoint.Z));
                bl.Radius = arcLine.Radius;
                BlockLines.Add(bl);
            }
        }


        #endregion
    }

    /// <summary>
    /// The model's BlockDefinitions. There should only one of these objects per DTM.
    /// </summary>
    public class CollectionOfBlocks
    {
        /// <summary>
        /// The Block "Templates" from where to get the render info from
        /// </summary>
        public Dictionary<string, BlockDefinition> BlockDefinitions { get; set; }

        /// <summary>
        /// The shared render item contains every instance of a block, not just their definitions
        /// </summary>
        public SharedRenderItem RenderItem { get; set; }
        

        public CollectionOfBlocks()
		{
            this.BlockDefinitions = new Dictionary<string, BlockDefinition>();
            this.RenderItem = new SharedRenderItem();
		}


	}

    #endregion


    #region DTM

    #region Points


    [Serializable]
    public class DTMPoint : Point3D
    {
        #region Properties

        public PointType Type { get; set; }
        public string PointID { get; set; }
        public string PointLabel { get; set; }
        public int ID { get; set; }
        public double HighlightBearing = -1; //added by ES:10.01.21 - for display purposes

        public string Layers { get { return FormatAllLayers(); } private set { } } //Getting this will return the two layer hashsets in a nice formatted string
        public HashSet<string> ManualLayers { get; set; } //Layers assigned manually by the user (or us).
        public HashSet<string> AutoLayers { get; set; } //Layers frequently wiped and reassigned depending on which entity this point is used by.

        public HashSet<string> OriginalEntityNames { get; set; } //A basic store of where this points come from/what it belonged to. maybe a dxf polyline etc etc. Nothing fancy

        public List<PointFeatureCollection> PointFeatures { get; set; }
        public List<LineFeatureItem> LineFeatures { get; set; }
        public List<CADLineFeatureItem> CADFeatures { get; set; }
        public List<Triangle> Triangles { get; set; }
        public List<BlockInsert> BlockInserts { get; set; }
        public Dictionary<int,string> Notes { get; set; }

        /// <summary>
        /// Setting Out Info Wrappers
        /// </summary>
        #region Setting Out
        private SettingOut SettingOutInfo { get; set; }
        public double Chainage {  get { return SettingOutInfo.Chainage; } }
        public double Offset { get { return SettingOutInfo.Offset; } }
        public double HorizontalRadius { get { return SettingOutInfo.HorizontalRadius; } }
        public double VerticalAlignmentLevel { get { return SettingOutInfo.VerticalAlignmentLevel; } }
        public double HorizontalVectorDistancetoNearestBMPoint { get { return SettingOutInfo.HorizontalVectorDistancetoNearestBMPoint; } }
        public double Vector3DDistancetoNearestBMPoint { get { return SettingOutInfo.Vector3DDistancetoNearestBMPoint; } }
        public double HeightDifferenceFromNearestBMPoint { get { return SettingOutInfo.HeightDifferenceFromNearestBMPoint; } }
        public double HeightDifferencetoBMModel { get { return SettingOutInfo.HeightDifferencetoBMModel; } }
        public double RadialOffsettoAlignment { get { return SettingOutInfo.RadialOffsettoAlignment; } }
        public double Nearest3DBGPointRadialOffset { get { return SettingOutInfo.Nearest3DBGPointRadialOffset; } }
        public string Nearest3DBGPointLabel { get { return SettingOutInfo.Nearest3DBGPointLabel; } }
        public double SlopeDistanceFromNearest3DBGPoint { get { return SettingOutInfo.SlopeDistanceFromNearest3DBGPoint; } }
        public double Lips { get { return SettingOutInfo.Lips; } }
        public double BMLevel { get { return SettingOutInfo.BMLevel; } }
        public double NearestBMPointEasting { get { return SettingOutInfo.NearestBMPointEasting; } }
        public double NearestBMPointNorthing { get { return SettingOutInfo.NearestBMPointNorthing; } }
        public double NearestBMPointLevel { get { return SettingOutInfo.NearestBMPointLevel; } }

        #endregion

        #endregion

        #region Setup



        /// <summary>
        /// Creates an empty <see cref="DTMPoint"/> object with a null level
        /// </summary>
        public DTMPoint()
        {
           
            X = 0;
            Y = 0;
            Z = -999;
            Type = PointType.Coordinate;
            PointID = "";
            SettingOutInfo = new SettingOut(this);
            ManualLayers = new HashSet<string> { "Default" };
            OriginalEntityNames = new HashSet<string>();
            PointLabel = "";

        }

        /// <summary>
        /// Converts a <see cref="Point3D"/> into a <see cref="DTMPoint"/>
        /// </summary>
        /// <param name="point">The <see cref="Point3D"/> to convert</param>
        public DTMPoint(Point3D point)
        {
            X = point.X;
            Y = point.Y;
            Z = point.Z;
            Type = PointType.Coordinate;
            PointID = "";
            ManualLayers = new HashSet<string> { "Default" };
            OriginalEntityNames = new HashSet<string>();
            PointLabel = "";
        }

        

        /// <summary>
        /// Converts a <see cref="Point2D"/> into a <see cref="DTMPoint"/> with null level
        /// </summary>
        /// <param name="point">The <see cref="Point2D"/> to convert</param>
        public DTMPoint(Point2D point)
        {
            X = point.X;
            Y = point.Y;
            Z = -999;
            Type = PointType.Coordinate;
            PointID = "";
            SettingOutInfo = new SettingOut(this);
            ManualLayers = new HashSet<string> { "Default" };
            OriginalEntityNames = new HashSet<string>();
            PointLabel = "";
        }

        public DTMPoint(Point2D point, double z)
        {
            X = point.X;
            Y = point.Y;
            Z = z;
            Type = PointType.Coordinate;
            PointID = "";
            SettingOutInfo = new SettingOut(this);
            ManualLayers = new HashSet<string> { "Default" };
            OriginalEntityNames = new HashSet<string>();
            PointLabel = "";
        }

        /// <summary>
        /// Creates a <see cref="DTMPoint"/> from a set of coordinates
        /// </summary>
        /// <param name="x">The X coordinate for the point</param>
        /// <param name="y">The Y coordinate for the point</param>
        /// <param name="z">The Z coordinate for the point</param>
        public DTMPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
            Type = PointType.Coordinate;
            PointID = "";
            SettingOutInfo = new SettingOut(this);
            ManualLayers = new HashSet<string> { "Default" };
            OriginalEntityNames = new HashSet<string>();
            PointLabel = "";
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Initialises the point with the information needed for the Setting Out collumns
        /// </summary>
        /// <param name="BackgroundModel"></param>
        /// <param name="Alg"></param>
        public void UpdateSettingOutInfo(DTM BackgroundModel = null, Alignment Alg = null)
        {
            if (SettingOutInfo == null) { SettingOutInfo = new SettingOut(this, BackgroundModel, Alg); }
            else 
            {
                if (BackgroundModel != null) { SettingOutInfo.UpdateBackgroundModel(BackgroundModel); }
                if (BackgroundModel != null) { SettingOutInfo.UpdateAlignment(Alg); }
            }
        }

        /// <summary>
        /// Initialises the point with the information needed for the tunnel related Setting Out collumns. These values should be from the registry
        /// </summary>
        public void UpdateSettingOutInfoTunnelSettings(double AlignmentOffsetLevel, double Max3DDistance, bool Max3DDistanceEnabled)
        {
            if (SettingOutInfo == null) { return; }
            else
            {
                SettingOutInfo.UpdateAlignmentOffsetLevel(AlignmentOffsetLevel);
                SettingOutInfo.UpdateMax3DDistance(Max3DDistance);
                SettingOutInfo.UpdateMax3DDistanceEnabled(Max3DDistanceEnabled);
            }
        }

        /// <summary>
        /// A wrapper to disable the "Moved", "chainageAndOffsetUpdated" and "nearestBMPoint" updated flags ready for the point to be moved again and the setting out columns to be recalculated.
        /// </summary>
        public void InitRecalc(bool ForceUpdateAll)
        {
            if (SettingOutInfo != null)
            {
                SettingOutInfo.InitialiseRecalc(ForceUpdateAll); //Set the internal flags to false so new values can be made (if a point is flagged as Moved)

            }
        }

        public void FinishedRecalc()
        {
            if (SettingOutInfo != null)
            {
                SettingOutInfo.FinishRecalculating();
            }
        }


        private string FormatAllLayers()
        {
            //Null reference checks
            if (AutoLayers == null) { AutoLayers = new HashSet<string>(); }
            if (ManualLayers == null) { ManualLayers = new HashSet<string>(); }

            string fullLayerString = "";

            var test = AutoLayers.Union(ManualLayers); //Join the two hashsets together without duplicates.
            HashSet<string> allLayers = new HashSet<string>(test);
            if (allLayers.Count == 0) { ManualLayers.Add("Default"); } //Ensure Default is set if no other layers are present

            foreach(string layer in allLayers)
            {
                fullLayerString += "," + layer;
            }

            fullLayerString = fullLayerString.Trim(',');

            return fullLayerString;

        }

        /// <summary>
        /// Add a layer to the points ManualLayers list. Pass a DTM for checks to be performed, "ExistingLayer" etc 
        /// </summary>
        /// <param name="dtm"></param>
        /// <param name="layerString"></param>
        /// <returns></returns>
        public bool AddALayer(DTM dtm, string layerString = "Default")
        {
            if (ManualLayers == null) { ManualLayers = new HashSet<string>(); }

            //If the layer passed isn't valid, add object to the Default layer
            if (layerString == null || string.IsNullOrWhiteSpace(layerString)) { layerString = "Default"; }

            //If layer doesn't exist in the model, a new layer is made.
            if (!dtm.CheckIfLayerExists(layerString))
            {
                dtm.AddLayer(layerString);
            }

            if (ManualLayers.Count == 0) { ManualLayers.Add("Default"); }

            return true;
        }

        /// <summary>
        /// Remove a layer from the points ManualLayers list.
        /// </summary>
        /// <param name="layerString"></param>
        /// <returns></returns>
        public bool RemoveALayer(string layerString)
        {
            if (ManualLayers == null) { ManualLayers = new HashSet<string>(); }

            if (string.IsNullOrWhiteSpace(layerString)) { return false; }

            return ManualLayers.Remove(layerString);
        }



        public DTMPoint DeepCopy()
        {
            DTMPoint newPoint = new DTMPoint();
            //coords...
            newPoint.X = this.X;
            newPoint.Y = this.Y;
            newPoint.Z = this.Z;
            //Extended stuff...
            //pt ID'
            newPoint.PointID = this.PointID;
            //label
            newPoint.PointLabel = this.PointLabel;
            //type
            newPoint.Type = this.Type;
            newPoint.Notes = this.Notes;
            newPoint.BlockInserts = this.BlockInserts;
            newPoint.R = this.R;
            newPoint.G = this.G;
            newPoint.B = this.B;
            newPoint.ID = this.ID;
            newPoint.AutoLayers = this.AutoLayers;
            newPoint.ManualLayers = this.ManualLayers;
            newPoint.OriginalEntityNames = this.OriginalEntityNames;

            //Not copying triangles as a triangle has a pointer to the original point... nor are we copying SettingOutInfo because.
            
            //And for the sake of sanity that's how deep it goes, for now.

            return newPoint;

        }

        public void AddTriangle(Triangle triangle)
        {
            if (Triangles == null)
                Triangles = new List<Triangle>();

            Triangles.Add(triangle);
        }

        public void RemoveTriangle(Triangle triangle)
        {
            if(Triangles != null && Triangles.Remove(triangle))
            {
                if (Triangles.Count <= 0)
                    Triangles = null;
            }
        }

        public void GetPointFeatures(ref List<PointFeatureCollection> pointFeatures)
        {
            if (pointFeatures == null)
                pointFeatures = new List<PointFeatureCollection>();

            if(PointFeatures != null)
            {
                foreach(var pointFeature in PointFeatures)
                {
                    if (!pointFeatures.Contains(pointFeature))
                        pointFeatures.Add(pointFeature);
                }
            }
        }

        public void GetLineFeatures(ref List<LineFeatureCollection> lineFeatures)
        {
            if (lineFeatures == null)
                lineFeatures = new List<LineFeatureCollection>();

            if(LineFeatures != null)
            {
                foreach(var item in LineFeatures)
                {
                    if(item.FeatureCollection != null && !lineFeatures.Contains(item.FeatureCollection))
                        lineFeatures.Add(item.FeatureCollection);
                }
            }

            if(CADFeatures != null)
            {
                foreach(var item in CADFeatures)
                {
                    if(item.FeatureCollection != null && !lineFeatures.Contains(item.FeatureCollection))
                        lineFeatures.Add(item.FeatureCollection);
                }
            }
        }

        public void GetSurfaces(ref List<TriangleSurface> surfaces)
        {
            if (surfaces == null)
                surfaces = new List<TriangleSurface>();

            if(Triangles != null)
            {
                foreach(var tri in Triangles)
                {
                    if(tri.Surface != null && !surfaces.Contains(tri.Surface))
                        surfaces.Add(tri.Surface);
                }
            }
        }

        

        #endregion
    }

    /// <summary>
    /// A wrapper for a DTMPoint which contains additional properties such as Chainage and Offset. Getting and Setting these properties modifies the point/properties.
    /// </summary>
    /// Initially used as a datasource for Spread grid as the grid requires properties to be bound to specific columns
    public class AlignmentPoint : DTMPoint
    {
        #region Properties
        private double chainage;
        private double offset;
        public double Chainage { get { return GetChainage(); } set { chainage = value; SetPoint(); } }
        public double Offset { get { return GetOffset(); } set { offset = value; SetPoint(); } }

        private Alignment Alignment { get; set; } = new Alignment();
        #endregion

        #region Setup
        /// <summary>
        /// A wrapper for a DTMPoint which adds a Chainage and Offset property. These properties are dynamically read and created depending on the point properties modified.
        /// </summary>
        /// <param name="alg">The alignment that the point's properties are generated from.</param>
        public AlignmentPoint(Alignment alg)
        {
            this.Alignment = alg;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the current chainage of the point from its X,Y coords
        /// </summary>
        /// <returns></returns>
        private double GetChainage()
        {
            double ch = 0;
            AlignmentVector5 outVec = new AlignmentVector5();
            if (Alignment.Horizontal.GetChainageAndOffset(this.X, this.Y, ref outVec))
            {
                ch = outVec.chainage;
            }

            return ch;
        }

        /// <summary>
        /// Gets the current offset of the point from its X,Y coords
        /// </summary>
        /// <returns></returns>
        private double GetOffset()
        {
            double off = 0;
            AlignmentVector5 outVec = new AlignmentVector5();
            if (Alignment.Horizontal.GetChainageAndOffset(this.X, this.Y, ref outVec))
            {
                off = outVec.offset;
            }

            return off;
        }

        /// <summary>
        /// Sets the current X,Y of the point based on its current Chainage and Offset values
        /// </summary>
        /// <returns></returns>
        private void SetPoint()
        {
            
            Point2D returnPoint = new Point2D();
            returnPoint = Alignment.Horizontal.CoordsAtChAndOffset(chainage, offset);
            {
                this.X = returnPoint.X;
                this.Y = returnPoint.Y;
            }
        }

       

        #endregion
    }

    public class StationPoint : DTMPoint 
    {
        #region Properties

        public StationSetup StationSetup { get; set; }
        public double ObjectHeight { get; set; } //instrument height of occupied or target height for reference object

        #endregion

        #region Setup

        /// <summary>
        /// Creates a new <see cref="StationPoint"/> with a given <see cref="PointType"/>
        /// </summary>
        /// <param name="type">The <see cref="PointType"/> for the <see cref="StationPoint"/>. Must be either <see cref="PointType.OccupiedStation"/> or <see cref="PointType.ReferenceStation"/></param>
        public StationPoint(PointType type)
        {
            Type = type;
        }

        #endregion
    }

    public class ObservationPoint : DTMPoint
    {
        #region Properties

        public double HCR { get; set; }
        public double VCR { get; set; }
        public double SD { get; set; }
        public double TargetHeight { get; set; }

        #endregion

        #region Setup

        public ObservationPoint()
        {
            Type = PointType.Observation;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Attempts to derive a new X, Y, Z coordinate for the point based on a given start coordinate and azimuth correction
        /// </summary>
        /// <param name="x">The X value of the occupied position</param>
        /// <param name="y">The Y value of the occupied position</param>
        /// <param name="z">The Z value of the occupied position</param>
        /// <param name="azi">The Azimuth correction to apply</param>
        /// <param name="objectHeight">The Instrument height of the occupied position</param>
        public void ReduceCoordinate(double x, double y, double z, double azi, double objectHeight, double scaleFactor = 1)
        {
            var web = azi + HCR;
            double diff = Math.Cos(VCR) * (SD * scaleFactor), dist = Math.Sin(VCR) * (SD * scaleFactor);

            if (TargetHeight == -999)
                Z = TargetHeight;
            else
                Z = z + objectHeight - TargetHeight - (Math.Cos(VCR) * (SD * scaleFactor)) + 2 * diff;

            X = x + Math.Sin(web) * dist;
            Y = y + Math.Cos(web) * dist;
        }

        /// <summary>
        /// Attempts to derive a new X, Y, Z coordinate for the point based on a given <see cref="StationSetup"/> object
        /// </summary>
        /// <param name="stationSetup">The <see cref="StationSetup"/> object to derive the coordinate from</param>
        public void ReduceCoordinate(StationSetup stationSetup)
        {
            ReduceCoordinate(stationSetup.X, stationSetup.Y, stationSetup.Z, stationSetup.Azi, stationSetup.ObjectHeight, stationSetup.ScaleFactor);
        }

        /// <summary>
        /// Attempts to conver the X, Y, Z values of the <see cref="ObservationPoint"/> to the HCR, VCR, SD and Target Height
        /// </summary>
        /// <param name="stationSetup">The <see cref="StationSetup"/> to use for the back calculation</param>
        public void ConvertXYZToObs(StationSetup stationSetup)
        {
            ConvertXYZToObs(stationSetup.X, stationSetup.Y, stationSetup.Z, stationSetup.Azi, stationSetup.ObjectHeight, stationSetup.ScaleFactor);
        }

        /// <summary>
        /// Attempts to convert the X, Y, Z values of the <see cref="ObservationPoint"/> to the HCR, VCR, SD and Target Height
        /// </summary>
        /// <param name="x">The X value of the occupied station</param>
        /// <param name="y">The Y value of the occupied station</param>
        /// <param name="z">The Z value of the occupied station</param>
        /// <param name="azi">The azimuth correction to use for the back calculation</param>
        /// <param name="objectHeight">The instrument height of the station setup to use for the back calculation</param>
        /// <param name="scaleFactor">The scale factor to apply to the slope distance</param>
        public void ConvertXYZToObs(double x, double y, double z, double azi, double objectHeight, double scaleFactor = 1)
        {
            double hAng = 0, vAng = 0, slpDst = 0, hzDist = 0, target = 0;

            //Calc the horizontal angle and distance to the point
            Trig.RPC(x, y, X, Y, ref hAng, ref hzDist);

            //Calc the vertical angle
            Trig.RPC(0, z, hzDist, Z, ref vAng, ref slpDst);
            double newX = 0, newZ = 0;

            //Calc the new target height
            Trig.PRC(0, z + objectHeight, vAng, slpDst, ref newX, ref newZ);
            target = newZ - Z;

            HCR = hAng - azi;
            VCR = vAng;
            SD = slpDst / scaleFactor;
            TargetHeight = target;
        }

        #endregion
    }

    public class ReferenceObservation : ObservationPoint
    {
        #region Properties

        public StationSetup StationSetup { get; set; }

        #endregion

        #region Setup

        public ReferenceObservation()
        {
            Type = PointType.ReferenceObservation;
        }

        #endregion
    }

    public class AzimuthCorrection : DTMPoint
    {
        #region Properties

        public StationSetup StationSetup { get; set; }
        public double HCR { get; set; }
        public new double X
        {
            get
            {
                if (StationSetup != null)
                    return StationSetup.X;
                else
                    return 0;
            }
        }
        public new double Y
        {
            get
            {
                if (StationSetup != null)
                    return StationSetup.Y;
                else
                    return 0;
            }
        }
        public new double Z
        {
            get
            {
                if (StationSetup != null)
                    return StationSetup.Z;
                else
                    return 0;
            }
        }

        #endregion

        #region Setup

        public AzimuthCorrection()
        {
            Type = PointType.AzimuthCorrection;
        }

        #endregion
    }

    public class StationSetup
    {
        #region Properties

        public AzimuthCorrection AzimuthCorrection { get; private set; }
        public StationPoint OccupiedStation { get; private set; }
        public StationPoint ReferenceStation { get; private set; }
        public ReferenceObservation ReferenceObservation { get; private set; }

        public double Azi { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }
        public double ObjectHeight { get; private set; }
        public double ScaleFactor { get; private set; }

        #endregion

        #region Setup

        public StationSetup()
        {
            ScaleFactor = 1;
        }

        public void SetupAzimuthCorrection(AzimuthCorrection azimuthCorrection)
        {
            this.AzimuthCorrection = azimuthCorrection;
            azimuthCorrection.StationSetup = this;
            UpdateStationSetup();
        }

        public void SetupOccupiedStation(StationPoint occupiedStation)
        {
            this.OccupiedStation = occupiedStation;
            occupiedStation.StationSetup = this;
            UpdateStationSetup();
        }

        public void SetupReferenceStation(StationPoint referenceStation)
        {
            this.ReferenceStation = referenceStation;
            referenceStation.StationSetup = this;
            UpdateStationSetup();
        }

        public void SetupReferenceObservation(ReferenceObservation referenceObservation)
        {
            this.ReferenceObservation = referenceObservation;
            referenceObservation.StationSetup = this;
            UpdateStationSetup();
        }

        #endregion

        #region Methods

        public void UpdateStationSetup()
        {
            if(OccupiedStation == null)
            {
                X = 0;
                Y = 0;
                Z = 0;
                ObjectHeight = 0;
            }
            else
            {
                X = OccupiedStation.X;
                Y = OccupiedStation.Y;
                Z = OccupiedStation.Z;
                ObjectHeight = OccupiedStation.ObjectHeight;
            }

            //Recalculate the resulting Azi correction value
            if(AzimuthCorrection != null)
            {
                //Get the value directly from the azimuth correction
                Azi = AzimuthCorrection.HCR;
            }
            else if(ReferenceStation != null && ReferenceObservation != null && OccupiedStation !=null)
            {
                //Calculate azimuth correction values
                var brg = Trig.RPC(OccupiedStation.X, OccupiedStation.Y, ReferenceStation.X, ReferenceStation.Y);
                Azi = brg - ReferenceObservation.HCR;
            }
            else if(ReferenceObservation != null)
            {
                //There is not valid reference station so treat the observation like an azimuth correction
                Azi = ReferenceObservation.HCR;
            }
            else
            {
                //Set the values to be default
                Azi = 0;
            }
        }

        #endregion
    }

    public class Edge
    {
        #region Properties

        public DTMPoint Point1 { get; set; }
        public DTMPoint Point2 { get; set; }

        public bool Used { get; set; }

        #endregion

        #region Setup

        public Edge(DTMPoint point1, DTMPoint point2)
        {
            Point1 = point1;
            Point2 = point2;
            Used = false;

        }

        #endregion

        #region Methods

        public void FlipEdge()
        {
            var temp = Point1;

            Point1 = null;
            Point1 = Point2;

            Point2 = null;
            Point2 = temp;
        }

        #endregion
    }

	#endregion

	public class TransformCommonData
	{
		public DTMPoint Survey = new DTMPoint();
		public DTMPoint Control = new DTMPoint();
		public DTMPoint Residual = new DTMPoint();

		public string PointID
		{
			get { return Survey.PointID; }
			set { Survey.PointID = value; }
			
		}

		public double SurveyX
		{
			get { return Survey.X; }
			set { Survey.X = value; }

		}
		public double SurveyY
		{
			get { return Survey.Y; }
			set { Survey.Y = value; }

		}
		public double SurveyZ
		{
			get { return Survey.Z; }
			set { Survey.Z = value; }

		}

		public double ControlX
		{
			get { return double.Parse(Control.X.ToString("0.0000")); }
			set { Control.X = value; }

		}

		public double ControlY
		{
			get { return double.Parse(Control.Y.ToString("0.0000")); }
			set { Control.Y= value; }

		}

		public double ControlZ
		{
			get { return double.Parse(Control.Z.ToString("0.0000")); }
			set { Control.Z = value; }

		}


		public double ResidualX
		{
			get { return double.Parse(Residual.X.ToString("0.0000")); }
			set { Residual.X = value; }

		}

		public double ResidualY
		{
			get { return double.Parse(Residual.Y.ToString("0.0000")); }
			set { Residual.Y = value; }

		}

		public double ResidualZ
		{
			get { return double.Parse(Residual.Z.ToString("0.0000")); }
			set { Residual.Z = value; }

		}
		  
	}


	public class TBMDTM : DTM
    {

		public List<TransformCommonData> TransformationData = new List<TransformCommonData>();
		 
        public TBMDTM()
        {

        }
        public TBMDTM(DTM InputModel)
        {
			this.Points = InputModel.Points;

			foreach (DTMPoint pt in InputModel.Points)
			{
				TransformCommonData newpt = new TransformCommonData();

				newpt.Survey = pt;
				DTMPoint nullpt = new DTMPoint();
				nullpt.X = -999;
				nullpt.Y = -999;
				nullpt.Z = -999;
				newpt.Control = nullpt;
				nullpt = new DTMPoint();
				nullpt.X = -999;
				nullpt.Y = -999;
				nullpt.Z = -999;
				newpt.Residual = nullpt;
				TransformationData.Add(newpt);
			}
        }

		public void AddResidual(string ID, double X, double Y, double Z)

		{
			if (TransformationData == null) { return; }
			foreach (TransformCommonData pt in TransformationData)
			{
				if (pt.PointID.ToUpper() == ID.ToUpper())
				{
					pt.Residual.X = X; pt.ResidualY = Y; pt.ResidualZ = Z;
					return;
				}
			}

		}

		public void AddControl(string ID, double X, double Y, double Z)

		{
			if (TransformationData == null) { return; }
			foreach (TransformCommonData pt in TransformationData)
			{
				if (pt.PointID.ToUpper() == ID.ToUpper())
				{
					pt.Control.X = X; pt.ControlY = Y; pt.ControlZ = Z;
					return;
				}
			}

			
		}
	}


    public class ModelSet : List<DTM>
    {
        #region Properties
        public bool ShowAlignment { get; set; } = false;
        public bool ShowPoints { get; set; } = true;
        public bool ShowLines { get; set; } = true;
        public bool ShowSurfaces { get; set; } = false;
        public bool ShowBlocks { get; set; } = true;
        public bool ShowTriangles { get; set; } = false;
        public bool ShowNonContourableTriangles { get; set; } = false;

        #region Merge Settings
        public bool RemoveOverlappingTriangles { get;  set; } = false;
        public bool ShiftPointsOntoModel { get; set; } = false;
        public bool ShiftOutsideToNull { get;  set; } = false;
        public bool MergeFeatures { get; set; } = true;
        #endregion



        public Bounds TotalBounds { get; set; } = new Bounds();
        public Point3D GlobalOffset { get; set; } = new Point3D(); //Should be set to TotalBounds.Centre

        #endregion

        public ModelSet()
        {

        }

		public ModelSet(List<DTM> passedModels)
		{
            this.AddRange(passedModels);
		}

        public void CopyMergeSettings(ModelSet modelSetToCopyFrom)
        {
            this.ShowPoints = true;
            this.ShowLines = modelSetToCopyFrom.ShowLines;
            this.ShowBlocks = modelSetToCopyFrom.ShowBlocks;
            this.ShowTriangles = modelSetToCopyFrom.ShowTriangles;
            this.ShowNonContourableTriangles = modelSetToCopyFrom.ShowNonContourableTriangles;
            this.MergeFeatures = modelSetToCopyFrom.MergeFeatures;

            this.RemoveOverlappingTriangles = modelSetToCopyFrom.RemoveOverlappingTriangles;
            this.ShiftPointsOntoModel = modelSetToCopyFrom.ShiftPointsOntoModel;
            this.ShiftOutsideToNull = modelSetToCopyFrom.ShiftOutsideToNull;
        }

        /// <summary>
        /// Calculates bounds for the individual models, then uses those bounds and finds the total bounds for the Model Set items (models). Also sets the GlobalOffset value.
        /// </summary>
        public void CalcBounds()
        {
            
            foreach (var model in this)
            {
                Bounds modelBounds = new Bounds();
                foreach (var point in model.Points)
                {
                    if (point.Type == PointType.FalsePoint)
                        continue;

                    modelBounds.Update(point);
                }

                foreach (Arc arc in model.GetArcs())
                {
                    modelBounds.Update(arc.StartPoint);
                    modelBounds.Update(arc.EndPoint);
                }


                model.Bounds = modelBounds;
                model.ModelOffset = modelBounds.Center;
                model.UpdateBounds = false;
                TotalBounds.Update(modelBounds); //Update the total bounds for the whole model set
            }

            GlobalOffset = TotalBounds.Center;

        }

        #region Find Nearest Point

        public DTMPoint FindNearestPt(Point2D Point)
        {
            return FindNearestPt(Point.X, Point.Y);

        }

        public DTMPoint FindNearestPt(Point3D Point, bool checkForDuplicates = false)
        {
            return FindNearestPt(Point.X, Point.Y, checkForDuplicates);

        }

        public DTMPoint FindNearestPt(DTMPoint Point)
        {
            return FindNearestPt(Point.X, Point.Y);

        }

        /// <summary>
        /// Find the nearest point in any of the models
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns></returns>
        public DTMPoint FindNearestPt(double X, double Y, bool checkForDuplicates = false)
        {

            DTMPoint closest = new DTMPoint();

            if (this == null || this.Count == 0)
            {
                return null;
            }
            else 
            {
                double curDist = double.PositiveInfinity;
                foreach (DTM mod in this)
                {
                    DTMPoint pt = mod.GetClosestPoint(new Point2D(X, Y),false,checkForDuplicates);

                    double thisdist = MathsHelpers.Vector.FnDistance(pt.X, pt.Y, X, Y);

                    if (thisdist < curDist)
                    {
                        curDist = thisdist;
                        closest = pt;
                    }
                    
                }
                return closest;
            }
            return null;

        }

        public DTMPoint FindNearestPt(Ray ray)
        {
            DTMPoint closest = new DTMPoint();

            if (this == null || this.Count == 0)
            {
                return null;
            }
            else
            {
                double curDist = double.PositiveInfinity;
                foreach (DTM mod in this)
                {
                    DTMPoint pt = mod.GetClosestPoint(ray, false);

                    double thisdist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, pt.X, pt.Y, pt.Z);

                    if (thisdist < curDist)
                    {
                        curDist = thisdist;
                        closest = pt;
                    }

                }
                return closest;
            }
            return null;
        }

        #region Find Nearest Lines
        public Tuple<StandardLine, double> FindNearestStandardLine2D(Point2D point)
        {
            double closestDist = double.PositiveInfinity;
            StandardLine closestLine = null;
            Tuple<StandardLine, double> closestTuple = Tuple.Create(closestLine, closestDist);
            foreach (DTM model in this)
            {
                closestTuple = model.GetClosestStandardLine2D(point);

                if (closestTuple.Item1 == null) { continue; }

                if (closestDist > closestTuple.Item2) //Item2 is distance
                {
                    closestLine = closestTuple.Item1;//Item1 is the Line object
                    closestDist = closestTuple.Item2;
                }
            }
            return closestTuple;
        }

        public Tuple<PolyLine,double> FindNearestPolyLine2D(Point2D point)
        {

            double closestDist = double.PositiveInfinity;
            PolyLine closestLine = null;
            Tuple<PolyLine, double> closestTuple = Tuple.Create(closestLine, closestDist);
            foreach (DTM model in this)
            {
                closestTuple = model.GetClosestPolyLine2D(point);

                if (closestTuple.Item1 == null) { continue; }

                if (closestDist > closestTuple.Item2) //Item2 is distance
                {
                    closestLine = closestTuple.Item1;//Item1 is the Line object
                    closestDist = closestTuple.Item2;
                }
            }
            return closestTuple;
        }

        public Tuple<Arc,double> FindNearestArc2D(Point2D point)
        {
            double closestDist = double.PositiveInfinity;
            Arc closestArc = null;
            Tuple<Arc, double> closestTuple = Tuple.Create(closestArc, closestDist);
            foreach (DTM model in this)
            {   
                closestTuple = model.GetClosestArc2D(point);
                if (closestDist > closestTuple.Item2) //Item2 is distance
                {
                    closestDist = closestTuple.Item2;
                    closestArc = closestTuple.Item1;//Item1 is the Line object
                }
            }
            return closestTuple;
        }

        public Tuple<LineFeatureItem, double> FindNearestLineFeatureItem(Point2D point)
        {

            double closestDist = double.PositiveInfinity;
            LineFeatureItem closestLine = null;
            Tuple<LineFeatureItem, double> closestTuple = Tuple.Create(closestLine, closestDist);
            foreach (DTM model in this)
            {
                closestTuple = model.GetClosestLineFeatureItem(point);

                if (closestTuple.Item1 == null) { continue; }

                if (closestDist > closestTuple.Item2) //Item2 is distance
                {
                    closestLine = closestTuple.Item1;//Item1 is the Line object
                    closestDist = closestTuple.Item2;
                }
            }
            return closestTuple;
        }

        public object FindNearestLine(Point2D point)
        {
            object returnLine = null;
            double closestDist = double.PositiveInfinity;
            //Find the closest line of each type we currently have
            Tuple<Arc, double> arcTuple = FindNearestArc2D(point);
            Tuple<PolyLine, double> polyTuple = FindNearestPolyLine2D(point);
            Tuple<StandardLine, double> lineTuple = FindNearestStandardLine2D(point);
            Tuple<LineFeatureItem, double> lineFeatureTuple = FindNearestLineFeatureItem(point);

            //Compare each closest distance (item2) of the tuple to find the ultimate closest line
            if (arcTuple.Item2 != double.NegativeInfinity && arcTuple.Item2 < closestDist) { closestDist = arcTuple.Item2; returnLine = arcTuple.Item1; }
            if (polyTuple.Item2 != double.NegativeInfinity && polyTuple.Item2 < closestDist) { closestDist = polyTuple.Item2; returnLine = polyTuple.Item1; }
            if (lineTuple.Item2 != double.NegativeInfinity && lineTuple.Item2 < closestDist) { closestDist = lineTuple.Item2; returnLine = lineTuple.Item1; }
            if (lineFeatureTuple.Item2 != double.NegativeInfinity && lineFeatureTuple.Item2 < closestDist) { closestDist = lineFeatureTuple.Item2; returnLine = lineFeatureTuple.Item1; }

            return returnLine;
        }

        #endregion
        #endregion
    }


    public class DTM : Datasource
    {
        #region Properties

        


        public ModelProperties Properties = new ModelProperties();

       // public String DisplayName { get { return Properties.ModelName; } } 
                            
        //===================================================================================
        /// <summary>
        /// The render item for the DTM model's points list.
        /// </summary>
        public SharedRenderItem AllPointsRenderItem { get; set; }
        public BindingList<DTMPoint> Points { get; set; }
        //===================================================================================
        Triangle LastTriSearched = null;
        public string FeaturesPath { get; private set; }
        private Dictionary<string, DTMPoint> PointsDictionary { get; set; }
        private bool PointsDictionaryBuilt { get; set; }
        public List<StationSetup> StationSetups { get; set; }
        public Bounds Bounds { get; set; }
        //==================================================================================
        //Feature shit related to survey data
        public Dictionary<char, List<Feature>> FeaturesDictionary { get; private set; }
        public Dictionary<string, LineFeatureCollection> LineFeatures { get; set; }
        public Dictionary<string, PointFeatureCollection> PointFeatures { get; set; }
        //==================================================================================
        public List<TriangleSurface> Surfaces { get; set; }
        public HashSet<Triangle> Triangles { get; set; }
        public bool UpdateBounds { get; set; }

        public TriangleNet.Mesh TNetMesh;

        private BindingList<DTMPoint> sortedPoints = null;

        //==================================================================================
        //Generic line data used for gerneral purposes - importing CAD or just general line drawing
        private CollectionOfAllPolyLines PolyLineCollection = new CollectionOfAllPolyLines();
        private CollectionOfAllStandardLines StandardLineCollection = new CollectionOfAllStandardLines();
        //==================================================================================
        private CollectionOfAllArcs ArcCollection = new CollectionOfAllArcs();
        private CollectionOfBlocks BlockCollection = new CollectionOfBlocks();
        //==================================================================================

        public SortedDictionary<string, DrawingLayer> DrawingLayers { get; set; } = new SortedDictionary<string, DrawingLayer>();

        #region ShowSettings setup

        public bool ShowSettingsEnabled { get; set; } //Added by TN: 03.08.21
        //Used for the tree view. When enabled, other show settings are unable to be changed (in the tree view).

        public bool ShowPoints { get; set; }
        public bool ShowLines { get; set; }
        public bool ShowBreakLines { get; set; }
        public bool ShowSurfaces { get; set; }
        public bool ShowTriangles { get; set; }
        public bool ShowNonContourableTriangles { get; set; }
        public bool ShowAlignment { get; set; }

        public bool ShowBlocks { get; set; }
        public bool ShowSection { get; set; }
        public Color ShowColor { get; set; }
        #endregion





        public BindingList<DTMPoint> SortedPoints
        {
            get
            {
                if (sortedPoints == null)
                    return Points;
                else
                    return sortedPoints;
            }
        }

        private Point3D modelOffset = null;
        public Point3D ModelOffset
        {
            get
            {
                if (modelOffset == null)
                    modelOffset = new Point3D();

                return modelOffset;
            }
            set
            {
                modelOffset = value;
            }
        }

        public BindingList<DTMPoint> PointsToUpdate { get; set; }
        public new bool IsEmpty
        {
            get
            {
                if (Points == null || Points.Count <= 0)
                    return true;

                foreach (var point in Points)
                {
                    if (point.Type != PointType.FalsePoint)
                        return false;
                }

                return true;
            }
        }





        #endregion

        #region Setup

        

        public DTM()
        {
            Points = new BindingList<DTMPoint>();
            StationSetups = new List<StationSetup>();
            Bounds = new Bounds();
            LineFeatures = new Dictionary<string, LineFeatureCollection>();
            PointFeatures = new Dictionary<string, PointFeatureCollection>();
            Surfaces = new List<TriangleSurface>();
            Triangles = new HashSet<Triangle>();
            ModelOffset = new Point3D();
            UpdateBounds = true;//false;
            SaveRequired = false;
            ShowPoints = true;
            ShowLines = true;
            ShowBlocks = false; //Disabled show blocks by default for now since they are performance intensive
            ShowAlignment = false;
            ShowSettingsEnabled = true;
            Properties.SourceFile = this.FilePath;


        }

        public override string ToString()
        {
            return this.Name;
        }

        public DTMPoint matchPoint(int idx, double x, double y, double z)
        {
            
            var Points = this.Points;

            if (Points != null && Points.Count > 0 && idx >= 0 && idx < Points.Count)
            {
                if (MathsHelpers.Vector.Fn3DDistance(x, y, z, Points[idx].X, Points[idx].Y, Points[idx].Z) < 0.002)
                {
                    return Points[idx];
                }

            }

            int incCounter = idx + 1;
            int decCounter = idx - 1;

            do
            {
                if (incCounter <= Points.Count)
                {
                    if (MathsHelpers.Vector.Fn3DDistance(x, y, z, Points[incCounter].X, Points[incCounter].Y, Points[incCounter].Z) < 0.002)
                    {
                        return Points[incCounter];
                    }
                }

                if (decCounter >= 0)
                {
                    if (MathsHelpers.Vector.Fn3DDistance(x, y, z, Points[decCounter].X, Points[decCounter].Y, Points[decCounter].Z) < 0.002)
                    {
                        return Points[decCounter];
                    }
                }

                incCounter++;
                decCounter--;
            } while (incCounter <= Points.Count*2 && decCounter >= -Points.Count);

            return null;
        }


        public void BuildPointsDictionary()
        {
            DestroyPointsDictionary();

            PointsDictionary = new Dictionary<string, DTMPoint>();
            foreach (var point in this.Points)
            {
                string key = point.X.ToString("0.000") + point.Y.ToString("0.000") + point.Z.ToString("0.000");

                if (!PointsDictionary.ContainsKey(key))
                {
                    PointsDictionary.Add(key, point);
                }
            }

            PointsDictionaryBuilt = true;
        }

        public void DestroyPointsDictionary()
        {
            if (PointsDictionaryBuilt)
                PointsDictionary = new Dictionary<string, DTMPoint>();

            PointsDictionaryBuilt = false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">x value of point you want to find</param>
        /// <param name="y">y value of point you want to find</param>
        /// <param name="z">z value of point you want to find</param>
        /// <param name="layer">The layer you want the newly created point to go on if a point isn't found</param>
        /// <param name="createNewPoint">Pass negative value when you're 1000% sure you never want a point to be automatically created if one can't be found</param>
        /// <param name="doSecondPass">Should be true most of the time (to catch point edits etc). False for most imports. True, force rebuilds dictionary before creating new point/returning null</param>
        /// <returns></returns>
        public DTMPoint MatchPointFromPointsDictionary(double x, double y, double z, bool createNewPoint = true, string layer = "Default", bool doSecondPass = true)
        {
            //If any point values have been modified, the dictionary needs rebuilding
            if (!PointsDictionaryBuilt)
                BuildPointsDictionary();

            //Create dictionary key, currently set to 3 decimal places (may want to change this in the future or provide a parameter for it)
            string key = x.ToString("0.000") + y.ToString("0.000") + z.ToString("0.000");


            //Check to see if point exists in the current dictionary
            if (PointsDictionary.ContainsKey(key))
            {
                //If a point is being matched to the dictionary we don't want to override its layer.
                return PointsDictionary[key];
            }
            //If the point has moved or been edited, the dictionary will not find anything on its first pass unless previously rebuilt.
            else if (doSecondPass) //Rebuild the points dictionary and call this method again if this is the first pass. 
            {
                PointsDictionaryBuilt = false;
                return MatchPointFromPointsDictionary(x, y, z, createNewPoint, layer, false);
            }
            
            
            //After the first check and a rebuild, if we want to ensure we return a point we create a new point here and return it.
            if (createNewPoint)
            {
                DTMPoint newPoint = new DTMPoint();
                newPoint.X = x;
                newPoint.Y = y;
                newPoint.Z = z;
                newPoint.AddALayer(this, layer);
                string newkey = x.ToString("0.000") + y.ToString("0.000") + z.ToString("0.000");
                PointsDictionary.Add(newkey, newPoint);
                this.Points.Add(newPoint);

                return newPoint;
            }

            return null; //If you're getting weird point relationship errors and this is being hit, this is probably why. TN 01.02.22
        }


        //Added by ES:25.08.20
        public Volumes.VolumeResult CalculateVolume(DTM othermodel)
        {
            Volumes.VolumeResult outvol = new Volumes.VolumeResult();

            if (this.Triangles != null && othermodel != null & othermodel.Triangles != null)
            {
                //we've got something that we might be able to work with
                foreach (Triangle tri in this.Triangles)
                {
                    if (tri.Surface.Contour == true)
                    {
                        var tmp = tri.CalculateVolume(othermodel);
                        outvol.Cut += Math.Abs(tmp.Cut);
                        outvol.Fill += Math.Abs(tmp.Fill);
                        outvol.CutPlanArea += Math.Abs(tmp.CutPlanArea);
                        outvol.FillPlanArea += Math.Abs(tmp.FillPlanArea);
                        outvol.CutSurfaceArea += Math.Abs(tmp.CutSurfaceArea);
                        outvol.FillSurfaceArea += Math.Abs(tmp.FillSurfaceArea);
                    }
                }
            }
            return outvol;
        }

        /// <summary>
        /// <para>Generates the <see cref="StationSetups"/> list from the <see cref="StationPoint"/>, <see cref="ReferenceObservation"/>, and <see cref="AzimuthCorrection"/> objects found within the <see cref="Points"/> list</para>
        /// <para></para>
        /// <para>Also applies the relevant station setup to each <see cref="ObservationPoint"/> found within the <see cref="Points"/> list</para>
        /// </summary>
        public void GenerateStationSetups()
        {
            var stationSetup = new StationSetup();
            stationSetup.UpdateStationSetup();
            foreach (var point in Points)
            {
                switch (point.Type)
                {
                    case PointType.OccupiedStation:
                        stationSetup = new StationSetup();
                        if (point is StationPoint)
                            stationSetup.SetupOccupiedStation((StationPoint)point);
                        break;
                    case PointType.ReferenceStation:
                        if (point is StationPoint)
                            stationSetup.SetupReferenceStation((StationPoint)point);
                        break;
                    case PointType.ReferenceObservation:
                        if (point is ReferenceObservation)
                        {
                            //If there is already a reference observation we should make a new station setup
                            if (stationSetup.ReferenceObservation != null)
                                stationSetup = new StationSetup();

                            stationSetup.SetupReferenceObservation((ReferenceObservation)point);

                            //If there is not a valid station setup for this reference then get the station points from the previous setup (if it exists)
                            if (stationSetup.OccupiedStation == null || stationSetup.ReferenceStation == null)
                            {
                                var lastIndex = StationSetups.Count - 1;
                                if (lastIndex > 0)
                                {
                                    //Index is valid
                                    var lastStation = StationSetups[lastIndex];

                                    //Set the occupied station if required
                                    if (stationSetup.OccupiedStation == null && lastStation.OccupiedStation != null)
                                        stationSetup.SetupOccupiedStation(lastStation.OccupiedStation);

                                    //Set the reference station if required
                                    if (stationSetup.ReferenceStation == null && lastStation.ReferenceStation != null)
                                        stationSetup.SetupReferenceStation(lastStation.ReferenceStation);
                                }
                            }

                            //Reduce the reference observation
                            (point as ReferenceObservation).ReduceCoordinate(stationSetup);

                            //Add the station setup and create a new instance to break the reference
                            StationSetups.Add(stationSetup);
                        }
                        break;
                    case PointType.AzimuthCorrection:
                        if (point is AzimuthCorrection)
                        {
                            //If there is already a azimuth correction we should make a new station setup
                            if (stationSetup.AzimuthCorrection != null)
                                stationSetup = new StationSetup();

                            stationSetup.SetupAzimuthCorrection((AzimuthCorrection)point);

                            //If there is not a valid occupied station then get it from the previous station
                            if (stationSetup.OccupiedStation == null)
                            {
                                var lastIndex = StationSetups.Count - 1;
                                if (lastIndex > 0)
                                {
                                    //Index is valid
                                    var lastStation = StationSetups[lastIndex];

                                    //Set the occupied station if it exists
                                    if (lastStation.OccupiedStation != null)
                                        stationSetup.SetupOccupiedStation(lastStation.OccupiedStation);
                                }
                            }

                            //Add the station setup and create a new instance to break the reference
                            StationSetups.Add(stationSetup);
                        }
                        break;
                    case PointType.Observation:
                        if (point is ObservationPoint)
                            (point as ObservationPoint).ReduceCoordinate(stationSetup);
                        break;
                }
            }
        }

        /// <summary>
        /// Find a point in the existing model that matches the input
        /// Added to sort GH issue#125
        /// </summary>
        /// <param name="CheckPoint">Point to query</param>
        /// <returns>either the matching point if theere is one, or the input if not</returns>
        public DTMPoint FindDuplicate(DTMPoint CheckPoint)
        {
            foreach (DTMPoint pt in Points)
            {
                if (MathsHelpers.Vector.Fn3DDistance(pt.X, pt.Y, pt.Z, CheckPoint.X, CheckPoint.Y, CheckPoint.Z) < 0.001)
                {
                    return pt;
                }
            }
            return CheckPoint;
        }


        /// <summary>
        /// Sets up and interprets features, as well as adding them to the correct layers. Features need to be created before this step.
        /// </summary>
        /// <param name="featuresDictionary"></param>
        /// <param name="featuresPath"></param>
        public void SetupFeaturesFromDictionary(Dictionary<char, List<Feature>> featuresDictionary, string featuresPath = null)
        {
            FeaturesPath = featuresPath;
            FeaturesDictionary = featuresDictionary;

            //Add the default point feault
            if (!FeaturesDictionary.ContainsKey('@'))
            {
                var defaultFeature = new PointFeature();
                defaultFeature.Code = "@";
                defaultFeature.FieldCode = "@";

                FeaturesDictionary.Add('@', new List<Feature> { defaultFeature });
            }

            InterpretFeatures();

            foreach (var featureList in FeaturesDictionary.Values)
                foreach (var feature in featureList)
                    feature.AddToLayer(this, feature.Layer);

            GeneratePointLayerStrings();
        }



        #endregion

        #region Methods

        /// <summary>
        /// Simple wrapper for getting all the line feature item things. can't be arsed re-writing these sodding loops all the time
        /// </summary>
        /// <returns></returns>
        public List<LineFeatureItem> GetAllLineFeatureItems()
        {
            List<LineFeatureItem> fullList = new List<LineFeatureItem>();

            foreach (LineFeatureCollection lfc in LineFeatures.Values)//Close eyes here
            {
                foreach (List<LineFeatureItem> listOfLineFeatureItems in lfc.FeatureItems.Values)
                {
                    fullList.AddRange(listOfLineFeatureItems);
                }
            }

            return fullList;
        }


        #region Layer Management

        public bool AddLayer(string layerToAdd)
        {
            if (DrawingLayers == null) 
                DrawingLayers = new SortedDictionary<string, DrawingLayer>();

            if (!string.IsNullOrWhiteSpace(layerToAdd))
            {
                layerToAdd = layerToAdd.Replace(" ", null);
            }

            if (DrawingLayers.ContainsKey(layerToAdd))
                return false;

            DrawingLayer newLayer = new DrawingLayer();
            newLayer.Name = layerToAdd;
            DrawingLayers.Add(layerToAdd, newLayer);
            return true;
        }

        public bool AddLayer(DrawingLayer layerToAdd)
        {
            if (DrawingLayers == null)
                DrawingLayers = new SortedDictionary<string, DrawingLayer>();

            if (!string.IsNullOrWhiteSpace(layerToAdd.Name))
            {
                layerToAdd.Name = layerToAdd.Name.Replace(" ", null);
            }
            if (CheckIfLayerExists(layerToAdd.Name))
                return false;

            DrawingLayers.Add(layerToAdd.Name, layerToAdd);
            return true;
        }

        public DrawingLayer GetLayer(string layerToReturn)
        {
            if (DrawingLayers == null)
                DrawingLayers = new SortedDictionary<string, DrawingLayer>();

            if (!string.IsNullOrWhiteSpace(layerToReturn))
            {
                layerToReturn = layerToReturn.Replace(" ", null);
            }

            if (CheckIfLayerExists(layerToReturn))
            {
                return DrawingLayers[layerToReturn];
            }
            else
            {
                if (!CheckIfLayerExists("Default"))
                    AddLayer("Default");

                return DrawingLayers["Default"];
            }
        }

        public bool CheckIfLayerExists(string layerToCheck)
        {
            if (layerToCheck == null)
                return false;

            layerToCheck = layerToCheck.Replace(" ", null);

            if (DrawingLayers == null)
                DrawingLayers = new SortedDictionary<string, DrawingLayer>();

            if (DrawingLayers.ContainsKey(layerToCheck))
                return true;

            return false;
        }

        /// <summary>
        /// Checks wether a layer is being used by objects. *This needs to be updated when new objects which can be given a layer are introduced.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public bool CheckForLayerUse(string layer)
        {
            layer = layer.Replace(" ", null);

            //Check points
            foreach (var point in Points)
            {
                if (point.Layers.Contains(layer))
                {
                    return true;
                }

                //Check block inserts
                if (point.BlockInserts != null)
                {
                    foreach (var blockInsert in point.BlockInserts)
                    {
                        if (blockInsert.Layer == layer)
                        {
                            return true;
                        }
                    }
                }
            }

            //Check standard lines
            foreach (var line in this.GetStandardLines())
            {
                if (line.Layer == layer)
                {
                    return true;
                }
            }


            //Check poly lines
            foreach (var line in this.GetPolyLines())
            {
                if (line.Layer == layer)
                {
                    return true;
                }
            }

            if (FeaturesDictionary != null)
            {
                //Check features
                foreach (var featureList in this.FeaturesDictionary.Values)
                {
                    foreach (var feature in featureList)
                    {
                        if (feature.Layer == layer)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;

        }

        /// <summary>
        /// Does what it says on the tin. Deletes entities from the model by layer. deleteAllPoints = false: only the entitiy gets deleted, not the point.
        /// true: every point on this layer gets deleted, which removes it from any entities it may lay on.
        /// </summary>
        /// <param name="layerToDeleteFrom"></param>
        /// <param name="deleteAllPoints"></param>
        /// <param name="deleteBlockInserts"></param>
        /// <param name="deleteLines"></param>
        public void DeleteEntitiesFromModelByLayer(string layerToDeleteFrom, bool deleteAllPoints = false, bool deleteBlockInserts = false, bool deleteLines = false, bool triangulateAreaWhenPointDeleted = false)
        {
            HashSet<DTMPoint> PointsToDelete = new HashSet<DTMPoint>(); //HashSets can not contain duplicates (works perfectly here since a point can be used by more than one entity)
            HashSet<StandardLine> StandardLinesToDelete = new HashSet<StandardLine>();
            HashSet<PolyLine> PolyLinesToDelete = new HashSet<PolyLine>();
            //HashSet<Feature> FeaturesToDelete = new HashSet<Feature>();



            //Points always come first
            #region Points & Points' Block Inserts & Point's Features

            foreach (var point in Points)
            {
                #region Points
                if (deleteAllPoints)
                {
                    if (point.Layers.Contains(layerToDeleteFrom))
                    {
                        PointsToDelete.Add(point);
                        continue; //The point is being deleted so there's no need to check for any block inserts or features
                    }
                }
                #endregion

                #region Block Inserts
                if (deleteBlockInserts)
                {
                    //We perform this check here just so we don't have to loop through the points again later.
                    if (point.BlockInserts == null) { continue; }

                    HashSet<BlockInsert> BlockInsertsToDelete = new HashSet<BlockInsert>();
                    //Flag block inserts which are on the layer to delete
                    foreach (var blockInsert in point.BlockInserts)
                    {
                        if (blockInsert.Layer == layerToDeleteFrom)
                        {
                            BlockInsertsToDelete.Add(blockInsert);
                        }
                    }

                    //Remove the block insert from the point (seperate loop because C#)
                    foreach (var blockIns in BlockInsertsToDelete)
                    {
                        point.BlockInserts.Remove(blockIns);
                    }
                }
                #endregion

                #region Features

                if (FeaturesDictionary != null && deleteLines)
                {
                    if (point.LineFeatures == null) { continue; }

                    //Loop through each line feature this point has associated with it
                    foreach (var feature in point.LineFeatures)
                    {
                        //If one of these line feature's layers is the layer we want to delete, we delete the whole point
                        if (feature.FeatureCollection.Feature.Layer == layerToDeleteFrom)
                        {
                            PointsToDelete.Add(point);
                        }

                    }
                }
                #endregion
            }
            //The below method is the big "RemovePoints" method. It will remove any references of the point from any entity (lines etc etc).
            //Call it here so the later checks may not be needed (if all points of a line are deleted, we don't need to check that line further on in this method)
            DeletePointsFromModel(PointsToDelete, triangulateAreaWhenPointDeleted);
            #endregion

            if (deleteLines)
            {
                #region StandardLines
                foreach (var line in this.GetStandardLines())
                {

                    if (line.Layer == layerToDeleteFrom)
                    {
                        StandardLinesToDelete.Add(line);
                    }
                }

                foreach (var standardLine in StandardLinesToDelete)
                {
                    //Remove line from model (including any nodes)
                    this.RemoveStandardLineFromModel(standardLine, true);
                }
                #endregion

                #region PolyLines
                foreach (var line in this.GetPolyLines())
                {
                    if (line.Layer == layerToDeleteFrom)
                    {
                        PolyLinesToDelete.Add(line);
                    }
                }
                foreach (var polyLine in PolyLinesToDelete)
                {
                    //Remove line from model (including any nodes)
                    this.RemovePolyLineFromModel(polyLine, true);
                }
                #endregion

            }

            InterpretFeatures();
            GeneratePointLayerStrings();


        }

        ///<summary>
        ///
        ///</summary>
        /// <param name="layerToDelete"></param>
        /// <returns></returns>
        public bool DeleteLayer(string layerToDelete, string replacementLayer = null)
        {
            if (DrawingLayers == null) 
                DrawingLayers = new SortedDictionary<string, DrawingLayer>();

            layerToDelete = layerToDelete.Replace(" ", null);

            if (DrawingLayers.ContainsKey(layerToDelete))
            {
                DrawingLayers[layerToDelete] = null;
                DrawingLayers.Remove(layerToDelete);
                DeleteLayerReferences(layerToDelete, replacementLayer);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Find and replaces all references to a layer with the newLayer parameter. If null, Default layer is used.
        /// </summary>
        /// <param name="layerToDelete"></param>
        /// <returns></returns>
        private void DeleteLayerReferences(string layerToDelete, string replacementLayer = null)
		{
            if (replacementLayer == null) { replacementLayer = "Default"; }
            AddLayer(replacementLayer);

            //Check points
            foreach (var point in Points)
            {
                if (point.Layers.Contains(layerToDelete))
                {
                    point.RemoveALayer(layerToDelete);
                    point.AddALayer(this, replacementLayer);
                }


                //Check block inserts
                if (point.BlockInserts != null)
                {
                    foreach(var blockInsert in point.BlockInserts)
                    {
                        if (blockInsert.Layer == layerToDelete)
                        {
                            blockInsert.Layer = replacementLayer;
                        }
                    }
                }
            }

            //Check standard lines
            foreach (var line in this.GetStandardLines())
            {
                if (line.Layer == layerToDelete)
                {
                    line.Layer = replacementLayer;
                }
            }


            //Check poly lines
            foreach (var line in this.GetPolyLines())
            {
                if (line.Layer == layerToDelete)
                {
                    line.Layer = replacementLayer;
                }
            }

            if (FeaturesDictionary != null)
            {
                //Check features
                foreach (var featureList in this.FeaturesDictionary.Values)
                {
                    foreach (var feature in featureList)
                    {
                        if (feature.Layer == layerToDelete)
                        {
                            feature.Layer = replacementLayer;
                        }
                    }
                }
            }

		}

        #region DTMPoint Layer Handling
        /// <summary>
        /// Unique check for DTMPoint. DTMPoint can have multiple layers. We want to see if at least one layer is being drawn.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsOneOrMoreLayerVisible(DTMPoint point)
        {
            if (point.Layers == null)
                point.AddALayer(this);

            string[] layers = point.Layers.Split();

            for(int i = 0; i < layers.Length; i++)
            {
                if (this.GetLayer(layers[i]).Draw)
                    return true;
            }

            return false;
        }

        private void GeneratePointLayersFromFeatures(DTMPoint point)
        { 
            //LineFeatureItems
            if (point.LineFeatures != null)
            {
                foreach (LineFeatureItem feature in point.LineFeatures)
                {
                    point.AutoLayers.Add(feature.FeatureCollection.Feature.Layer);
                }
            }
        }
        private void GeneratePointLayersFromBlockInserts(DTMPoint point)
        {
            //BlockInserts
            if (point.BlockInserts != null)
            {
                foreach (BlockInsert blockIns in point.BlockInserts)
                {
                    point.AutoLayers.Add(blockIns.Layer);
                }
            }
        }
        private void GeneratePointLayersFromPolyLines()
        {
            foreach(PolyLine pl in GetPolyLines())
            {
                foreach(DTMPoint node in pl.Nodes)
                {
                    node.AutoLayers.Add(pl.Layer);
                }
            }
        }
        private void GeneratePointLayersFromStandardLines()
        {
            foreach (StandardLine sl in GetStandardLines())
            {
                sl.StartPoint.AutoLayers.Add(sl.Layer);
                sl.EndPoint.AutoLayers.Add(sl.Layer);
            }
        }

        /// <summary>
        /// Goes through each point and regenerates its AutoLayers property. All points used by an entity have their layers automatically assigned.
        /// </summary>
        public void GeneratePointLayerStrings(List<DTMPoint> points = null)
        {
            if (points == null) { points = Points.ToList(); } //No override, use Model Points list

            //Use a point-first loop for attributes of the point (features, block definitions)
            foreach (DTMPoint pt in points)
            {
                pt.AutoLayers = new HashSet<string>();

                GeneratePointLayersFromFeatures(pt);
                GeneratePointLayersFromBlockInserts(pt);
            }

            //Use a line-first loop for these to save comparisons/performance.
            GeneratePointLayersFromPolyLines();
            GeneratePointLayersFromStandardLines();

        }
        #endregion
        #endregion

        #region PolyLine Methods
        public void AddPolyline(PolyLine pl, string layerToGoOn = null)
        {
            if (PolyLineCollection == null)
                PolyLineCollection = new CollectionOfAllPolyLines();
            if (PolyLineCollection.PolyLines == null)
                PolyLineCollection.PolyLines = new List<PolyLine>();

            //the logic for this is perfect do not touch or I will cry TN: 17.09.21
            //Ensure the layer of this entity is added to the model
            if (pl.Layer == null || layerToGoOn != null)
                pl.AddToLayer(this, layerToGoOn);

            PolyLineCollection.PolyLines.Add(pl);
        }

        public void AddPolyLineList(List<PolyLine> plList)
        {
            if (PolyLineCollection == null)
                PolyLineCollection = new CollectionOfAllPolyLines();
            if (PolyLineCollection.PolyLines == null)
                PolyLineCollection.PolyLines = new List<PolyLine>();

            //Ensure the layer of this entity is added to the model
            foreach (var pl in plList)
            {
                    pl.AddToLayer(this,pl.Layer);
            }

            PolyLineCollection.PolyLines.AddRange(plList);
        }

        public List<PolyLine> GetPolyLines()
        {
            if (PolyLineCollection == null)
                PolyLineCollection = new CollectionOfAllPolyLines();
            if (PolyLineCollection.PolyLines == null)
                PolyLineCollection.PolyLines = new List<PolyLine>();

            return this.PolyLineCollection.PolyLines;

        }
        public CollectionOfAllPolyLines GetPolyLineCollection()
        {
            if (PolyLineCollection == null)
                PolyLineCollection = new CollectionOfAllPolyLines();

            return this.PolyLineCollection;
        }
        public int NumberOfPolyLines()
        {
            if (this.PolyLineCollection == null || this.PolyLineCollection.PolyLines == null)
            {
                return 0;
            }
            else
            {
                return PolyLineCollection.PolyLines.Count;
            }
        }

        /// <summary>
        /// Removes the passed points from any polylines that the points are nodes of. If polyline ends up with less than 2 points, polyline is disposed.
        /// </summary>
        /// <param name="pointsToRemove"></param>
        public void RemovePointFromAllPolyLines(HashSet<DTMPoint> pointsToRemove)
		{
            List<PolyLine> linesToRemove = new List<PolyLine>();
            foreach (var polyline in GetPolyLines())
            {
                List<DTMPoint> nodesToRemoveFromLine = new List<DTMPoint>();
                foreach (var point in polyline.Nodes)
                {
                    if (pointsToRemove.Contains(point))
                    {
                        nodesToRemoveFromLine.Add(point);
                    }
                }

                foreach (var pt in nodesToRemoveFromLine)
                {
                    polyline.Nodes.Remove(pt);
                }

                //polyline.Nodes = new BindingList<DTMPoint>(polyline.Nodes.ToList().Except(pointsToRemove).ToList()); slow as fuck

                //Check if polyline is still valid (needs at least 2 nodes)
                if (polyline.Nodes.Count <= 1)
                {
                    linesToRemove.Add(polyline);
                }
           
            }

            foreach (var pl in linesToRemove)
                PolyLineCollection.PolyLines.Remove(pl);
		}
        /// <summary>
        /// Removes the passed point from any polylines that the point is a node of. If polyline ends up with less than 2 points, polyline is disposed.
        /// </summary>
        /// <param name="pointToRemove"></param>
        public void RemovePointFromAllPolyLines(DTMPoint pointToRemove)
		{
            if (pointToRemove == null) { return; }

            RemovePointFromAllPolyLines(new HashSet<DTMPoint> {pointToRemove});
		}

        /// <summary>
        /// Removes a polyline from the model, keeps points unless stated otherwise.
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="deletePoints">When set to <see langword="true"/>, DeletePointsFromDTMModel is called.</param>
        /// <returns></returns>
        public bool RemovePolyLineFromModel(PolyLine polyline, bool deletePoints = false)
        {
            if (this.GetPolyLines().Count == 0) { return false; }


            if (deletePoints)
            {
                HashSet<DTMPoint> nodes = new HashSet<DTMPoint>(polyline.Nodes);
                //Removes point from everything. Triangles, lines etc.. wipes it off the face of the earth (also removes empty polyline)
                var dict = CreatePointIncrementDictionary();
                DeletePointsFromEntites(nodes);
                InterpretPointIncrementDictionary(dict);
            }
            else
            {
                this.GetPolyLines().Remove(polyline);
                polyline = null;
            }

            //Final check to make sure the polyline is gone.
            if (GetPolyLines().Count > 0 && polyline != null && GetPolyLines().Contains(polyline)){ return false; }
            else { return true; }

        }

        #endregion

        #region StandardLine Methods
        public void AddStandardLine(StandardLine ln, string layerToGoOn = null)
        {
            if (StandardLineCollection == null)
                StandardLineCollection = new CollectionOfAllStandardLines();
            if (StandardLineCollection.StandardLines == null)
                StandardLineCollection.StandardLines = new List<StandardLine>();

            //the logic for this is perfect do not touch or I will cry TN: 17.09.21

            //first checks if a layer has already been set, if yes, we then check wether to overwrite it
            if (ln.Layer == null || layerToGoOn != null)
                ln.AddToLayer(this, layerToGoOn);

            StandardLineCollection.StandardLines.Add(ln);
        }

        public void AddStandardLineList(List<StandardLine> slList)
        {
            if (StandardLineCollection == null)
                StandardLineCollection = new CollectionOfAllStandardLines();
            if (StandardLineCollection.StandardLines == null)
                StandardLineCollection.StandardLines = new List<StandardLine>();

            //Ensure the layer of this entity is added to the model
            foreach (var sl in slList)
            {
                sl.AddToLayer(this, sl.Layer);
            }

            StandardLineCollection.StandardLines.AddRange(slList);
        }

        public List<StandardLine> GetStandardLines()
        {
            if (StandardLineCollection == null)
                StandardLineCollection = new CollectionOfAllStandardLines();
            if (StandardLineCollection.StandardLines == null)
                StandardLineCollection.StandardLines = new List<StandardLine>();

            return this.StandardLineCollection.StandardLines;
        }
        public CollectionOfAllStandardLines GetStandardLineCollection()
        {
            if (StandardLineCollection == null)
                StandardLineCollection = new CollectionOfAllStandardLines();

            return this.StandardLineCollection;
        }
        public int NumberOfStandardLines()
        {
            if (this.StandardLineCollection == null || this.StandardLineCollection.StandardLines == null)
            {
                return 0;
            }
            else
            {
                return StandardLineCollection.StandardLines.Count;
            }
        }

        /// <summary>
        /// Removes the passed points from any standardlines that the points are nodes of. The line is disposed of if either the start or end point is removed.
        /// </summary>
        /// <param name="pointsToRemove"></param>
        public void RemovePointFromAllStandardLines(HashSet<DTMPoint> pointsToRemove)
		{
            List<StandardLine> linesToremove = new List<StandardLine>();

            foreach (var standardline in GetStandardLines())
            {
                if (pointsToRemove.Contains(standardline.StartPoint) || pointsToRemove.Contains(standardline.EndPoint))
                    linesToremove.Add(standardline);
            }

            foreach (var sl in linesToremove)
                StandardLineCollection.StandardLines.Remove(sl);
		}

        public void RemovePointFromAllStandardLines(DTMPoint pointToRemove)
		{
            if (pointToRemove == null) { return; }

            RemovePointFromAllStandardLines(new HashSet<DTMPoint> { pointToRemove });
        }

        /// <summary>
        /// Removes a standardline from the model, keeps points unless stated otherwise.
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="deletePoints">When set to <see langword="true"/>, DeletePointsFromDTMModel is called.</param>
        /// <returns></returns>
        public bool RemoveStandardLineFromModel(StandardLine standardline, bool deletePoints = false)
        {
            if (this.GetStandardLines().Count == 0) { return false; }


            if (deletePoints)
            {
                HashSet<DTMPoint> nodes = new HashSet<DTMPoint>{standardline.StartPoint, standardline.EndPoint};
                //Removes point from everything. Triangles, lines etc.. wipes it off the face of the earth (also removes empty polyline)
                DeletePointsFromEntites(nodes);
            }
            else
            {
                this.GetStandardLines().Remove(standardline);
                standardline = null;
            }

            //Final check to make sure the polyline is gone.
            if (GetStandardLines().Count > 0 && standardline != null && GetStandardLines().Contains(standardline)) { return false; }
            else { return true; }

        }

        #endregion

        #region Arc Methods
        public void AddArcLine(Arc arc, string layerToGoOn = null)
        {
            if (ArcCollection == null)
                ArcCollection = new CollectionOfAllArcs();
            if (ArcCollection.Arcs == null)
                ArcCollection.Arcs = new List<Arc>();

            //the logic for this is perfect do not touch or I will cry TN: 17.09.21, 23.06.2

            //first checks if a layer has already been set, if yes, we then check wether to overwrite it
            if (arc.Layer == null || layerToGoOn != null)
                arc.AddToLayer(this, layerToGoOn);

            ArcCollection.Arcs.Add(arc);
        }

        public void AddArcList(List<Arc> arcList)
        {
            if (ArcCollection == null)
                ArcCollection = new CollectionOfAllArcs();
            if (ArcCollection.Arcs == null)
                ArcCollection.Arcs = new List<Arc>();

            //Ensure the layer of this entity is added to the model
            foreach (Arc arc in arcList)
            {
                arc.AddToLayer(this, arc.Layer);
            }

            ArcCollection.Arcs.AddRange(arcList);
        }

        public List<Arc> GetArcs()
        {
            if (ArcCollection == null)
                ArcCollection = new CollectionOfAllArcs();
            if (ArcCollection.Arcs == null)
                ArcCollection.Arcs = new List<Arc>();

            return this.ArcCollection.Arcs;
        }
        public CollectionOfAllArcs GetArcCollection()
        {
            if (ArcCollection == null)
                ArcCollection = new CollectionOfAllArcs();

            return this.ArcCollection;
        }
        public int NumberOfArcs()
        {
            if (this.ArcCollection == null || this.ArcCollection.Arcs == null)
            {
                return 0;
            }
            else
            {
                return ArcCollection.Arcs.Count;
            }
        }

        /// <summary>
        /// Removes the passed points from any arcs that the points are nodes of. The arc is disposed of if either the start or end point is removed.
        /// </summary>
        /// <param name="pointsToRemove"></param>
        public void RemovePointFromAllArcs(HashSet<DTMPoint> pointsToRemove)
        {
            List<Arc> arcsToRemove = new List<Arc>();

            foreach (Arc arc in GetArcs())
            {
                if (pointsToRemove.Contains(arc.CentrePoint))
                    arcsToRemove.Add(arc);
            }

            foreach (Arc arc in arcsToRemove)
                ArcCollection.Arcs.Remove(arc);
        }

        public void RemovePointFromAllArcs(DTMPoint pointToRemove)
        {
            if (pointToRemove == null) { return; }

            RemovePointFromAllArcs(new HashSet<DTMPoint> { pointToRemove });
        }

        /// <summary>
        /// Removes an arc from the model, keeps points unless stated otherwise.
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="deletePoints">When set to <see langword="true"/>, DeletePointsFromDTMModel is called.</param>
        /// <returns></returns>
        public bool RemoveArcFromModel(Arc arc, bool deletePoints = false)
        {
            if (this.GetArcs().Count == 0) { return false; }


            if (deletePoints)
            {
                HashSet<DTMPoint> nodes = new HashSet<DTMPoint> { arc.StartPoint, arc.EndPoint };
                //Removes point from everything. Triangles, lines etc.. wipes it off the face of the earth (also removes empty arc)
                DeletePointsFromEntites(nodes);
            }
            else
            {
                this.GetArcs().Remove(arc);
                arc = null;
            }

            //Final check to make sure the arc is gone.
            if (GetArcs().Count > 0 && arc != null && GetArcs().Contains(arc)) { return false; }
            else { return true; }

        }
        #endregion

        #region 

        /// <summary>
        /// Convert polylines and standardlines to feature lines using the line's Name property. Assumes that the lines' nodes are already in the models Points list.
        /// </summary>
        /// <param name="Polylines"></param>
        /// <param name="StandardLines"></param>
        public void ConvertPolyAndStandardLinesToFeatureLines(List<PolyLine> Polylines = null, List<StandardLine> StandardLines = null)
        {
            if (Polylines != null)
            {
                //Create a copy of the list to avoid editing collection error
                List<PolyLine> PolylineListCopy = new List<PolyLine>();
                PolylineListCopy.AddRange(Polylines);

                foreach (PolyLine polyline in PolylineListCopy)
                {
                    string newFeatureCode = polyline.OriginalEntityName;

                    //Give the points their codes
                    for (int i = 0; i < polyline.Nodes.Count; i++)
                    {
                        //Line start, easy
                        if (i == 0)
                        {
                            polyline.Nodes[i].PointLabel += " " + newFeatureCode + "/ST"; //Add the code
                            polyline.Nodes[i].PointLabel = polyline.Nodes[i].PointLabel.Trim();
                        }

                        if (i + 1 >= polyline.Nodes.Count) { break; } //We reached the last point, fin.



                        //Test the current nodes main Point list index vs the next nodes main Point list index
                        int currentNodeIndex = Points.IndexOf(polyline.Nodes[i]); //(a nice dictionary of points and indices built at the start of the method would be cool...)
                        int nextNodeIndex = Points.IndexOf(polyline.Nodes[i + 1]);

                        if (currentNodeIndex + 1 == nextNodeIndex) //Next point's index is immediatley after the current index
                        {
                            polyline.Nodes[i + 1].PointLabel += " " + newFeatureCode; //Add the code to the next point
                            polyline.Nodes[i + 1].PointLabel = polyline.Nodes[i+1].PointLabel.Trim();
                            continue;
                        }
                        else if (currentNodeIndex > nextNodeIndex) //Next point's index is behind the current point's index
                        {
                            int rowsBehind = currentNodeIndex - nextNodeIndex;
                            polyline.Nodes[i].PointLabel += " " + newFeatureCode + "/-" + rowsBehind.ToString(); //Add the code to the current point
                            polyline.Nodes[i].PointLabel = polyline.Nodes[i].PointLabel.Trim();
                        }
                        else if (currentNodeIndex < nextNodeIndex) //Next point's index is ahead (but not immediatley after) the current point's index
                        {
                            int rowsAhead = nextNodeIndex - currentNodeIndex;
                            polyline.Nodes[i].PointLabel += " " + newFeatureCode + "/+" + rowsAhead.ToString(); //Add the code to the current point
                            polyline.Nodes[i].PointLabel = polyline.Nodes[i].PointLabel.Trim();
                        }
                        else //The last point and the current point are the same point?? Easy to ignore here, might want to check the line creation though.
                        {
                            continue;
                        }

                    }

                    //Murder and hide the body of the polyline
                    RemovePolyLineFromModel(polyline);
                }

                
            }


            if (StandardLines != null)
            {
                //Create a copy of the list to avoid editing collection error
                List<StandardLine> StandardLinesCopy = new List<StandardLine>();
                StandardLinesCopy.AddRange(StandardLines);

                foreach (StandardLine line in StandardLinesCopy)
                {
                    string newFeatureCode = line.OriginalEntityName;

                    //Give the points their codes

                    //Line start, easy
                    line.StartPoint.PointLabel += " " + newFeatureCode + "/ST"; //Add the code
                    line.StartPoint.PointLabel = line.StartPoint.PointLabel.Trim();

                    //Get index of the points in the main Points list
                    int currentNodeIndex = Points.IndexOf(line.StartPoint); //(a nice dictionary of points and indices built at the start of the method would be cool...)
                    int nextNodeIndex = Points.IndexOf(line.EndPoint);

                    if (currentNodeIndex + 1 == nextNodeIndex) //Next point's index is immediatley after the current index
                    {
                        line.EndPoint.PointLabel += " " + newFeatureCode; //Add the code to the next point
                        line.EndPoint.PointLabel = line.EndPoint.PointLabel.Trim();
                    }
                    else if (currentNodeIndex > nextNodeIndex) //Next point's index is behind the current point's index
                    {
                        int rowsBehind = currentNodeIndex - nextNodeIndex;
                        line.StartPoint.PointLabel += " " + newFeatureCode + "/-" + rowsBehind.ToString(); //Add the code to the current point
                        line.StartPoint.PointLabel = line.StartPoint.PointLabel.Trim();
                    }
                    else if (currentNodeIndex < nextNodeIndex) //Next point's index is ahead (but not immediatley after) the current point's index
                    {
                        int rowsAhead = nextNodeIndex - currentNodeIndex;
                        line.StartPoint.PointLabel += " " + newFeatureCode + "/+" + rowsAhead.ToString(); //Add the code to the current point
                        line.StartPoint.PointLabel = line.StartPoint.PointLabel.Trim();
                    }
                    else //The last point and the current point are the same point?? Easy to ignore here, might want to check the line creation though.
                    {
                        line.StartPoint.PointLabel = "";
                        line.EndPoint.PointLabel = "";
                        continue;
                    }

                    //Murder and hide the body of the line
                    RemoveStandardLineFromModel(line);
                }


            }


            //to do, [option] create actual features from the polyline and standardline properties and load the model with those features
            //itll make the dxf import look pretty much identical to the original
        }

        #endregion

        #region Block Management

        public Dictionary<string,BlockDefinition> GetBlockDefinitionsDictionary()
		{
            if (BlockCollection == null)
                BlockCollection = new CollectionOfBlocks();


            if (BlockCollection.BlockDefinitions == null)
                BlockCollection.BlockDefinitions = new Dictionary<string, BlockDefinition>();

            return this.BlockCollection.BlockDefinitions;
        }

        public CollectionOfBlocks GetBlockCollection()
		{
            if (BlockCollection == null)
                BlockCollection = new CollectionOfBlocks();

            return this.BlockCollection;
        }

       

    

           

        



        /// <summary>
        /// Returns a list of all block inserts used by every point. More specifically, the order in which the block inserts were used to create the Block Collection SharedRenderItem
        /// </summary>
        /// <returns></returns>
        public List<BlockInsert> GetListOfAllBlockInsertsUsed()
		{
            List<BlockInsert> usedInserts = new List<BlockInsert>();

            foreach( var point in Points)
			{
                if (point.BlockInserts != null && point.BlockInserts.Count > 0)
                {
                    usedInserts.AddRange(point.BlockInserts);
                }
			}
            return usedInserts;
		}


		#endregion

		#region Update Points

		//Added by ES:13.05.21
		/// <summary>
		/// RegenerateBounds
		/// Run through the entiree points list to determine the model extents
		/// </summary>
		public void RegenerateBounds()
        {
            if (Points != null && Points.Count > 0)
            {
                Bounds = new Bounds();
                foreach (DTMPoint pt in Points)
                {
                    Bounds.Update(pt.X, pt.Y, pt.Z);
                }

                modelOffset = Bounds.Center;
                UpdateBounds = false;

            }
        }

        public void RegenerateBoundsByLine(Line line)
        {
            if (line.Points != null && line.Points.Count > 0)
            {
                Bounds = new Bounds();
                foreach (Point3D pt in line.Points)
                {
                    Bounds.Update(pt.X, pt.Y, pt.Z);
                }

                modelOffset = Bounds.Center;
                UpdateBounds = false;
            }
        }

        /// <summary>
        /// Attmempts to update the property of a <see cref="DTMPoint"/> found in the datagrid with its new value
        /// </summary>
        /// <param name="pointIndex">The index of the <see cref="DTMPoint"/> in the <see cref="Points"/> list</param>
        /// <param name="oldValue">The old value for the property</param>
        /// <param name="newValue">The new value for the property</param>
        /// <param name="property">The property type to update</param>
        public bool UpdatePointFromGrid(int pointIndex, string newValue, PointProperties property)
        {
            //It is assumed there is a difference between the new and old value

            //If the point is not in the list we don't need to do anything
            if (pointIndex >= 0 && pointIndex > Points.Count - 1)
                return false;

            var point = Points[pointIndex];

            //Pt Num, Label, Notes 1 and Notes 2 are generic so update them here
            if (property == PointProperties.PtNum)
            {
                //Update the point ID
                point.PointID = newValue;
                SaveRequired = true;
            }
            else if (property == PointProperties.Label)
            {
                //Update the label
                if (point.Type == PointType.Coordinate || point.Type == PointType.Observation)
                {
                    if (RemovePointFeature(point, true))
                    {
                        point.PointLabel = newValue;
                        SaveRequired = true;
                        return AddPointFeature(point);
                    }
                    else
                    {
                        //The attempt to update the point was not successful
                        return false;
                    }
                }
                else
                {
                    //The label doesn't effect how the point is drawn
                    point.PointLabel = newValue;
                    SaveRequired = true;
                }
            }
            //else if (property == PointProperties.Notes1)
            //{
            //    //Update Notes 1

            //    //Make sure there is a valid notes array, if not create one
            //    if (point.Notes == null || point.Notes.Length < 1)
            //        point.Notes = new string[1];

            //    point.Notes[0] = newValue;
            //    SaveRequired = true;
            //}
            //else if (property == PointProperties.Notes2)
            //{
            //    //Update Notes 2

            //    //Make sure there is a valid notes array, if not create one
            //    if (point.Notes == null)
            //        point.Notes = new string[2];
            //    else if (point.Notes.Length == 1)
            //    {
            //        //If there is only 1 note we need to preserve it
            //        var note = point.Notes[0];
            //        point.Notes = new string[2];
            //        point.Notes[0] = note;
            //    }
            //    else if (point.Notes.Length < 2)
            //        point.Notes = new string[2];

            //    point.Notes[1] = newValue;
            //    SaveRequired = true;
            //}
            else
            {
                //Now update the property involved with point type specific logic
                bool pointUpdated = false;
                double originalX = point.X, originalY = point.Y, originalZ = point.Z;

                switch (point.Type)
                {
                    case PointType.FalsePoint:
                        if (UpdateFalsePoint(ref point, newValue, property))
                        {
                            AddPointFeature(point);
                            pointUpdated = true;
                        }
                        break;
                    case PointType.Coordinate:
                        pointUpdated = UpdateDTMPoint(point, newValue, property);
                        break;
                    case PointType.Observation:
                        if (point is ObservationPoint)
                            pointUpdated = UpdateObservationPoint((ObservationPoint)point, newValue, property);
                        break;
                    case PointType.ReferenceObservation:
                        if (point is ReferenceObservation)
                            pointUpdated = UpdateReferenceObservation((ReferenceObservation)point, newValue, property);
                        break;
                    case PointType.OccupiedStation:
                    case PointType.ReferenceStation:
                        if (point is StationPoint)
                            pointUpdated = UpdateStationPoint((StationPoint)point, newValue, property);
                        break;
                    case PointType.AzimuthCorrection:
                        if (point is AzimuthCorrection)
                            pointUpdated = UpdateAzimuthCorrection((AzimuthCorrection)point, newValue, property);
                        break;
                    default:
                        return false;
                }

                if (pointUpdated)
                {
                    SaveRequired = true;

                    //Check to see if the point is on the edge of the model and therefore needs to trigger a bounds update
                    if (Bounds.Min.X == originalX || Bounds.Min.Y == originalY || Bounds.Min.Z == originalZ)
                        UpdateBounds = true;
                    else if (Bounds.Max.X == originalX || Bounds.Max.Y == originalY || Bounds.Max.Z == originalZ)
                        UpdateBounds = true;

                    if (Bounds.Update(point))
                        UpdateBounds = true;

                    //Check triangle validity
                    if (point.Triangles == null)
                        return pointUpdated;

                    bool updateModel = false;

                    //Determine if any triangle connected to the point intersects with any triangle connected to those triangle
                    foreach (var triangle in point.Triangles)
                    {
                        if (triangle.Point1 != point && triangle.Point1.Triangles != null)
                        {
                            foreach (var nexTri in triangle.Point1.Triangles)
                            {
                                if (nexTri == triangle)
                                    continue;
                                else if (nexTri.IntersectsWith(triangle))
                                {
                                    updateModel = true;
                                    break;
                                }

                            }

                            if (updateModel)
                                break;
                        }

                        if (triangle.Point2 != point && triangle.Point2.Triangles != null)
                        {
                            foreach (var nextTri in triangle.Point2.Triangles)
                            {
                                if (nextTri == triangle)
                                    continue;
                                else if (nextTri.IntersectsWith(triangle))
                                {
                                    updateModel = true;
                                    break;
                                }
                            }

                            if (updateModel)
                                break;
                        }

                        if (triangle.Point3 != point && triangle.Point3.Triangles != null)
                        {
                            foreach (var nextTri in triangle.Point3.Triangles)
                            {
                                if (nextTri == triangle)
                                    continue;
                                else if (nextTri.IntersectsWith(triangle))
                                {
                                    updateModel = true;
                                    break;
                                }
                            }

                            if (updateModel)
                                break;
                        }
                    }

                    if (updateModel)
                    {
                        double newX = point.X, newY = point.Y, newZ = point.Z;

                        //Set the X, Y, Z values to previous so the remove from triangle model works correctly
                        point.X = originalX;
                        point.Y = originalY;
                        point.Z = originalZ;
                        RemovePointFromTriangleModel(point);



                        //Now we can return to the new X, Y, Z
                        point.X = newX;
                        point.Y = newY;
                        point.Z = newZ;
                        InsertPointIntoTriangleModel(point);


                    }

                    return pointUpdated;
                }
            }

            //If we get to here the only changes made did not alter the coordinates of the point
            return false;
        }

        /// <summary>
        /// Attempts to update a property of a <see cref="StationPoint"/> and pushes that update out to all derived observation coordinates 
        /// </summary>
        /// <param name="point">The <see cref="StationPoint"/> to update</param>
        /// <param name="oldValue">The old property value</param>
        /// <param name="newValue">The new property value</param>
        /// <param name="property">The property type to update. Supported types are: 
        /// <para><see cref="PointProperties.TgtHt"/>, <see cref="PointProperties.X"/>, <see cref="PointProperties.Y"/>, <see cref="PointProperties.Z"/></para></param>
        private bool UpdateStationPoint(StationPoint point, string newValue, PointProperties property)
        {
            //The new value should always be a double
            if (!double.TryParse(newValue, out double val))
                return false;

            bool pointUpdated = true;

            //Update the property
            switch (property)
            {
                case PointProperties.TgtHt:
                    point.ObjectHeight = val;
                    break;
                case PointProperties.X:
                    point.X = val;
                    break;
                case PointProperties.Y:
                    point.Y = val;
                    break;
                case PointProperties.Z:
                    point.Z = val;
                    break;
                default:
                    pointUpdated = false;
                    break;
            }

            //If the property is not an accepted one the point will not be updated
            if (pointUpdated)
                UpdatePointsFromStationSetup(point.StationSetup);

            return pointUpdated;
        }

        /// <summary>
        /// Attempts to update a property of a <see cref="ReferenceObservation"/> and pushes that update out to all derived observation coordinates
        /// </summary>
        /// <param name="point">The <see cref="ReferenceObservation"/> to update</param>
        /// <param name="oldValue">The old property value</param>
        /// <param name="newValue">The new property value</param>
        /// <param name="property">The property type to update. Supported types are:
        /// <para><see cref="PointProperties.HCR"/>, <see cref="PointProperties.VCR"/>, <see cref="PointProperties.SD"/>, <see cref="PointProperties.TgtHt"/></para></param>
        private bool UpdateReferenceObservation(ReferenceObservation point, string newValue, PointProperties property)
        {
            double val = 0;
            bool reduceRequired = false;

            //Update the property
            switch (property)
            {
                case PointProperties.HCR:
                    val = Trig.DMSToDeg(newValue);
                    if (val != -999)
                    {
                        while (val < 0)
                            val += 360;
                        while (val > 360)
                            val -= 360;

                        point.HCR = Trig.DegToRad(val);
                        reduceRequired = true;
                    }
                    break;
                case PointProperties.VCR:
                    val = Trig.DMSToDeg(newValue);
                    if (val != -999)
                    {
                        while (val < 0)
                            val += 360;
                        while (val > 360)
                            val -= 360;

                        point.VCR = Trig.DegToRad(val);
                        reduceRequired = true;
                    }
                    break;
                case PointProperties.SD:
                    if (double.TryParse(newValue, out val))
                    {
                        point.SD = val;
                        reduceRequired = true;
                    }
                    break;
                case PointProperties.TgtHt:
                    if (double.TryParse(newValue, out val))
                    {
                        point.TargetHeight = val;
                        reduceRequired = true;
                    }
                    break;
            }

            if (reduceRequired)
            {
                //Reduced the reference observation to its new value
                point.ReduceCoordinate(point.StationSetup);

                //Update all points that make use of this station setup
                UpdatePointsFromStationSetup(point.StationSetup);
            }

            return reduceRequired;
        }

        /// <summary>
        /// Attempts to update a property of a <see cref="AzimuthCorrection"/> and pushes that update out to all derived observation coordinates 
        /// </summary>
        /// <param name="point">The <see cref="AzimuthCorrection"/> to update</param>
        /// <param name="oldValue">The old property value</param>
        /// <param name="newValue">The new property value</param>
        /// <param name="property">The property type to update. Supported types are:
        /// <para><see cref="PointProperties.HCR"/></para></param>
        private bool UpdateAzimuthCorrection(AzimuthCorrection point, string newValue, PointProperties property)
        {
            double val = 0;
            bool pointUpdated = false;

            //Update the property
            switch (property)
            {
                case PointProperties.HCR:
                    val = Trig.DMSToDeg(newValue);
                    if (val != -999)
                    {
                        //Sanitize
                        while (val < 0)
                            val += 360;
                        while (val > 360)
                            val -= 360;

                        point.HCR = Trig.DegToRad(val);
                        pointUpdated = true;
                    }
                    break;
            }

            //Update all the related observation points
            if (pointUpdated)
                UpdatePointsFromStationSetup(point.StationSetup);

            return pointUpdated;
        }

        /// <summary>
        /// Attempts to update a property of a <see cref="ObservationPoint"/>
        /// </summary>
        /// <param name="point">The <see cref="ObservationPoint"/> to update</param>
        /// <param name="oldValue">The old property value</param>
        /// <param name="newValue">The new property value</param>
        /// <param name="property">The property type to update. Supported types are:
        /// <para><see cref="PointProperties.HCR"/>, <see cref="PointProperties.VCR"/>, <see cref="PointProperties.SD"/>, <see cref="PointProperties.TgtHt"/>
        /// <see cref="PointProperties.X"/>, <see cref="PointProperties.Y"/>, <see cref="PointProperties.Z"/></para></param>
        private bool UpdateObservationPoint(ObservationPoint point, string newValue, PointProperties property)
        {
            double val = 0;
            bool reduceRequired = false, recalcRequired = false;

            //Update the property
            switch (property)
            {
                case PointProperties.HCR:
                    val = Trig.DMSToDeg(newValue);
                    if (val != -999)
                    {
                        //Sanitize
                        while (val < 0)
                            val += 360;
                        while (val > 360)
                            val -= 360;

                        point.HCR = Trig.DegToRad(val);
                        reduceRequired = true;
                    }
                    break;
                case PointProperties.VCR:
                    val = Trig.DMSToDeg(newValue);
                    if (val != -999)
                    {
                        //Sanitize
                        while (val < 0)
                            val += 360;
                        while (val > 360)
                            val -= 360;

                        point.VCR = Trig.DegToRad(val);
                        reduceRequired = true;
                    }
                    break;
                case PointProperties.SD:
                    if (double.TryParse(newValue, out val))
                    {
                        point.SD = val;
                        reduceRequired = true;
                    }
                    break;
                case PointProperties.TgtHt:
                    if (double.TryParse(newValue, out val))
                    {
                        point.TargetHeight = val;
                        reduceRequired = true;
                    }
                    break;
                case PointProperties.X:
                    if (double.TryParse(newValue, out val))
                    {
                        point.X = val;
                        recalcRequired = true;
                    }
                    break;
                case PointProperties.Y:
                    if (double.TryParse(newValue, out val))
                    {
                        point.Y = val;
                        recalcRequired = true;
                    }
                    break;
                case PointProperties.Z:
                    if (double.TryParse(newValue, out val))
                    {
                        point.Z = val;
                        recalcRequired = true;
                    }
                    break;
            }

            if (reduceRequired || recalcRequired)
            {
                //Find the appropriate station setup
                if (Points.Contains(point))
                {
                    var index = Points.IndexOf(point);
                    if (index <= Points.Count - 1)
                    {
                        //Not sure if this is faster than getting the station setup from the list of setups, i guess it depends on the size / setup of the file
                        for (int i = index; i > 0; i--)
                        {
                            var p = Points[i];
                            if (p.Type == PointType.ReferenceObservation)
                            {
                                if (p is ReferenceObservation tmp)
                                {
                                    if (reduceRequired)
                                        point.ReduceCoordinate(tmp.StationSetup);
                                    else if (recalcRequired)
                                        point.ConvertXYZToObs(tmp.StationSetup);
                                    break;
                                }
                            }
                            else if (p.Type == PointType.AzimuthCorrection)
                            {
                                if (p is AzimuthCorrection tmp)
                                {
                                    if (reduceRequired)
                                        point.ReduceCoordinate(tmp.StationSetup);
                                    else if (recalcRequired)
                                        point.ConvertXYZToObs(tmp.StationSetup);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return reduceRequired || recalcRequired;
        }

        /// <summary>
        /// Attempts to update a property of a <see cref="DTMPoint"/>
        /// </summary>
        /// <param name="point">The <see cref="DTMPoint"/> to update</param>
        /// <param name="oldValue">The old property value</param>
        /// <param name="newValue">The new property value</param>
        /// <param name="property">The property type to update. Supported types are:
        /// <para><see cref="PointProperties.X"/>, <see cref="PointProperties.Y"/>, <see cref="PointProperties.Z"/></para></param>
        private bool UpdateDTMPoint(DTMPoint point, string newValue, PointProperties property)
        {
            //The new value should always be a double
            if (!double.TryParse(newValue, out double val))
                return false;

            bool pointUpdated = true;

            //Update the property
            switch (property)
            {
                case PointProperties.X:
                    point.X = val;
                    break;
                case PointProperties.Y:
                    point.Y = val;
                    break;
                case PointProperties.Z:
                    point.Z = val;
                    break;
                default:
                    pointUpdated = false;
                    break;
            }

            return pointUpdated;
        }

        private bool UpdateFalsePoint(ref DTMPoint point, string newValue, PointProperties property)
        {
            if (!Points.Contains(point))
                return false;

            var index = Points.IndexOf(point);

            double val = 0;
            bool reduceRequired = false;
            bool pointUpdated = false;

            //Update the property
            switch (property)
            {
                case PointProperties.HCR:
                    val = Trig.DMSToDeg(newValue);
                    if (val != -999)
                    {
                        //Sanitize
                        while (val < 0)
                            val += 360;
                        while (val > 360)
                            val -= 360;

                        //Create new point object
                        Points[index] = new ObservationPoint()
                        {
                            X = point.X,
                            Y = point.Y,
                            Z = point.Z,
                            PointID = point.PointID,
                            PointLabel = point.PointLabel,
                            Notes = point.Notes
                        };
                        point = Points[index];

                        (point as ObservationPoint).HCR = Trig.DegToRad(val);
                        reduceRequired = true;
                    }
                    break;
                case PointProperties.VCR:
                    val = Trig.DMSToDeg(newValue);
                    if (val != -999)
                    {
                        //Sanitize
                        while (val < 0)
                            val += 360;
                        while (val > 360)
                            val -= 360;

                        //Create new point object
                        Points[index] = new ObservationPoint()
                        {
                            X = point.X,
                            Y = point.Y,
                            Z = point.Z,
                            PointID = point.PointID,
                            PointLabel = point.PointLabel,
                            Notes = point.Notes
                        };
                        point = Points[index];

                        (point as ObservationPoint).VCR = Trig.DegToRad(val);
                        reduceRequired = true;
                    }
                    break;
                case PointProperties.SD:
                    if (double.TryParse(newValue, out val))
                    {
                        Points[index] = new ObservationPoint()
                        {
                            X = point.X,
                            Y = point.Y,
                            Z = point.Z,
                            PointID = point.PointID,
                            PointLabel = point.PointLabel,
                            Notes = point.Notes
                        };
                        point = Points[index];

                        (point as ObservationPoint).SD = val;
                        reduceRequired = true;
                    }
                    break;
                case PointProperties.TgtHt:
                    if (double.TryParse(newValue, out val))
                    {
                        Points[index] = new ObservationPoint()
                        {
                            X = point.X,
                            Y = point.Y,
                            Z = point.Z,
                            PointID = point.PointID,
                            PointLabel = point.PointLabel,
                            Notes = point.Notes
                        };
                        point = Points[index];

                        (point as ObservationPoint).TargetHeight = val;
                        reduceRequired = true;
                    }
                    break;
                case PointProperties.X:
                    if (double.TryParse(newValue, out val))
                    {
                        point.X = val;
                        pointUpdated = true;
                    }
                    break;
                case PointProperties.Y:
                    if (double.TryParse(newValue, out val))
                    {
                        point.Y = val;
                        pointUpdated = true;
                    }
                    break;
                case PointProperties.Z:
                    if (double.TryParse(newValue, out val))
                    {
                        point.Z = val;
                        pointUpdated = true;
                    }
                    break;
            }

            if (reduceRequired)
            {
                //Find the appropriate station setup
                if (Points.Contains(point))
                {
                    if (index <= Points.Count - 1)
                    {
                        //Not sure if this is faster than getting the station setup from the list of setups, i guess it depends on the size / setup of the file
                        for (int i = index; i > 0; i--)
                        {
                            var p = Points[i];
                            if (p.Type == PointType.ReferenceObservation)
                            {
                                if (p is ReferenceObservation tmp)
                                {
                                    if (reduceRequired)
                                        (point as ObservationPoint)?.ReduceCoordinate(tmp.StationSetup);

                                    break;
                                }
                            }
                            else if (p.Type == PointType.AzimuthCorrection)
                            {
                                if (p is AzimuthCorrection tmp)
                                {
                                    if (reduceRequired)
                                        (point as ObservationPoint)?.ReduceCoordinate(tmp.StationSetup);

                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (pointUpdated)
                point.Type = PointType.Coordinate;

            return (pointUpdated | reduceRequired);
        }

        /// <summary>
        /// Attempts to update the reduced coordinates of every point that derives from the provided <see cref="StationSetup"/>
        /// </summary>
        /// <param name="setup">The <see cref="StationSetup"/> to use for coordinate reductions</param>
        private void UpdatePointsFromStationSetup(StationSetup setup)
        {
            //Check there is actually a station setup
            if (setup == null || !StationSetups.Contains(setup))
                return;

            //Make sure the values are up to date
            setup.UpdateStationSetup();

            //Get the start index of the setup (this will be the index of the reference observation or the azimuth correction
            int startIndex = -1;

            //Find the start index of the setup, if valid
            if (setup.ReferenceObservation != null && Points.Contains(setup.ReferenceObservation))
                startIndex = Points.IndexOf(setup.ReferenceObservation) + 1;
            else if (setup.AzimuthCorrection != null && Points.Contains(setup.AzimuthCorrection))
                startIndex = Points.IndexOf(setup.AzimuthCorrection) + 1;
            else
                return;

            //Check start index is valid
            if (startIndex < 0 || startIndex > Points.Count - 1)
                return;

            if (PointsToUpdate == null)
                PointsToUpdate = new BindingList<DTMPoint>();

            //Iterate through all the points to check and update coordinates where required
            for (int i = startIndex; i < Points.Count; i++)
            {
                var point = Points[i];

                if (point.Type == PointType.Observation)
                {
                    //Update the point based on the station setup
                    if (point is ObservationPoint tmp)
                    {
                        tmp.ReduceCoordinate(setup);
                        if (!PointsToUpdate.Contains(point))
                            PointsToUpdate.Add(point);
                    }
                }
                else if (point.Type == PointType.Coordinate)
                    continue;
                else
                    break;
            }

            if (PointsToUpdate.Count <= 0)
                PointsToUpdate = null;
        }

        #endregion

        public void CalcAlphaShape(double alpha = 10)
		{
            if (this.Points != null && this.Points.Count > 2)
            {
                //this.TriangulateModel();

                //if (this.Triangles != null & this.Triangles.Count > 0)
                //{


                //    foreach (Triangle t in this.Triangles)
                //    {
                //        BindingList<DTMPoint> pts = new BindingList<DTMPoint>();
                //        pts.Add
                //    }


                    AlphaShapes.AlphaShape alphas = new AlphaShapes.AlphaShape(this.Points, alpha);
                    
                    

                    
                    if (alphas.BorderEdges != null && alphas.BorderEdges.Count > 0)
                    {

                        foreach (AlphaShapes.Edge ed in alphas.BorderEdges)
                        {
                            StandardLine ln = new StandardLine();
                            ln.StartPoint = ed.A;
                            ln.EndPoint = ed.B;
                            this.AddStandardLine(ln);
                        }

                    }
                //}
            }

		}

		#region Add Remove Points

		/// <summary>
		/// Attempts to remove a point feature from the <see cref="DTM"/>
		/// </summary>
		/// <param name="point">The <see cref="DTMPoint"/> to attempt to remove</param>
		/// <param name="updatePoint">Whether or not the operation is to update the point label</param>
		/// <returns>Returns true if the point feature was successfully removed</returns>
		public bool RemovePointFeature(DTMPoint point, bool updatePoint = false)
        {
        //If the point is not in the model we cannot update it
        if (!Points.Contains(point))
            return false;

        if (FeaturesDictionary == null)
            return true;

        //Sort the cad features
        if (point.CADFeatures != null)
        {
            for (int i = 0; i < point.CADFeatures.Count;)
            {
                var item = point.CADFeatures[i];
                if (point == item.StartLine)
                {
                    //We must remove the cadfeature since this point contains the definition to it
                    point.CADFeatures.Remove(item);
                    item.EndLine.CADFeatures.Remove(item);
                    item.FeatureCollection.CADFeatureItems[item.Code].Remove(item);
                }
                else if (updatePoint)
                {
                    //The end point is being removed so we must remove the string from the start point lavel

                    var startIndex = Points.IndexOf(item.StartLine);
                    var endIndex = Points.IndexOf(item.EndLine);

                    var increment = endIndex - startIndex;

                    if (increment < 0)
                    {
                        //The increment is negative
                        var cadCode = item.Code + "/" + increment.ToString();
                        var pointCode = item.StartLine.PointLabel.ToUpper();

                        //Remove any instance of the cad code (E.g. C/-10)
                        for (int index = 0; ; index += cadCode.Length)
                        {
                            index = pointCode.IndexOf(cadCode, index);
                            if (index == -1)
                            {
                                //There are no more entries of this code in the label
                                break;
                            }
                            else
                            {
                                //Remove the code from the label
                                pointCode.Remove(index, cadCode.Length);
                                item.StartLine.PointLabel.Remove(index, cadCode.Length);
                            }
                        }
                    }
                    else
                    {
                        //The increment is positive and can be two different strings
                        var cadCode = item.Code + "/" + increment.ToString();
                        var cadPlusCode = item.Code + "/+" + increment.ToString();
                        var pointCode = item.StartLine.PointLabel.ToUpper();

                        //Remove any instance of the cad code (E.g. C/10)
                        for (int index = 0; ; index += cadCode.Length)
                        {
                            index = pointCode.IndexOf(cadCode, index);
                            if (index == -1)
                            {
                                //There are no more entries of this code in the label
                                break;
                            }
                            else
                            {
                                //Remove the code from the label
                                pointCode.Remove(index, cadCode.Length);
                                item.StartLine.PointLabel.Remove(index, cadCode.Length);
                            }
                        }

                        //Remove any instance of the cadPlusCode (E.g. C/+10)
                        for (int index = 0; ; index += cadPlusCode.Length)
                        {
                            index = pointCode.IndexOf(cadPlusCode, index);
                            if (index == -1)
                            {
                                //There are no more entries of this code in the label
                                break;
                            }
                            else
                            {
                                //Remove the code from the label
                                pointCode.Remove(index, cadPlusCode.Length);
                                item.StartLine.PointLabel.Remove(index, cadPlusCode.Length);
                            }
                        }
                    }

                    //The cad line has been sufficiently removed
                    i++;
                }
                else
                {
                    //The index of the point remains the same so cad line is still valid
                    i++;
                }
            }
        }

        //Sort the point features
        if (point.PointFeatures != null)
        {
            for (int i = 0; i < point.PointFeatures.Count; i++)
            {
                var collection = point.PointFeatures[i];

                //Remove the point from the collection
                if (collection.Points.Contains(point))
                    collection.Points.Remove(point);

                if (collection.Points.Count <= 0)
                {
                    //If there are no points we don't need the collection at all
                    PointFeatures.Remove(collection.Feature.FieldCode);
                }
            }
        }

        //Remove all the point features
        point.PointFeatures = null;

        //Sort the Line Features
        if (point.LineFeatures != null)
        {
            for (int i = 0; i < point.LineFeatures.Count;)
            {

                var lineFeature = point.LineFeatures[i];

                //Assuming the point is part of the line
                if (lineFeature.Points.Contains(point))
                {
                    if (lineFeature.Points.Count <= 1)
                    {
                        lineFeature.RemovePoint(point);
                        //If there is only this point or no points we can dispose of the line feature so remove it from the collection
                        lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].Remove(lineFeature);
                        if (lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].Count <= 0)
                        {
                            //If there are no more features items for the collection we can remove the collection
                            lineFeature.FeatureCollection.FeatureItems.Remove(lineFeature.Code);
                        }

                        //At this point we are done
                        continue;
                    }

                    //If this is the only feature for this code
                    if (lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].Count == 1)
                    {
                        //We can just freely remove the point from the line
                        if (lineFeature.Points.Last() == point)
                        {
                            lineFeature.RemovePoint(point);
                            lineFeature.Closed = false;
                        }
                        else
                        {
                            lineFeature.RemovePoint(point);
                        }

                        //At this point we are done
                        continue;
                    }

                    bool mergeWithNext = false, mergeWithPrev = false;

                    //If this is the first feature for this code
                    if (lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].First() == lineFeature)
                    {
                        if (lineFeature.Points.Last() != point)
                        {
                            //If this is not the last point we can go ahead and remove it
                            lineFeature.RemovePoint(point);
                            continue;
                        }
                        else
                        {
                            //We need to attempt to merge with the next feature
                            mergeWithNext = true;
                        }
                    }
                    else
                    {
                        //At this point we know there is more than 1 feature and this is not the first
                        if (lineFeature.Points.First() == point)
                        {
                            //We must check for a start flag on this point or a close flag on the previous feature
                            mergeWithPrev = true;
                        }
                        else if (lineFeature.Points.Last() == point)
                        {
                            //We must check for a close flag on this point or a start flag on the next point
                            mergeWithNext = true;
                        }
                        else
                        {
                            //Its not the start or the end so we can just remove it
                            lineFeature.RemovePoint(point);
                            mergeWithPrev = true;

                            continue;
                        }
                    }

                    if (mergeWithNext)
                    {
                        //We now only need to check to see if the point label contains a close flag
                        var upperLabel = point.PointLabel.ToUpper();
                        if (upperLabel.Contains(lineFeature.Code + "/CL") || upperLabel.Contains(lineFeature.Code + "/CLOSE"))
                        {
                            LineFeatureItem nextFeature = null;

                            //Find the next valid featureItem
                            var index = lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].IndexOf(lineFeature) + 1;
                            if (index < lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].Count)
                            {
                                nextFeature = lineFeature.FeatureCollection.FeatureItems[lineFeature.Code][index++];

                                while (nextFeature.Points.Count <= 0)
                                {
                                    //Remove the feature from the collection since its not needed and get the next (if applicable)
                                    lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].Remove(nextFeature);
                                    if (lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].Count <= 0 || index >= lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].Count)
                                    {
                                        //There are not more valid items to check
                                        nextFeature = null;
                                        break;
                                    }

                                    //Get the next feature
                                    nextFeature = lineFeature.FeatureCollection.FeatureItems[lineFeature.Code][index++];
                                }
                            }

                            if (nextFeature == null)
                            {
                                //There is no next feature so we can remove the point and be done
                                lineFeature.RemovePoint(point);
                                lineFeature.Closed = false;
                                continue;
                            }

                            //Check the first point for a start flag
                            if (nextFeature.Started)
                            {
                                //There is a start flag so we are good to remove the last point
                                lineFeature.RemovePoint(point);
                                lineFeature.Closed = false;
                                continue;
                            }
                            else
                            {
                                //There is no start flag so we must add all the point from the next feature to this feature

                                //First remove the current point
                                lineFeature.Points.Remove(point);
                                lineFeature.Closed = false;

                                //Add the points
                                for (int j = 0; j < nextFeature.Points.Count;)
                                {
                                    var p = nextFeature.Points[j];
                                    nextFeature.RemovePoint(p);
                                    lineFeature.AddPoint(p);
                                }

                                lineFeature.Closed = nextFeature.Closed;

                                //Now we must dispose of the next feature
                                lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].Remove(nextFeature);
                                continue;
                            }
                        }
                        else
                        {
                            //We can go ahead and remove the point
                            lineFeature.Points.Remove(point);
                            continue;
                        }
                    }

                    if (mergeWithPrev)
                    {
                        //We now only need to check to see if the point label contains a start flag
                        var upperLabel = point.PointLabel.ToUpper();
                        if (upperLabel.Contains(lineFeature.Code + "/START") || upperLabel.Contains(lineFeature.Code + "/ST"))
                        {
                            //Find the prev valid featureItem
                            var index = lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].IndexOf(lineFeature) - 1;
                            var prevFeature = lineFeature.FeatureCollection.FeatureItems[lineFeature.Code][index--];

                            while (prevFeature.Points.Count <= 0)
                            {
                                //Remove the feature from the collection since its not needed and get the next (if applicable)
                                lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].Remove(prevFeature);
                                if (index < 0 || lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].Count <= 0)
                                {
                                    //There are no more valid items to check
                                    prevFeature = null;
                                    break;
                                }

                                //Get the next feature
                                prevFeature = lineFeature.FeatureCollection.FeatureItems[lineFeature.Code][index--];
                            }

                            if (prevFeature == null)
                            {
                                //There is no prev feature so we can remove the point and be done
                                lineFeature.RemovePoint(point);
                                continue;
                            }

                            //Check the last point for a close flag
                            if (prevFeature.Closed)
                            {
                                //There is a close flag so we are good to remove the first point
                                lineFeature.RemovePoint(point);
                                lineFeature.Started = false;
                                continue;
                            }
                            else
                            {
                                //There is no close flag so we must add all the points from this feature to the previous feature

                                //First remove the current point
                                lineFeature.RemovePoint(point);

                                //Add the points
                                for (int j = 0; j < lineFeature.Points.Count;)
                                {
                                    var p = lineFeature.Points[j];
                                    lineFeature.RemovePoint(p);
                                    prevFeature.AddPoint(p);
                                }

                                prevFeature.Closed = lineFeature.Closed;

                                //Now we must dispose of the current feature
                                lineFeature.FeatureCollection.FeatureItems[lineFeature.Code].Remove(lineFeature);
                                continue;
                            }
                        }
                        else
                        {
                            //We can go ahead and remove the point
                            lineFeature.RemovePoint(point);
                            continue;
                        }
                    }

                    //We shouldn't get here but just incase
                    lineFeature.RemovePoint(point);
                    continue;
                }
                else
                {
                    i++;
                }
            }
        }

        //Remove all the line features
        point.LineFeatures = null;

        return true;
    }

        /// <summary>
        /// Attempts to add a point feature to the <see cref="DTM"/>
        /// </summary>
        /// <param name="point">The <see cref="DTMPoint"/> to add</param>
        /// <returns>Returns true if the point feature was successfully added</returns>
        public bool AddPointFeature(DTMPoint point)
        {
            if (!Points.Contains(point))
                return false;

            if (FeaturesDictionary == null)
                FeaturesDictionary = new Dictionary<char, List<Feature>>();
            //return true;

            //Add to the default points list
            if (point.PointLabel == "" || point.PointLabel == null)
            {
                PointFeature defaultPointFeature = null;

                //Get the default feature if there is one
                if (FeaturesDictionary.ContainsKey('@'))
                {
                    if (FeaturesDictionary['@'].Count <= 0)
                    {
                        defaultPointFeature = new PointFeature() { Code = "@", FieldCode = "@" };
                        FeaturesDictionary['@'] = new List<Feature> { defaultPointFeature };
                    }
                    else
                    {
                        var defaultFeature = FeaturesDictionary['@'][0];
                        if (defaultFeature is PointFeature)
                            defaultPointFeature = defaultFeature as PointFeature;
                    }
                }
                else
                {
                    //We should add a default feature
                    defaultPointFeature = new PointFeature() { Code = "@", FieldCode = "@" };
                    FeaturesDictionary.Add('@', new List<Feature> { defaultPointFeature });
                }

                if (defaultPointFeature == null)
                    return true;

                if (!PointFeatures.ContainsKey(defaultPointFeature.FieldCode))
                    PointFeatures.Add(defaultPointFeature.FieldCode, new PointFeatureCollection(defaultPointFeature, this));

                var collection = PointFeatures[defaultPointFeature.FieldCode];
                collection.AddPoint(point);

                return true;
            }

            //Interpret the features
            if (point.PointLabel == "" || FeaturesDictionary == null)
                return true;

            var pointIndex = Points.IndexOf(point);

            var codes = point.PointLabel.ToUpper().Split(' ');




            //For each code
            for (int i = 0; i < codes.Length; i++)
            {
                if (codes[i] == "")
                    continue;

                var switches = codes[i].Split('/');

                if (switches.Length <= 0)
                    continue;

                var code = switches[0];

                //Check whether this is a line start or end
                bool start = false, close = false, cadLine = false;
                int cadIncrement = 0;

                //Search the switches for relevent info
                for (int j = 1; j < switches.Length; j++)
                {
                    var sw = switches[j];

                    if (sw == "ST" || sw == "START")
                        start = true;

                    if (sw == "CL" || sw == "CLOSE")
                        close = true;

                    //If its a number then we know it is a cad line
                    if (int.TryParse(sw, out cadIncrement))
                    {
                        if (pointIndex + cadIncrement <= Points.Count - 1 && pointIndex + cadIncrement >= 0)
                            cadLine = true;
                    }
                }


                Feature currentFeature = null;
                int currentMatchScore = 0;
                int currentFinalScore = 0;

                var firstChar = code[0];

                //Load the default code
                if (!FeaturesDictionary.ContainsKey(firstChar))
                    continue;

                //Find the most valid feature
                var featuresToCheck = FeaturesDictionary[firstChar];
                foreach (var feature in featuresToCheck)
                {
                    int matchScore = 0, finalScore = 0;
                    if (feature.IsCodeValid(code, ref matchScore, ref finalScore))
                    {
                        if (matchScore > currentMatchScore || (matchScore == currentMatchScore && finalScore > currentFinalScore))
                        {
                            currentFeature = feature;
                            currentMatchScore = matchScore;
                            currentFinalScore = finalScore;
                        }
                    }
                }

                //Try to add the feature
                if (currentFeature != null)
                {
                    //There is a best fitting feature so try to add the point to it
                    if (currentFeature is LineFeature)
                    {
                        if (LineFeatures.ContainsKey(currentFeature.FieldCode))
                        {
                            //Get the line feature collection to use
                            var collection = LineFeatures[currentFeature.FieldCode];

                            if (cadLine)
                            {
                                //If there is no entry for the feature add one
                                if (!collection.CADFeatureItems.ContainsKey(code))
                                    collection.CADFeatureItems.Add(code, new List<CADLineFeatureItem>());

                                //Create a new cadfeatureitem
                                var item = new CADLineFeatureItem(code, collection);
                                item.AddStartPoint(point);
                                item.AddEndPoint(Points[pointIndex + cadIncrement]);
                                collection.CADFeatureItems[code].Add(item);
                            }
                            else
                            {
                                //If there is already an entry
                                if (collection.FeatureItems.ContainsKey(code))
                                {
                                    //We must identify the valid feature item to check against
                                    for (int j = 0; j < collection.FeatureItems[code].Count; j++)
                                    {
                                        var item = collection.FeatureItems[code][j];

                                        var startIndex = Points.IndexOf(item.Points.First());
                                        var endIndex = Points.IndexOf(item.Points.Last());

                                        //This works on the basis that the index cannot be the same
                                        if (pointIndex > startIndex && pointIndex < endIndex)
                                        {
                                            //The point is contained within the current item
                                            if (start)
                                            {
                                                //We must create a new item to add at j + 1
                                                var newItem = new LineFeatureItem(code, collection);
                                                newItem.AddPoint(point);
                                                newItem.Closed = item.Closed;
                                                newItem.Started = start;

                                                //Add the points after the new point
                                                for (int k = 0; k < item.Points.Count;)
                                                {
                                                    var p = item.Points[k];
                                                    if (Points.IndexOf(p) > pointIndex)
                                                    {
                                                        item.RemovePoint(p);
                                                        newItem.AddPoint(p);
                                                    }
                                                    else
                                                        k++;
                                                }

                                                //Add the line feature item after the current
                                                if (collection.FeatureItems[code].Count - 1 <= j + 1)
                                                    collection.FeatureItems[code].Add(newItem);
                                                else
                                                    collection.FeatureItems[code].Insert(j + 1, newItem);

                                                break;
                                            }
                                            else if (close)
                                            {
                                                //We must create a new item to add at j + 1
                                                var newItem = new LineFeatureItem(code, collection);
                                                newItem.Closed = close;
                                                newItem.Started = item.Started;

                                                //Add the points before the new point
                                                for (int k = 0; k < item.Points.Count;)
                                                {
                                                    var p = item.Points[k];
                                                    if (Points.IndexOf(p) < pointIndex)
                                                    {
                                                        item.RemovePoint(p);
                                                        newItem.AddPoint(p);
                                                    }
                                                    else
                                                        k++;
                                                }

                                                //Add the final point
                                                newItem.AddPoint(point);

                                                //Add the line feature item at the current position
                                                collection.FeatureItems[code].Insert(j, newItem);
                                                break;
                                            }
                                            else
                                            {
                                                //We just add the point into the current list where appropriate
                                                for (int k = 0; k < item.Points.Count; k++)
                                                {
                                                    var p = item.Points[k];
                                                    var index = Points.IndexOf(p);

                                                    if (pointIndex < index)
                                                    {
                                                        //Insert the point at this index
                                                        item.InsertPoint(k, point);
                                                        break;
                                                    }
                                                }

                                                break;
                                            }
                                        }
                                        else if (pointIndex < startIndex)
                                        {
                                            if (j > 0)
                                            {
                                                //Try to add to the end of the last one
                                                var prevItem = collection.FeatureItems[code][j - 1];
                                                if (!prevItem.Closed)
                                                {
                                                    prevItem.AddPoint(point);
                                                    prevItem.Closed = close;
                                                    break;
                                                }
                                            }

                                            //The point occurs before the current item so we must try to insert it at the start of the item if possible
                                            if (item.Started || close)
                                            {
                                                //We must create a new item and insert at j
                                                var newItem = new LineFeatureItem(code, collection);
                                                newItem.Closed = close;
                                                newItem.Started = start;
                                                newItem.AddPoint(point);

                                                //Add to the collection at j
                                                collection.FeatureItems[code].Insert(j, newItem);

                                                break;
                                            }
                                            else
                                            {
                                                //Insert the point at the start of the item
                                                item.InsertPoint(0, point);
                                                item.Started = start;

                                                break;
                                            }
                                        }
                                        else if (pointIndex > endIndex && j + 1 >= collection.FeatureItems[code].Count)
                                        {
                                            //The point occurs after the last feature item so get the last one
                                            var lastItem = collection.FeatureItems[code].Last();

                                            if (lastItem.Closed || start)
                                            {
                                                //Create and add a new item
                                                var newItem = new LineFeatureItem(code, collection);
                                                newItem.Closed = close;
                                                newItem.Started = start;
                                                newItem.AddPoint(point);

                                                collection.FeatureItems[code].Add(newItem);
                                                break;
                                            }
                                            else
                                            {
                                                //Add the point at the end of the last item
                                                lastItem.AddPoint(point);
                                                lastItem.Closed = close;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //Add the entry and add a new line feature item for it
                                    collection.FeatureItems.Add(code, new List<LineFeatureItem>());

                                    var item = new LineFeatureItem(code, collection);
                                    item.AddPoint(point);
                                    item.Closed = close;
                                    item.Started = start;

                                    collection.FeatureItems[code].Add(item);
                                }
                            }
                        }
                        else
                        {
                            //Create the new line feature collection
                            var lineFeatureCollection = new LineFeatureCollection(currentFeature as LineFeature, this);

                            if (cadLine)
                            {
                                //Create a new cadfeatureitem
                                var item = new CADLineFeatureItem(code, lineFeatureCollection);
                                item.AddStartPoint(point);
                                item.AddEndPoint(Points[pointIndex + cadIncrement]);
                                lineFeatureCollection.CADFeatureItems.Add(code, new List<CADLineFeatureItem> { item });
                            }
                            else
                            {
                                //Create and setup the new LineFeatureItem
                                var item = new LineFeatureItem(code, lineFeatureCollection);
                                item.AddPoint(point);
                                item.Closed = close;
                                item.Started = start;

                                //Add the feature item to the collection and add the collection the LineFeatures
                                lineFeatureCollection.FeatureItems.Add(code, new List<LineFeatureItem>() { item });
                            }

                            LineFeatures.Add(currentFeature.FieldCode, lineFeatureCollection);
                        }
                    }
                    else if (currentFeature is PointFeature)
                    {
                        if (PointFeatures.ContainsKey(currentFeature.FieldCode))
                        {
                            //Get the point feature collection to use
                            var collection = PointFeatures[currentFeature.FieldCode];

                            //Add the point to the collection
                            collection.AddPoint(point);
                        }
                        else
                        {
                            //Create the new point feature collection
                            var collection = new PointFeatureCollection(currentFeature as PointFeature, this);

                            //Add the point to the collection
                            collection.AddPoint(point);

                            //Add the collection to the model
                            PointFeatures.Add(currentFeature.FieldCode, collection);
                        }
                    }
                }
                else
                {
                    PointFeature defaultPointFeature = null;

                    //Get the default feature if there is one
                    if (FeaturesDictionary.ContainsKey('@'))
                    {
                        if (FeaturesDictionary['@'].Count <= 0)
                        {
                            defaultPointFeature = new PointFeature() { Code = "@", FieldCode = "@" };
                            FeaturesDictionary['@'] = new List<Feature> { defaultPointFeature };
                        }
                        else
                        {
                            var defaultFeature = FeaturesDictionary['@'][0];
                            if (defaultFeature is PointFeature)
                                defaultPointFeature = defaultFeature as PointFeature;
                        }
                    }
                    else
                    {
                        //We should add a default feature
                        defaultPointFeature = new PointFeature() { Code = "@", FieldCode = "@" };
                        FeaturesDictionary.Add('@', new List<Feature> { defaultPointFeature });
                    }

                    if (defaultPointFeature == null)
                        return true;

                    if (!PointFeatures.ContainsKey(defaultPointFeature.FieldCode))
                        PointFeatures.Add(defaultPointFeature.FieldCode, new PointFeatureCollection(defaultPointFeature, this));

                    var collection = PointFeatures[defaultPointFeature.FieldCode];
                    collection.AddPoint(point);

                    return true;
                }
            }

            return true;
        }


        /// <summary>
        /// Attempts to remove a <see cref="DTMPoint"/> from the <see cref="Triangle"/> model for the <see cref="DTM"/>
        /// Retriangulates the leftover hole. Seems to work fine so not converting to the new triangulation method - TN 20.05.22
        /// </summary>
        /// <param name="point">The <see cref="DTMPoint"/> to remove</param>
        public void RemovePointFromTriangleModel(DTMPoint point)
        {
            this.PointsDictionaryBuilt = false;

            if (point.Triangles == null || point.Triangles.Count <= 0)
                return;

            //Create a copy of the original triangles for later reference
            var originalTriangles = point.Triangles.ToList();
            var perimeterPoints = new BindingList<DTMPoint>();
            var segments = new List<Segment>();

            foreach (var triangle in point.Triangles)
            {
                //==========================================================================================================================
                //If conditions added by ES:24.05.21 - triangle model is phuqued(sorry for the mispelling, but this is a family show)
                if (triangle == null || triangle.Surface == null)
                {
                    return;
                }
                //==========================================================================================================================   

                //Remove the triangle from its surface
                if (triangle.Surface.Triangles != null && triangle.Surface.Triangles.Contains(triangle))
                {
                    triangle.Surface.Triangles.Remove(triangle);
                    if (triangle.Surface.Triangles.Count <= 0)
                        triangle.Surface.Triangles = null;
                }

                //Remove triangle from Triangle.Point1
                if (triangle.Point1 != point && triangle.Point1.Triangles != null && triangle.Point1.Triangles.Contains(triangle))
                {
                    triangle.Point1.Triangles.Remove(triangle);
                    if (triangle.Point1.Triangles.Count <= 0)
                        triangle.Point1.Triangles = null;
                }

                //Remove triangle from Triangle.Point2
                if (triangle.Point2 != point && triangle.Point2.Triangles != null && triangle.Point2.Triangles.Contains(triangle))
                {
                    triangle.Point2.Triangles.Remove(triangle);
                    if (triangle.Point2.Triangles.Count <= 0)
                        triangle.Point2.Triangles = null;
                }

                //Remove triangle from Triangle.Point3
                if (triangle.Point3 != point && triangle.Point3.Triangles != null && triangle.Point3.Triangles.Contains(triangle))
                {
                    triangle.Point3.Triangles.Remove(triangle);
                    if (triangle.Point3.Triangles.Count <= 0)
                        triangle.Point3.Triangles = null;
                }

                if (triangle.Point1 == point)
                {
                    segments.Add(new Segment(new Vertex(triangle.Point2.X, triangle.Point2.Y), new Vertex(triangle.Point3.X, triangle.Point3.Y)));
                }
                else if (triangle.Point2 == point)
                {
                    segments.Add(new Segment(new Vertex(triangle.Point1.X, triangle.Point1.Y), new Vertex(triangle.Point3.X, triangle.Point3.Y)));
                }
                else if (triangle.Point3 == point)
                {
                    segments.Add(new Segment(new Vertex(triangle.Point1.X, triangle.Point1.Y), new Vertex(triangle.Point2.X, triangle.Point2.Y)));
                }

                //Add the triangle points
                if (triangle.Point1 != point && !perimeterPoints.Contains(triangle.Point1))
                    perimeterPoints.Add(triangle.Point1);

                if (triangle.Point2 != point && !perimeterPoints.Contains(triangle.Point2))
                    perimeterPoints.Add(triangle.Point2);

                if (triangle.Point3 != point && !perimeterPoints.Contains(triangle.Point3))
                    perimeterPoints.Add(triangle.Point3);
            }

            //Now we retriangulate the perimeter and assign those new triangles
            var poly = new TriangleNet.Geometry.Polygon(perimeterPoints.Count);

            


            if (perimeterPoints.Count <= 2)
                return;

            foreach (var p in perimeterPoints)
                poly.Add(new Vertex(p.X, p.Y));

            foreach (var segment in segments)
                poly.Add(segment);

            point.Triangles = null;

            var constraintOptions = new TriangleNet.Meshing.ConstraintOptions()
            {
                ConformingDelaunay = false,
                Convex = true,
                SegmentSplitting = 2
            };

            var qualityOptions = new TriangleNet.Meshing.QualityOptions()
            {
                MaximumAngle = 360,
                MinimumAngle = 0,
            };

            var mesh = poly.Triangulate(constraintOptions, qualityOptions);

            var pointsCount = perimeterPoints.Count;
            var pointsToRemove = new BindingList<DTMPoint>();

            //Add all the new points
            if (mesh.Vertices.Count > perimeterPoints.Count)
            {
                for (int i = perimeterPoints.Count; i < mesh.Vertices.Count; i++)
                {
                    var vertex = mesh.Vertices.ElementAt(i);

                    var newPoint = new DTMPoint()
                    {
                        X = vertex.X,
                        Y = vertex.Y,
                        Z = -999
                    };
                    perimeterPoints.Add(newPoint);
                    pointsToRemove.Add(newPoint);
                }
            }

            foreach (var triangle in mesh.Triangles)
            {
                int index1 = triangle.GetVertexID(0), index2 = triangle.GetVertexID(1), index3 = triangle.GetVertexID(2);

                if (index1 >= perimeterPoints.Count || index2 >= perimeterPoints.Count || index3 >= perimeterPoints.Count)
                {
                    continue;
                }

                //Get the points
                var point1 = perimeterPoints[index1];
                var point2 = perimeterPoints[index2];
                var point3 = perimeterPoints[index3];

                TriangleSurface surface = null;

                var centerX = (point1.X + point2.X + point3.X) / 3;
                var centerY = (point1.Y + point2.Y + point3.Y) / 3;

                //Get the correctSurface or set to 0 if it exists
                foreach (var tri in originalTriangles)
                {
                    //Check the triangle is valid and get the surface
                    if (tri.PointInTri(centerX, centerY))
                    {
                        if (tri.Surface == null)
                            surface = GetSurface(0);
                        else
                            surface = tri.Surface;
                        break;
                    }
                }

                if (surface != null)
                {
                    //Create the triangle object and add it to the model
                    var newTriangle = new Triangle(surface, point1, point2, point3);
                    Triangles.Add(newTriangle);
                }
            }

            //Remove all the old triangles
            for (int i = 0; i < originalTriangles.Count; i++)
            {
                var tri = originalTriangles[i];

                if (Triangles.Contains(tri))
                    Triangles.Remove(tri);

                tri = null;
            }

            //Remove the added points
            foreach (var p in pointsToRemove)
                RemovePointFromTriangleModel(p);
        }

        /// <summary>
        /// Attempts to remove a Triangle from the model and from any points which also has this triangle.
        /// </summary>
        /// <param name="triangle"></param>
        public void RemoveTriangleFromModel(Triangle triangle)
        {
            triangle.Point1.RemoveTriangle(triangle);
            triangle.Point2.RemoveTriangle(triangle);
            triangle.Point3.RemoveTriangle(triangle);

            if (Triangles != null && Triangles.Count > 0)
            {
                Triangles.Remove(triangle);
            }
            foreach(TriangleSurface surf in Surfaces)
            {
                surf.RemoveTriangle(triangle);
            }
        }
       

        /// <summary>
        /// Attempts to insert a <see cref="DTMPoint"/> into the <see cref="Triangle"/> model for the <see cref="DTM"/>
        /// </summary>
        /// <param name="point">The <see cref="DTMPoint"/> to insert</param>
        public void InsertPointIntoTriangleModel(DTMPoint point)
        {
            this.PointsDictionaryBuilt = false;


            //Get the triangle to insert the point into
            var triangle = FindTriangle(point.X, point.Y);

            if (triangle == null)
                return;

            if (point == triangle.Point1 || point == triangle.Point2 || point == triangle.Point3)
                return;

            //Make sure the point is not a duplicate
            if (point.X == triangle.Point1.X && point.Y == triangle.Point1.Y)
                return;

            if (point.X == triangle.Point2.X && point.Y == triangle.Point2.Y)
                return;

            if (point.X == triangle.Point3.X && point.Y == triangle.Point3.Y)
                return;

            DTMPoint point1 = null, point2 = null, point3 = null;

            //Check if the point intersects the triangle
            if (Vector.FnOnLine3(triangle.Point1.X, triangle.Point1.Y, triangle.Point2.X, triangle.Point2.Y, point.X, point.Y) == 0)
            {
                point1 = triangle.Point1;
                point2 = triangle.Point2;
                point3 = triangle.Point3;
            }
            else if (Vector.FnOnLine3(triangle.Point2.X, triangle.Point2.Y, triangle.Point3.X, triangle.Point3.Y, point.X, point.Y) == 0)
            {
                point1 = triangle.Point2;
                point2 = triangle.Point3;
                point3 = triangle.Point1;
            }
            else if (Vector.FnOnLine3(triangle.Point1.X, triangle.Point1.Y, triangle.Point3.X, triangle.Point3.Y, point.X, point.Y) == 0)
            {
                point1 = triangle.Point1;
                point2 = triangle.Point3;
                point3 = triangle.Point2;
            }
            else
            {
                //The point does not fall on any line so we can create 3 new triangles
                var surface = triangle.Surface;

                //Remove the old triangles and surfaces
                surface.RemoveTriangle(triangle);
                triangle.Point1.RemoveTriangle(triangle);
                triangle.Point2.RemoveTriangle(triangle);
                triangle.Point3.RemoveTriangle(triangle);

                if (Triangles.Contains(triangle))
                    Triangles.Remove(triangle);

                //Add the new triangles
                Triangles.Add(new Triangle(surface, triangle.Point1, triangle.Point2, point));
                Triangles.Add(new Triangle(surface, triangle.Point2, triangle.Point3, point));
                Triangles.Add(new Triangle(surface, triangle.Point3, triangle.Point1, point));

                //We are now done inserting the point
                return;
            }

            if (point1 == null || point2 == null || point3 == null)
                return;

            //Find the adjacent triangle
            Triangle adjacent = null;
            foreach (var tri in point1.Triangles)
            {
                //If the triangle is current move on
                if (tri == triangle)
                    continue;

                foreach (var tri2 in point2.Triangles)
                {
                    //If the triangle matches we have found the adjacent
                    if (tri == tri2)
                    {
                        adjacent = tri;
                        break;
                    }
                }

                if (adjacent != null)
                    break;
            }

            //If there is no adjacent triangle we should split the current triangle into 2
            if (adjacent == null)
            {
                var surface = triangle.Surface;

                //Remove the current triangles
                surface.RemoveTriangle(triangle);
                triangle.Point1.RemoveTriangle(triangle);
                triangle.Point2.RemoveTriangle(triangle);
                triangle.Point3.RemoveTriangle(triangle);

                if (Triangles.Contains(triangle))
                    Triangles.Remove(triangle);

                Triangles.Add(new Triangle(surface, point1, point, point3));
                Triangles.Add(new Triangle(surface, point2, point, point3));
            }
            else
            {
                //Find the last point of the adjacent triangle that we do not currently have
                DTMPoint point4 = null;

                if (adjacent.Point1 != point1 || adjacent.Point1 != point2)
                    point4 = adjacent.Point1;
                else if (adjacent.Point2 != point1 || adjacent.Point2 != point2)
                    point4 = adjacent.Point2;
                else if (adjacent.Point3 != point1 || adjacent.Point3 != point3)
                    point4 = adjacent.Point3;

                if (point4 == null)
                    return;

                var surface = triangle.Surface;
                var adjSurface = adjacent.Surface;

                //Remove the current triangle
                surface.RemoveTriangle(triangle);
                triangle.Point1.RemoveTriangle(triangle);
                triangle.Point2.RemoveTriangle(triangle);
                triangle.Point3.RemoveTriangle(triangle);

                if (Triangles.Contains(triangle))
                    Triangles.Remove(triangle);

                //Remove the adjacent triangle
                surface.RemoveTriangle(adjacent);
                adjacent.Point1.RemoveTriangle(adjacent);
                adjacent.Point2.RemoveTriangle(adjacent);
                adjacent.Point3.RemoveTriangle(adjacent);

                if (Triangles.Contains(adjacent))
                    Triangles.Remove(adjacent);

                //Add the four new triangles
                Triangles.Add(new Triangle(surface, point1, point, point3));
                Triangles.Add(new Triangle(surface, point2, point, point3));
                Triangles.Add(new Triangle(adjSurface, point1, point, point4));
                Triangles.Add(new Triangle(adjSurface, point2, point, point4));
            }
        }

        /// <summary>
        /// Old way of removing point
        /// </summary>
        /// <param name="point"></param>
        //public void RemovePoint(DTMPoint point)
        //{
        //    //Remove the point from the features, remove from the triangle model
        //    RemovePointFeature(point);


        //    //if conditiion added by ES:04.08.20
        //    if (point.Triangles != null && point.Triangles.Count != 0)
        //        RemovePointFromTriangleModel(point);

        //    //For now just remove the point from the list
        //    if (Points.Contains(point))
        //        Points.Remove(point);

        //    //If the point lies directly on the bounds we may have to redo the bounds to get a new accurate offset
        //    if (Bounds.Min.X == point.X || Bounds.Min.Y == point.Y || Bounds.Min.Z == point.Z)
        //        UpdateBounds = true;
        //    else if (Bounds.Max.X == point.X || Bounds.Max.Y == point.Y || Bounds.Max.Z == point.Z)
        //        UpdateBounds = true;

        //    //If the point is a station setup or part of one then we must update the point
        //    if ((point.Type == PointType.OccupiedStation || point.Type == PointType.ReferenceStation) && point is StationPoint stationPoint)
        //    {
        //        if (stationPoint.StationSetup != null)
        //        {
        //            if (point == stationPoint.StationSetup.OccupiedStation)
        //            {
        //                //If we are removing the occupied station we remove the station setup completely and update all points that used it
        //                RemoveStationSetup(stationPoint.StationSetup);
        //            }
        //            else if (point == stationPoint.StationSetup.ReferenceStation)
        //            {
        //                stationPoint.StationSetup.SetupReferenceStation(null);
        //                stationPoint.StationSetup.UpdateStationSetup();
        //                UpdatePointsFromStationSetup(stationPoint.StationSetup);
        //            }
        //        }
        //    }
        //    else if (point.Type == PointType.ReferenceObservation && point is ReferenceObservation refObservation)
        //    {
        //        if (refObservation.StationSetup != null && point == refObservation.StationSetup.ReferenceObservation)
        //        {
        //            refObservation.StationSetup.SetupReferenceObservation(null);
        //            refObservation.StationSetup.UpdateStationSetup();
        //            UpdatePointsFromStationSetup(refObservation.StationSetup);
        //        }
        //    }
        //}

        private void RemoveStationSetup(StationSetup setup)
        {
            //We want to replace all coordinates relying on this station with a new station setup (default values)
            if (!StationSetups.Contains(setup))
                return;

            var newSetup = new StationSetup();
            newSetup.UpdateStationSetup();

            //Get the start index of the setup (this will be the index of the reference observation or the azimuth correction
            int startIndex = -1;

            //Find the start index of the setup, if valid
            if (setup.ReferenceObservation != null && Points.Contains(setup.ReferenceObservation))
                startIndex = Points.IndexOf(setup.ReferenceObservation) + 1;
            else if (setup.AzimuthCorrection != null && Points.Contains(setup.AzimuthCorrection))
                startIndex = Points.IndexOf(setup.AzimuthCorrection) + 1;
            else
                return;

            //Check start index is valid
            if (startIndex < 0 || startIndex > Points.Count - 1)
                return;

            if (PointsToUpdate == null)
                PointsToUpdate = new BindingList<DTMPoint>();

            //Iterate through all the points to check and update coordinates where required
            for (int i = startIndex; i < Points.Count; i++)
            {
                var point = Points[i];

                if (point.Type == PointType.Observation)
                {
                    //Update the point based on the station setup
                    if (point is ObservationPoint tmp)
                    {
                        tmp.ReduceCoordinate(newSetup);
                        if (!PointsToUpdate.Contains(tmp))
                            PointsToUpdate.Add(tmp);
                    }
                }
                else if (point.Type == PointType.Coordinate)
                    continue;
                else
                    break;
            }

            if (PointsToUpdate.Count <= 0)
                PointsToUpdate = null;

            StationSetups.Remove(setup);
        }


        /// <summary>
        /// Superseded by "DeletePointsFromDTMModel". 
        /// </summary>
        /// <param name="pointsToRemove"></param>
        //public void RemovePoints(HashSet<DTMPoint> pointsToRemove)
        //{
        //    if (pointsToRemove?.Count <= 0)
        //        return;

        //    var trianglesToRemove = new HashSet<Triangle>();

        //    foreach (var point in pointsToRemove)
        //    {


        //        //        if (point.LineFeatures != null)
        //        //        {
        //        //            if (point.LineFeatures.Count > 0)
        //        //            {
        //        //                foreach (var ln in point.LineFeatures)
        //        //                {
        //        //                    int idx = ln.Points.IndexOf(point);
        //        //                    if (idx < ln.Points.Count - 1)
        //        //                    {
        //        //                        var nextpt = ln.Points.ElementAt(idx + 1);
        //        //nextpt.PointLabel = nextpt.PointLabel + "/st";
        //        //foreach (var ln2 in nextpt.LineFeatures)
        //        //{
        //        //	ln2.Started = true;	
        //        //}
        //        //                    }
        //        //                }
        //        //            }
        //        //        }
        //        //InterpretFeatures();


        //        RemovePointFeature(point);

        //        Points.Remove(point);

        //        //Gather each triangle to remove
        //        if (point.Triangles != null)
        //        {
        //            foreach (var tri in point.Triangles)
        //            {
        //                if (!trianglesToRemove.Contains(tri))
        //                    trianglesToRemove.Add(tri);
        //            }
        //        }

        //        //If we already need to update bounds we don't need to do this check;
        //        if (!UpdateBounds)
        //        {
        //            //If the point lies directly on the bounds we may have to redo the bounds to get a new accurate offset
        //            if (Bounds.Min.X == point.X || Bounds.Min.Y == point.Y || Bounds.Min.Z == point.Z)
        //                UpdateBounds = true;
        //            else if (Bounds.Max.X == point.X || Bounds.Max.Y == point.Y || Bounds.Max.Z == point.Z)
        //                UpdateBounds = true;
        //        }

        //        //If the point is a station setup or part of one then we must update the point
        //        if ((point.Type == PointType.OccupiedStation || point.Type == PointType.ReferenceStation) && point is StationPoint stationPoint)
        //        {
        //            if (stationPoint.StationSetup != null)
        //            {
        //                if (point == stationPoint.StationSetup.OccupiedStation)
        //                {
        //                    //If we are removing the occupied station we remove the station setup completely and update all points that used it
        //                    RemoveStationSetup(stationPoint.StationSetup);
        //                }
        //                else if (point == stationPoint.StationSetup.ReferenceStation)
        //                {
        //                    stationPoint.StationSetup.SetupReferenceStation(null);
        //                    stationPoint.StationSetup.UpdateStationSetup();
        //                    UpdatePointsFromStationSetup(stationPoint.StationSetup);
        //                }
        //            }
        //        }
        //        else if (point.Type == PointType.ReferenceObservation && point is ReferenceObservation refObservation)
        //        {
        //            if (refObservation.StationSetup != null && point == refObservation.StationSetup.ReferenceObservation)
        //            {
        //                refObservation.StationSetup.SetupReferenceObservation(null);
        //                refObservation.StationSetup.UpdateStationSetup();
        //                UpdatePointsFromStationSetup(refObservation.StationSetup);
        //            }
        //        }
        //    }

        //    if (trianglesToRemove.Count > 0)
        //    {
        //        var perimeterPoints = new BindingList<DTMPoint>();
        //        var segments = new List<Segment>();

        //        //Identify the perimeter and remove triangle references from points
        //        foreach (var triangle in trianglesToRemove)
        //        {
        //            if (triangle == null)
        //                continue;

        //            //Remove triangle from surface
        //            if (triangle.Surface != null && triangle.Surface.Triangles != null && triangle.Surface.Triangles.Contains(triangle))
        //            {
        //                triangle.Surface.Triangles.Remove(triangle);
        //                if (triangle.Surface.Triangles.Count <= 0)
        //                    triangle.Surface.Triangles = null;
        //            }

        //            //Remove from point1
        //            triangle.Point1.RemoveTriangle(triangle);
        //            triangle.Point2.RemoveTriangle(triangle);
        //            triangle.Point3.RemoveTriangle(triangle);

        //            //Check for segments
        //            if (pointsToRemove.Contains(triangle.Point1) && !pointsToRemove.Contains(triangle.Point2) && !pointsToRemove.Contains(triangle.Point3))
        //            {
        //                //Segment is points 2-3
        //                segments.Add(new Segment(new Vertex(triangle.Point2.X, triangle.Point2.Y), new Vertex(triangle.Point3.X, triangle.Point3.Y)));
        //            }
        //            else if (pointsToRemove.Contains(triangle.Point2) && !pointsToRemove.Contains(triangle.Point1) && !pointsToRemove.Contains(triangle.Point3))
        //            {
        //                //Segment is points 1-3
        //                segments.Add(new Segment(new Vertex(triangle.Point1.X, triangle.Point1.Y), new Vertex(triangle.Point3.X, triangle.Point3.Y)));
        //            }
        //            else if (pointsToRemove.Contains(triangle.Point3) && !pointsToRemove.Contains(triangle.Point1) && !pointsToRemove.Contains(triangle.Point2))
        //            {
        //                //Segment is points 1-2
        //                segments.Add(new Segment(new Vertex(triangle.Point1.X, triangle.Point1.Y), new Vertex(triangle.Point2.X, triangle.Point2.Y)));
        //            }

        //            //Check for perimter points
        //            if (!pointsToRemove.Contains(triangle.Point1) && !perimeterPoints.Contains(triangle.Point1))
        //                perimeterPoints.Add(triangle.Point1);

        //            if (!pointsToRemove.Contains(triangle.Point2) && !perimeterPoints.Contains(triangle.Point2))
        //                perimeterPoints.Add(triangle.Point2);

        //            if (!pointsToRemove.Contains(triangle.Point3) && !perimeterPoints.Contains(triangle.Point3))
        //                perimeterPoints.Add(triangle.Point3);
        //        }

        //        //Retriangulate the perimeter
        //        var poly = new TriangleNet.Geometry.Polygon(perimeterPoints.Count);

        //        //There will be no new triangles
        //        if (perimeterPoints.Count <= 2)
        //            return;

        //        foreach (var p in perimeterPoints)
        //            poly.Add(new Vertex(p.X, p.Y));

        //        foreach (var seg in segments)
        //            poly.Add(seg);

        //        var constraintOptions = new TriangleNet.Meshing.ConstraintOptions()
        //        {
        //            ConformingDelaunay = true,
        //            Convex = true,
        //            SegmentSplitting = 2
        //        };

        //        var qualityOptions = new TriangleNet.Meshing.QualityOptions()
        //        {
        //            MaximumAngle = 360,
        //            MinimumAngle = 0
        //        };

        //        var mesh = poly.Triangulate(constraintOptions, qualityOptions);

        //        var pointsToDelete = new BindingList<DTMPoint>();

        //        //Add all the new points
        //        if (mesh.Vertices.Count > perimeterPoints.Count)
        //        {
        //            for (int i = perimeterPoints.Count; i < mesh.Vertices.Count; i++)
        //            {
        //                var vertex = mesh.Vertices.ElementAt(i);
        //                var newPoint = new DTMPoint()
        //                {
        //                    X = vertex.X,
        //                    Y = vertex.Y,
        //                    Z = -999,
        //                    Type = PointType.Steiner
        //                };
        //                perimeterPoints.Add(newPoint);
        //                pointsToDelete.Add(newPoint);
        //            }
        //        }

        //        //Create new triangles
        //        foreach (var triangle in mesh.Triangles)
        //        {
        //            int index1 = triangle.GetVertexID(0), index2 = triangle.GetVertexID(1), index3 = triangle.GetVertexID(2);
        //            if (index1 >= perimeterPoints.Count || index2 >= perimeterPoints.Count || index3 >= perimeterPoints.Count)
        //                continue;

        //            //Get the points
        //            var point1 = perimeterPoints[index1];
        //            var point2 = perimeterPoints[index2];
        //            var point3 = perimeterPoints[index3];

        //            var centerX = (point1.X + point2.X + point3.X) / 3;
        //            var centerY = (point1.Y + point2.Y + point3.Y) / 3;

        //            TriangleSurface surface = null;

        //            //Get the correct surface or set to 0
        //            foreach (var tri in trianglesToRemove)
        //            {
        //                //check the triangle is valid and get the surface
        //                if (tri.PointInTri(centerX, centerY))
        //                {
        //                    if (tri.Surface == null)
        //                        surface = GetSurface(0);
        //                    else
        //                        surface = tri.Surface;

        //                    break;
        //                }
        //            }

        //            //Create the new triangle
        //            if (surface != null)
        //            {
        //                //Create the triangle object and add it to the model
        //                var newTriangle = new Triangle(surface, point1, point2, point3);
        //                Triangles.Add(newTriangle);
        //            }
        //        }

        //        //Remove all the odl triangles
        //        foreach (var triangle in trianglesToRemove)
        //        {
        //            Triangles.Remove(triangle);
        //        }

        //        trianglesToRemove.Clear();

        //        perimeterPoints.Clear();
        //        pointsToRemove.Clear();

        //        //Remove the added points
        //        foreach (var p in pointsToDelete)
        //            RemovePointFromTriangleModel(p);
        //    }
        //}

        /// <summary>
        /// The fastest way to remove points from the model. Also removes associated triangles from the model and triangles from the correct surfaces, as well as points from standard and polylines.
        /// Make sure to call Interpret Features after this. When points are removed the features need to be remade. *Does nothing to the leftover holes in the model. Use DeletePointsFromModel for that...
        /// </summary>
        /// <param name="pointsToRemove"></param>
        public void DeletePointsFromEntites(HashSet<DTMPoint> pointsToRemove)
        {

            if (pointsToRemove == null) { return; }

            HashSet<NRG.Models.Triangle> trianglesToRemoveFromModel = new HashSet<NRG.Models.Triangle>();

            foreach (var pt in pointsToRemove)
            {
                if (pt.Triangles == null) { continue; }

                trianglesToRemoveFromModel.UnionWith(pt.Triangles); //adds duplicates, don't think this is an issue though
            }

            //Tidy the many-many point-triangle relationship
            foreach (var tri in trianglesToRemoveFromModel)
            {
                tri.Point1.Triangles.Remove(tri);
                tri.Point2.Triangles.Remove(tri);
                tri.Point3.Triangles.Remove(tri);
            }

            //Remove all the tagged triangles from every possible surface in the model. This could be done faster if needed (only edit surfaces actually used by the triangles)
            foreach (var surface in Surfaces)
            {
                surface.RemoveTriangles(trianglesToRemoveFromModel);
            }


            //Remove the triangles from the Model
            var newTriList = Triangles.Except(trianglesToRemoveFromModel.ToList());
            Triangles = new HashSet<Triangle>(newTriList);

            //Remove the point from PolyLines
            RemovePointFromAllPolyLines(pointsToRemove);

            //Remove the point from StandardLines
            RemovePointFromAllStandardLines(pointsToRemove);

            //Remove the point from Arcs
            RemovePointFromAllArcs(pointsToRemove);

            //Remove the points from the model
            var newPointList = Points.Except(pointsToRemove.ToList());
            Points = new BindingList<DTMPoint>(newPointList.ToList());


        }

        public List<object> FindEntitiesContainingPoint(DTMPoint point)
        {
            List<object> entityList = new List<object>();

            #region StandardLines
            foreach (StandardLine sl in GetStandardLines())
            {
                if (sl.StartPoint == point || sl.EndPoint == point) { entityList.Add(sl);}
            }
            #endregion

            #region PolyLines
            foreach(PolyLine pl in GetPolyLines())
            {
                if(pl.Nodes != null && pl.Nodes.Contains(point)) { entityList.Add(pl);}
            }
            #endregion

            #region Arcs
            foreach(Arc arc in GetArcs())
            {
                if (arc.StartPoint == point || arc.EndPoint == point) { entityList.Add(arc); }
            }
            #endregion

            #region LineFeatureItems
            foreach (LineFeatureItem lfi in GetAllLineFeatureItems())
            {
                if (lfi.Points != null && lfi.Points.Contains(point)) { entityList.Add(lfi); }
            }
            #endregion




            return entityList;
        }

        #endregion

        #region Features


        private string FindChars(string inputString, string LookFor, int NumCharsAfter)
        {

            int pos = inputString.ToUpper().IndexOf(LookFor.ToUpper());
            if (pos > 0)
            {
                return inputString.Substring(pos + LookFor.Length, NumCharsAfter);
            }

            return "";
        }

        public System.Drawing.Color InterpretColourString(string input)
        {

            input.Replace(" ", ""); //Remove spaces

            int a = 255;
            int r = 0;
            int g = 0;
            int b = 0;

            string txt = "";

            txt = FindChars(input, "A=", 3);
            int.TryParse(txt, out a);

            txt = FindChars(input, "R=", 3);
            int.TryParse(txt, out r);

            txt = FindChars(input, "G=", 3);
            int.TryParse(txt, out g);

            txt = FindChars(input, "B=", 3);
            int.TryParse(txt, out b);


            System.Drawing.Color col = System.Drawing.Color.FromArgb(a, r, g, b);

            return col;

            return System.Drawing.Color.Black;


        }

        /// <summary>
        /// Attempts to interpret the Features of the current model
        /// </summary>
        /// <param name="featuresDict"></param>
        public void InterpretFeatures()
        {
            
            if (FeaturesDictionary == null)
                return;

            //Remove all old features first
            PointFeatures = new Dictionary<string, PointFeatureCollection>();
            LineFeatures = new Dictionary<string, LineFeatureCollection>();

            foreach(var point in Points)
			{
                point.LineFeatures = null;
                point.PointFeatures = null;
			}


            PointFeature defaultPointFeature = null;

            //Get the default feature if there is one
            if (FeaturesDictionary.ContainsKey('@'))
            {
                if (FeaturesDictionary['@'].Count <= 0)
                {
                    defaultPointFeature = new PointFeature() { Code = "@", FieldCode = "@" };
                    FeaturesDictionary['@'] = new List<Feature> { defaultPointFeature };
                }
                else
                {
                    var defaultFeature = FeaturesDictionary['@'][0];
                    if (defaultFeature is PointFeature)
                        defaultPointFeature = defaultFeature as PointFeature;
                }
            }
            else
            {
                //We should add a default feature
                defaultPointFeature = new PointFeature() { Code = "@", FieldCode = "@" };
                FeaturesDictionary.Add('@', new List<Feature> { defaultPointFeature });
            }



            for (int i = 0; i < Points.Count; i++)
            {
                var point = Points[i];

                //If its a false point ignore it
                if (point.Type == PointType.FalsePoint)
                    continue;

                //If its a point with a yet to be designated feature code (a polyline point), ignore it
                if (!string.IsNullOrWhiteSpace(point.PointLabel) && point.PointLabel.StartsWith("(") && point.PointLabel.EndsWith(")"))
                    continue;

                //If its not a coordinate or observation point or has no label we should add it to the default points
                if ((point.Type != PointType.Coordinate && point.Type != PointType.Observation) || point.PointLabel == "" || point.PointLabel == null && defaultPointFeature != null)
                {
                    if (!PointFeatures.ContainsKey(defaultPointFeature.FieldCode))
                        PointFeatures.Add(defaultPointFeature.FieldCode, new PointFeatureCollection(defaultPointFeature, this));

                    PointFeatures[defaultPointFeature.FieldCode].AddPoint(point);

                    //We don't need to move the rest
                    continue;
                }

                var codes = point.PointLabel.ToUpper().Split(' ');




                //For each code
                for (int j = 0; j < codes.Length; j++)
                
                {



                    System.Drawing.Color overridecol;
                    overridecol = System.Drawing.Color.Transparent;
                    if (codes[j].ToUpper().Contains("{COLOR"))
                    {
                        overridecol = InterpretColourString(codes[j]);
                    }

                    bool isDefault = false;

                    if (codes[j] == "")
                        isDefault = true;

                    var switches = codes[j].Split('/');

                    if (switches.Length <= 0)
                        isDefault = true;

                    var code = switches[0];
                    if (code == "")
                        isDefault = true;

                    if (isDefault)
                    {
                        if (PointFeatures.TryGetValue(defaultPointFeature.FieldCode, out PointFeatureCollection collection))
                        {
                            collection.AddPoint(point);
                        }
                        else
                        {
                            collection = new PointFeatureCollection(defaultPointFeature, this);
                            collection.AddPoint(point);
                            PointFeatures.Add(defaultPointFeature.FieldCode, collection);
                        }

                        continue;
                    }

                    //Check whether this is a line start or end
                    bool start = false, close = false, cadLine = false;
                    int cadIncrement = 0;

                    //Search the switches for relevent info
                    for(int k = 1; k < switches.Length; k++)
                    {
                        var sw = switches[k];

                        if (sw == "ST" || sw == "START")
                            start = true;

                        if (sw == "CL" || sw == "CLOSE")
                            close = true;

                        //If its a number then we know it is a cad line
                        if (int.TryParse(sw, out cadIncrement))
                        {
                            if (i + cadIncrement <= Points.Count - 1 && i + cadIncrement >= 0)
                                cadLine = true;
                        }
                    }

                    Feature currentFeature = null;
                    int currentMatchScore = 0;
                    int currentFinalScore = 0;

                    var firstChar = code[0];

                    //Load the default code
                    if (!FeaturesDictionary.ContainsKey(firstChar))
                        continue;

                    //Find the most valid feature
                    var featuresToCheck = FeaturesDictionary[firstChar];
                    foreach (var feature in featuresToCheck)
                    {
                        int matchScore = 0, finalScore = 0;
                        if (feature.IsCodeValid(code, ref matchScore, ref finalScore))
                        {
                            if (matchScore > currentMatchScore || (matchScore == currentMatchScore && finalScore > currentFinalScore))
                            {
                                currentFeature = feature;
                                currentMatchScore = matchScore;
                                currentFinalScore = finalScore;
                            }
                        }
                    }

                    //Try to add the feature
                    if (currentFeature != null)
                    {

                        //There is a best fitting feature so try to add the point to it
                        if (currentFeature is LineFeature)
                        {

                            if (LineFeatures.ContainsKey(currentFeature.FieldCode))
                            {
                                //Get the line feature collection to use
                                var collection = LineFeatures[currentFeature.FieldCode];

                                if (cadLine)
                                {
                                    //If there is no entry for the feature add one
                                    if (!collection.CADFeatureItems.ContainsKey(code))
                                        collection.CADFeatureItems.Add(code, new List<CADLineFeatureItem>());

                                    //Create a new cadfeatureitem
                                    var item = new CADLineFeatureItem(code, collection);
                                    item.AddStartPoint(point);
                                    item.AddEndPoint(Points[i + cadIncrement]);
                                    item.OverrideColour = overridecol;

                                    collection.CADFeatureItems[code].Add(item);
                                }
                                else
                                {
                                    //If there is no entry for the feature add one
                                    if (!collection.FeatureItems.ContainsKey(code))
                                        collection.FeatureItems.Add(code, new List<LineFeatureItem>());

                                    //If there is no last point or the new point is start or the last point is close then generate a new item
                                    if (start || collection.FeatureItems[code].Count <= 0 || collection.FeatureItems[code].Last().Closed)
                                    {
                                        var item = new LineFeatureItem(code, collection);
                                        item.AddPoint(point);
                                        item.Closed = close;
                                        item.Started = start;
                                        item.OverrideColour = overridecol;
                                        collection.FeatureItems[code].Add(item);
                                    }
                                    else
                                    {
                                        //Add the point to the existing item
                                        var item = collection.FeatureItems[code].Last();
                                        if (overridecol.A > 0) { item.OverrideColour = overridecol; }
                                        item.AddPoint(point);
                                        item.Closed = close;

                                    }
                                }
                            }
                            else
                            {
                                //Create the new line feature collection
                                var lineFeatureCollection = new LineFeatureCollection(currentFeature as LineFeature, this);

                                if (cadLine)
                                {
                                    //Create a new cadfeatureitem
                                    var item = new CADLineFeatureItem(code, lineFeatureCollection);
                                    item.AddStartPoint(point);
                                    item.AddEndPoint(Points[i + cadIncrement]);
                                    item.OverrideColour = overridecol;
                                    lineFeatureCollection.CADFeatureItems.Add(code, new List<CADLineFeatureItem> { item });

                                }
                                else
                                {
                                    //Create and setup the new LineFeatureItem
                                    var item = new LineFeatureItem(code, lineFeatureCollection);
                                    item.AddPoint(point);
                                    item.Closed = close;
                                    item.Started = start;
                                    item.OverrideColour = overridecol;

                                    //Add the feature item to the collection and add the collection the LineFeatures
                                    lineFeatureCollection.FeatureItems.Add(code, new List<LineFeatureItem>() { item });

                                }


                                //currentFeature.R = overridecol.R;
                                //currentFeature.G = overridecol.G;
                                //currentFeature.B = overridecol.B;
                                LineFeatures.Add(currentFeature.FieldCode, lineFeatureCollection);

                            }
                        }
                        else if (currentFeature is PointFeature)
                        {
                            if (PointFeatures.ContainsKey(currentFeature.FieldCode))
                            {
                                //Get the point feature collection to use
                                var collection = PointFeatures[currentFeature.FieldCode];

                                //Add the point to the collection
                                collection.AddPoint(point);
                            }
                            else
                            {
                                //Create the new point feature collection
                                var collection = new PointFeatureCollection(currentFeature as PointFeature, this);

                                //Add the point to the collection
                                collection.AddPoint(point);

                                //Add the collection to the model
                                PointFeatures.Add(currentFeature.FieldCode, collection);
                            }
                        }
                    }
                    else
                    {
                        //Add to the default
                        if (PointFeatures.TryGetValue(defaultPointFeature.FieldCode, out PointFeatureCollection collection))
                        {
                            collection.AddPoint(point);
                        }
                        else
                        {
                            collection = new PointFeatureCollection(defaultPointFeature, this);
                            collection.AddPoint(point);
                            PointFeatures.Add(defaultPointFeature.FieldCode, collection);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Loop through all points and store data on which points have a /+### or /-### pointer. Used in tandem with InterpretPointIncrementDictionary which is called after
        /// the points list has been modified.
        /// </summary>
        /// <returns>A nested dictionary: InitialPoint: FieldCode Pointer > PointedAtPoint </returns>
        /// Use "CreatePointIncrementDictionary" before the model has been adjusted (e.g Remove points, insert new points etc)
        /// Adjust the model
        /// Use "InterpretPointIncrementDictionary" to fix the field code pointers in the model (e.g W/+10 now needs to be W/+4...)
        public Dictionary<DTMPoint, Dictionary<string, DTMPoint>> CreatePointIncrementDictionary()
        {
            if (this.FeaturesDictionary == null)
                return null;

            DTMHelpers.RenumberPointIDs(Points.ToList());

            var AllPointsIncrementsDictionary = new Dictionary<DTMPoint, Dictionary<string, DTMPoint>>();
            
            //Loop through all points
            foreach (DTMPoint point in this.Points)
            {
                if (point.PointLabel == null)
                    continue;

                //Find a point which contains a pointer in its field code
                if (!string.IsNullOrWhiteSpace(point.PointLabel) && point.PointLabel.Contains("/"))
                {
                    var SinglePointIncrementsDictionary = new Dictionary<string, DTMPoint>(); //this goes inside "AllPointsIncrementsDictionary". string = fieldcode, dtmpoint = the 'pointed at' point
                    int indexOfPoint = point.ID;
                    var codes = point.PointLabel.ToUpper().Split(' ');

                    //Loop through each potential field code
                    for (int j = 0; j < codes.Length; j++)
                    {
                        if (codes[j] == "")
                            continue;


                        var sw = codes[j].Split('/').Last();

                        if (int.TryParse(sw, out int pointerIncrement))
                        {
                            //Check if pointer increment is within bounds of points list (incase this pointer is of no use to start with)
                            if (indexOfPoint + pointerIncrement <= this.Points.Count - 1 && indexOfPoint + pointerIncrement >= 0)
                            {
                                //Find the 'pointed at' point
                                DTMPoint foundPoint = this.Points[indexOfPoint + pointerIncrement];

                                //Store the original field code and the DTMPoint object for the found point

                                if (!SinglePointIncrementsDictionary.ContainsKey(codes[j])) //If this is every true it means there are two pointers of the same feature code in the Point Label pointing to the same row. One will be adjusted, the other not so much, oh well. TN 07.02.2022
                                    SinglePointIncrementsDictionary.Add(codes[j], foundPoint);

                            }
                        }
                        
                    }

                    AllPointsIncrementsDictionary.Add(point, SinglePointIncrementsDictionary);
                }

            }

            return AllPointsIncrementsDictionary;
        }


        /// <summary>
        /// Interpret the dictionary which contains data for all the pointers in the field code of the model's points, and then adjust the field codes respectively of any changes to made to the Points list.
        /// </summary>
        /// <param name="dict"></param>
        /// Use "CreatePointIncrementDictionary" before the model has been adjusted (e.g Remove points, insert new points etc)
        /// Adjust the model
        /// Use "InterpretPointIncrementDictionary" to fix the field code pointers in the model (e.g W/+10 now needs to be W/+4...)
        public void InterpretPointIncrementDictionary(Dictionary<DTMPoint, Dictionary<string, DTMPoint>> dict)
        {
            if (dict == null)
                return;

            //Regen point IDs. faster to do this method rather than getting IndexOf each point.
            DTMHelpers.RenumberPointIDs(Points.ToList());

            HashSet<DTMPoint> PointHashSetCopy = new HashSet<DTMPoint>(Points);

            foreach (DTMPoint point in dict.Keys)
            {
                //The point may have been removed from the model so quickly check it sitll exists
                if (point == null || !PointHashSetCopy.Contains(point))
                    continue;

                int indexOfInitialPoint = point.ID; 
                //Cycle through each code & 'pointed at' point that the /+ or /- values represented
                foreach (var codePairing in dict[point])
                {
                    //The 'pointed at' point of the +/ or -/
                    DTMPoint foundPoint = codePairing.Value;
                    //The individual code string (e.g W/+10)
                    string fieldCode = codePairing.Key;
                    string fieldCodeIncrement = fieldCode.Split('/').Last();
                    //This point may also have been removed from the model so quickly check
                    if (point == null || !PointHashSetCopy.Contains(foundPoint))
                    {
                        //If the point doesn't exist anymore, we want to remove the initial point's coding so it doesn't draw random lines to another point
                        var listOfCodes = point.PointLabel.Split().ToList();
                        listOfCodes.Remove(fieldCode);
                        string concatCode = "";

                        //Rebuild the Point Label with correct space delimiters
                        foreach (string code in listOfCodes)
                        {
                            concatCode += " " + code;
                        }
                        point.PointLabel = concatCode.Trim();
                        continue;
                    }

                    //Get the new index of the foundPoint in the Points list
                    int indexOfFoundPoint = foundPoint.ID;

                    if (int.TryParse(fieldCodeIncrement, out int increment))
                    {
                        //Calculate the new pointer value (e.g W/10 could now be W/4 if points were deleted) for the 'pointed at' point
                        int newIncrement = indexOfFoundPoint - indexOfInitialPoint;

                        //Create the new field code
                        string newFieldCode = fieldCode.Replace(increment.ToString(), newIncrement.ToString());

                        //Create the new PointLabel and replace the old field code with the new correct one (Only do a Replace as there could be more field codes within this PointLabel)
                        point.PointLabel = point.PointLabel.Replace(fieldCode, newFieldCode);
                    }
                    else
                    {
                        continue;
                    }

                }
            }
        }
        #region commentedout colour code
        //     private System.Drawing.Color InterpretColour(string code)
        //     {
        //         string colString = GetStringInsideBraces(code);
        //         if (colString != "")
        //         {
        //             if (int.TryParse(colString, out int colint))
        //             {
        //                 return System.Drawing.Color.FromArgb(colint);
        //             }
        //         }
        //         return System.Drawing.Color.FromArgb(-999);
        //     }

        //     private string GetStringInsideBraces(string input)
        //     {
        //         int pos1 = input.IndexOf("{");
        //         int pos2 = input.IndexOf('}');
        //         if (pos1 < pos2)
        //{
        //             return input.Substring(pos1 + 1, pos2 - pos1);
        //}
        //         return "";
        //     }
        #endregion


        /// <summary>
        /// Attempts to make each string with the given code unique
        /// </summary>
        /// <param name="collection">The <see cref="LineFeatureCollection"/> to perform this operation on</param>
        /// <param name="code">The <see cref="string"/> code to make unique</param>
        public void MakeStringUnique(LineFeatureCollection collection, string code)
        {
            if (collection == null || code == null)
                return;

            //Its possible that his process will mess up the string list so we need to keep an eye on it.
            //For e.g. with freature codes C*, C1*, C2* making C strings unique will result in C1, C2 etc which are actually different features entirely.

            //Now get the string points to change
            var pointsToChange = new HashSet<DTMPoint>();
            var upperCode = code.ToUpper();
            int count = 1;

            //Get the line feature items
            if (collection.FeatureItems != null)
            {
                if (collection.FeatureItems.ContainsKey(code))
                {
                    var items = collection.FeatureItems[code];
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];

                        if (item.Points == null)
                            continue;

                        //Get a unique name
                        var newCode = upperCode + "(" + count + ")";
                        while ((collection.FeatureItems?.ContainsKey(newCode) ?? false) || (collection.CADFeatureItems?.ContainsKey(newCode) ?? false))
                        {
                            count++;
                            newCode = upperCode + "(" + count + ")";
                        }
                        item.Code = newCode;

                        //Rename the points
                        foreach (var point in item.Points)
                        {
                            if (point.PointLabel == null || point.PointLabel == "")
                                continue;

                            var newCodeString = "";
                            var codes = point.PointLabel.ToUpper().Split(' ');

                            for (int j = 0; j < codes.Length; j++)
                            {
                                var codeString = codes[j];
                                if (j == 0)
                                    newCodeString += codeString.Replace(upperCode, newCode);
                                else
                                    newCodeString += " " + codeString.Replace(upperCode, newCode);
                            }

                            point.PointLabel = newCodeString;
                        }

                        collection.FeatureItems.Add(newCode, new List<LineFeatureItem> { item });
                    }

                    collection.FeatureItems.Remove(code);
                }
            }

            //Get the cad feature items
            if (collection.CADFeatureItems != null)
            {
                if (collection.CADFeatureItems.ContainsKey(code))
                {
                    var items = collection.CADFeatureItems[code];
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];

                        if (item.StartLine == null || item.EndLine == null)
                            continue;

                        //Get a unique name
                        var newCode = upperCode + "(" + count + ")";
                        while ((collection.FeatureItems?.ContainsKey(newCode) ?? false) || (collection.CADFeatureItems?.ContainsKey(newCode) ?? false))
                        {
                            count++;
                            newCode = upperCode + "(" + count + ")";
                        }
                        item.Code = newCode;

                        if (item.StartLine.PointLabel == null || item.StartLine.PointLabel == "" || item.EndLine.PointLabel == null || item.EndLine.PointLabel == "")
                            continue;

                        //Rename the startPoint
                        var newCodeString = "";
                        var codes = item.StartLine.PointLabel.ToUpper().Split(' ');

                        for (int j = 0; j < codes.Length; j++)
                        {
                            var codeString = codes[j];
                            if (j == 0)
                                newCodeString += codeString.Replace(upperCode, newCode);
                            else
                                newCodeString += " " + codeString.Replace(upperCode, newCode);
                        }

                        item.StartLine.PointLabel = newCodeString;

                        //Rename the endPoint
                        newCodeString = "";
                        codes = item.EndLine.PointLabel.ToUpper().Split(' ');

                        for (int j = 0; j < codes.Length; j++)
                        {
                            var codeString = codes[j];
                            if (j == 0)
                                newCodeString += codeString.Replace(upperCode, newCode);
                            else
                                newCodeString += " " + codeString.Replace(upperCode, newCode);
                        }

                        item.EndLine.PointLabel = newCodeString;

                        //Set the new code
                        collection.CADFeatureItems.Add(newCode, new List<CADLineFeatureItem> { item });
                    }

                    collection.CADFeatureItems.Remove(code);
                }
            }
        }

        #endregion

        #region Surfaces

        /// <summary>
        /// Attempts to get the <see cref="TriangleSurface"/> from <see cref="DTM.Surfaces"/> with the correspoinding index
        /// </summary>
        /// <param name="index">An <see cref="int"/> representing the index of the point</param>
        /// <returns>Returns the <see cref="TriangleSurface"/> found at the given index</returns>
        public TriangleSurface GetSurface(int index)
        {
            while (Surfaces.Count <= index)
            {
                if (Surfaces.Count <= 0)
                {
                    var surface = new TriangleSurface();
                    surface.Contour = false;
                    surface.Volume = false;
                    surface.R = 255;
                    surface.G = 255;
                    surface.ID = 0;
                    Surfaces.Add(surface);
                }
                else
                    Surfaces.Add(new TriangleSurface() { ID = Surfaces.Count });
            }

            return Surfaces[index];
        }

        public void CreateNewSurface(Color surfaceColour, string surfaceName = null, bool Contour = true, bool Volume = true, bool Locked = false)
        {
            if (this.Surfaces == null)
            {
                this.Surfaces = new List<TriangleSurface>();
            }
            TriangleSurface surf = new TriangleSurface();
            surf.ID = Surfaces.Count;

            if (surfaceName != null)
                surf.Name = surfaceName;
            else
                surf.Name = "Unnamed";

            if (Surfaces.Count == 0)
            {
                surf.Contour = false;
                surf.Volume = false;
                surf.Locked = true;

                surf.R = 255; surf.G = 255; surf.B = 255;
            }
            else
            {
                surf.Contour = true;
                surf.Volume = true;
                surf.Locked = false;

                if (surf.R == 128 & surf.G == 128 & surf.B == 128)
                {
                    surf.R = surfaceColour.R;
                    surf.G = surfaceColour.G;
                    surf.B = surfaceColour.B;
                }


            }
            Surfaces.Add(surf);
        }

        public void RegenerateSurfaceIDs(List<TriangleSurface> surfaces = null)
        {
            if (surfaces == null)
                surfaces = this.Surfaces;

            if (surfaces == null)
                surfaces = new List<TriangleSurface>();

            //surfaces = surfaces.OrderBy(x=>x.ID).ToList();


            for(int i = 0; i < Surfaces.Count; i++)
            {
                surfaces[i].ID = i;
            }
        }


        #endregion

        #region Triangle calcs

        /// <summary>
        /// CalcLevelFromTriangles - calculates the triangle model level for the point defined by X, Y
        /// </summary>
        /// <param name="X">X coords of the point we're interested in</param>
        /// <param name="Y">Y coords of the point we're interested in</param>
        /// <returns>double representing the level. If no level was found then returns -999(null level)</returns>
        public double CalcLevelFromTriangles(double X, double Y, Triangle startTri = null)
        {

            Triangle t = new Triangle();

            t = FindTriangle(X, Y, LastTriSearched); //get the triangle X & Y live in
            LastTriSearched = t; //optimisation added by ES:26.08.20
            if (t == null || t.Surface.Contour == false)
                return -999; //none found, return null level

            return t.CalcLevelInTri(X, Y);
        }

        //public Triangle FindTriangleFromClosestPoint(double x, double y)
        //{
        //    double distance = double.MaxValue;
        //    DTMPoint closestPoint = null;
        //    double xDiff = 0, yDiff = 0, dist = 0;

        //    //Find the point closest to x and y by judging the squared distance to it
        //    foreach(var point in Points)
        //    {
        //        xDiff = point.X - x;
        //        yDiff = point.Y - y;

        //        dist = (xDiff * xDiff) + (yDiff * yDiff);
        //        if(dist < distance)
        //        {
        //            //If the closest point has no triangles we cannot find a triangle
        //            if (point.Triangles == null || point.Triangles.Count <= 0)
        //                continue;

        //            distance = dist;
        //            closestPoint = point;
        //        }
        //    }

        //    //Otherwise look for a triangle that the point can be found within
        //    foreach (var tri in closestPoint.Triangles)
        //    {
        //        if (tri.PointInTri(x, y))
        //        {
        //            return tri;
        //        }
        //    }

        //    return null;
        //}


        public Triangle FindTriangleSerially(double X, double Y, Triangle startTriangle = null)
        {
            if (startTriangle != null)
            {
                if (startTriangle.PointInTri(X, Y))
                    return startTriangle;
            }


            foreach (var t in Triangles)
            {
                if (t.PointInTri(X, Y))
                    return t;

            }
            return null;
        }

        #region testing

        public Triangle FindTriangle2(double X, double Y)
        {
            //bool finished = false;
            //JunctionTriangle startTri = FindStartTri(X, Y);
            //if (startTri == null) return null; //no chance and probably can't happen

            //List<JunctionTriangle> junctions = new List<JunctionTriangle>(); //recursive stack

            //junctions.Add(startTri);

            //Triangle thisTri = startTri.GetNextSearchTri(X, Y);
            //int count = 0;
            //do 
            //{
            //    count++;
            //    if (thisTri.PointInTri(X, Y)) return thisTri;//we live in hope!

            //    if (TriangleJunction(thisTri,X,Y, out int side1, out int side2))
            //    {
            //        JunctionTriangle jt = new  JunctionTriangle(thisTri, side1, side2);
            //        if (junctions.Contains(jt) == false)
            //            junctions.Add(jt);
            //    }

            //}
            //while (finished==false);
            Triangle startTri = FindStartTri(X, Y).Triangle;
            if (startTri != null)
            {
                Triangle t = TraverseMesh(startTri, X, Y);
                return t;
            }
            else
                return null;

        }

        private Triangle TraverseMesh(Triangle ThisTriangle, double X, double Y)
        {
            if (ThisTriangle.PointInTri(X, Y))
                return ThisTriangle;
            else
            {
                Triangle tri = CheckAdjacentTriangles(ThisTriangle, X, Y);
                if (tri != null) return tri;
            }

            if (TriangleJunction(ThisTriangle, X, Y, out int side1, out int side2))
            {
                Triangle tri = ThisTriangle.FindAdjacentTriangle(side1);
                if (tri != null)
                {
                    Triangle t = TraverseMesh(tri, X, Y);
                    if (t == null)
                    {
                        tri = ThisTriangle.FindAdjacentTriangle(side2);
                        if (tri != null)
                            t = TraverseMesh(tri, X, Y);
                        else
                            return null;

                        return t;
                    }
                    else
                    {
                        return t;
                    }
                }
            }
            else
            {
                Triangle nexttri = ThisTriangle.GetNextSearchTri(X, Y);
                if (nexttri != null)
                {
                    Triangle t = TraverseMesh(nexttri, X, Y);
                    return t;
                }

            }
            return null;
        }

        private Triangle CheckAdjacentTriangles(Triangle Tri, double X, double Y)
        {
            for (int i = 1; i < 4; i++)
            {
                Triangle tri = Tri.FindAdjacentTriangle(i);
                if (tri != null && Tri.PointInTri(X, Y) == true)
                {
                    return Tri;
                }
            }
            return null;
        }

        /// <summary>
        /// FindStartTri - find a triangle which has 2 potential paths to the triangle containing the point described by the X, Y inputs
        ///                this means that the point (x,y) is to the right of 2 sides. We can then derecurse in some sort of sudo recuresive,
        ///                massivley over complicated way in a method that I'm about to write LOLZ
        ///                
        ///                My potentially massively flawed therory is that one path has to lead to the pot of gold 
        /// </summary>
        /// <param name="X">X coord of the point we're looking for</param>
        /// <param name="Y">Y coord of the point we're looking for</param>
        /// <returns>A triangle with 2 sides that have the seek coord to the right of them</returns>
        private JunctionTriangle FindStartTri(double X, double Y)
        {

            foreach (Triangle t in Triangles)
            {
                if (TriangleJunction(t, X, Y, out int Side1, out int Side2))
                {
                    JunctionTriangle jt = new JunctionTriangle(t, Side1, Side2);
                    return jt;
                }
            }
            return null; //not sure this can happen with a well ordered mesh
        }

        /// <summary>
        /// PrimeTraingle - A triangle which has 2 sides from which the seek point (X,Y) is to the right of
        /// </summary>
        /// <param name="Tri">Triangle to test</param>
        /// <param name="X">X coord of the seek point</param>
        /// <param name="Y">Y coord of the seek point</param>
        /// <returns>True if the input (X,Y) is to the right of 2 of the triangle sides, False else</returns>
        private bool TriangleJunction(Triangle Tri, double X, double Y, out int Side1, out int Side2)
        {
            int count = 0;
            Side1 = 0; Side2 = 0;
            if (MathsHelpers.Vector.FNRightOf(Tri.Point1.X, Tri.Point1.Y, Tri.Point2.X, Tri.Point2.Y, X, Y))
            {
                count++;
                Side1 = 1;
            }
            if (MathsHelpers.Vector.FNRightOf(Tri.Point2.X, Tri.Point2.Y, Tri.Point3.X, Tri.Point3.Y, X, Y))
            {
                count++;
                if (Side1 == 0) Side1 = 2; else Side2 = 2;
            }
            if (MathsHelpers.Vector.FNRightOf(Tri.Point3.X, Tri.Point3.Y, Tri.Point1.X, Tri.Point1.Y, X, Y))
            {
                count++;
                if (Side1 == 0)
                    return false;
                else
                    Side2 = 3;
            }
            return count > 1;//todo optimize
        }

        #endregion testing

        /// <summary>
        /// FindTriangle - finds the triangle that point X,Y sits within
        /// </summary>
        /// <param name="X">X coord of the point</param>
        /// <param name="Y">Y coord of the point</param>
        /// <returns>a triangle if one was found, NULL else</returns>
        public Triangle FindTriangle(double X, double Y, Triangle startTriangle = null)
        {

            //#region serial test commented out
            //if (startTriangle != null)
            //{
            //	if (startTriangle.PointInTri(X, Y))
            //		return startTriangle;
            //}

            //foreach (var t in Triangles)
            //{
            //	if (t.PointInTri(X, Y))
            //		return t;

            //}
            //return null;
            //#endregion serial test

            #region bounds check
            if (MathsHelpers.Intersection.FnInBox(this.Bounds.Min.X, this.Bounds.Min.Y, this.Bounds.Max.X, this.Bounds.Max.Y, X, Y) == false)
                return null;
            #endregion bound check



            //should optimize to start looking in the last triangle found
            //just do start at the beggining for testing the algorithm

            if (Triangles == null || Triangles.Count <= 0)
                return null;

            Triangle startTri = null, thisTri = null;

            if (startTriangle != null)
            {
                startTri = startTriangle;
                thisTri = startTriangle;
            }
            else
            {
                startTri = this.Triangles.ElementAt(0);
                thisTri = this.Triangles.ElementAt(0);
            }

            //startTri = FindStartTri(X, Y);
            thisTri = startTri;
            var trianglesChecked = new List<Triangle>();

            do
            {
                if (thisTri.PointInTri(X, Y) == true)
                    return thisTri; //this is the one...wahhhheeey

                Triangle nexttri = thisTri.GetNextSearchTri(X, Y); //next triangle to check
                if (nexttri == null)
                    return FindTriangleSerially(X, Y);
                //return null;

                if (trianglesChecked.Contains(nexttri))
                {
                    //We have gone in a circle, most likely the result of triangles being crossed over by moving / dragging a point


                    //return null;
                    nexttri = thisTri.GetNextSearchTri(X, Y, nexttri);
                    if (nexttri == null)
                        //return null;
                        return FindTriangleSerially(X, Y);
                    if (trianglesChecked.Contains(nexttri))
                        return FindTriangleSerially(X, Y);
                    //return null;
                }

                trianglesChecked.Add(nexttri);
                thisTri = new Triangle();
                thisTri = nexttri; //try again on the next iteration

            }
            while (thisTri != startTri); //if we've gone 'round in a circle the point is not in the model

            #region serial test commented out
            if (startTriangle != null)
            {
                if (startTriangle.PointInTri(X, Y))
                    return startTriangle;
            }

            foreach (var t in Triangles)
            {
                if (t.PointInTri(X, Y))
                    return t;

            }
            //return null;
            #endregion serial test


            return null; // we have either got lost or the coordinate is not in the model
        }

 


        /// <summary>
        /// Wrapper for a basic triangulation of the model. (Breaklines applied by us)
        /// </summary>
        /// <returns></returns>
        public (bool Success, string Message) TriangulateModel()
        {
            //Generate breaklines from the model (LineFeautres traditionally. PolyLines+StandardLines for "CAD" lines)
            List<StandardLine> Breaklines = DTMHelpers.GetBreaklines(null, this.LineFeatures, this.GetPolyLines(), this.GetStandardLines());

            DeleteTriangleModel();

            //Renumber points so we can match them up to triangles after triangulation.
            DTMHelpers.RenumberPointIDs(Points.ToList());

            HashSet<DTMPoint> points = new HashSet<DTMPoint>(Points.ToList());

            bool result = false;
            string message = "";
            DTM outModel = new DTM();
            (result, message, outModel) = DTMHelpers.TriangulatePoints(points, null); //We aren't doing anything with breaklines at this point.

            if (!result) { return (result, message); } //Triangulation failed at some point. Read output message for a hint :)

            this.TNetMesh = outModel.TNetMesh;

            //Check and Handle Steiner Points
            if (outModel.Points.Count > Points.Count)
            {//A steiner point was created. We currently add it to the original model to ensure model integrity. Or you can find out why they're being made and fix it.
                for (int i = Points.Count; i < outModel.Points.Count; i++)
                {
                    Points.Add(outModel.Points[i].DeepCopy());
                }
            }
            
            //Recreate all triangles in the outModel, with their appropriately matched points.
            foreach (Triangle tri in outModel.Triangles)
            {
                TriangleSurface surface1 = GetSurface(1);
                DTMPoint point1 = Points[tri.Point1.ID];
                DTMPoint point2 = Points[tri.Point2.ID];
                DTMPoint point3 = Points[tri.Point3.ID];

                Triangle newTri = new Triangle(surface1, point1, point2, point3); //Builds relationships

                this.Triangles.Add(newTri);
            }

            ApplyBreaklines(Breaklines); //Apply breaklines to the model.

            
            return (result, message);
        }



        ///// <summary>
        ///// (Mainly for nice polygon selections as it works out groups to triangulate)Deletes points from model, retriangulates the affected area, deletes all appropriate triangle & point relationships, reinterprets feature codes and redraws everything.
        ///// </summary>
        ///// <param name="pointsToRemove"></param>
        //public void DeletePointsFromModel(HashSet<DTMPoint> pointsToRemove, bool triangulate = true)
        //{
        //    if (Points?.Count <= 0 || pointsToRemove?.Count <= 0 || pointsToRemove == null)
        //        return;


        //    var edges = FindSurroundingEdges(pointsToRemove);
        //    var listOfPolygons = new List<List<DTMPoint>>();

        //    if (edges != null && edges.Count > 0)
        //    {
        //        listOfPolygons = FindGroupsOfEdges(edges);
        //    }

        //    //#region Stuff to tidy the edges and check for donuts/islands etcetcetcetc. Can be made more efficient
        //    ////First check all polygons and see if any other points from the other polygons lay inside it. If so, we want to flag the nested poly for removal
        //    //HashSet<List<DTMPoint>> nestedPolygonsToRemove = new HashSet<List<DTMPoint>>();
        //    //foreach(List<DTMPoint> boundingPoly in listOfPolygons)
        //    //{
        //    //    foreach(List<DTMPoint> poly in listOfPolygons)
        //    //    {
        //    //        if (nestedPolygonsToRemove.Contains(poly)) { continue; } //This poly has been flagged for removal, no need to check if anything is inside it


        //    //        foreach(DTMPoint point in poly)
        //    //        {
        //    //            if (boundingPoly.Contains(point)) { continue; } //Skip the points which are in both polygons (edges that share points). Since we can't tell which one is the
        //    //            //parent one just form this. (we find that out with the other points in this loop)

        //    //            if (Geometry.InPoly2D(boundingPoly, point.X, point.Y)) //if a point in one poly is inside another polygon, the nested poly gets flagged for removal
        //    //            {
        //    //                nestedPolygonsToRemove.Add(poly);
        //    //                break;
        //    //            }
        //    //            else
        //    //            {

        //    //            }

        //    //        }
        //    //    }

        //    //}

        //    ////Go through those nested polygons and remove them from the main list of polygons
        //    //foreach(var nestedPoly in nestedPolygonsToRemove)
        //    //{
        //    //    listOfPolygons.Remove(nestedPoly);
        //    //}

        //    //HashSet<DTMPoint> interiorPoints = new HashSet<DTMPoint>();

        //    ////Now we may have random points (that we want to keep in the model) inside the bounding polygon. We want to triangulate these with the bounding poly so we add them to the respective bounding poly
        //    //foreach(List<DTMPoint> boundingPoly in listOfPolygons)
        //    //{
        //    //    foreach (DTMPoint point in Points)
        //    //    {//To make this more effiecient. Change FindAllEdges to returna list of points where a triangle of a pointtoberemoves has an edge with a single point that isn't to be removed. Then use that list here instead of ALL points.
        //    //        if (pointsToRemove.Contains(point)) { continue; }//These points are getting removed anyway so phuck em off

        //    //        if (Geometry.InPoly2D(boundingPoly, point.X, point.Y) && !boundingPoly.Contains(point))
        //    //        {
        //    //            //if theres a point (that we want to keep) inside our pointsToRemove bounding poly, we add it to the "boundingPoly" so it gets triangulated too
        //    //            interiorPoints.Add(point);
        //    //            boundingPoly.Add(point);
        //    //        }
        //    //    }
        //    //}
        //    //#endregion


        //    var dict = CreatePointIncrementDictionary();
        //    DeletePointsFromDTMModel(pointsToRemove); //New and Improved™ method of removing points and associated stuff from the model


        //    //foreach(DTMPoint interiorPoint in interiorPoints)
        //    //{
        //    //    List<Triangle> trisToRemove = new List<Triangle>(interiorPoint.Triangles);
        //    //    if (interiorPoint.Triangles == null) { continue; }
        //    //    foreach(Triangle triToRemove in trisToRemove)
        //    //    {
        //    //        RemoveTriangleFromModel(triToRemove);
        //    //    }

        //    //}

        //    foreach (var polyList in listOfPolygons)
        //    {

        //        if (polyList.First() != null && polyList.Last() != null && triangulate)
        //        {
        //            TriangulateInsideofPolygon(polyList);
        //        }
        //    }
            
        //    InterpretPointIncrementDictionary(dict);
        //    InterpretFeatures();
        //}


        /// <summary>
        /// Deletes points individually from the model and retriangulates area left behind. TN 20.05.2022
        /// </summary>
        /// <param name="pointsToRemove"></param>
        /// <param name="triangulate"></param>
        public void DeletePointsFromModel(HashSet<DTMPoint> pointsToRemove, bool triangulate = true)
        {
            if (Points?.Count <= 0 || pointsToRemove?.Count <= 0 || pointsToRemove == null)
                return;

            var dict = CreatePointIncrementDictionary();

            //Regen point ID indexing for use in merging the models later
            DTMHelpers.RenumberPointIDs(this.Points.ToList());
            if (triangulate)
            {
                

                foreach (DTMPoint point in pointsToRemove) //Find surrounding points and triangulate this area only.
                {
                    HashSet<DTMPoint> boundaryPoints = new HashSet<DTMPoint>();
                    List<Triangle> oldTriangles = new List<Triangle>();

                    if (point.Triangles != null) 
                    {
                        foreach (Triangle pointsTriangle in point.Triangles)
                        {
                            oldTriangles.Add(pointsTriangle);
                            boundaryPoints.Add(pointsTriangle.Point1);
                            boundaryPoints.Add(pointsTriangle.Point2);
                            boundaryPoints.Add(pointsTriangle.Point3);

                        }

                        boundaryPoints.Remove(point); //We don't want to include the point we're removing!

                        foreach(Triangle triToRemove in oldTriangles)
                        { //Remove the triangles here so this point doesn't get included when triangulating the next point group
                            RemoveTriangleFromModel(triToRemove);
                        }
                    }

                    //Triangulate this group of points
                    if (boundaryPoints.Count > 2)
                    {
                        (bool success, string message, DTM outModel) = DTMHelpers.TriangulatePoints(boundaryPoints);

                        //Add the triangulation back into the model, check all new triangles reside in the old ones
                        DTMHelpers.MergeNewTriangulationIntoParentModel(outModel, this, oldTriangles);
                    }

                }
            }

            InterpretPointIncrementDictionary(dict);

            DeletePointsFromEntites(pointsToRemove);

            InterpretFeatures();

        }

        /// <summary>
        /// Wrapper for DeletePoinstFromModel.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="triangulate"></param>
        public void DeletePointFromModel(DTMPoint point, bool triangulate = true) 
        {
            DeletePointsFromModel(new HashSet<DTMPoint> { point }, triangulate);
        }

        /// <summary>
        /// Warning: Only works if no changes to points have been made. i.e point movements, deletions, additions, will all mess up the index matching between vertices, points, and triangles.
        /// </summary>
        public void RemakeTriangleModelFromMesh()
        {
            if (this.TNetMesh == null || Points == null || Points.Count == 0) { return; }

            List<Vertex> vertList = TNetMesh.Vertices.ToList(); //Convert the ICollection to a list. Using the raw collection you have to use elementAt, which is S L O W
            //Match the points from the mesh with the original points from the model
            for (int j = 0; j < vertList.Count; j++)
            {

                Vertex vert = vertList[j];
                //DTMPoint returnedPoint = this.MatchPointFromPointsDictionary(vert.X, vert.Y, z, false, "Default", true); Match point doesn't really work here since we aren't sure what TriangleNET does with duplicates.
                //It messes up the indexing for the reintegration of triangles and points later on.

                DTMPoint returnedPoint = new DTMPoint();

                if (vertList.Count > Points.Count)
                {
                    //If this ever gets hit then a steiner point is being created somewhere. This may be in the initial triangulation or in any of the
                    //triangulations when we apply breaklines.
                    DTMPoint steinerPoint = new DTMPoint(vert.X, vert.Y, 0);
                    steinerPoint.PointLabel = "SteinerPoint" + j.ToString();
                    Points.Add(steinerPoint);
                    returnedPoint = steinerPoint;
                }
                else
                {
                    returnedPoint = Points[j];
                }
            }

            //Add the triangles generated by TriangleNet
            foreach (var triangle in TNetMesh.Triangles)
            {
                //Get the vertex IDs (positions in Points/Vertex arrays)
                int index1 = triangle.GetVertexID(0);
                int index2 = triangle.GetVertexID(1);
                int index3 = triangle.GetVertexID(2);

                DTMPoint point1 = this.Points[index1];
                DTMPoint point2 = this.Points[index2];
                DTMPoint point3 = this.Points[index3];

                //Check the triangle isn't co-linear
                if (NRG.MathsHelpers.Vector.FnOnLine3(point1.X, point1.Y, point2.X, point2.Y, point3.X, point3.Y, 0.0000001) == 0) { continue; }//Skip co-liner triangles
                //They can cause steiner points down the line. Hypothetically there's a chance that skipping the co-linear triangle form being added, it can cause a hole in the model.
                //The nasty co-linear triangles have an extremely small difference in linear positioning.
                //Having the FnOnLine tolerance higher than 0.0001 has shown to leave some small holes in the model. Increasing tolerance accuracy reduces this occurance, a good balance seems to be  0.0000001 
                //TriangleNET producing co-linear triangles is a bit out of our hands so leaving this as the best found solution for now. TN 30.05.22

                //Temp surface
                TriangleSurface surface = new TriangleSurface();
                surface.ID = 1;

                //This sorts the triangle-point relationship
                var newTriangle = new Triangle(surface, point1, point2, point3);

                //Add the triangle to the model
                this.Triangles.Add(newTriangle);
            }
        }

        /// <summary>
        /// Applies a breakline retrospecivley to a model. Finds all triangles that the breaklines crosses, retriangulates those points with boundary segments made from the
        /// triangle outlines and a segment straight down the middle for the initial breakline.
        /// Returns the originally passed model but with the breakline applied .
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="Breakline"></param>
        /// <returns></returns>
        public bool ApplyBreaklines(StandardLine Breakline, bool renumberPoints = true)
        {
            if (renumberPoints) { DTMHelpers.RenumberPointIDs(this.Points.ToList()); }

            DTMPoint startPoint = Breakline.StartPoint;
            DTMPoint endPoint = Breakline.EndPoint;

            //Find all the triangles that our breaklines crosses. Stops at end of breakline or end of model.
            HashSet<Triangle> foundTriangles = FindTrianglesOnLine(Breakline);

            if (foundTriangles == null || foundTriangles.Count == 0) { return true; } //Returning true because this isn't an error, just a non-applicable breakline

            //Find the edges of the triangles that either lead to a triangle not in the list or lead to the hull
            List<Edge> allEdges = GetBoundaryEdgesOfTriangles(foundTriangles.ToList());
            List<StandardLine> Breaklines = new List<StandardLine>();

            foreach (var edge in allEdges) //Convert those edges to standardlines (to be used as boundary lines for the triangulation)
            {
                StandardLine bl = new StandardLine();
                bl.StartPoint = edge.Point1;
                bl.EndPoint = edge.Point2;
                Breaklines.Add(bl);
            }

            HashSet<DTMPoint> boundaryPoints = new HashSet<DTMPoint>();
            //Make a hole in the Model where our foundTriangles exist. Add all the nodes of these triangles to the list of points we want to triangulate.
            if (foundTriangles != null && foundTriangles.Count > 0)
            {
                foreach (NRG.Models.Triangle t in foundTriangles)
                {
                    boundaryPoints.Add(t.Point1);
                    boundaryPoints.Add(t.Point2);
                    boundaryPoints.Add(t.Point3);

                    t.Point1.Triangles.Remove(t);
                    t.Point2.Triangles.Remove(t);
                    t.Point3.Triangles.Remove(t);

                    TriangleSurface s = t.Surface;
                    s.RemoveTriangle(t);
                    Triangles.Remove(t);
                }
            }
            else
            {

            }

            //Using the actual passed breakline, we have to make sure its start and end point are part of the points being triangulated (boundaryPoints).
            //Our found triangles can be in seperate "groupings" (if the breakline runs parralel to a a few triangle for a bit).
            //We need to find all the boundaryPoints that lie on the original breakline line, and then make smaller breaklines between them.
            //Failure to do this will result in a failed triangulation or even worse...STEINER POINTS(maybe).
            List<DTMPoint> breakLineNodes = new List<DTMPoint>();

            foreach(DTMPoint bp in boundaryPoints)
            {
                if (NRG.MathsHelpers.Vector.FnOnLine3(startPoint.X,startPoint.Y,endPoint.X,endPoint.Y, bp.X, bp.Y) == 0)
                {
                    breakLineNodes.Add(bp);
                }
            }
            //Make "mini" breaklines from these intersected boundary points
            for(int i = 0; i<breakLineNodes.Count-1; i++)
            {
                StandardLine bl = new StandardLine();
                bl.StartPoint = breakLineNodes[i];
                bl.EndPoint = breakLineNodes[i + 1];
                Breaklines.Add(bl);
            }

            bool success = false;
            string message = "";
            DTM tempModel = null;

            if (boundaryPoints != null && boundaryPoints.Count < 3) { }
            

            //Triangluate this group of boundary points and the passed boundarylines+breakline.
            (success, message, tempModel) = DTMHelpers.TriangulatePoints(boundaryPoints, Breaklines);

            if (!success && message == "Steiners created")
            {
                return false;
            }

            //Merge the smaller model with the main model. This also checks for overlapping triangles, should the foundTriangles boundaries have had any concave areas.
            DTMHelpers.MergeNewTriangulationIntoParentModel(tempModel, this, foundTriangles.ToList());
            
            return true;
        }

        /// <summary>
        /// A wrapper for "ApplyBreaklines(DTM Model, StandardLine)"
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="Breaklines"></param>
        /// <returns></returns>
        public void ApplyBreaklines(List<StandardLine> Breaklines)
        {
            DTMHelpers.RenumberPointIDs(this.Points.ToList());
            int count = 0; //you can use this for a progress meter
            foreach (StandardLine breakLine in Breaklines)
            {
                count++;
                if(!ApplyBreaklines(breakLine, false))
                {
                    break; //If breakline application failed (due to steiner point creation) we break here rather than do more damage and act like its normal.
                }

            }
        }

        public HashSet<Triangle> FindTrianglesOnLine(StandardLine Line)
        {//Could do an override for point3D/2D. Just find the triangle the point sits in for the start point TN 20.05.22

            HashSet<Triangle> usedTriangles = new HashSet<Triangle>(); //All triangles that have been tested (including triangles with sides parralel to line)
            HashSet<Triangle> crossedTriangles = new HashSet<Triangle>();//Triangles that the line has crossed (triangles that are parralel to line don't count)
            HashSet<DTMPoint> usedPoints = new HashSet<DTMPoint>(); //A list of edges previously tested. Keep a store so we don't go back on ourselves (dont use edge object here)

            DTMPoint sp = Line.StartPoint;
            DTMPoint ep = Line.EndPoint;

            DTMPoint nextPoint = sp;
            Triangle nextTriangle = null;

            bool continueSearch = true;
            bool endPointFound = false;

            while (continueSearch) //Iterate through the last checked point or triangle and find the next triangles the line intersects
            {

                if (nextPoint != null)
                {
                    if (nextPoint == ep) { continueSearch = false; endPointFound = true; break; } //We can't go any futher than this point since it's the end point

                    nextTriangle = null; //Clear any previous searches

                    //- Given a point, loop through the triangles, do the same as "nextTriangle". break on first valid return of either nextTriangle or nextPoint
                    if (nextPoint.Triangles == null || nextPoint.Triangles.Count  == 0)
                    {
                        continueSearch = false;
                    }

                    DTMPoint currentTestPoint = nextPoint;

                    bool exitFound = false;
                    foreach(Triangle tri in nextPoint.Triangles)
                    {
                        if (!usedTriangles.Contains(tri))
                        {
                            nextTriangle = tri;

                            (nextPoint, nextTriangle) = FindExitTriOrNode();

                            if (nextPoint !=null || nextTriangle != null) //We've found an exit using one of these triangles, we break the loop and continue the search
                            {
                                exitFound = true;
                                break;
                            }
                        }
                    }
                    continueSearch = exitFound; //If no exit has been found, we're at the end of the hull/our desination point
                }
                else if (nextTriangle != null)
                {
                    nextPoint = null; //Clear any previous searches

                    if (!usedTriangles.Contains(nextTriangle)) { (nextPoint, nextTriangle) = FindExitTriOrNode(); }
                    else
                    {//we shouldn't be searching a triangle thats already been searched. somethings wrong here.
                     // 19.05.22 TN, this can be hit when we get to a triangle that is very very very thin, which causes issues with intersection points etc.
                     //these very thin triangles are usually the result of multiple overlapping breaklines anyway
                        continueSearch = false;
                    }

                }



                if (nextPoint == null && nextTriangle == null) { continueSearch = false; } //The search has finished
            }

            (DTMPoint returnPoint, Triangle returnTriangle) FindExitTriOrNode()
            {
                //test each side, if the line runs along two nodes, we use this triangle and its adjacent triangle, return the second node (closer to endPt).
                //Add all triangles of the entrypoint to usedTriangles and foundTriangles.
                //The current triangle gets added no matter what;
                usedTriangles.Add(nextTriangle);

                DTMPoint returnPoint = null;
                Triangle returnTriangle = null;
                Triangle adjacentTri = null;
                Point3D ip1 = null;
                Point3D ip2 = null;

                int totalNodesOnLine = 0;
                #region  Check if any of the nodes are on the testline

                int p1OnLine = 0; int p2OnLine = 0; int p3OnLine = 0;
                if (NRG.MathsHelpers.Vector.FnOnLine3(sp.X, sp.Y, ep.X, ep.Y, nextTriangle.Point1.X, nextTriangle.Point1.Y) == 0) { p1OnLine++; }
                if (NRG.MathsHelpers.Vector.FnOnLine3(sp.X, sp.Y, ep.X, ep.Y, nextTriangle.Point2.X, nextTriangle.Point2.Y) == 0) { p2OnLine++; }
                if (NRG.MathsHelpers.Vector.FnOnLine3(sp.X, sp.Y, ep.X, ep.Y, nextTriangle.Point3.X, nextTriangle.Point3.Y) == 0) { p3OnLine++; }
                totalNodesOnLine = p1OnLine + p2OnLine + p3OnLine;

                double dist1 = double.PositiveInfinity;
                double dist2 = double.PositiveInfinity;
                double dist3 = double.PositiveInfinity;
                if (p1OnLine == 1) { dist1 = NRG.MathsHelpers.Vector.FnDistance(nextTriangle.Point1.X, nextTriangle.Point1.Y, ep.X, ep.Y); }
                if (p2OnLine == 1) { dist2 = NRG.MathsHelpers.Vector.FnDistance(nextTriangle.Point2.X, nextTriangle.Point2.Y, ep.X, ep.Y); }
                if (p3OnLine == 1) { dist3 = NRG.MathsHelpers.Vector.FnDistance(nextTriangle.Point3.X, nextTriangle.Point3.Y, ep.X, ep.Y); }
                #endregion

                //Two nodes are on the line. We return the closest node to the end point and add this triangle and its adjacent one to the usedLists. We don't add it to the crossed list
                if (totalNodesOnLine == 2)
                {
                    #region Get Closest of two nodes

                    if (p1OnLine == 1 && p2OnLine == 1)
                    {
                        //Closest point out of two nodes from the line end point
                        if (dist1 < dist2) { returnPoint = nextTriangle.Point1; }
                        else { returnPoint = nextTriangle.Point2; }

                    }
                    else if (p2OnLine == 1 && p3OnLine == 1)
                    {
                        //Closest point out of two nodes from the line end point
                        if (dist2 < dist3) { returnPoint = nextTriangle.Point2; }
                        else { returnPoint = nextTriangle.Point3; }
                    }
                    else if (p3OnLine == 1 && p1OnLine == 1)
                    {
                        //Closest point out of two nodes from the line end point
                        if (dist3 < dist1) { returnPoint = nextTriangle.Point3; }
                        else { returnPoint = nextTriangle.Point1; }
                    }
                    #endregion
                }
                //One node on the line. Find the other intersection point and compare the two. If point furthest to line start point is node, return that point
                //if point furthest to line start point is an intersection point, return the adjacent triangle
                else if (totalNodesOnLine == 1)
                {
                    Point3D side1ip = NRG.MathsHelpers.Intersection.IntersectLines(sp, ep, nextTriangle.Point1, nextTriangle.Point2, 1);
                    Point3D side2ip = NRG.MathsHelpers.Intersection.IntersectLines(sp, ep, nextTriangle.Point2, nextTriangle.Point3, 1);
                    Point3D side3ip = NRG.MathsHelpers.Intersection.IntersectLines(sp, ep, nextTriangle.Point3, nextTriangle.Point1, 1);

                    if (p1OnLine == 1)//Nodes 1 is an intersection point, so intersection must be node 2 to 3
                    {
                        if (side2ip == null) { return (null, null); }//The line crosses a node but it does not exit, meaning this is either the end triangle or we can move on(if this test has come from a "nextPoint")

                        ip1 = side2ip;
                        double ipDistToEndOfLine = NRG.MathsHelpers.Vector.Distance(ep.X, ep.Y, ip1.X, ip1.Y);
                        if (dist1 < ipDistToEndOfLine)
                        {
                            returnPoint = nextTriangle.Point1;
                        }
                        else
                        {
                            returnTriangle = nextTriangle.FindAdjacentTriangle(2);
                        }
                    }
                    else if (p2OnLine == 1)//Nodes 2 is an intersection point, so intersection must be node 3 to 1
                    {
                        if (side3ip == null) { return (null, null); }//The line crosses a node but it does not exit, meaning this is either the end triangle or we can move on(if this test has come from a "nextPoint")

                        ip1 = side3ip;
                        double ipDistToEndOfLine = NRG.MathsHelpers.Vector.Distance(ep.X, ep.Y, ip1.X, ip1.Y);
                        if (dist2 < ipDistToEndOfLine)
                        {
                            returnPoint = nextTriangle.Point2;
                        }
                        else
                        {
                            returnTriangle = nextTriangle.FindAdjacentTriangle(3);
                        }
                    }
                    else if (p3OnLine == 1)//Nodes 3 is an intersection point, so intersection must be node 1 to 2
                    {
                        if (side1ip == null) { return (null, null); }//The line crosses a node but it does not exit, meaning this is either the end triangle or we can move on(if this test has come from a "nextPoint")

                        ip1 = side1ip;
                        double ipDistToEndOfLine = NRG.MathsHelpers.Vector.Distance(ep.X, ep.Y, ip1.X, ip1.Y);
                        if (dist3 < ipDistToEndOfLine)
                        {
                            returnPoint = nextTriangle.Point3;
                        }
                        else
                        {
                            returnTriangle = nextTriangle.FindAdjacentTriangle(1);
                        }
                    }
                }
                //Find both intersection points, get distance from start point, use furthest intersection side to find the adjacent triangle, return that
                else if (totalNodesOnLine == 0)
                {
                    Point3D side1ip = NRG.MathsHelpers.Intersection.IntersectLines(sp, ep, nextTriangle.Point1, nextTriangle.Point2, 1);
                    Point3D side2ip = NRG.MathsHelpers.Intersection.IntersectLines(sp, ep, nextTriangle.Point2, nextTriangle.Point3, 1);
                    Point3D side3ip = NRG.MathsHelpers.Intersection.IntersectLines(sp, ep, nextTriangle.Point3, nextTriangle.Point1, 1);

                    double side1ipDist = double.PositiveInfinity; double side2ipDist = double.PositiveInfinity; double side3ipDist = double.PositiveInfinity;
                    if (side1ip != null) { side1ipDist = NRG.MathsHelpers.Vector.Distance(ep.X, ep.Y, side1ip.X, side1ip.Y); }
                    if (side2ip != null) { side2ipDist = NRG.MathsHelpers.Vector.Distance(ep.X, ep.Y, side2ip.X, side2ip.Y); }
                    if (side3ip != null) { side3ipDist = NRG.MathsHelpers.Vector.Distance(ep.X, ep.Y, side3ip.X, side3ip.Y); }

                    if (side1ip != null && side2ip != null)
                    {//Line goes through side 1 and side 2

                        //Find the intersection point closest to the line's start end
                        if (side1ipDist < side2ipDist)
                        { returnTriangle = nextTriangle.FindAdjacentTriangle(1); }
                        else
                        { returnTriangle = nextTriangle.FindAdjacentTriangle(2); }
                    }
                    else if (side2ip != null && side3ip != null)
                    {//Line goes through side 2 and side 3

                        //Find the intersection point furthest from the line's start point
                        if (side2ipDist < side3ipDist)
                        { returnTriangle = nextTriangle.FindAdjacentTriangle(2); }
                        else
                        { returnTriangle = nextTriangle.FindAdjacentTriangle(3); }
                    }
                    else if (side3ip != null && side1ip != null)
                    {//Line goes through side 3 and side 1

                        //Find the intersection point furthest from the line's start point
                        if (side3ipDist < side1ipDist)
                        { returnTriangle = nextTriangle.FindAdjacentTriangle(3); }
                        else
                        { returnTriangle = nextTriangle.FindAdjacentTriangle(1); }
                    }

                }

                if (totalNodesOnLine < 2)
                { //Less than two nodes on a line means the line either crosses this triangle. We have returned earlier if there's 1 node on the line and no intersection point
                  //so theres no need to check for that situation here.
                    crossedTriangles.Add(nextTriangle);
                }


                if (adjacentTri != null)
                {//This will only and should always have a value if the triangle we have just tested had two nodes on a line
                    usedTriangles.Add(adjacentTri);
                }


                //At least one of nextTriangle and nextPoint should always be null
                if (returnPoint != null) 
                { 
                    return (returnPoint, null);  
                }
                else if (returnTriangle != null) 
                {
                    return (null,returnTriangle); 
                }

                return (null,null); //Nothing was found
            }


            //The search has finished here. This is a test to see if the end point is in one of the triangles we've returned
            //endPointFound = false can happen due to hitting hole/edge of model or getting to an extremely thin triangle which causes tolerance issues etc
            //none of these issues are anything major
            foreach(var tri in usedTriangles)
            {
                if (tri.Point1 == ep || tri.Point2 == ep || tri.Point3 == ep || tri.PointInTri(ep.X, ep.Y))
                {
                    endPointFound = true;
                    break;
                }
            }

            return crossedTriangles;
        }

        public void DeleteTriangleModel()
        {
            foreach(TriangleSurface surface in Surfaces)
            {
                surface.RemoveTriangles(Triangles);
            }
            foreach(DTMPoint point in Points)
            {
                point.Triangles = new List<Triangle>();
                point.ID = -1; //ID is used for assigning triangle vertex after triangulations. Resetting to -1 needs to be done before any triangulation
            }

            Triangles = new HashSet<Triangle>();
        }


        public DTM CreateBaseModel()
        {

            DTM mod = new DTM();

            if (this.Surfaces == null)
            {
                //DTMHelpers hlp = new DTMHelpers();
                DTMHelpers.CreateBaseSurfaces(this);
            }

            foreach (Triangle tri in this.Triangles)
            {

                if (tri.FindAdjacentTriangle(1) == null)
                {
                    DTMPoint pt1 = new DTMPoint(tri.Point1.X, tri.Point1.Y, tri.Point1.Z);
                    DTMPoint pt2 = new DTMPoint(tri.Point2.X, tri.Point2.Y, tri.Point2.Z);
                    mod.Points.Add(pt1);
                    mod.Points.Add(pt2);
                }

                if (tri.FindAdjacentTriangle(2) == null)
                {
                    //mod.Points.Add(tri.Point2);
                    //mod.Points.Add(tri.Point3);
                    DTMPoint pt1 = new DTMPoint(tri.Point3.X, tri.Point3.Y, tri.Point3.Z);
                    DTMPoint pt2 = new DTMPoint(tri.Point2.X, tri.Point2.Y, tri.Point2.Z);
                    mod.Points.Add(pt1);
                    mod.Points.Add(pt2);
                }

                if (tri.FindAdjacentTriangle(3) == null)
                {
                    //mod.Points.Add(tri.Point1);
                    //mod.Points.Add(tri.Point3);
                    DTMPoint pt1 = new DTMPoint(tri.Point1.X, tri.Point1.Y, tri.Point1.Z);
                    DTMPoint pt2 = new DTMPoint(tri.Point3.X, tri.Point3.Y, tri.Point3.Z);
                    mod.Points.Add(pt1);
                    mod.Points.Add(pt2);
                }
            }

            mod.TriangulateModel();


            return mod;

        }

		#endregion Triangle calcs

		#region Sections
		//please note this my own interpretation of how this should go off...



		public List<Point3D> CalcSectionThroughModel(Point3D StartPoint, Point3D EndPoint , ref List<Triangle> TrianglesUsed )
		{
			List<SectionPoint> Section = new List<SectionPoint>();

			if (this.Triangles == null || this.Triangles.Count == 0) { return null; }

			double sectionBrg = NRG.MathsHelpers.Trig.RPC(StartPoint.X, StartPoint.Y, EndPoint.X, EndPoint.Y);
			List<Triangle> trilist = new List<Triangle>();
			trilist = GetTrianglesInBox(StartPoint, EndPoint);

            //Remove all non-contourable triangles
            trilist.RemoveAll(t => t.Surface.Contour == false);

            List<Point3D> Pts = new List<Point3D>();
			if (trilist != null && trilist.Count > 0)
			{
				foreach (Triangle t in trilist)
				{
					List<Point3D> pts = t.FindIntersections(StartPoint, EndPoint);
					if (pts.Count > 0)
					{
						foreach (Point3D pt in pts)
						{
							SectionPoint sec = new SectionPoint();
							sec.X = pt.X;
							sec.Y = pt.Y;
							sec.Z = pt.Z;
                            if (t.Surface != null)
                            {
                                sec.R = (byte)t.Surface.R;
                                sec.G = (byte)t.Surface.G;
                                sec.B = (byte)t.Surface.B;
                            }
							Section.Add(sec);
						}
					}
				}

				if (Section != null && Section.Count > 0)
				{


					List<Point3D> retsec = new List<Point3D>();
					foreach (Point3D s in Section)
					{
						Point3D sp = new Point3D();
						double off = 0;
						double dist = 0;
						MathsHelpers.Trig.DistanceAndOffsetFromLine(StartPoint.X, StartPoint.Y, sectionBrg, s.X, s.Y, ref dist, ref off);

						sp.X = dist;
						sp.Y = s.Z;
                        sp.Z = 0;

                        sp.R = s.R;
                        sp.G = s.G;
                        sp.B = s.B;
						//sp.Y = (sp.Z + s.Z);
						
						retsec.Add(sp);
					}

					Point3D pt = new Point3D();

					if (retsec != null && retsec.Count > 0)

					{
						//pt.X = 0;//retsec.ElementAt(0).X;
						//pt.Y = retsec.ElementAt(0).Y;
						//Console.WriteLine("");
					}

					Point3D sectionStart = new Point3D(0, StartPoint.Z, 0);

					retsec = SearchingAndSorting.BubbleSortSortPointsByDistance(sectionStart, retsec);
                    TrianglesUsed = trilist;
					return retsec;
				}


				return null;
			}
			return null;
		}

        public List<Point3D> GetSection(Point3D startPoint, Point3D endPoint, bool getLineIntersections, bool getModelIntersections, bool showSurfaceColours = false)
        {
            List<Point3D> retsec = new List<Point3D>();
            double sectionBrg = NRG.MathsHelpers.Trig.RPC(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y);
            List<Triangle> triList = new List<Triangle>();
            
            if (retsec == null) { retsec = new List<Point3D>(); }

            if (getLineIntersections)
            {
                #region "Feature" Lines
                foreach (var key in LineFeatures.Keys)
                {
                    var collection = LineFeatures[key];
                    if (collection == null)
                        continue;

                    if (collection.FeatureItems != null)
                    {
                        foreach (var itemKey in collection.FeatureItems.Keys)
                        {
                            var featureItems = collection.FeatureItems[itemKey];
                            if (featureItems?.Count <= 0)
                                continue;

                            foreach (var featureItem in featureItems)
                            {
                                if (featureItem?.Points?.Count <= 0)
                                    continue;
                                //DrawingLayer lay = this.GetLayer(featureItem.Layer);
                                //if (lay != null)
                                //{
                                //    if (lay.Draw == false)
                                //    {
                                //        continue;
                                //    }
                                //}
                                DTMPoint startLine = null;
                                DTMPoint endLine = null;

                                for (int i = 0; i < featureItem.Points.Count; i++)
                                {
                                    startLine = featureItem.Points[i];

                                    if (i + 1 >= featureItem.Points.Count)
                                    {
                                        //break;//endLine = featureItem.Points[0]; removed by TN: 13.09.21 //At the end of a line/feature line, the last point is going back to the first one and an intersection is being made from this "line".
                                        if (featureItem.Closed == true)
                                            endLine = featureItem.Points[0];
                                        else
                                            break;
                                    }
                                    else
                                    {
                                        endLine = featureItem.Points[i + 1];
                                    }

                                    Point3D intersect = Intersection.IntersectLines(startPoint, endPoint, startLine, endLine, 2);
                                    if (intersect != null && startLine.Z != -999 && endLine.Z != -999)
                                    {
                                        Point3D sp = new Point3D();
                                        double off = 0;
                                        double dist = 0;
                                        MathsHelpers.Trig.DistanceAndOffsetFromLine(startPoint.X, startPoint.Y, sectionBrg, intersect.X, intersect.Y, ref dist, ref off);
                                        double totaldist = MathsHelpers.Vector.FnDistance(startLine.X, startLine.Y, endLine.X, endLine.Y);
                                        //if (dist <= totaldist)
                                        //{

                                        double distAlong= MathsHelpers.Vector.FnDistance(startLine.X, startLine.Y, intersect.X, intersect.Y);
                                        double ht = MathsHelpers.Interpolation.FnInterpolateY(0, startLine.Z,totaldist , endLine.Z, distAlong);

                                        sp.X = dist;
                                        sp.Y = ht;
                                        sp.SectionLabel = featureItem.Code;
                                        retsec.Add(sp);

                                        //}

                                        //break; removed by TN: 02.09.21 - It was preventing a single line from having more than one intersection.
                                    }
                                }
                            }
                        }

                    }

                }
                #endregion

                #region PolyLines
                foreach (var polyLine in GetPolyLines())
                {
                    for (int i = 0; i < polyLine.Nodes.Count; i++)
                    {
                        var startLine = polyLine.Nodes[i];
                        var endLine = new DTMPoint();

                        if (i + 1 >= polyLine.Nodes.Count) //does this mean that polyline is closed by default? If so it's a bit dodgy
                        {
                            if (polyLine.IsClosed)
                            {
                                endLine = polyLine.Nodes[0];
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            endLine = polyLine.Nodes[i + 1];
                        }

                        Point3D intersect = Intersection.IntersectLines(startPoint, endPoint, startLine, endLine, 2);

                        if (intersect != null && startLine.Z != -999 && endLine.Z != -999)
                        {
                            Point3D sp = new Point3D();
                            double off = 0;
                            double dist = 0;
                            MathsHelpers.Trig.DistanceAndOffsetFromLine(startPoint.X, startPoint.Y, sectionBrg, intersect.X, intersect.Y, ref dist, ref off);

                            sp.X = dist;
                            sp.Y = intersect.Z;
                            sp.SectionLabel = polyLine.Layer;

                            
                            retsec.Add(sp);
                        }
                    }
                }
                #endregion

                #region Standard Lines
                foreach (var stdLine in GetStandardLines())
                {
                    Point3D intersect = Intersection.IntersectLines(startPoint, endPoint, stdLine.StartPoint, stdLine.EndPoint, 2);

                    if (intersect != null && stdLine.StartPoint.Z != -999 && stdLine.EndPoint.Z != -999)
                    {
                        Point3D sp = new Point3D();
                        double off = 0;
                        double dist = 0;
                        MathsHelpers.Trig.DistanceAndOffsetFromLine(startPoint.X, startPoint.Y, sectionBrg, intersect.X, intersect.Y, ref dist, ref off);

                        sp.X = dist;
                        sp.Y = intersect.Z;
                        sp.SectionLabel = stdLine.Layer;


                        retsec.Add(sp);
                    }
                    
                }
                #endregion
            }

            if (getModelIntersections)
            {

                List<Point3D> modsec= CalcSectionThroughModel(startPoint, endPoint, ref triList);

                if (modsec != null)
                {
                    if (retsec != null && retsec.Count > 0)
                    {
                        retsec.AddRange(modsec);
                    }
                    else
                    {
                        retsec = new List<Point3D>();
                        retsec = modsec;
                    }

                    
                }
            }

            if (retsec != null)
            {
                Point3D sectionStart = new Point3D(0, startPoint.Z, 0);
                retsec = SearchingAndSorting.BubbleSortSortPointsByDistance(sectionStart, retsec);
            }

            if (retsec != null && retsec.Count > 0)
            {
                for (int i = retsec.Count - 1; i > 0; i--)
                {
                    Point3D pt = retsec.ElementAt(i);
                    Point3D lastpt = retsec.ElementAt(i - 1);

                    if (MathsHelpers.Vector.FnDistance(pt.X, pt.Y, lastpt.X, lastpt.Y) < 0.002f)
                    {
                        lastpt.R = pt.R;lastpt.G = pt.G; lastpt.B = pt.B;
                        retsec.Remove(pt);
                    }
                }
            }


            if (retsec != null && retsec.Count > 0 && this.Triangles != null && this.Triangles.Count > 0 && showSurfaceColours)
            {

                for (int i = 0; i < retsec.Count - 1; i++)
                {
                    Point2D cp = new Point2D();
                    cp = (MathsHelpers.Trig.PRC(startPoint.X, startPoint.Y, sectionBrg, retsec[i].X) + MathsHelpers.Trig.PRC(startPoint.X, startPoint.Y, sectionBrg, retsec[i + 1].X)) / 2;
                    Triangle tri = new Triangle();
                    //====================================================================================================================
                    //Optimised by ES:22.09.21 - we have the list of triangles used for the section so limit our search to these triangles
                    if (triList != null && triList.Count >0)
                    {
                        foreach (Triangle t in triList)
                        {
                            if (t.PointInTri(cp.X, cp.Y))
                            {
                                tri = t;
                                break;
                            }
                        }
                    }
                    else
                    { 
                        tri = this.FindTriangle(cp.X, cp.Y);
                    }
                    //====================================================================================================================
                    if (tri != null && tri.Surface != null)
                    {
                        retsec[i].R = (byte)tri.Surface.R;
                        retsec[i].G = (byte)tri.Surface.G;
                        retsec[i].B = (byte)tri.Surface.B;
                    }

                }
            }

            return retsec;
        }
        //public List<Point3D> CalcSectionThroughModel(Point3D StartPoint, Point3D EndPoint)
        //{
        //    List<SectionPoint> Section = new List<SectionPoint>();

        //    //if (this.Triangles == null || this.Triangles.Count == 0) { return null; }//line intersections need adding here!:)


        //    foreach (var key in LineFeatures.Keys)
        //    {
        //        var collection = LineFeatures[key];
        //        if (collection == null)
        //            continue;

        //        if (collection.FeatureItems != null)
        //        {
        //            foreach (var itemKey in collection.FeatureItems.Keys)
        //            {
        //                var featureItems = collection.FeatureItems[itemKey];
        //                if (featureItems?.Count <= 0)
        //                    continue;

        //                foreach (var featureItem in featureItems)
        //                {
        //                    if (featureItem?.Points?.Count <= 0)
        //                        continue;

        //                    DTMPoint startLine = null;
        //                    DTMPoint endLine = null;
        //                    
        //                    for (int i = 0; i < featureItem.Points.Count; i++)
        //                    {
        //                        startLine = featureItem.Points[i];
        //                        if (i + 1 >= featureItem.Points.Count)
        //                            endLine = featureItem.Points[0];
        //                        else
        //                            endLine = featureItem.Points[i + 1];
        //                        Point3D intersect = Intersection.IntersectLines(StartPoint, EndPoint, startLine, endLine, 2);
        //                        if (intersect != null)
        //                        {
        //                            SectionPoint sec = new SectionPoint();
        //                            sec.X = intersect.X;
        //                            sec.Y = intersect.Y;
        //                            sec.Z = intersect.Z;
        //                            Section.Add(sec);
        //                            intersects = true;
        //                            break;
        //                        }
        //                    }
        //                }
        //            } 




        //            double sectionBrg = NRG.MathsHelpers.Trig.RPC(StartPoint.X, StartPoint.Y, EndPoint.X, EndPoint.Y);
        //            List<Triangle> trilist = new List<Triangle>();
        //            trilist = GetTrianglesInBox(StartPoint, EndPoint);
        //            List<Point3D> Pts = new List<Point3D>();
        //            if (trilist != null && trilist.Count > 0)
        //            {
        //                foreach (Triangle t in trilist)
        //                {
        //                    List<Point3D> pts = t.FindIntersections(StartPoint, EndPoint);
        //                    if (pts.Count > 0)
        //                    {
        //                        foreach (Point3D pt in pts)
        //                        {
        //                            SectionPoint sec = new SectionPoint();
        //                            sec.X = pt.X;
        //                            sec.Y = pt.Y;
        //                            sec.Z = pt.Z;
        //                            Section.Add(sec);
        //                        }
        //                    }
        //                }



        //                if (Section != null && Section.Count > 0)
        //                {


        //                    List<Point3D> retsec = new List<Point3D>();
        //                    foreach (Point3D s in Section)
        //                    {
        //                        Point3D sp = new Point3D();
        //                        double off = 0;
        //                        double dist = 0;
        //                        MathsHelpers.Trig.DistanceAndOffsetFromLine(StartPoint.X, StartPoint.Y, sectionBrg, s.X, s.Y, ref dist, ref off);

        //                        sp.X = dist;//MathsHelpers.Vector.FnDistance(StartPoint.X, StartPoint.Y, s.X, s.Y);
        //                        sp.Y = s.Z;

        //                        //sp.Y = (sp.Z + s.Z);

        //                        sp.Z = 0;
        //                        retsec.Add(sp);
        //                    }

        //                    Point3D pt = new Point3D();

        //                    if (retsec != null && retsec.Count > 0)

        //                    {
        //                        //pt.X = 0;//retsec.ElementAt(0).X;
        //                        //pt.Y = retsec.ElementAt(0).Y;
        //                        //Console.WriteLine("");
        //                    }

        //                    Point3D sectionStart = new Point3D(0, StartPoint.Z, 0);

        //                    retsec = SearchingAndSorting.BubbleSortSortPointsByDistance(sectionStart, retsec);
        //                    return retsec;
        //                }

        //            }

        //        }

        //    }

        //    return null;

        //}

        //List <Point3D>  SectionStartPoint(Point2D StartPoint, Point2D EndPoint)
        //{
        //    DTMPoint StartPt = new DTMPoint();
        //    DTMPoint Endpt = new DTMPoint();


        //    BindingList<DTMPoint> Points = new BindingList<DTMPoint>();
        //    Triangle tri = this.FindTriangle(StartPoint.X, StartPoint.Y);
        //    if (tri != null)
        //    {
        //        //cool: the first point is in the triangle model
        //        DTMPoint thisPoint = new DTMPoint();
        //        thisPoint.X = StartPoint.X;
        //        thisPoint.Y = StartPoint.Y;
        //        thisPoint.Z = tri.CalcLevelInTri(StartPoint.X, StartPoint.Y);
        //        Points.Add(thisPoint);
        //    }
        //    else 
        //    {
        //        //not so cool: we'll have to do some programmering
        //        //Basically the software has instigated a section out side of the model so we need to find the first model intersection point.
        //        //I'm gonna try a serial search approach and see what happens.
        //        foreach (Triangle t in Triangles)
        //        {


        //        }



        //    return null;
        //}

        /// <summary>
        /// GetTrianglesInBox - find all triangle that have a node within or side that intersects with a box. The box is orientated polar north - south, east west
        /// </summary>
        /// <param name="Point1">first corner of the box</param>
        /// <param name="Point2">second corner of the box</param>
        /// <returns>list containing all the triangle that meet the critera</returns>
        public List<Triangle> GetTrianglesInBox(Point3D Point1, Point3D Point2)
        {
            List<Triangle> trilist = new List<Triangle>();
            foreach (Triangle t in Triangles)
            {
                if (MathsHelpers.Intersection.FnInBox(Point1.X, Point1.Y, Point2.X, Point2.Y, t.Point1.X, t.Point1.Y) || (MathsHelpers.Intersection.FnInBox(Point1.X, Point1.Y, Point2.X, Point2.Y, t.Point2.X, t.Point2.Y) || MathsHelpers.Intersection.FnInBox(Point1.X, Point1.Y, Point2.X, Point2.Y, t.Point3.X, t.Point3.Y)))
                {
                    trilist.Add(t); 
                }
                else 
                {
                    //and I hate having to do this...
                    double  ipx=double.PositiveInfinity;
                    double  ipy=double.PositiveInfinity;
                    if (NRG.MathsHelpers.Intersection.IntersectLines(t.Point1.X, t.Point1.Y, t.Point2.X, t.Point2.Y, Point1.X, Point1.Y, Point2.X, Point2.Y, ref ipx, ref ipy) || NRG.MathsHelpers.Intersection.IntersectLines(t.Point2.X, t.Point2.Y, t.Point3.X, t.Point3.Y, Point1.X, Point1.Y, Point2.X, Point2.Y, ref ipx, ref ipy) || NRG.MathsHelpers.Intersection.IntersectLines(t.Point3.X, t.Point3.Y, t.Point1.X, t.Point1.Y, Point1.X, Point1.Y, Point2.X, Point2.Y, ref ipx, ref ipy))
                        trilist.Add(t); 
                 }
            }

            return trilist;
        }

        #endregion Sections

        #region Sorting

        //public void SortPoints(PointProperties property)
        //{
        //    sortedPoints = null;
        //    switch(property)
        //    {
        //        case PointProperties.Command:
        //            sortedPoints = new BindingList<DTMPoint>(Points.OrderBy(p => p.Type).ToList());
        //            break;
        //        case PointProperties.PtNum:
        //            sortedPoints = new BindingList<DTMPoint>(Points.OrderBy(p => p.PointID).ToList());
        //            break;
        //        case PointProperties.Label:
        //            sortedPoints = new BindingList<DTMPoint>(Points.OrderBy(p => p.PointLabel).ToList());
        //            break;
        //        case PointProperties.Notes1:
        //            var notes1Points = new BindingList<DTMPoint>();
        //            foreach(var point in Points)
        //            {
        //                if (point.Notes != null && point.Notes.Length >= 1)
        //                    notes1Points.Add(point);
        //            }
        //            sortedPoints = new BindingList<DTMPoint>(notes1Points.OrderBy(p => p.Notes[0]).ToList());
        //            foreach(var point in Points)
        //            {
        //                if (!sortedPoints.Contains(point))
        //                    sortedPoints.Add(point);
        //            }
        //            break;
        //        case PointProperties.Notes2:
        //            var notes2Points = new BindingList<DTMPoint>();
        //            foreach(var point in Points)
        //            {
        //                if (point.Notes != null && point.Notes.Length >= 2)
        //                    notes2Points.Add(point);
        //            }
        //            sortedPoints = new BindingList<DTMPoint>(notes2Points.OrderBy(p => p.Notes[1]).ToList());
        //            foreach(var point in Points)
        //            {
        //                if (!sortedPoints.Contains(point))
        //                    sortedPoints.Add(point);
        //            }
        //            break;
        //        case PointProperties.HCR:
        //            var hcrPoints = new List<(double HCR, DTMPoint Point)>();
        //            foreach(var point in Points)
        //            {
        //                if(point is ObservationPoint obsPoint)
        //                {
        //                    hcrPoints.Add((obsPoint.HCR, obsPoint));
        //                }
        //                else if(point is AzimuthCorrection aziPoint)
        //                {
        //                    hcrPoints.Add((aziPoint.HCR, aziPoint));
        //                }
        //            }
        //            sortedPoints = new BindingList<DTMPoint>(hcrPoints.OrderBy(p => p.HCR).Select(p => p.Point).ToList());

        //            foreach(var point in Points)
        //            {
        //                if (!sortedPoints.Contains(point))
        //                    sortedPoints.Contains(point);
        //            }
        //            break;
        //        case PointProperties.VCR:
        //            var vcrPoints = new List<ObservationPoint>();
        //            foreach(var point in Points)
        //            {
        //                if(point is ObservationPoint obsPoint)
        //                {
        //                    vcrPoints.Add(obsPoint);
        //                }
        //            }
        //            vcrPoints = vcrPoints.OrderBy(p => p.VCR).ToList();
        //            sortedPoints = new BindingList<DTMPoint>();

        //            foreach(var point in vcrPoints)
        //            {
        //                sortedPoints.Add(point);
        //            }

        //            foreach(var point in Points)
        //            {
        //                if (!sortedPoints.Contains(point))
        //                    sortedPoints.Add(point);
        //            }
        //            break;
        //        case PointProperties.SD:
        //            var sdPoints = new List<ObservationPoint>();
        //            foreach(var point in Points)
        //            {
        //                if(point is ObservationPoint obsPoint)
        //                {
        //                    sdPoints.Add(obsPoint);
        //                }
        //            }
        //            sdPoints = sdPoints.OrderBy(p => p.SD).ToList();
        //            sortedPoints = new BindingList<DTMPoint>();

        //            foreach(var point in sdPoints)
        //            {
        //                sortedPoints.Add(point);
        //            }

        //            foreach(var point in Points)
        //            {
        //                if (!sortedPoints.Contains(point))
        //                    sortedPoints.Add(point);
        //            }
        //            break;
        //        case PointProperties.TgtHt:
        //            var tgtPoints = new List<(double Tgt, DTMPoint point)>();
        //            foreach(var point in Points)
        //            {
        //                if(point is ObservationPoint obsPoint)
        //                {
        //                    tgtPoints.Add((obsPoint.TargetHeight, obsPoint));
        //                }
        //                else if(point is StationPoint stationPoint)
        //                {
        //                    tgtPoints.Add((stationPoint.ObjectHeight, stationPoint));
        //                }
        //            }

        //            sortedPoints = new BindingList<DTMPoint>(tgtPoints.OrderBy(p => p.Tgt).Select(p => p.point).ToList());

        //            foreach(var point in Points)
        //            {
        //                if (!sortedPoints.Contains(point))
        //                    sortedPoints.Add(point);
        //            }

        //            break;
        //        case PointProperties.X:
        //            sortedPoints = new BindingList<DTMPoint>(Points.OrderBy(p => p.X).ToList());
        //            break;
        //        case PointProperties.Y:
        //            sortedPoints = new BindingList<DTMPoint>(Points.OrderBy(p => p.Y).ToList());
        //            break;
        //        case PointProperties.Z:
        //            sortedPoints = new BindingList<DTMPoint>(Points.OrderBy(p => p.Z).ToList());
        //            break;
        //    }
        //}

        #endregion

        #region Testing

        #endregion

        #endregion

        #region Data Extraction

        
        
        /// <summary>
        /// Attempts to find the point in the <see cref="DTM"/> that is closest to the provided <see cref="Point3D"/>
        /// </summary>
        /// <param name="point">The <see cref="Point2D"/> point to check against</param>
        /// <returns>Returns a <see cref="DTMPoint"/> containing the closest point in the <see cref="DTM"/> or <see cref="null"/> if there are no points.</returns>
        /// Modified by ES:04.11.21 - if there is duplication then it shows a dialog asking the user which point they would like to show in the grid.
        public DTMPoint GetClosestPoint(Point2D point, bool CheckForTriangles = false, bool CheckForDuplicates = true, bool CheckLayer = true)
        {
            if(Points?.Count <= 0)
                return null;

            List <DTMPoint> closestPoints = new List<DTMPoint>();
            double dist = 0, minDist = double.MaxValue;

            foreach(var p in Points)
            {
                if (p.Type == PointType.FalsePoint)
                    continue;

                if (CheckForTriangles == true && (p.Triangles == null || p.Triangles.Count == 0))
                    continue;

                if (CheckLayer && !IsOneOrMoreLayerVisible(p)) //Skip if all point's layers are not shown
                    continue;


                dist = MathsHelpers.Vector.FnDistance(p.X, p.Y, point.X, point.Y);//(xDiff * xDiff) + (yDiff * yDiff);

                if(dist <= minDist)
                {
                    minDist = dist;
                }
            }

            foreach (var p in Points) 
            {
                dist = MathsHelpers.Vector.FnDistance(p.X, p.Y, point.X, point.Y);
                if (dist == minDist) { closestPoints.Add(p); }
                //p.ID = Points.IndexOf(p);
            }

            if (closestPoints.Count == 0)
                return null;


            if (closestPoints.Count == 1 || !CheckForDuplicates)
            {
                return closestPoints[0];
            }
            else 
            {
                NRG.Models.PickDuplicateDlg dlg = new NRG.Models.PickDuplicateDlg();
                dlg.Points = closestPoints;
                dlg.ShowDialog();
                return dlg.SelectedPoint;
            }
        }

       

        #region Edge Z0NE

        public List<Edge> FindSurroundingEdges(HashSet<DTMPoint> PointsToRemove)
        {
            if (PointsToRemove == null)
                return null;


            if (this.Triangles == null || this.Triangles.Count == 0) { return null; }
            List<Edge> edges = new List<Edge>();
            foreach (DTMPoint pt in PointsToRemove)
            {
                if (pt.Triangles == null || pt.Triangles.Count == 0) { continue; }
                foreach (Triangle t in pt.Triangles)
                {
                    Edge ed;
                    if (PointsToRemove.Contains(t.Point1) == false && PointsToRemove.Contains(t.Point2) == false)
                    {
                        ed = new Edge(t.Point1, t.Point2);
                        edges.Add(ed);
                    }
                    if (PointsToRemove.Contains(t.Point2) == false && PointsToRemove.Contains(t.Point3) == false)
                    {
                        ed = new Edge(t.Point2, t.Point3);
                        edges.Add(ed);
                    }
                    if (PointsToRemove.Contains(t.Point3) == false && PointsToRemove.Contains(t.Point1) == false)
                    {
                        ed = new Edge(t.Point3, t.Point1);
                        edges.Add(ed);
                    }
                }
            }

            //HashSet<Edge> mirroredEdgesToRemove = new HashSet<Edge>();
            //foreach (Edge firstEdge in edges)
            //{
            //    //Temp testing
            //    StandardLine edgeLine = new StandardLine(firstEdge.Point1, firstEdge.Point2);
            //    edgeLine.Lineweight = 3;
            //    edgeLine.Colour = Color.Orange;
            //    this.AddStandardLine(edgeLine, "edgeLines");


            //    foreach (Edge secondEdge in edges)
            //    {
            //        if (firstEdge.Point1 == secondEdge.Point2 && firstEdge.Point2 == secondEdge.Point1)
            //        {
            //            mirroredEdgesToRemove.Add(firstEdge);
            //            mirroredEdgesToRemove.Add(secondEdge);
            //        }
            //    }
            //}


            //foreach(Edge edge in mirroredEdgesToRemove)
            //{
            //    //Temp testing
            //    StandardLine edgeLine = new StandardLine(edge.Point1, edge.Point2);
            //    edgeLine.Lineweight = 5;
            //    edgeLine.Colour = Color.Red;
            //    this.AddStandardLine(edgeLine, "MirroredEdgeLines");
            //    //edges.Remove(edge);
            //}



            return edges;
        }

        public List<List<DTMPoint>> FindGroupsOfEdges(List<Edge> masterEdgeList)
		{
            List<List<DTMPoint>> polyListOut = new List<List<DTMPoint>>();

            Edge startEdge = null;


            foreach (Edge edge in masterEdgeList)
			{
                List<DTMPoint> poly = new List<DTMPoint>();

                if (!edge.Used)
				{
                    startEdge = edge;

                    poly.Add(startEdge.Point1);
                    poly.Add(startEdge.Point2);
                   //startEdge.Used = true;

                    DTMPoint newPoint = new DTMPoint();


                    while (FindNextEdge((DTMPoint)poly.Last(), masterEdgeList, out newPoint, startEdge))
                    {
                        if (newPoint == poly.First())
                            break; //Our initial edge has just been flagged as used and the while loop has returned the Point2 again (closed poly). We break since we don't want to add it again

                        poly.Add(newPoint);
                    }
                    if (poly.Count >= 0) { polyListOut.Add(poly); }

                }
            }  
           
            return polyListOut ;

		}

        /// <summary>
        /// Using a list of triangles, return polygons of the groupings of triangles. i.e boundary lines of the triangles
        /// </summary>
        /// <param name="triangles"></param>
        /// <returns></returns>
        public List<Edge> GetBoundaryEdgesOfTriangles(List<Triangle> triangles)
        {
            List<Edge> edges = new List<Edge>();
            foreach(Triangle tri in triangles)
            {
                //Side 1
                Triangle triFromSide1 = tri.FindAdjacentTriangle(1);
                if (triFromSide1 == null || !triangles.Contains(triFromSide1))
                {
                    Edge edge1 = new Edge(tri.Point1, tri.Point2);
                    edges.Add(edge1);
                }
                //Side 2
                Triangle triFromSide2 = tri.FindAdjacentTriangle(2);
                if (triFromSide2 == null || !triangles.Contains(triFromSide2))
                {
                    Edge edge2 = new Edge(tri.Point2, tri.Point3);
                    edges.Add(edge2);
                }
                //Side 3
                Triangle triFromSide3 = tri.FindAdjacentTriangle(3);
                if (triFromSide3 == null || !triangles.Contains(triFromSide3))
                {
                    Edge edge3 = new Edge(tri.Point3, tri.Point1);
                    edges.Add(edge3);
                }
            }
            return edges;
        }

        public bool FindNextEdge(DTMPoint lastPoint,  List<Edge>masterEdgeList, out DTMPoint returnPoint, Edge startEdge)
		{
            returnPoint = null;
            foreach(var edge in masterEdgeList)
			{

                if (edge.Used || edge == startEdge)
                    continue;

                if (edge.Point1 == lastPoint)
                {
                    edge.Used = true;
                    returnPoint = edge.Point2;
                    return true;
                }
			}
            return false;

            
		}


        

		#endregion
		/// <summary>
		/// Attempts to find the point in the <see cref="DTM"/> that is closest to the provided <see cref="Ray"/>
		/// </summary>
		/// <param name="ray">The <see cref="Ray"/> to check for distance against</param>
		/// <returns>Returns a <see cref="DTMPoint"/> containing the closest point in the <see cref="DTM"/> or <see cref="null"/> if there are no points.</returns>
		public DTMPoint GetClosestPoint(Ray ray, bool CheckLayer = true)
        {
            if(Points?.Count <= 0)
                return null;

            DTMPoint closestPoint = null;
            double dist = 0, minDist = double.MaxValue;

            foreach(var p in Points)
            {
                if (p.Type == PointType.FalsePoint)
                    continue;

                if (CheckLayer && !this.IsOneOrMoreLayerVisible(p)) //Skip if all point's layers aren't shown
                    continue;

                dist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, p.X, p.Y, p.Z);
                if(dist < minDist)
                {
                    minDist = dist;
                    closestPoint = p;
                }
            }

            return closestPoint;
        }

        public DTMPoint GetClosestPoint3D(Point3D point, bool CheckForDuplicates = true , bool CheckAgainstMaxDist = false, double maxDist = 0, bool CheckLayer = true)
        {
            if (Points?.Count <= 0)
                return null;

            List<DTMPoint> closestPoints = new List<DTMPoint>();
            double dist = 0, minDist = double.MaxValue;

            foreach (var p in Points)
            {
                if (p.Type == PointType.FalsePoint)
                    continue;

                if (CheckLayer && !this.IsOneOrMoreLayerVisible(p)) //Skip if all point's layers aren't shown
                    continue;


                dist = MathsHelpers.Vector.Fn3DDistance(p.X, p.Y, p.Z, point.X, point.Y, point.Z);


                if (CheckAgainstMaxDist && dist>maxDist)
                {
                    //Distance is greater than the max distance so we skip this point
                    continue;
                }
                
                if (dist <= minDist)
                {
                    minDist = dist;
                }
            }

            foreach (var p in Points)
            {
                dist = MathsHelpers.Vector.Fn3DDistance(p.X, p.Y, p.Z, point.X, point.Y, point.Z);
                if (dist == minDist) { closestPoints.Add(p); }

            }

            if (closestPoints.Count == 0)
                return null;


            if (closestPoints.Count == 1 || !CheckForDuplicates)
            {
                return closestPoints[0];
            }
            else
            {
                NRG.Models.PickDuplicateDlg dlg = new NRG.Models.PickDuplicateDlg();
                dlg.Points = closestPoints;
                dlg.ShowDialog();
                return dlg.SelectedPoint;
            }
        }

        public Point3D GetModelIntersection(Ray ray, out Triangle IntersectionTri)
        {
            IntersectionTri = new Triangle();
            if (this.Triangles != null && this.Triangles.Count > 0)
            {
                Point3D closestIntersection = new Point3D(double.PositiveInfinity, double.PositiveInfinity, double .PositiveInfinity);
                foreach (Triangle tri in this.Triangles)
                {
                    Point3D pt = tri.RayIntersection(ray);
                    if (pt != null)
                    {
                        if (MathsHelpers.Vector.Fn3DDistance(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, pt.X, pt.Y, pt.Z) < MathsHelpers.Vector.Fn3DDistance(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, closestIntersection.X, closestIntersection.Y, closestIntersection.Z))
                        {
                            closestIntersection.X = pt.X;
                            closestIntersection.Y = pt.Y;
                            closestIntersection.Z = pt.Z;
                            IntersectionTri = tri;
                        }
                    }
                }
                if (closestIntersection.X < double.PositiveInfinity)
                {
                    return closestIntersection;
                }
            }
            return null;
        }
        public Triangle FindNearestTri(double ptX, double ptY)
        {
            return DTMHelpers.FindNearestTri(ptX, ptY, this.Triangles);
        }

        #region Get Closest Lines
        public Tuple<StandardLine, double> GetClosestStandardLine2D(Point2D point)
        {
            double closestDist = double.PositiveInfinity;
            double currentDistance = double.PositiveInfinity;
            StandardLine closestLine = null;
            foreach(StandardLine line in GetStandardLines())
            {               
                double offsetFromLine = 0;
                double distanceAlongLine = 0;
                NRG.MathsHelpers.Trig.DistanceAndOffsetFromLine(line.StartPoint.X, line.StartPoint.Y, line.EndPoint.X, line.EndPoint.Y, point.X, point.Y, ref distanceAlongLine, ref offsetFromLine);
                double lineDistance = Vector.FnDistance(line.StartPoint.X, line.StartPoint.Y, line.EndPoint.X, line.EndPoint.Y);

                if (distanceAlongLine < 0 ) //Test point is closest to the start node
                {
                    currentDistance = Vector.FnDistance(line.StartPoint.X, line.StartPoint.Y, point.X, point.Y);

                } 
                else if (distanceAlongLine > lineDistance) //Test point is closest to the end node
                {
                    currentDistance = Vector.FnDistance(line.EndPoint.X, line.EndPoint.Y, point.X, point.Y);
                }
                else //Test point is between start and end node, return the offset
                {
                    currentDistance = offsetFromLine;
                }

                if (Math.Abs(currentDistance) < Math.Abs(closestDist))
                {
                    closestDist = currentDistance;
                    closestLine = line;
                }
            }


            if (closestLine != null)
            {
                //We don't want to return anything if the closest distance/line found is smaller than the total line length
                double lineDistance = Vector.FnDistance(closestLine.StartPoint.X, closestLine.StartPoint.Y, closestLine.EndPoint.X, closestLine.EndPoint.Y); //Get the line distance again
                //Final validation of the line. (Minimum bounds check)
                if (Math.Abs(closestDist) > Math.Abs(lineDistance)) { closestLine = null; closestDist = double.PositiveInfinity; }
            }


            return Tuple.Create(closestLine, Math.Abs(closestDist));
        }

        public Tuple<PolyLine, double> GetClosestPolyLine2D(Point2D point)
        {
            double closestDist = double.PositiveInfinity;
            double currentDistance = double.PositiveInfinity;
            PolyLine closestLine = null;
            foreach (PolyLine line in GetPolyLines())
            {
                for (int i = 0; i < line.Nodes.Count - 1; i++)
                {
                    DTMPoint sp = line.Nodes[i];
                    DTMPoint ep = line.Nodes[i + 1];

                    double offsetFromLine = 0;
                    double distanceAlongLine = 0;
                    NRG.MathsHelpers.Trig.DistanceAndOffsetFromLine(sp.X, sp.Y, ep.X, ep.Y, point.X, point.Y, ref distanceAlongLine, ref offsetFromLine);
                    double lineDistance = Vector.FnDistance(sp.X, sp.Y, ep.X, ep.Y);

                    if (distanceAlongLine < 0) //Test point is closest to the start node
                    {
                        currentDistance = Vector.FnDistance(sp.X, sp.Y, point.X, point.Y);
                    }
                    else if (distanceAlongLine > lineDistance) //Test point is closest to the end node
                    {
                        currentDistance = Vector.FnDistance(ep.X, ep.Y, point.X, point.Y);
                    }
                    else //Test point is between start and end node, return the offset
                    {
                        currentDistance = offsetFromLine;
                    }
                }

                if(Math.Abs(currentDistance) < Math.Abs(closestDist))
                {
                    closestDist = currentDistance;
                    closestLine = line;
                }
            }

            return Tuple.Create(closestLine, Math.Abs(closestDist));
        }

        public Tuple<Arc, double> GetClosestArc2D(Point2D point)
        {
            double closestDist = double.PositiveInfinity;
            double currentDistance = double.PositiveInfinity;
            Arc closestArc = null;
            foreach (Arc arc in GetArcs())
            {
                DistanceAndOffset dao = arc.GetDistanceAndOffset(point);
                
                if (dao.Distance == double.NegativeInfinity) //Test point is closest to the start node
                {
                    double distToStartNode = Math.Abs(Vector.FnDistance(arc.StartPoint.X, arc.StartPoint.Y, point.X, point.Y));
                    double distToEndNode = Math.Abs(Vector.FnDistance(arc.EndPoint.X, arc.EndPoint.Y, point.X, point.Y));

                    if (distToStartNode > distToEndNode) { currentDistance = distToEndNode; }
                    else { currentDistance = distToStartNode; }

                }
                else //Test point is between start and end node, return the offset
                {
                    currentDistance = dao.Offset;
                }

                if (Math.Abs(currentDistance) < Math.Abs(closestDist))
                {
                    closestDist = currentDistance;
                    closestArc = arc;
                }
            }


            if (closestArc != null)
            {
                //We don't want to return anything if the closest distance/line found is smaller than the total line length
                double lineDistance = Vector.FnDistance(closestArc.StartPoint.X, closestArc.StartPoint.Y, closestArc.EndPoint.X, closestArc.EndPoint.Y); //Get the line distance again
                //Final validation of the line. (Minimum bounds check)
                if (Math.Abs(closestDist) > Math.Abs(lineDistance)) { closestArc = null; closestDist = double.PositiveInfinity; }
            }


            return Tuple.Create(closestArc, Math.Abs(closestDist));
        }

        public Tuple<LineFeatureItem,double> GetClosestLineFeatureItem(Point2D point)//not tested yet :) 07.07.22 TN
        {
            double closestDist = double.PositiveInfinity;
            double currentDistance = double.PositiveInfinity;
            LineFeatureItem closestLine = null;
            
            
            foreach (LineFeatureItem lineFeatureItem in GetAllLineFeatureItems())
            {
                if (lineFeatureItem.Code.Contains("336"))
                {
                    Console.WriteLine("");
                }

                for (int i = 0; i < lineFeatureItem.Points.Count; i++)
                {
                    DTMPoint sp = null;
                    DTMPoint ep = null;
                   

                    #region Get Start and End Point of Segment to Test
                    if (i == lineFeatureItem.Points.Count -1) //If we're at the last point in the list
                    {
                        if (lineFeatureItem.Closed) //If the featureItem is closed, we loop to the start
                        {
                            sp = lineFeatureItem.Points[i];
                            ep = lineFeatureItem.Points[0];
                        }
                        else //Othewise we can finish here
                        {
                            continue;
                        }
                    }
                    else //We're not yet at the last point in the list, just use index and index+1
                    {
                        sp = lineFeatureItem.Points[i];
                        ep = lineFeatureItem.Points[i + 1];
                    }
                    #endregion

                    double offsetFromLine = 0;
                    double distanceAlongLine = 0;
                    NRG.MathsHelpers.Trig.DistanceAndOffsetFromLine(sp.X, sp.Y, ep.X, ep.Y, point.X, point.Y, ref distanceAlongLine, ref offsetFromLine); //Find the dist and offset of test point
                    double lineDistance = Vector.FnDistance(sp.X, sp.Y, ep.X, ep.Y); //Get the line distance

                    //===========================================================================================
                    //Added by ES:17.11.22 - replaces the gibberish below:
                    if (distanceAlongLine >= 0 && distanceAlongLine <= lineDistance)
                    {
                        if (Math.Abs(offsetFromLine) < Math.Abs( currentDistance))
                        {
                            currentDistance = offsetFromLine;
                        }
                    }
                    //===========================================================================================
                    //Commented out by ES:17.11.22 - not sure what this lot was all about, but it was wrong!
                    #region commented out original code
                    //if (sp.PointID == "7" && ep.PointID == "8")
                    //{
                    //    Console.WriteLine("");
                    //}

                    //if (Math.Abs( distanceAlongLine) < 0) //Test point is closest to the start node
                    //{
                    //    currentDistance = Vector.FnDistance(sp.X, sp.Y, point.X, point.Y);
                    //}
                    //else if (Math.Abs(distanceAlongLine) > lineDistance) //Test point is closest to the end node
                    //{
                    //    currentDistance = Vector.FnDistance(ep.X, ep.Y, point.X, point.Y);
                    //}
                    //else //Test point is between start and end node, return the offset
                    //{
                    //    currentDistance = offsetFromLine;
                    //}
                    #endregion commented out original code
                    //===========================================================================================
                }

                if (Math.Abs(currentDistance) < Math.Abs(closestDist))
                {
                    closestDist = currentDistance;
                    closestLine = lineFeatureItem;
                }
            }
                
            

            return Tuple.Create(closestLine, Math.Abs(closestDist));
        }

        public object GetClosestLine(Point2D point)
        {
            object returnLine = null;
            double closestDist = double.PositiveInfinity;
            //Find the closest line of each type we currently have
            Tuple<Arc, double> arcTuple = GetClosestArc2D(point);
            Tuple<PolyLine, double> polyTuple = GetClosestPolyLine2D(point);
            Tuple<StandardLine, double> lineTuple = GetClosestStandardLine2D(point);
            Tuple<LineFeatureItem, double> lineFeatureTuple = GetClosestLineFeatureItem(point);

            //Compare each closest distance (item2) of the tuple to find the ultimate closest line
            if (arcTuple.Item2 != double.NegativeInfinity && arcTuple.Item2 < closestDist) { closestDist = arcTuple.Item2; returnLine = arcTuple.Item1; }
            if (polyTuple.Item2 != double.NegativeInfinity && polyTuple.Item2 < closestDist) { closestDist = polyTuple.Item2; returnLine = polyTuple.Item1; }
            if (lineTuple.Item2 != double.NegativeInfinity && lineTuple.Item2 < closestDist) { closestDist = lineTuple.Item2; returnLine = lineTuple.Item1; }
            if (lineFeatureTuple.Item2 != double.NegativeInfinity && lineFeatureTuple.Item2 < closestDist) { closestDist = lineFeatureTuple.Item2; returnLine = lineFeatureTuple.Item1; }

            return returnLine;
        }

        #endregion


        #endregion
    }

    #region Surfaces

    public class TriangleSurface
    {
        #region Properties

        public int ID { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public bool Volume { get; set; }
        public bool Contour { get; set; }
        public double Dip { get; set; }
        public string Name { get; set; }
        public FillType FillType { get; set; }
        public bool Locked { get; set; }
        
        public HashSet<Triangle> Triangles { get; set; }
        public SharedRenderItem RenderItem { get; set; }

        #endregion

        #region Setup

        public TriangleSurface()
        {
            ID = 0;
            R = 128;
            G = 128;
            B = 128;
            Volume = true;
            Contour = true;
            Dip = 0;
            Name = "";
            Locked = false;
        }

        #endregion

        #region Methods

        public void AddTriangle(Triangle triangle)
        {
            triangle.Surface = this;

            if (Triangles == null)
                Triangles = new HashSet<Triangle>();

            Triangles.Add(triangle);
        }

        public void RemoveTriangle(Triangle triangle)
        {
            if (triangle == null)
                return;

           // triangle.Surface = null;

            if(Triangles != null && Triangles.Remove(triangle))
            {
                if (Triangles.Count <= 0)
                    Triangles = null;
            }
        }

        public void RemoveTriangles(HashSet<Triangle> triangles)
		{
            if (triangles == null)
                return;

            foreach (var tri in triangles)
                tri.Surface = null;

            if (Triangles == null)
                return;

            Triangles = new HashSet<Triangle>(Triangles.Except(triangles.ToList()));
		}

        #endregion
    }

    #endregion

    #region Triangle

    public class JunctionTriangle : Triangle
    {
        public Triangle Triangle { get; set;}
        public int FirstExitSide { get; set;}
        public int SecondExitSide { get; set;}

        #region setup
        public JunctionTriangle(Triangle Tri, int Exit1, int Exit2)
        {
            this.Triangle = Tri;
            FirstExitSide = Exit1;
            SecondExitSide = Exit2;
        }
        #endregion region setup
    }

    public class Triangle
    {
        #region Properties

        public TriangleSurface Surface { get; set; }
        public DTMPoint Point1 { get; set; }
        public DTMPoint Point2 { get; set; }
        public DTMPoint Point3 { get; set; }
        public int ID { get; set; }

        public enum Triangle_Direction
        { 
            CCW = 0,
            CW = 1
        }
        #endregion

        #region Setup

        public Triangle()
        {

        }

        public Triangle(TriangleSurface surface, DTMPoint point1, DTMPoint point2, DTMPoint point3)
        {
            surface?.AddTriangle(this);
            Point1 = point1;
            point1?.AddTriangle(this);

            Point2 = point2;
            point2?.AddTriangle(this);

            Point3 = point3;
            point3?.AddTriangle(this);
        }

        #endregion

        public double MaxHeight()
        {
            double outHeight = Point1.Z;
            if (Point2.Z > outHeight)
                outHeight = Point2.Z;
            if (Point3.Z > outHeight)
                outHeight = Point3.Z;
            return outHeight;
        }

        public double MinHeight()
        {
            double outHeight = Point1.Z;
            if (Point2.Z < outHeight)
                outHeight = Point2.Z;
            if (Point3.Z < outHeight)
                outHeight = Point3.Z;
            return outHeight;
        }

        public double PlanArea()
        {
            return MathsHelpers.Triangle.CalcTriPlanArea(Point1.X, Point1.Y, Point2.X, Point2.Y, Point3.X, Point3.Y);
        }

        public double SurfaceArea()
        {
            return MathsHelpers.Triangle.CalcTriSurfaceArea(Point1.X, Point1.Y, Point1.Z, Point2.X, Point2.Y, Point2.Z, Point3.X, Point3.Y, Point3.Z);
        }

        public Triangle_Direction ValidateTriangleDirection()
        {
            if (MathsHelpers.Vector.FNRightOf(Point1.X, Point1.Y, Point2.X, Point2.Y, Point3.X, Point3.Y))
            {
                return Triangle_Direction.CW;
            }
            else
            {
                return Triangle_Direction.CCW;
            }
        }

        public List<Point3D> FindIntersections(Point3D startPoint, Point3D endPoint)
        {
            List<Point3D> retList = new List<Point3D>();
 
            if (this.PointInTri(startPoint.X, startPoint.Y) )
            {
                Point3D pt = startPoint;
                pt.Z = CalcLevelOnPlane(startPoint.X, startPoint.Y);
                retList.Add(pt);
            }
            if (this.PointInTri(endPoint.X, endPoint.Y))
            {
                Point3D pt = endPoint;
                pt.Z = CalcLevelOnPlane(endPoint.X, endPoint.Y);
                retList.Add(pt);
            }

            if (retList.Count > 1)
            { return retList; }

            double ipx = 0;
            double ipy = 0;

            if (MathsHelpers.Intersection.IntersectLines(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y, Point1.X, Point1.Y, Point2.X, Point2.Y, ref ipx, ref ipy))
            {
                Point3D pt = new Point3D();
                pt.X = ipx;
                pt.Y = ipy;
                pt.Z = CalcLevelOnPlane(pt.X, pt.Y);
                retList.Add(pt);
            }

            if (retList.Count > 1) { return retList; }

            if (MathsHelpers.Intersection.IntersectLines(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y, Point2.X, Point2.Y, Point3.X, Point3.Y, ref ipx, ref ipy))
            {
                Point3D pt = new Point3D();
                pt.X = ipx;
                pt.Y = ipy;
                pt.Z = CalcLevelOnPlane(pt.X, pt.Y);
                retList.Add(pt);
            }

            if (retList.Count > 1) { return retList; }


            if (MathsHelpers.Intersection.IntersectLines(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y, Point3.X, Point3.Y, Point1.X, Point1.Y, ref ipx, ref ipy))
            {
                Point3D pt = new Point3D();
                pt.X = ipx;
                pt.Y = ipy;
                pt.Z = CalcLevelOnPlane(pt.X, pt.Y);
                retList.Add(pt);
            }
            return retList; 
        }

		#region Methods
		/// <summary>
		/// Calculate whether a line passes through a triangle in 2D
		/// </summary>
		/// <param name="startPt">start of the line</param>
		/// <param name="endPt">end of the line</param>
		/// <returns>True if the line interects with the triangle - False else</returns>
		public bool LineThrough(Point2D startPt, Point2D endPt, bool includeNodes = true)
		{

			double ipx = double.PositiveInfinity;
			double ipy = double.PositiveInfinity;
            if (includeNodes)
            {
                if (this.PointInTri(startPt.X, startPt.Y) || this.PointInTri(endPt.X, endPt.Y)) { return true; } //one or both of the points on the line lies within the triangle so there must be an intersection
            }
            if (MathsHelpers.Intersection.IntersectLines(startPt.X, startPt.Y, endPt.X, endPt.Y, Point1.X, Point1.Y, Point2.X, Point2.Y, ref ipx, ref ipy))
            {   if (includeNodes)
                {
                    return true;
                }
                else
                {
                    if (MathsHelpers.Vector.FnDistance(Point1.X, Point1.Y, ipx, ipy) > 0.001 && MathsHelpers.Vector.FnDistance(Point2.X, Point2.Y, ipx, ipy) > 0.001)
                    {
                        return true;
                    }
                }
			}
			if (MathsHelpers.Intersection.IntersectLines(startPt.X, startPt.Y, endPt.X, endPt.Y, Point2.X, Point2.Y, Point3.X, Point3.Y, ref ipx, ref ipy))
			{
                if (includeNodes)
                {
                    return true;
                }
                else
                {
                    if (MathsHelpers.Vector.FnDistance(Point2.X, Point2.Y, ipx, ipy) > 0.001 && MathsHelpers.Vector.FnDistance(Point3.X, Point3.Y, ipx, ipy) > 0.001)
                    {
                        return true;
                    }
                }
            }
			if (MathsHelpers.Intersection.IntersectLines(startPt.X, startPt.Y, endPt.X, endPt.Y, Point1.X, Point1.Y, Point3.X, Point3.Y, ref ipx, ref ipy))
			{
                if (includeNodes)
                {
                    return true;
                }
                else
                {
                    if (MathsHelpers.Vector.FnDistance(Point1.X, Point1.Y, ipx, ipy) > 0.001 && MathsHelpers.Vector.FnDistance(Point3.X, Point3.Y, ipx, ipy) > 0.001)
                    {
                        return true;
                    }
                }
                
			}
			return false;
		}

        public bool LineThrough(DTMPoint startPt, DTMPoint endPt, bool includeNodes = true)
        {
            double ipx = double.PositiveInfinity;
            double ipy = double.PositiveInfinity;
            if (includeNodes)
            {
                if (this.PointInTri(startPt.X, startPt.Y) || this.PointInTri(endPt.X, endPt.Y)) { return true; } //one or both of the points on the line lies within the triangle so there must be an intersection
            }
            if (MathsHelpers.Intersection.IntersectLines(startPt.X, startPt.Y, endPt.X, endPt.Y, Point1.X, Point1.Y, Point2.X, Point2.Y, ref ipx, ref ipy))
            {
                if (includeNodes)
                {
                    return true;
                }
                else
                {
                    if (startPt != Point1 && startPt != Point2 && endPt != Point1 && endPt != Point2)
                    {
                        return true;
                    }
                }
            }
            if (MathsHelpers.Intersection.IntersectLines(startPt.X, startPt.Y, endPt.X, endPt.Y, Point2.X, Point2.Y, Point3.X, Point3.Y, ref ipx, ref ipy))
            {
                if (includeNodes)
                {
                    return true;
                }
                else
                {
                    if (startPt != Point2 && startPt != Point3 && endPt != Point2 && endPt != Point3)
                    {
                        return true;
                    }
                }
            }
            if (MathsHelpers.Intersection.IntersectLines(startPt.X, startPt.Y, endPt.X, endPt.Y, Point1.X, Point1.Y, Point3.X, Point3.Y, ref ipx, ref ipy))
            {
                if (includeNodes)
                {
                    return true;
                }
                else
                {
                    if (startPt != Point1 && startPt != Point3 && endPt != Point1 && endPt != Point3)
                    {
                        return true;
                    }
                }

            }
            return false;
        }


        /// <summary>
        /// Will return the edge the line exits from. If the line is parralel with an edge, it returns that edge, with point 2
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <returns></returns>
        public (DTMPoint, Edge) FindExitNode(DTMPoint startPt, DTMPoint endPt, List<(DTMPoint, DTMPoint)> UsedEdges)
        {
            double ipx = double.PositiveInfinity;
            double ipy = double.PositiveInfinity;
            double exitIntersectionX = double.PositiveInfinity;
            double exitIntersectionY = double.PositiveInfinity;
            List<DTMPoint> edgePoints = new List<DTMPoint>();
            bool lineIsParralelToAnEdge = false;

            #region Check if the line runs along and of the edges
            //A line may be parallel to an edge, in which case we want add both points to the edgePoints list and we can skip the intersection part
            if (NRG.MathsHelpers.Vector.FnOnLine3(startPt.X, startPt.Y, endPt.X, endPt.Y, Point1.X, Point1.Y) == 0) { edgePoints.Add(Point1); }
            if (NRG.MathsHelpers.Vector.FnOnLine3(startPt.X, startPt.Y, endPt.X, endPt.Y, Point2.X, Point2.Y) == 0) { edgePoints.Add(Point2); }
            if (NRG.MathsHelpers.Vector.FnOnLine3(startPt.X, startPt.Y, endPt.X, endPt.Y, Point3.X, Point3.Y) == 0) { edgePoints.Add(Point3); }



            if (edgePoints.Count > 1) 
            {
                #region Check if edge has been tested previously
                //Check previous edges and see if we have already used this pairing
                bool unusedEdge = true;
                foreach (var edgePair in UsedEdges)
                {
                    if ((edgePoints[0] == edgePair.Item1 || edgePoints[0] == edgePair.Item2) && (edgePoints[1] == edgePair.Item1 || edgePoints[1] == edgePair.Item2))
                    {
                        unusedEdge = false;
                        break;
                    }
                }

                if (!unusedEdge)
                {
                    return (null, null);
                }
                #endregion

                lineIsParralelToAnEdge = true;
            }
            else { edgePoints.Clear(); } //Clear the edge points as it may just contain a single node, which we don't want to include

            #endregion

            if (lineIsParralelToAnEdge == false)
            {
                #region Find the points which the line exits from
                //Test each edge and see if the vector crosses it. If the intersection is within a tolerance (0.001), then this is a node (possibly starting node) and we can ignore it
                
                void CheckForIntersection(DTMPoint pointA, DTMPoint pointB)
                {
                    if (MathsHelpers.Intersection.IntersectLines(startPt.X, startPt.Y, endPt.X, endPt.Y, pointA.X, pointA.Y, pointB.X, pointB.Y, ref ipx, ref ipy))
                    {
                        if (MathsHelpers.Vector.FnDistance(pointA.X, pointA.Y, ipx, ipy) > 0.001 && MathsHelpers.Vector.FnDistance(pointB.X, pointB.Y, ipx, ipy) > 0.001)
                        {
                            //Check previous edges and see if we have already used this pairing
                            bool unusedEdge = true;
                            foreach (var edgePair in UsedEdges)
                            {
                                if ((pointA == edgePair.Item1 || pointA == edgePair.Item2) && (pointB == edgePair.Item1 || pointB == edgePair.Item2))
                                {
                                    unusedEdge = false;
                                    break;
                                }
                            }
                            if (unusedEdge)
                            {
                                edgePoints.Add(pointA);
                                edgePoints.Add(pointB);
                                exitIntersectionX = ipx;
                                exitIntersectionY = ipy;
                            }
                        }
                    }
                }
                CheckForIntersection(Point1, Point2);
                CheckForIntersection(Point2, Point3);
                CheckForIntersection(Point3, Point1);
                #endregion
            }

            #region Get the triangle node which is closest to the end point
            //If no intersection was found it might be because the line is parallel to an edge, in which case we can use the endPt here.
            if (exitIntersectionX == double.PositiveInfinity && exitIntersectionY == double.PositiveInfinity)
            {
                exitIntersectionX = endPt.X;
                exitIntersectionY = endPt.Y;
            }

            //Check we have a valid exit edge. If not, check if we're in the final triangle
            if (edgePoints.Count < 2) 
            { 
                if (Point1 == endPt || Point2 == endPt || Point3 == endPt || this.PointInTri(endPt.X, endPt.Y)) 
                { //This is the final triangle

                    return (endPt, null);
                }
                return (null, null); 
            }

            //Go through each intersection (if there are more than 2 then something is up)
            //and find the closest triangle edge point to the exit vector intersection
            double distance1 = NRG.MathsHelpers.Vector.FnDistance(exitIntersectionX, exitIntersectionY, edgePoints[0].X, edgePoints[0].Y );
            double distance2 = NRG.MathsHelpers.Vector.FnDistance(exitIntersectionX, exitIntersectionY, edgePoints[1].X, edgePoints[1].Y);

            DTMPoint closestPoint = null;
            Edge exitEdge = null;

            //Return the triangle node closest to the endPt. Also return the edge the vector crosses. Format the edge so Point1 of the edge is the side closest to the end point
            if (distance1 < distance2)
            {
                closestPoint = edgePoints[0];
                exitEdge = new Edge(closestPoint, edgePoints[1]);
            }
            else
            {
                closestPoint = edgePoints[1];
                exitEdge = new Edge(closestPoint, edgePoints[0]);
            }
            
            #endregion
            

            return (closestPoint, exitEdge);
        }


        

        /// <summary>
        /// Find the closest vector inside a triangle to that of the passed point. Only checks distances in plan.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Point2D GetClosestPoint(Point2D point)
        {
            Point3D closestPoint = new Point3D();
            List<Point2D> intersectionPoints = new List<Point2D>();

            //If point is inside the triangle in Plan, we just want to find the level of that point in the triangle
            if (this.PointInTri(point.X, point.Y))
            {
                closestPoint.X = point.X;
                closestPoint.Y = point.Y;
                return closestPoint;
            }

            double ipX = 0;
            double ipY = 0;

            #region Line Ends @ Point 1
            //point > Point1 X Point1 > Point2
            if (Intersection.IntersectLines(point.X, point.Y, this.Point1.X, this.Point1.Y, this.Point1.X, this.Point1.Y, this.Point2.X, this.Point2.Y, ref ipX, ref ipY))
            {
                intersectionPoints.Add(new Point2D(ipX, ipY));
            }
            //point > Point1 X Point2 > Point3
            if (Intersection.IntersectLines(point.X, point.Y, this.Point1.X, this.Point1.Y, this.Point2.X, this.Point2.Y, this.Point3.X, this.Point3.Y, ref ipX, ref ipY))
            {
                intersectionPoints.Add(new Point2D(ipX, ipY));
            }
            //point > Point1 X Point3 > Point1
            if (Intersection.IntersectLines(point.X, point.Y, this.Point1.X, this.Point1.Y, this.Point3.X, this.Point3.Y, this.Point1.X, this.Point1.Y, ref ipX, ref ipY))
            {
                intersectionPoints.Add(new Point2D(ipX, ipY));
            }
            #endregion
            #region Line Ends @ Point 2
            //point > Point2 X Point1 > Point2
            if (Intersection.IntersectLines(point.X, point.Y, this.Point2.X, this.Point2.Y, this.Point1.X, this.Point1.Y, this.Point2.X, this.Point2.Y, ref ipX, ref ipY))
            {
                intersectionPoints.Add(new Point2D(ipX, ipY));
            }
            //point > Point2 X Point2 > Point3
            if (Intersection.IntersectLines(point.X, point.Y, this.Point2.X, this.Point2.Y, this.Point2.X, this.Point2.Y, this.Point3.X, this.Point3.Y, ref ipX, ref ipY))
            {
                intersectionPoints.Add(new Point2D(ipX, ipY));
            }
            //point > Point2 X Point3 > Point1
            if (Intersection.IntersectLines(point.X, point.Y, this.Point2.X, this.Point2.Y, this.Point3.X, this.Point3.Y, this.Point1.X, this.Point1.Y, ref ipX, ref ipY))
            {
                intersectionPoints.Add(new Point2D(ipX, ipY));
            }
            #endregion
            #region Line Ends @ Point 3
            //point > Point3 X Point1 > Point2
            if (Intersection.IntersectLines(point.X, point.Y, this.Point3.X, this.Point3.Y, this.Point1.X, this.Point1.Y, this.Point2.X, this.Point2.Y, ref ipX, ref ipY))
            {
                intersectionPoints.Add(new Point2D(ipX, ipY));
            }
            //point > Point3 X Point2 > Point3
            if (Intersection.IntersectLines(point.X, point.Y, this.Point3.X, this.Point3.Y, this.Point2.X, this.Point2.Y, this.Point3.X, this.Point3.Y, ref ipX, ref ipY))
            {
                intersectionPoints.Add(new Point2D(ipX, ipY));
            }
            //point > Point3 X Point3 > Point1
            if (Intersection.IntersectLines(point.X, point.Y, this.Point3.X, this.Point3.Y, this.Point3.X, this.Point3.Y, this.Point1.X, this.Point1.Y, ref ipX, ref ipY))
            {
                intersectionPoints.Add(new Point2D(ipX, ipY));
            }
            #endregion

            //Now compare all intersections point distances to find the closest
            double minDistance = double.PositiveInfinity;
            foreach (var ip in intersectionPoints)
            {
                double dist = NRG.MathsHelpers.Vector.FnDistance(ip.X, ip.Y, point.X, point.Y);
                if (dist < minDistance) 
                { 
                    minDistance = dist;
                    closestPoint.X = ip.X;
                    closestPoint.Y = ip.Y;
                }
            }
            return closestPoint;
        }

        public bool PolyLineThrough(List<Point3D> pts)
        {
            List<Point2D> pt2DList = new List<Point2D>();
            foreach(var pt3d in pts)
            {
                Point2D p2d = new Point2D();
                //========================================
                //Modified by ES:16.11.22 - replaced:
                //pt3d.X = p2d.X;
                //pt3d.Y = p2d.Y;
                //With:
                p2d.X= pt3d.X;
                p2d.Y = pt3d.Y;
                //========================================
                pt2DList.Add(p2d);
            }
            return PolyLineThrough(pt2DList);
        }

        public bool PolyLineThrough(List<Point2D> pts)
		{
			if (pts != null && pts.Count > 1)
			{
				for (int i = 0; i < pts.Count - 1; i++)
				{
					Point2D thisPt = pts.ElementAt(i);
					Point2D nextPt = pts.ElementAt(i+1);
					if (LineThrough(thisPt, nextPt))
					{ return true; }
				}
			}
			return false;

		}

        public bool TriangleInPoly(List<Point3D> pts)
        {
            List<Point2D> pt2DList = new List<Point2D>();
            foreach (var pt3d in pts)
            {
                Point2D p2d = new Point2D();
                pt3d.X = p2d.X;
                pt3d.Y = p2d.Y;
                pt2DList.Add(p2d);
            }
            return TriangleInPoly(pt2DList);
        }

        public bool TriangleInPoly(List<Point2D> pts)
        {

            if (MathsHelpers.Geometry.InPoly2D(pts, Point1.X, Point1.Y) && MathsHelpers.Geometry.InPoly2D(pts, Point2.X, Point2.Y) && MathsHelpers.Geometry.InPoly2D(pts, Point3.X, Point3.Y))
            {
                return true;
            }
            return false;
        }

        public Volumes.VolumeResult CalculateVolume(DTM OtherModel)
        {

            Volumes.VolumeResult outvol = new Volumes.VolumeResult();

            double zb1 = 0, zb2 = 0, zb3 = 0;

            //=========================================================================================
            //for the time being just ignore the triangle if if doesn't lie wholly with the other model
            zb1 = OtherModel.CalcLevelFromTriangles(this.Point1.X,this.Point1.Y);
            if (zb1 == -999)
                return outvol;
            zb2 = OtherModel.CalcLevelFromTriangles(this.Point2.X, this.Point2.Y);
            if (zb2 == -999)
                return outvol;
            zb3= OtherModel.CalcLevelFromTriangles(this.Point3.X, this.Point3.Y);
            if (zb3 == -999)
                return outvol;
            //=========================================================================================

            Volumes.VolumeTriangle tri = new Volumes.VolumeTriangle();
            tri.Vector1.X = Point1.X;
            tri.Vector2.X = Point2.X;
            tri.Vector3.X = Point3.X;

            tri.Vector1.Y = Point1.Y;
            tri.Vector2.Y = Point2.Y;
            tri.Vector3.Y = Point3.Y;

            tri.Vector1.Z = Point1.Z;
            tri.Vector2.Z = Point2.Z;
            tri.Vector3.Z = Point3.Z;

            tri.Vector1.ZB = zb1;
            tri.Vector2.ZB = zb2;
            tri.Vector3.ZB = zb3;

            return tri.GetCutAndFill();
        }

        public void UpdateSurface(TriangleSurface newSurface)
        {
            if (Surface.Triangles.Contains(this))
                Surface.Triangles.Remove(this);

			if (newSurface.Triangles == null)
				newSurface.Triangles = new HashSet<Triangle>();

            newSurface.Triangles.Add(this);
			
            Surface = newSurface;
        }

        public bool IntersectsWith(Triangle triangle)
        {
            if (triangle == null || triangle.Point1 == null || triangle.Point2 == null || triangle.Point3 == null)
                return false;

            double ipX = 0, ipY = 0;

            //If there is a difference
            if(Point1 != triangle.Point1 && Point1 != triangle.Point2 && Point2 != triangle.Point1 && Point2 != triangle.Point2)
            {
                if (Intersection.IntersectLines(Point1.X, Point1.Y, Point2.X, Point2.Y, triangle.Point1.X, triangle.Point1.Y, triangle.Point2.X, triangle.Point2.Y, ref ipX, ref ipY))
                    return true;
            }

            if(Point2 != triangle.Point2 && Point2 != triangle.Point3 && Point3 != triangle.Point2 && Point3 != triangle.Point3)
            {
                if (Intersection.IntersectLines(Point2.X, Point2.Y, Point3.X, Point3.Y, triangle.Point2.X, triangle.Point2.Y, triangle.Point3.X, triangle.Point3.Y, ref ipX, ref ipY))
                    return true;
            }

            if(Point3 != triangle.Point3 && Point3 != triangle.Point1 && Point1 != triangle.Point3 && Point1 != triangle.Point1)
            {
                if (Intersection.IntersectLines(Point3.X, Point3.Y, Point1.X, Point1.Y, triangle.Point3.X, triangle.Point3.Y, triangle.Point1.X, triangle.Point1.Y, ref ipX, ref ipY))
                    return true;
            }

            //If all 3 points are the same there is an intersection
            return false;
        }

        #region RayTracing
        private const double Epsilon = 0.000001d;

        public  Point3D RayIntersection(Ray ray)
        {
            Point3D edge1 = new Point3D();
            Point3D edge2 = new Point3D();

            Point3D tvec = new Point3D();
            Point3D pvec = new Point3D();
            Point3D qvec = new Point3D();

            double det, invDet;

            edge1 = this.Point2 - this.Point1;
            edge2 = this.Point3 - this.Point1;

            pvec = Cross(ray.Direction, edge2);

            det = Dot(edge1, pvec);

            if (det > -Epsilon && det < Epsilon)
            {
                return null;
            }

            invDet = 1d / det;

            tvec = ray.Origin - this.Point1;

            double t, u, v;

            u = Dot(tvec, pvec) * invDet;

            if (u < 0 || u > 1)
            {
                return null;
            }

            qvec = Cross(tvec, edge1);

            v = Dot(ray.Direction, qvec) * invDet;

            if (v < 0 || u + v > 1)
            {
                return null;
            }

            t = Dot(edge2, qvec) * invDet;

            return GetTrilinearCoordinateOfTheHit(t, ray);
        }

        private  double Dot(Point3D v1, Point3D v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        private  Point3D Cross(Point3D v1, Point3D v2)
        {
            Point3D dest = new Point3D();

            dest.X = v1.Y * v2.Z - v1.Z * v2.Y;
            dest.Y = v1.Z * v2.X - v1.X * v2.Z;
            dest.Z = v1.X * v2.Y - v1.Y * v2.X;

            return dest;
        }

        public static Point3D GetTrilinearCoordinateOfTheHit(double t, Ray ray)
        {
            return ray.Direction * t + ray.Origin;
        }
        #endregion ray intersection

        #region level calcs

        /// <summary>
        /// GetNextSearchTri - helper when trying to locate the triangle that a given point sits within
        /// </summary>
        /// <param name="X">X coord of the point we're looking for</param>
        /// <param name="Y">Y coord of the point we're looking for</param>
        /// <returns>The next triangle to check</returns>
        public Triangle GetNextSearchTri(double X, double Y, Triangle AvoidTri = null)
        {
            DTMPoint p1 = new DTMPoint();
            DTMPoint p2 = new DTMPoint();
            Triangle testTri = new Triangle();
            //Triangle should have 3 distinct points or it is not valid
            if((Point1.X == Point2.X && Point1.Y == Point2.Y) || (Point2.X == Point3.X && Point2.Y == Point3.Y) || (Point1.X == Point3.X && Point1.Y == Point3.Y))
            {
                
                return null;
            }
            else if (MathsHelpers.Vector.FNRightOf(Point1.X, Point1.Y, Point2.X, Point2.Y, X, Y))
            {
                p1 = Point1;p2 = Point2;
                testTri=  this.FindAdjacentTriangle(1);
                if (AvoidTri == null || testTri != AvoidTri)
                    return testTri;
            }
            else if (MathsHelpers.Vector.FNRightOf(Point2.X, Point2.Y, Point3.X, Point3.Y, X, Y))
            {
                p1 = Point2; p2 = Point3;
                testTri = this.FindAdjacentTriangle(2);
                if (AvoidTri == null || testTri != AvoidTri)
                    return testTri;
            }
            else if (MathsHelpers.Vector.FNRightOf(Point3.X, Point3.Y, Point1.X, Point1.Y, X, Y))
            {
                p1 = Point3; p2 = Point1;
                testTri = this.FindAdjacentTriangle(3);
                if (AvoidTri == null || testTri != AvoidTri)
                    return testTri;
            }
            else
                return null; //it's this triangle or we're lost

            if (p1.Triangles == null)
                return null;

            foreach (Triangle t in p1.Triangles)
            {
                if (t == this)
                    continue;

                if (p2.Triangles == null)
                    return null;

                foreach (Triangle t2 in p2.Triangles)
                {
                    if (t == t2 && (AvoidTri == null || t != AvoidTri ))
                        return t;
                }
            }

            return null; 
        }

        private BindingList<DTMPoint> SectionTriangle(Point3D StartPoint, Point3D EndPoint)

        {
            Point3D ip1 = MathsHelpers.Intersection.IntersectLines(StartPoint,EndPoint, this.Point1,this.Point1, 1);
            Point3D ip2 = MathsHelpers.Intersection.IntersectLines(StartPoint,EndPoint, this.Point2, this.Point2, 1);
            
            

            return null;
        
        }

        /// <summary>
        /// AdjacentTriangle - find the triangle which is adjacent to this given the input side
        /// </summary>
        /// <param name="Side">the side of the triangle to find</param>
        /// <returns>The adjacent triangle or null if on hull</returns>
        public Triangle FindAdjacentTriangle(int Side)
        {
            DTMPoint p1 = new DTMPoint();
            DTMPoint p2 = new DTMPoint();
            switch (Side)
            {
                case 1:
                    p1 = Point1; p2 = Point2;
                    break;
                case 2:
                    p1 = Point2; p2 = Point3; 
                    break;
                case 3:
                    p1 = Point3; p2 = Point1;
                    break;
            }
			if (p1.Triangles != null && p2.Triangles != null)
			{
				foreach (Triangle t in p1.Triangles)
				{
					if (t == this)
						continue;

					foreach (Triangle t2 in p2.Triangles)
					{
						if (t == t2)
							return t;
					}
				}
			}

            return null; //no adjacent triangle on this side, must have reached the hull (or whatever passes for it nowadays)
        }

        /// <summary>
        /// Calulate the level of point defined by X,Y is this triangle
        /// Only return a valid level if within the triangle
        /// </summary>
        /// <param name="X">test point x</param>
        /// <param name="Y">test point y</param>
        /// <returns></returns>
        public double CalcLevelInTri(double X, double Y)
        {
            double lev = -999;
            if (PointInTri(X, Y))
                lev = CalcLevelOnPlane(X, Y);
            return lev;
        }

        /// <summary>
        /// Test whether a point is located within this triangle
        /// </summary>
        /// <param name="X">test point x</param>
        /// <param name="Y">test point y</param>
        /// <returns>True if point is within this triangle, false else</returns>
        public bool PointInTri(double X, double Y)
        {
            //if ((Y - Point1.Y) * (Point2.X - X) < (X - Point1.X) * (Point2.Y - Y))
            //    return false;

            //if ((Y - Point2.Y) * (Point3.X - X) < (X - Point2.X) * (Point3.Y - Y))
            //    return false;

            //if ((Y - Point3.Y) * (Point1.X - X) < (X - Point3.X) * (Point1.Y - Y))
            //    return false;

            if (MathsHelpers.Vector.FNRightOf(this.Point1.X, this.Point1.Y, this.Point2.X, this.Point2.Y, X, Y) || MathsHelpers.Vector.FNRightOf(this.Point2.X, this.Point2.Y, this.Point3.X, this.Point3.Y, X, Y) || MathsHelpers.Vector.FNRightOf(this.Point3.X, this.Point3.Y, this.Point1.X, this.Point1.Y, X, Y))
            {
                //Point is to the right of one of the triangle side - must be outside
                return false;
            }
            else
            {
                //Point is to the left of all three sides - must be within
                return true;
            }
            return true;
        }

        /// <summary>
        /// Override for CalcLevelOnPlane - calculates the level for point X,Y
        /// on an infinate plane defined by the vertices of this triangle
        /// </summary>
        /// <param name="X">test point x</param>
        /// <param name="Y">test point y</param>
        /// <returns>level as double</returns>
        public double CalcLevelOnPlane(double X, double Y)
        {
            return CalcLevelOnPlane(this.Point1.X, this.Point1.Y, this.Point1.Z, this.Point2.X, this.Point2.Y, this.Point2.Z, this.Point3.X, this.Point3.Y, this.Point3.Z, X, Y);
        }

        /// <summary>
        /// Calculate the level for point X,Y on an infinate plane defined by
        /// point X, Y 1 through 3
        /// </summary>
        /// <param name="X1">plane x 1</param>
        /// <param name="Y1">plane y 1</param>
        /// <param name="Z1">plane z 1</param>
        /// <param name="X2">plane x 2</param>
        /// <param name="Y2">plane y 2</param>
        /// <param name="Z2">plane z 2</param>
        /// <param name="X3">plane x 3</param>
        /// <param name="Y3">plane y 3</param>
        /// <param name="Z3">plane z 3</param>
        /// <param name="X">test point x</param>
        /// <param name="Y">test point y</param>
        /// <returns>level as double</returns>
        public double CalcLevelOnPlane(double X1, double Y1, double Z1, double X2 , double Y2, double Z2, double X3, double Y3, double Z3, double X, double Y)
        {
            double a1 = X - X1; double a2 = Y - Y1; double b1 = X - X2; double b2 = Y - Y2; double c1 = X - X3; ; double c2 = Y - Y3; double az = Z1; double bz = Z2; double cz = Z3;
            return ((a1 * b2 * cz) - (a1 * bz * c2) + (a2 * bz * c1) - (a2 * b1 * cz) + (az * b1 * c2) - (az * b2 * c1)) / ((a1 * b2) - (a1 * c2) + (a2 * c1) - (a2 * b1) + (b1 * c2) - (b2 * c1));
        }

        #endregion level calcs



        #endregion
    }

    //here here here

    #endregion


    #endregion
    public class BoundHorizontalElement
    {

        private HorizontalElementType HorizontalEleType = HorizontalElementType.Straight;
        private double HorizontalChainage = 0;
        private double HorizontalEast = 0;
        private double HorizontalNorth = 0;
        private double HorizontalRadius= 0;
        private double HorizontalSpiralLength= 0;


        public HorizontalElementType HzEleType { get { return HorizontalEleType; } set { HorizontalEleType = value; } }
        public double Chainage { get { return HorizontalChainage; } set { HorizontalChainage = value; } }
        public double Easting { get { return HorizontalEast ; } set { HorizontalEast= value; } }
        public double Northing { get { return HorizontalNorth; } set { HorizontalNorth= value; } }
        public double Radius { get { return HorizontalRadius; } set { HorizontalRadius = value; } }
        public double SpiralLength { get { return HorizontalSpiralLength; } set { HorizontalSpiralLength = value; } }

    }

    public class BoundAlignment  :Alignment
    {

        private string name;
        public string AlignmentName { get { return name; } set { name = value; } }

        public List<BoundHorizontalElement> Horizontal = new List<BoundHorizontalElement>();
    }
    public class ModelProperties
    {

        

        [CategoryAttribute("General File Info"), DescriptionAttribute("Individual model name within the file")]
        public string ModelName { get;  set; }
        [CategoryAttribute("General File Info"), DescriptionAttribute("File imported")]
        public string SourceFile { get; set; }
        [CategoryAttribute("General File Info"), DescriptionAttribute("Source file type") ]
        public string SourceFileType{get;  set;}
        [CategoryAttribute("Entity Info"), DescriptionAttribute("Number of points in model")]
        public string PointCount { get; set; }
        [CategoryAttribute("Entity Info"), DescriptionAttribute("Number of triangles in model")]
        public string TriangleCount { get; set; }
        [CategoryAttribute("Bounds"), DescriptionAttribute("Minimum X")]
        public string MinX { get; set; }
        [CategoryAttribute("Bounds"), DescriptionAttribute("Minimum Y")]
        public string MinY { get; set; }
        [CategoryAttribute("Bounds"), DescriptionAttribute("Maximum X")]
        public string MaxX { get; set; }
        [CategoryAttribute("Bounds"), DescriptionAttribute("Maximum Y")]
        public string MaxY { get; set; }
    }

    public static class DTMHelpers
    {
        #region Triangulation
        /// <summary>
        /// The most basic triangulation method. Generates a Constrained Delauny mesh from a set of points and a set of constraints.
        /// The breakline parameter is used by "ApplyBreaklines". Recommended not to pass anything if you're using this method directly.
        /// Returns a fresh model, the points have no connection to their previous state other than their location and ID property (which should be setup before triangulation)
        /// </summary>
        public static (bool Success, string Message, DTM outModel) TriangulatePoints(HashSet<DTMPoint> PointsToTriangulate, List<StandardLine> Breaklines = null)
        {
            #region Null Refernce/Null Levels Checks & TriangleNET Poly/Vertex Setup
            //Null reference checks
            if (PointsToTriangulate == null || PointsToTriangulate.Count < 3) { return (false, "Not enough points", null); }

            //Filter out null level triangles
            HashSet<DTMPoint> tempNoNullLevelPoints = new HashSet<DTMPoint>(); //temp store the valid points
            foreach (DTMPoint pt in PointsToTriangulate)
            {
                if (pt.Z != -999) { tempNoNullLevelPoints.Add(pt); }
            }
            PointsToTriangulate = tempNoNullLevelPoints; //Transfer over the valid points. We've disregarded the rest.

            //Point Count Re-Check
            if (PointsToTriangulate.Count < 3) { return (false, "Not enough points", null); }


            //Create the polygon for the model
            TriangleNet.Geometry.Polygon poly = new TriangleNet.Geometry.Polygon(PointsToTriangulate.Count);

            //Add points to the poly
            foreach(DTMPoint pt in PointsToTriangulate)
            {
                
                poly.Add(new Vertex(pt.X, pt.Y));
            }
            #endregion

            #region Setup the Breakline (if we're applying one)
            //Convert the breakline to a "segment" and add it to the polymesh
            if (Breaklines != null && Breaklines.Count > 0)
            {
                foreach (StandardLine bl in Breaklines)
                {
                    if (bl.StartPoint == null || bl.EndPoint == null) { continue; }

                    if (bl.StartPoint.Z != -999 && bl.EndPoint.Z != -999)
                    {
                        //Create the vertices
                        Vertex startVertex = new Vertex(bl.StartPoint.X, bl.StartPoint.Y);
                        Vertex endVertex = new Vertex(bl.EndPoint.X, bl.EndPoint.Y);
                        //Create the segment
                        Segment seg = new Segment(startVertex, endVertex);
                        //Add the segment to the polymesh
                        poly.Add(seg);
                    }
                }
            }
            #endregion

            //"Settings" for triangulation...
            var constraintOptions = new TriangleNet.Meshing.ConstraintOptions()
            {
                ConformingDelaunay = false,
                Convex = true,
                SegmentSplitting = 2
            };
            var qualityOptions = new TriangleNet.Meshing.QualityOptions()
            {
                //MaximumAngle = 10,
                //MinimumAngle = 0,
                //VariableArea = true,
            };

            TriangleNet.Meshing.IMesh mesh = null;
            bool success = true;
            string message = "Success";

            #region Perform Triangulation
            //Perform the triangulation
            try { mesh = poly.Triangulate(constraintOptions, qualityOptions); }
            catch (Exception e) { success = false; message = e.Message; }

            //Check to see if triangulation was successful
            if (mesh == null) { return (false, "No mesh", null); }
            #endregion

            if (mesh.Vertices.Count > PointsToTriangulate.Count)
            {
                return (false, "Steiners created", null); //Error in triangulation. Usually caused by crossing breaklines.
            }

            #region Create an output DTM model
            //Create the output model
            DTM outputModel = new DTM();
            //Setup the outmodel with the initial points we're triangulating and our mesh we just created
            foreach(DTMPoint originalPoint in PointsToTriangulate)
            {
                outputModel.Points.Add(originalPoint.DeepCopy()); //Make a deep copy for the outmodel so we don't mess up triangle to point relationships
            }
            outputModel.TNetMesh = (Mesh)mesh;
            outputModel.RemakeTriangleModelFromMesh(); //Make a completely independant outModel with all its point to triangle relationships intact
            #endregion

            return (success, message, outputModel);
        }

        public static void RenumberPointIDs(List<DTMPoint> Points)
        {
            int count = 0;
            foreach (var pt in Points)
            {
                pt.ID = count;
                count++;
            }
        }

        /// <summary>
        /// Insert the new, small, triangulation into the initial model. (Checks overlaps etc) Call "RenumberPointIDs" before this or the point matching won't work.
        /// </summary>
        /// <param name="tempModel">THe smaller, probably newly created model, which is to be inserted into the parent model</param>
        /// <param name="originalModel">The parent model, the one that the smaller model is to be merged into</param>
        /// <param name="PreviousTriangles">The triangles that used to be in the sport that the tempModel now resides in. These are used to test for overlaps etc</param>
        public static void MergeNewTriangulationIntoParentModel(DTM tempModel, DTM originalModel, List<Triangle> PreviousTriangles)
        {
            if (tempModel == null) { return; }

            TriangleSurface sf = new TriangleSurface();
            sf = originalModel.GetSurface(1);

            foreach (NRG.Models.Triangle t in tempModel.Triangles)
            {
                Point2D cp = NRG.MathsHelpers.Triangle.TriangleCentre(t.Point1.X, t.Point1.Y, t.Point2.X, t.Point2.Y, t.Point3.X, t.Point3.Y);

                bool validTriangle = false;
                //Check and make sure we don't add any triangles that were created outside our boundaries to our orignalModel. If there are a lot of found triangles 
                //then this would obviously get a bit slow. Fastest and most reliable way I've found of doing it so far. (Don't attempt to use InPoly. Nightmare.) TN 18.05.22
                foreach(Triangle oldTriangle in PreviousTriangles)
                {
                    if (oldTriangle.PointInTri(cp.X, cp.Y)) { validTriangle = true; break; }
                }

                if (validTriangle)
                {

                    DTMPoint p1 = originalModel.Points[t.Point1.ID];
                    DTMPoint p2 = originalModel.Points[t.Point2.ID];
                    DTMPoint p3 = originalModel.Points[t.Point3.ID];

                    if (p1 == null || p2 == null || p3 == null)
                    {
                        continue; //Failed to match point from the tempModel to the parentModel. Probably due to RenumberPointIDs not being called before this method.
                    }

                    //NRG.Models.Triangle triangleToAdd = new NRG.Models.Triangle(sf, t.Point1, t.Point2, t.Point3);
                    NRG.Models.Triangle triangleToAdd = new NRG.Models.Triangle(sf, p1, p2, p3);
                    originalModel.Triangles.Add(triangleToAdd);
                    //sf.AddTriangle(triangleToAdd); removed by TN: 11.01.21, surface > triangle relationship gets assigned in the triangle object creation (i love many to many relationships)
                }
                else
                {
                    //A triangle was created in the tempModel due to the boundary lines being concave at some point. We don't want this
                    //triangle to be merged so we do nothing here.
                }
            }
            return;
        }

        
        


        /// <summary>
        /// Genereate a list of breaklines from numerous sources (e.g from Features or PolyLines).
        /// </summary>
        /// <param name="Points"></param>
        /// <returns></returns>
        public static List<StandardLine> GetBreaklines(List<DTMPoint> Points = null, Dictionary<string, LineFeatureCollection> LineFeaturesDictionary = null, List<PolyLine> PolyLines = null, List<StandardLine> StandardLines = null)
        {
            List<StandardLine> Breaklines = new List<StandardLine>();

            //Null reference checks
            if (Points == null) { Points = new List<DTMPoint>(); }
            if (LineFeaturesDictionary == null) { LineFeaturesDictionary = new Dictionary<string, LineFeatureCollection>(); }
            if (PolyLines == null) { PolyLines = new List<PolyLine>(); }
            if (StandardLines == null) { StandardLines = new List<StandardLine>(); }

            #region GetBreaklinesFromFeatures
            foreach (string key in LineFeaturesDictionary.Keys)
            {
                LineFeatureCollection collection = LineFeaturesDictionary[key];
                if (collection.Feature.BreakLineType == BreakLineType.BreakLine)
                {
                    foreach (var itemKey in collection.FeatureItems.Keys)
                    {
                        var items = collection.FeatureItems[itemKey];
                        foreach (var item in items)
                        {
                            //There are not enough points for a breakline to be created
                            if (item.Points.Count < 2) { continue; }

                            //Loop through each pairing of this feature line and create a "StandardLine" for each pair
                            for (int i = 0; i < item.Points.Count - 1; i++)
                            {
                                StandardLine bl = new StandardLine();
                                bl.StartPoint = item.Points[i];
                                bl.EndPoint = item.Points[i + 1];
                                
                                //Check if both points are valid
                                if (bl.StartPoint.Z == -999 || bl.EndPoint.Z == -999) { continue; }

                                //Add the breakline to the list
                                Breaklines.Add(bl);
                            }
                            
                            //Closed lines need their start and end points adding as a breakline too
                            if (item.Closed)
                            {
                                StandardLine bl = new StandardLine();
                                //Add the last point then the first point
                                bl.StartPoint = item.Points.Last();
                                bl.EndPoint = item.Points.First();

                                //Check if both points are valid
                                if (bl.StartPoint.Z == -999 || bl.EndPoint.Z == -999) { continue; }

                                //Add the breakline to the list
                                Breaklines.Add(bl);
                            }
                        }
                    }
                }
            }
            #endregion

            #region GetBreakLinesFromPolyLines
            foreach(PolyLine pl in PolyLines)
            {
                //Loop through each node and create a breakline from every pairing
                for (int i = 0; i < pl.Nodes.Count; i++)
                {
                    if (i== pl.Nodes.Count-1) { break; }

                    StandardLine bl = new StandardLine();
                    bl.StartPoint = pl.Nodes[i];
                    bl.EndPoint = pl.Nodes[i + 1];

                    //Check if both points are valid
                    if (bl.StartPoint.Z == -999 || bl.EndPoint.Z == -999) { continue; }

                    //Add the breakline to the list
                    Breaklines.Add(bl);
                }

                //Check if polyline is closed, if yes, add another breakline using the first and last points (not if they are identical though)
                if (pl.IsClosed && pl.Nodes.First() != pl.Nodes.Last())
                {
                    StandardLine bl = new StandardLine();
                    //Add the last point then the first point
                    bl.StartPoint = pl.Nodes.Last();
                    bl.EndPoint = pl.Nodes.First();

                    //Check if both points are valid
                    if (bl.StartPoint.Z == -999 || bl.EndPoint.Z == -999) { continue; }

                    //Add the breakline to the list
                    Breaklines.Add(bl);
                }
            }
            #endregion

            #region GetBreakLinesFromStandardLines
            foreach (StandardLine sl in StandardLines)
            {
                StandardLine bl = new StandardLine();

                bl.StartPoint = sl.StartPoint;
                bl.EndPoint = sl.EndPoint;
                
                //Check if both points are valid
                if (sl.StartPoint.Z == -999 || sl.EndPoint.Z == -999) { continue; }

                //Add the breakline to the list
                Breaklines.Add(sl);
            }
            #endregion
            
            return Breaklines;
        }

        #endregion

        
        /// <summary>
        /// Merges DTM Models, make sure to use NGRDTM.Services DTM.DeleteModelRenderItems() after calling this method.
        /// </summary>
        /// <returns>A combined model</returns>
        public static DTM MergeModels(ModelSet modelsToMerge)
        {
            //Whichever model is first in the list will have priority over the rest, in that order, in terms of overlapping (layers, triangles etc)

            if (modelsToMerge == null || modelsToMerge.Count == 0)

                return null;

            DTM outmodel = new DTM();

            foreach (var mod in modelsToMerge)
            {
                var dict = outmodel.CreatePointIncrementDictionary(); //Not really needed since points are added sequentially, meaning the pointers won't really ever get out of sync

                #region Points
                List<Triangle> trianglesToRemoveFromModel = new List<Triangle>();
                List<Triangle> trianglesToRemoveFromPoint = new List<Triangle>();

                foreach (DTMPoint pt in mod.Points)
                {
                    #region Block Inserts
                    //If we don't want to import the block inserts, we just wipe it
                    if (!modelsToMerge.ShowBlocks)
                    {
                        pt.BlockInserts = null;
                    }
                    #endregion

                    #region Triangles
                    //Handles point-triangle relationship, merging of triangles to the merged model and shifting of point levels onto the merged model

                    if (modelsToMerge.ShowTriangles) //If we aren't even merging triangles at all, we can skip all this
                    {
                        List<DTMPoint> pointsWithTrianglesToRemove = new List<DTMPoint>();
                        bool pointOverlapsModel = false;

                        //Loop through each triangle in the outmodel and test the point against it
                        foreach (var tri in outmodel.Triangles)
                        {
                            if(tri.PointInTri(pt.X, pt.Y))
                            {
                                pointOverlapsModel = true;

                                //Shift level onto outmodel
                                if (modelsToMerge.ShiftPointsOntoModel)
                                {
                                    pt.Z = tri.CalcLevelInTri(pt.X, pt.Y);
                                }
                                break; //Point doesn't need to be tested against any other triangles in the outmodel (we have its new level and know it overlaps)
                            }
                        }

                        //Shift points outside parent model to -999 if setting calls for it
                        if (modelsToMerge.ShiftPointsOntoModel && modelsToMerge.ShiftOutsideToNull && !pointOverlapsModel && outmodel.Triangles.Count > 0)
                        {
                            pt.Z = -999;
                        }

                        //Note: The overlapping triangle check is not 100% fool-proof. There are certain triangle overlaps which are difficult to check for (e.g hexagram)
                        #region Handle triangle removal (specific way due to point-triangle many-to-many Relationship)

                        if (pt.Triangles != null) //If the point has no triangles, we can't remove any...
                        { 

                            foreach (var pointTriangle in pt.Triangles)
                            {
                                //Null check, if we hit this, one of the triangle models is broken
                                if (pointTriangle == null || pointTriangle.Surface == null)
                                {
                                    return null;
                                }

                                bool removeReason1 = false;
                                bool removeReason2 = false;

                                if (modelsToMerge.RemoveOverlappingTriangles && pointOverlapsModel) //First test for triangle removal (overlap setting)
                                {
                                    removeReason1 = true;
                                }
                                if (!modelsToMerge.ShowNonContourableTriangles && !pointTriangle.Surface.Contour) //Second test (non-contourable setting)
                                {
                                    removeReason2 = true;
                                }
                                if (!removeReason1 && !removeReason2) { continue; }//We have no reason to remove this triangle, so continue


                                //Remove the triangle from its surface
                                if (pointTriangle.Surface.Triangles != null && pointTriangle.Surface.Triangles.Contains(pointTriangle))
                                {
                                    pointTriangle.Surface.Triangles.Remove(pointTriangle);
                                    if (pointTriangle.Surface.Triangles.Count <= 0)
                                        pointTriangle.Surface.Triangles = null;
                                }

                                //Remove triangle from Triangle.Point1
                                if (pt != pointTriangle.Point1 && pointTriangle.Point1.Triangles != null && pointTriangle.Point1.Triangles.Contains(pointTriangle))
                                {
                                    pointTriangle.Point1.Triangles.Remove(pointTriangle);
                                    if (pointTriangle.Point1.Triangles.Count <= 0)
                                        pointTriangle.Point1.Triangles = null;
                                }

                                //Remove triangle from Triangle.Point2
                                if (pt != pointTriangle.Point2 && pointTriangle.Point2.Triangles != null && pointTriangle.Point2.Triangles.Contains(pointTriangle))
                                {
                                    pointTriangle.Point2.Triangles.Remove(pointTriangle);
                                    if (pointTriangle.Point2.Triangles.Count <= 0)
                                        pointTriangle.Point2.Triangles = null;
                                }

                                //Remove triangle from Triangle.Point3
                                if (pt != pointTriangle.Point3 && pointTriangle.Point3.Triangles != null && pointTriangle.Point3.Triangles.Contains(pointTriangle))
                                {
                                    pointTriangle.Point3.Triangles.Remove(pointTriangle);
                                    if (pointTriangle.Point3.Triangles.Count <= 0)
                                        pointTriangle.Point3.Triangles = null;
                                }

                                trianglesToRemoveFromModel.Add(pointTriangle);


                            }
                            ///Empty the point's triangle list after loop finished
                            pt.Triangles = new List<Triangle>();
                        }
                        #endregion

                    }
                    #endregion

                    #region Feature Lines
                    //Wipe point label if lines aren't being merged
                    if (!modelsToMerge.ShowLines)
                        pt.PointLabel = "";
                    #endregion

                    //We can now add the point
                    outmodel.Points.Add(pt);
                }

                //Unwanted triangles removal
                //We have to store and loop through the triangles we want to remove seperatley, as you cant change the collection during a loop.
                foreach (var triangle in trianglesToRemoveFromModel)
                {
                    mod.Triangles.Remove(triangle);
                }
                #endregion

                #region Triangles
                //Merge model.Triangles
                //We should only have triangles want to keep at this point so adding the triangles is simple
                if (modelsToMerge.ShowTriangles)
                {
                    outmodel.Triangles.UnionWith(mod.Triangles);
                }

                #endregion

                #region Surfaces 
                //Merging surfaces concatenates surface lists but attempts to do an actual merge for surface 0.

                if (modelsToMerge.ShowSurfaces)
                {
                    //(Handles many - to - many surface - triangle relationships)
                    var surf0 = new TriangleSurface(); //Not really meant to create actual surfaces like this but its only used temporarily
                    bool surf0found = false; //Flag for finding surface 0 in the outmodel
                    if (outmodel.Surfaces != null)
                    {
                        //Find surface 0 in the outmodel
                        foreach (var surface in outmodel.Surfaces)
                        {
                            if (surface.ID == 0)
                            {
                                surf0 = surface;
                                surf0found = true;
                                break;
                            }
                        }
                    }

                    //Loop through all the surfaces and add them to the outmodel. If its surface 0, we merge the surfaces and its triangles
                    foreach (var newSurface in mod.Surfaces)
                    {
                        if (newSurface.ID == 0) //Handle Surface-Triangle relationship for merging surface 0
                        {
                            //If this is the first surface 0 being merged from the modellist, we just add it
                            if (!surf0found)
                            {
                                outmodel.Surfaces.Add(newSurface);
                            }
                            else //Otherwise, we need to merge its triangles with the existing surface 0
                            {
                                if (surf0.Triangles == null)
                                    surf0.Triangles = new HashSet<Triangle>();

                                if (newSurface.Triangles == null)
                                    newSurface.Triangles = new HashSet<Triangle>();

                                //Move the triangles over to the outmodels surface 0
                                surf0.Triangles.UnionWith(newSurface.Triangles);
                            }
                        }
                        else //Surface isn't surface 0 so we can just add it normally
                        {
                            outmodel.Surfaces.Add(newSurface);
                        }
                    }

                    //Loop through all the triangles on surface 0 in the model to merge and set their surface to the new surface 0 in the outmodel.
                    List<Triangle> surf0triList = new List<Triangle>();
                    surf0triList.AddRange(mod.Triangles.Where(x => x.Surface.ID == 0));
                    foreach (var tri in surf0triList)
                    {
                        tri.Surface = surf0;
                    }

                    outmodel.RegenerateSurfaceIDs();
                }
                else
                {
                    outmodel.Surfaces = null;
                    outmodel.CreateNewSurface(Color.White);
                    outmodel.CreateNewSurface(Color.Gray);
                    foreach (var tri in outmodel.Triangles)
                        outmodel.Surfaces[1].AddTriangle(tri);
                }
                #endregion

                #region Layers
                //Duplicate layer results will just go off priority of model loop
                if(mod.DrawingLayers != null && mod.DrawingLayers.Count > 0)
                {
                    foreach (DrawingLayer layer in mod.DrawingLayers.Values)
                    {
                        outmodel.AddLayer(layer);
                    }
                }
                #endregion

                

                #region PolyLines
                
                if (mod.NumberOfPolyLines() > 0 && modelsToMerge.ShowLines)
                {
                    outmodel.AddPolyLineList(mod.GetPolyLines());
                }
                #endregion

                #region StandardLines
                if (mod.NumberOfStandardLines() > 0 && modelsToMerge.ShowLines)
                {
                    outmodel.AddStandardLineList(mod.GetStandardLines());
                }
                #endregion

                #region Block Definitonss
                //Block Inserts come in with the point
                //Their definitions need copying over
                //As per, the first model takes priority over the definitions

                if (modelsToMerge.ShowBlocks)
                {
                    foreach (var blockDefPair in mod.GetBlockDefinitionsDictionary())
                    {
                        if (!outmodel.GetBlockDefinitionsDictionary().ContainsKey(blockDefPair.Key))
                            outmodel.GetBlockDefinitionsDictionary().Add(blockDefPair.Key, blockDefPair.Value);
                    }
                }

                #endregion

                #region Feature Definitions
                if (modelsToMerge.MergeFeatures) //A "simple" merge of feature definitions. Follows mod loop for priority. (later loops do not overwrite if duplicates found)
                {
                    //Deconstruct the new model's features
                    var newModelsLineFeatures = new List<Feature>();
                    var newModelsPointFeatures = new List<Feature>();
                    DeconstructFeatureDict(mod.FeaturesDictionary, ref newModelsLineFeatures, ref newModelsPointFeatures, false);

                    //Deconstruct the outmodel's features, pass the newModels features in and weed out any duplicates (outmodel has priority)
                    DeconstructFeatureDict(outmodel.FeaturesDictionary, ref newModelsLineFeatures, ref newModelsPointFeatures, true);

                    //Concat the seperate lists, which now contain no duplicate features
                    List<Feature> totalFeaturesToKeep = new List<Feature>();
                    totalFeaturesToKeep.AddRange(newModelsLineFeatures);
                    totalFeaturesToKeep.AddRange(newModelsPointFeatures);

                    //Format the Feature objects into a fresh FeatureDictionary
                    Dictionary<char, List<Feature>> featureDict = DTMHelpers.FormatFeatures(totalFeaturesToKeep);

                    //Clear the model's existing feature and setup+interpret the new features dictionary
                    if (outmodel.FeaturesDictionary != null)
                        outmodel.FeaturesDictionary.Clear();

                    //Set the model up with the new features dictionary
                    outmodel.SetupFeaturesFromDictionary(featureDict);
                }
                #endregion

                outmodel.InterpretPointIncrementDictionary(dict);

            }

            outmodel.InterpretFeatures();

            outmodel.GeneratePointLayerStrings();

            return outmodel;
        }


        ///// <summary>
        ///// cloneobject is a deep clone of the object.
        ///// </summary>
        ///// <param name=></param>
        ///// 
        //public static DTMPoint CloneObject(DTMPoint obj)
        //{
        //    var newObj = new DTMPoint();
        //    newObj.CopyFrom(obj);
        //    return newObj;
        //}

        public static void CreateBaseSurfaces(DTM model)
		{
			if (model.Surfaces == null)
			{
				model.Surfaces = new List<TriangleSurface>();
			}

            var rand = new Random();


            for (int i = 0; i < 25; i++)
			{
				TriangleSurface surf = new TriangleSurface();
				surf.ID = i;
				surf.Name = "";
				if (i == 0)
				{
					surf.Contour = false;
					surf.Volume = false;
					surf.Locked = true;
					surf.R = 255;surf.G = 255; surf.B = 255;

                    

                }
				else
				{
					surf.Contour = true;
					surf.Volume = true;
					surf.Locked = false;
					surf.R = rand.Next(256); 
                    surf.G = rand.Next(256); 
                    surf.B = rand.Next(256);

                    //Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));

                }

                model.Surfaces.Add(surf);

			}

		}

        /// <summary>
        /// Pass a list of points and the bearings between each point will be calculate.
        /// </summary>
        /// <param name="lineOfPoints"></param>
        public static void CalculatePointBearingsAlongLine(List<DTMPoint> lineOfPoints)
        {
            foreach (var point in lineOfPoints)
            {

                int indexOfPoint = lineOfPoints.IndexOf(point);

                if (point != lineOfPoints.Last())
                {
                    point.HighlightBearing = NRG.MathsHelpers.Trig.RPC(point.X, point.Y, lineOfPoints[indexOfPoint + 1].X, lineOfPoints[indexOfPoint + 1].Y);
                }
                else
                {
                    if (point != lineOfPoints.First())
                    {

                        double brg = NRG.MathsHelpers.Trig.RPC(point.X, point.Y, lineOfPoints[indexOfPoint - 1].X, lineOfPoints[indexOfPoint - 1].Y);
                        brg = brg + Math.PI;
                        point.HighlightBearing = Trig.CheckBearing(brg);
                    }
                }

            }
        }

        public static void RotatePoints(HashSet<DTMPoint> points, Point2D origin, double angle, bool clockwise = true)
        {
            if (points == null || points.Count == 0 || origin == null) { return; }

            foreach (var point in points)
            {
                Point2D newPosition = new Point2D();

                if (clockwise)
                { newPosition = Trig.RotateXYClockWiseAroundOrigin(point.X, point.Y, angle, origin.X, origin.Y); }
                else
                { newPosition = Trig.RotateXYAroundOrigin(point.X, point.Y, angle, origin.X, origin.Y); }

                //double brg = 0;
                //double dist = 0;
                //NRG.MathsHelpers.Trig.RPC(origin.X, origin.Y, point.X, point.Y, ref brg, ref dist);
                //Point2D newPosition = NRG.MathsHelpers.Trig.PRC(origin.X, origin.Y, angle + brg, dist);
                point.X = newPosition.X;
                point.Y = newPosition.Y;
            }
        }

        public static double GetAngleForRotationTransformation(Point2D origin1, Point2D destination1, Point2D origin2, Point2D destination2)
        {
            if(origin1 == null || destination1 == null || origin2 == null || destination2 == null) { return 0; }

            double originBearing = NRG.MathsHelpers.Trig.RPC(origin1.X, origin1.Y, origin2.X, origin2.Y);
            double destinationBearing = NRG.MathsHelpers.Trig.RPC(destination1.X, destination1.Y, destination2.X, destination2.Y);
            double finalBearing = NRG.MathsHelpers.Trig.FnAngle(destinationBearing, originBearing);

            return finalBearing;
        }

        public static void StretchPoints(HashSet<DTMPoint> points, Point2D origin, double scaleFactor)
        {
            if (points == null || points.Count == 0 || origin == null) { return; }

            foreach(var point in points)
            {
                double brg = 0;
                double dist = 0;
                NRG.MathsHelpers.Trig.RPC(origin.X, origin.Y, point.X, point.Y, ref brg, ref dist);
                Point2D newPosition = NRG.MathsHelpers.Trig.PRC(origin.X, origin.Y, brg, dist, scaleFactor);
                point.X = newPosition.X;
                point.Y = newPosition.Y;
            }


        }

        public static double GetScaleFactorForTransformation(Point2D origin1, Point2D destination1, Point2D origin2, Point2D destination2)
        {
            if (origin1 == null || destination1 == null|| origin2 == null|| destination2 == null) { return 1; }

            double dist1 = NRG.MathsHelpers.Vector.FnDistance(origin1, origin2);
            double dist2 = NRG.MathsHelpers.Vector.FnDistance(destination1, destination2);
            double sf = dist2 / dist1;

            return sf;
        }

        public static void TiltPoints(HashSet<DTMPoint> points, Triangle originTri, Triangle destTri)
        {

            if (points == null || points.Count == 0 || originTri == null || destTri == null) { return; }

            foreach (var point in points)
            {
                double originLevel = originTri.CalcLevelOnPlane(point.X, point.Y);
                double delta = point.Z - originLevel;

                double destLevel = destTri.CalcLevelOnPlane(point.X, point.Y);
                point.Z = delta + destLevel;
            }
        }

        /// <summary>
        /// Shift points by passed values. Set shiftToLevel to true for all points to share the passed z value as their levels.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="shiftToLevel">Leave as false for levels to be adjusted by passed value. Set to true for levels to be set to passed value.</param>
        public static void ShiftPoints(HashSet<DTMPoint> points, double x, double y, double z, bool shiftToLevel = false)
        {
            foreach(var point in points)
            {
                point.ShiftBy(x, y, z);

                //For shifting points to specified level
                if (shiftToLevel) { point.Z = z; }


            }
        }

        #region Shift Points Onto Triangle Model
        /// <summary>
        /// Setting used in (ShiftPointLevelsOntoTriangleModel) for what to do with point levels that lay outside the triangle model
        /// </summary>
        public enum PointsOutsideModel
        {
            Null, //Set point level as null
            Extend, //Find closest triangle, calculate level based off an infinite plane
            Near //Find closest point and copy that level
        }

        /// <summary>
        /// Setting used in (ShiftPointLevelsOntoTriangleModel) for selecting which point levels get modified when shifting levels onto a triangle model
        /// </summary>
        public enum PointsRelativeToModel
        {
            All, //Every point can be modified
            Above, //Only point levels above the model get modified
            Below //Only point levels below the model get modified
        }

        public static void ShiftPointLevelsOntoTriangleModel(HashSet<DTMPoint> points, HashSet<Triangle> triangles, PointsOutsideModel outside, PointsRelativeToModel relative, bool useDippedLevels)
        {
            bool pointOverlapsModel = false;

            foreach(var point in points)
            {
                //Flag for point being inside or outside model
                pointOverlapsModel = false;

                //Loop through each triangle in the list and test the point against it
                foreach (var triangle in triangles)
                {
                    //Point is inside the model
                    if (triangle.PointInTri(point.X, point.Y))
                    {
                        pointOverlapsModel = true;

                        double triLevel = triangle.CalcLevelInTri(point.X, point.Y);

                        //Check which points user wants changing
                        if (relative == PointsRelativeToModel.Above && point.Z < triLevel) { continue; } //Point is below found level, we only want to change the points above so we skip it
                        if (relative == PointsRelativeToModel.Below && point.Z > triLevel) { continue; } //Point is above found level, we only want to change the points below so we skip it 

                        point.Z = triLevel;

                        if (useDippedLevels) { point.Z -= triangle.Surface.Dip; }

                        break;
                    }
                }

                //If flag == true, point level has been set, we can go to the next point
                if (pointOverlapsModel) { continue; } 

                //Point is outside of the model, additional checks needed
                if (outside == PointsOutsideModel.Null)
                {
                    //Set point level to null
                    point.Z = -999;
                    continue;
                }
                else if (outside == PointsOutsideModel.Near)
                {
                    //Set point level to that of the closest point in a triangle
                    Triangle nearestTriangle = FindNearestTri(point.X, point.Y, triangles);

                    if (nearestTriangle == null) { continue; }

                    Point2D closestIntersectionPoint = nearestTriangle.GetClosestPoint(point);
                    Point3D closestPoint = new Point3D(closestIntersectionPoint, nearestTriangle.CalcLevelOnPlane(closestIntersectionPoint.X, closestIntersectionPoint.Y));

                    //Check which points user wants changing
                    if (relative == PointsRelativeToModel.Above && point.Z < closestPoint.Z) { continue; } //Point is below found level, we only want to change the points above so we skip it
                    if (relative == PointsRelativeToModel.Below && point.Z > closestPoint.Z) { continue; } //Point is above found level, we only want to change the points below so we skip it 

                    //Set new level
                    point.Z = closestPoint.Z;
                    if (useDippedLevels) { point.Z =- nearestTriangle.Surface.Dip; }
                    continue;
                }
                else if (outside == PointsOutsideModel.Extend)
                {
                    //Find the nearest triangle and calculate the level from that
                    Triangle nearestTriangle = FindNearestTri(point.X, point.Y, triangles);
                    if (nearestTriangle == null) { continue; }
                    
                    double extendedLevel = nearestTriangle.CalcLevelOnPlane(point.X, point.Y);

                    //Check which points user wants changing
                    if (relative == PointsRelativeToModel.Above && point.Z < extendedLevel) { continue; } //Point is below found level, we only want to change the points above so we skip it
                    if (relative == PointsRelativeToModel.Below && point.Z > extendedLevel) { continue; } //Point is above found level, we only want to change the points below so we skip it 

                    //Set new level
                    point.Z = extendedLevel;
                    if (useDippedLevels) { point.Z -= nearestTriangle.Surface.Dip; }
                    continue;
                }
            }
        }

        #endregion

        public static void OffsetLine(List<DTMPoint> points, double hz, double level, bool shiftToLevel = false)
        {
            CalculatePointBearingsAlongLine(points);

            foreach(var point in points)
            {
                double newX = point.X;
                double newY = point.Y;

                //Add 90degrees to point bearing and validate it
                double brg = Trig.CheckBearing(point.HighlightBearing + Math.PI / 2);

                NRG.MathsHelpers.Trig.PRC(point.X, point.Y, brg, hz, ref newX, ref newY);
                point.X = newX;
                point.Y = newY;
                
                //Adjust level
                if(shiftToLevel) { point.Z = level; }
                else { point.Z += level; }
            }
        }

        //Alignment
        //Linear by chainage
        //Lateral by offset

        public static void OffsetPointsFromAlignment(Alignment alg, List<DTMPoint> points, double lat, double lin, bool shiftToSpecificOffset = false, bool adjustLevel = false)
        {
            if (alg == null || points == null) { return; }

            foreach(var point in points)
            {
                AlignmentVector5 initialResult = new AlignmentVector5();

                double chainage = 0;
                double offset = 0;

                //Get current chainage and offset of point
                if (alg.Horizontal.GetChainageAndOffset(point, ref initialResult))
                {
                    chainage = initialResult.chainage;
                    offset = initialResult.offset;
                }
                else { continue; } //Point is outside the chainage, skip shifting it
                

                //Get final offset and alignment values
                chainage += lin;
                if (shiftToSpecificOffset) 
                {
                    offset = lat; //Shift point to this offset value
                }
                else
                {
                    offset += lat;  //Shift point by this offset value
                }

                //Alignment extent checks
                if (chainage > alg.Horizontal.MaxChainage) { chainage = alg.Horizontal.MaxChainage; }
                if (chainage < alg.Horizontal.MinChainage) { chainage = alg.Horizontal.MinChainage; }


                //Find the coords at this new chainage and offset
                Point2D endPoint = alg.Horizontal.CoordsAtChAndOffset(chainage, offset);
                point.X = endPoint.X;
                point.Y = endPoint.Y;


                if (adjustLevel) //Adjust the level of the new point to follow the vertical aspect of the alignment
                {
                    //Alignment level at start position
                    double algStartLevel = alg.Vertical.LocateVerticalElement(initialResult.chainage);

                    //Alignment level at end position
                    double algEndLevel = alg.Vertical.LocateVerticalElement(chainage);
                    point.Z += (algEndLevel - algStartLevel);
                }

            }


        }

        public static Triangle FindNearestTri(double ptX, double ptY, HashSet<Triangle> triangles)
        {
            if (triangles == null || triangles.Count == 0)
                return null;

            double closestDistance = double.PositiveInfinity;
            Triangle nearestTriangle = new Triangle();

            foreach (Triangle triangle in triangles) //Loop through triangles
            {
                if (triangle.Surface != null && triangle.Surface.Contour == false) { continue; } //Non-Contourable triangle check

                bool triangleIsCloser = false;

                //Distance to Point1 - Point2 side
                double side1Dist = FindDistanceToTriangleSide(triangle.Point1.X, triangle.Point1.Y, triangle.Point2.X, triangle.Point2.Y, ptX, ptY);
                //Distance to Point2 - Point3 side
                double side2Dist = FindDistanceToTriangleSide(triangle.Point2.X, triangle.Point2.Y, triangle.Point3.X, triangle.Point3.Y, ptX, ptY);
                //Distance to Point3 - Point1 side
                double side3Dist = FindDistanceToTriangleSide(triangle.Point3.X, triangle.Point3.Y, triangle.Point1.X, triangle.Point1.Y, ptX, ptY);

                //Test found distances against closestDistance found so far for the other triangles
                if (side1Dist < closestDistance) { closestDistance = side1Dist; triangleIsCloser = true; }
                if (side2Dist < closestDistance) { closestDistance = side2Dist; triangleIsCloser = true; }
                if (side3Dist < closestDistance) { closestDistance = side3Dist; triangleIsCloser = true; }

                //Log the index of the triangle if the triangle is the new closest
                if (triangleIsCloser) { nearestTriangle = triangle; }
            }

            return nearestTriangle;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x1">The X value of the sides first point</param>
        /// <param name="y1">The Y value of the sides first point</param>
        /// <param name="x2">The X value of the sides second point</param>
        /// <param name="y2">The Y value of the sides second point</param>
        /// <param name="x3">The X value of the test point</param>
        /// <param name="y3">The Y value of the test point</param>
        /// <returns></returns>
        private static double FindDistanceToTriangleSide(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            //How far is point from side (normal to line)
            double dist = Math.Abs(NRG.MathsHelpers.Vector.FnPerpendicular(x1, y1, x2, y2, x3, y3));
            //How long is the side
            double dist1 = Vector.FnDistance(x1, y1, x2, y2);
            if (dist1 == 0) { dist1 = 1E-20; }
            //How far is point from ends of side
            double dist2 = Vector.FnDistance(x1, y1, x3, y3);
            double dist3 = Vector.FnDistance(x2, y2, x3, y3);

            if (dist2 == 0 || dist3 == 0) { dist = 0; } //test point is on an end of the side
            else
            {
                //Work out cosine of angles formed by side and point
                double CosAngle1 = (dist2 * dist2 + dist1 * dist1 - dist3 * dist3) / (2 * dist1 * dist2);
                double CosAngle2 = (dist3 * dist3 + dist1 * dist1 - dist2 * dist2) / (2 * dist1 * dist3);

                if (CosAngle1 < 0 || CosAngle2 < 0) { dist = dist2; }//point is normal to the line
                if (dist2 < dist) { dist = dist2; } //Node 1 is nearer
                if (dist3 < dist) { dist = dist3; } //Node 2 is nearer
            }

            return dist;
        }

        #region FindPoints
        /// <summary>
        /// Copy of DTM.GetClosestPoint but uses a passed list of DTMPoints rather than suing the DTM object. *Cannot check for layer use as that requires a DTM model*
        /// </summary>
        /// <param name="point"></param>
        /// <param name="Points"></param>
        /// <param name="CheckForTriangles"></param>
        /// <param name="CheckForDuplicates"></param>
        /// <returns></returns>
        public static DTMPoint GetClosestPoint(Point2D point, List<DTMPoint> Points, bool CheckForTriangles = false, bool CheckForDuplicates = true)
        {
            if (Points?.Count <= 0)
                return null;

            List<DTMPoint> closestPoints = new List<DTMPoint>();
            double dist = 0, minDist = double.MaxValue;

            foreach (var p in Points)
            {
                if (p.Type == PointType.FalsePoint)
                    continue;

                if (CheckForTriangles == true && (p.Triangles == null || p.Triangles.Count == 0))
                    continue;

                dist = MathsHelpers.Vector.FnDistance(p.X, p.Y, point.X, point.Y);//(xDiff * xDiff) + (yDiff * yDiff);

                if (dist <= minDist)
                {
                    minDist = dist;
                }
            }

            foreach (var p in Points)
            {
                dist = MathsHelpers.Vector.FnDistance(p.X, p.Y, point.X, point.Y);
                if (dist == minDist) { closestPoints.Add(p); }
                //p.ID = Points.IndexOf(p);
            }

            if (closestPoints.Count == 0)
                return null;


            if (closestPoints.Count == 1 || !CheckForDuplicates)
            {
                return closestPoints[0];
            }
            else
            {
                NRG.Models.PickDuplicateDlg dlg = new NRG.Models.PickDuplicateDlg();
                dlg.Points = closestPoints;
                dlg.ShowDialog();
                return dlg.SelectedPoint;
            }
        }

        /// <summary>
        /// Copy of DTM.GetClosestPoint3D but uses a passed list of DTMPoints rather than suing the DTM object. *Cannot check for layer use as that requires a DTM model*
        /// </summary>
        /// <param name="point"></param>
        /// <param name="Points"></param>
        /// <param name="CheckForDuplicates"></param>
        /// <param name="CheckAgainstMaxDist"></param>
        /// <param name="maxDist"></param>
        /// <returns></returns>
        public static DTMPoint GetClosestPoint3D(Point3D point, List<DTMPoint> Points, bool CheckForDuplicates = true, bool CheckAgainstMaxDist = false, double maxDist = 0)
        {
            if (Points?.Count <= 0)
                return null;

            List<DTMPoint> closestPoints = new List<DTMPoint>();
            double dist = 0, minDist = double.MaxValue;

            foreach (var p in Points)
            {
                if (p.Type == PointType.FalsePoint)
                    continue;


                dist = MathsHelpers.Vector.Fn3DDistance(p.X, p.Y, p.Z, point.X, point.Y, point.Z);


                if (CheckAgainstMaxDist && dist > maxDist)
                {
                    //Distance is greater than the max distance so we skip this point
                    continue;
                }

                if (dist <= minDist)
                {
                    minDist = dist;
                }
            }

            foreach (var p in Points)
            {
                dist = MathsHelpers.Vector.Fn3DDistance(p.X, p.Y, p.Z, point.X, point.Y, point.Z);
                if (dist == minDist) { closestPoints.Add(p); }

            }

            if (closestPoints.Count == 0)
                return null;


            if (closestPoints.Count == 1 || !CheckForDuplicates)
            {
                return closestPoints[0];
            }
            else
            {
                NRG.Models.PickDuplicateDlg dlg = new NRG.Models.PickDuplicateDlg();
                dlg.Points = closestPoints;
                dlg.ShowDialog();
                return dlg.SelectedPoint;
            }
        }
        #endregion

        #region Feature Stuff
        /// <summary>
        /// Seperates the Line Features and the Point Features into two simple lists. If merge is true, new features (from dict) overwrite ones passed in the ref list
        /// </summary>
        /// <param name="featuresDict"></param>
        public static void DeconstructFeatureDict(Dictionary<char, List<Feature>> featuresDict, ref List<Feature> LineFeaturesList, ref List<Feature> PointFeaturesList, bool mergeWithExisting = true)
        {
            if (featuresDict == null)
                return;

            //If we aren't merging with existing features, we clear the list
            if (!mergeWithExisting)
            {
                LineFeaturesList = new List<Feature>();
                PointFeaturesList = new List<Feature>();
            }

            foreach (var featureList in featuresDict.Values)
            {
                foreach (var feature in featureList)
                {

                    if (feature.GetType().ToString() == ("NRG.Models.LineFeature"))
                        AddFeature(feature, LineFeaturesList, mergeWithExisting);

                    if (feature.GetType().ToString() == ("NRG.Models.PointFeature"))
                        AddFeature(feature, PointFeaturesList, mergeWithExisting);

                }
            }
        }

        /// <summary>
		/// Adds a feature to the correct feature List, as well as handling a merge. New features will overwrite existing
		/// </summary>
		/// <param name="newFeature"></param>
		private static void AddFeature(Feature newFeature, List<Feature> featureList, bool mergeWithExisting)
        {
            List<Feature> featuresToRemove = new List<Feature>();
            //Remove duplicates from existing list

            if (mergeWithExisting && featureList != null)
            {

                foreach (var existingFeature in featureList)
                    if (existingFeature.FieldCode.ToLower() == newFeature.FieldCode.ToLower())
                        featuresToRemove.Add(existingFeature);

            }

            //Removal of duplicate features has to be done in a seperate loop or Bill Gates gets angry
            foreach (var feature in featuresToRemove)
                featureList.Remove(feature);

            //Add the new feature
            featureList.Add(newFeature);
        }

        /// <summary>
        /// Pass a raw list of feature objects and in return recieve a fully populated Features Dictionary 
        /// </summary>
        /// <param name="FeatureList"></param>
        /// <returns></returns>
        public static Dictionary<char, List<Feature>> FormatFeatures(List<Feature> featureObjList)
        {
            Dictionary<char, List<Feature>> featureDict = new Dictionary<char, List<Feature>>();
            foreach (Feature feature in featureObjList)
            {
                var str = feature.Code.ToCharArray();

                if (featureDict.ContainsKey(str[0]))
                    featureDict[str[0]].Add(feature);
                else
                {
                    List<Feature> singleCharFeatureList = new List<Feature>();
                    singleCharFeatureList.Add(feature);
                    featureDict.Add(str[0], singleCharFeatureList);
                }
            }

            //this step below does a "test-run" of setting up the features so they are formatted appropriatley and duplicates are handled.
            var tempDTM = new DTM();
            tempDTM.SetupFeaturesFromDictionary(featureDict);

            return tempDTM.FeaturesDictionary;

        }




        #endregion





    }

}

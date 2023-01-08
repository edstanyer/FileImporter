using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using MathNet.Numerics.LinearAlgebra;
using Meta.Numerics.Matrices;

namespace NRG.MathsHelpers
{
    public static class Geometry
    {
        #region general helpers
        /// <summary>
        /// ArcLength - added by ES:26.11.21
        /// </summary>
        /// <param name="start"></param> point2d represents the start of the arc
        /// <param name="end"></param> point2d represents the end of the arc
        /// <param name="radius"></param>I refuse to comment this!
        /// <returns></returns>
        public static double ArcLength(Point2D start, Point2D end, double radius)
        {
            double LC = MathsHelpers.Vector.FnDistance(start, end);//Long coord 
            if (MathsHelpers.Vector.FnEquals(LC, 0) || radius == 0) { return 0; } //Arc is bollocks - so get the hell out of Dodge (sorry calling method!)
            double DA = Math.Asin(LC / (Math.Abs(radius) * 2));//Deflection angle
            return (2 * DA * radius);
        }

        public static double ArcLength(double  startAngle, double  endAngle, double radius)
        {
            if (startAngle== endAngle || radius == 0) { return 0; } 
            double CA = MathsHelpers.Trig.FnSmallAngle(startAngle, endAngle);
            if (CA == 0)
            {
                return 0;
            }
            else
            {
                return CA * Math.Abs( radius);
                //return 2 * Math.PI * radius * ((CA * (180 / Math.PI)) / 360);
            }

        }

        #endregion

        #region Shapes

        /// <summary>
        /// CircleCentre - calculates the logical centre of a circle defined by an arc
        ///                shitty name need changing.
        /// </summary>
        /// <param name="StartPt"> start of the arc</param>
        /// <param name="endPt"> end of the acr</param>
        /// <param name="radius"> redius of the arc</param>
        /// <returns>The centre of the circle</returns>
        public static Point2D CircleCentre(Point2D StartPt, Point2D endPt, double radius)
        {

            double lc = 0, adj = 0, brg = 0;
            Point2D cent = new Point2D();
            Trig.RPC(StartPt.X, StartPt.Y, endPt.X, endPt.Y, ref brg, ref lc);
            adj = Math.Sqrt((Math.Pow(Math.Abs(radius), 2)) - (Math.Pow((lc / 2), 2)));

            if (radius >= 0)
            {
                brg = brg + Math.PI * 2;
                if (brg > 2 * Math.PI)
                {
                    brg = brg - 2 * Math.PI;
                }
            }
            else
            {
                brg = brg - Math.PI / 2;
                if (brg < 0)
                {
                    brg = brg + 2 * Math.PI;
                }
            }

            cent = Trig.PRC((StartPt.X + endPt.X) / 2, (StartPt.Y + endPt.Y) / 2, brg, adj);
            return cent;
        }

        /// <summary>
        /// Generates a series of <see cref="Point3D"/> representing a circle based on a given center, radius and number of segments
        /// </summary>
        /// <param name="originX">The X coordinate of the circle centre</param>
        /// <param name="originY">The Y coordinate of the circle centre</param>
        /// <param name="maxZ">The Z value of the 3D coordinates</param>
        /// <param name="radius">The radius of the circle</param>
        /// <param name="segments">The number of points to generate</param>
        /// <returns>Returns a List of <see cref="Point3D"/> containing the points of the circle</returns>
        public static List<Point3D> GenerateCircle(double originX, double originY, double maxZ, double radius, double segments)
        {
            var points = new List<Point3D>();
            const double twoPI = Math.PI * 2;
            double interval = twoPI / segments;
           
            for(int i = 0; i < segments; i++)
            {
                var angle = i * interval;
                points.Add(new Point3D(originX + (Math.Cos(angle) * radius), originY + (Math.Sin(angle) * radius), maxZ));
            }

            return points;

        }

        /// <summary>
        /// Generates a series of <see cref="Point2D"/> representing a circle based on a given center, radius and number of segments
        /// </summary>
        /// <param name="originX">The X coordinate of the circle centre</param>
        /// <param name="originY">The Y coordinate of the circle centre</param>
        /// <param name="radius">The radius of the circle</param>
        /// <param name="segments">The number of points to generate</param>
        /// <returns>Returns a List of <see cref="Point2D"/> containing the points of the circle</returns>
        public static List<Point2D> GenerateCircle(double originX, double originY, double radius, double segments)
        {
            var points = new List<Point2D>();
            const double twoPI = Math.PI * 2;
            double interval = twoPI / segments;

            for(int i = 0; i < segments; i++)
            {
                var angle = i * interval;
                points.Add(new Point2D(originX + (Math.Cos(angle) * radius), originY + (Math.Sin(angle) * radius)));
            }

            return points;
        }

        /// <summary>
        /// Generates points along an arc based on a given radius, start, and end coordinates
        /// </summary>
        /// <param name="startX">The start X coordinate of the Arc</param>
        /// <param name="startY">The start Y coordinate of the Arc</param>
        /// <param name="endX">The end X coordinate of the Arc</param>
        /// <param name="endY">The end Y coordinate of the Arc</param>
        /// <param name="radius">The radius of the Arc</param>
        /// <param name="numPoints">The number of points to be generated along the Arc</param>
        /// <returns>A List of <see cref="Point3D"/> to represent the generated Arc</returns>
        public static List<Point2D> GenerateArc(double startX, double startY, double endX, double endY, double radius, int numPoints)
        {
            var pointsList = new List<Point2D>();

            double brg = 0, dist = 0;
            Trig.GetBrgAndDist(startX, startY, endX, endY, ref brg, ref dist);

            //Calc center point
            var oppo = Math.Sqrt((radius * radius) - ((dist / 2) * (dist / 2)));
            double cx = 0, cy = 0;
            var clx = (startX + endX) / 2;
            var cly = (startY + endY) / 2;

            if (Vector.FnEquals(radius, (dist / 2)))
            {
                cx = clx;
                cy = cly;
            }
            else
            {
                if (radius < 0)
                    Trig.GetCoords(clx, cly, brg - (Math.PI / 2), oppo, ref cx, ref cy);
                else
                    Trig.GetCoords(clx, cly, brg + (Math.PI / 2), oppo, ref cx, ref cy);
            }

            double startAngle = 0, endAngle = 0;
            Trig.GetBrgAndDist(cx, cy, startX, startY, ref startAngle, ref dist);
            Trig.GetBrgAndDist(cx, cy, endX, endY, ref endAngle, ref dist);

            if (startAngle == 0 && endAngle == 0)
                endAngle = Math.PI * 2;

            if (radius >= 0)
            {
                if (endAngle < startAngle)
                    endAngle += (Math.PI * 2);
            }
            else
            {
                if (endAngle > startAngle)
                    startAngle += (Math.PI * 2);
            }

            double step = 0;

            if(numPoints == -1)
            {
                if (radius < 0)
                    step = -(Math.PI / 30);
                else
                    step = (Math.PI / 30);

                numPoints = (int)Math.Round((endAngle - startAngle) / step);
            }
            else
                step = (endAngle - startAngle) / numPoints;
            var currentAngle = startAngle;

            //Add Segements
            if (radius < 0)
            {
                radius *= -1;
                if (startAngle <= endAngle)
                    step *= -1;
            }
            else if (startAngle > endAngle)
                step *= -1;

            for (int i = 0; i < numPoints; i++)
            {
                double nX = 0, nY = 0;
                Trig.GetCoords(cx, cy, currentAngle, radius, ref nX, ref nY);
                pointsList.Add(new Point2D(nX, nY));
                currentAngle += step;
            }

            //Add last point
            pointsList.Add(new Point2D(endX, endY));

            return pointsList;
        }

        /// <summary>
        /// Generates the points for a sphere map centered at a given x, y, z with a given radius
        /// </summary>
        /// <param name="originX">The X value for the center of the sphere</param>
        /// <param name="originY">The Y value for the center of the sphere</param>
        /// <param name="originZ">The Z value for the center of the sphere</param>
        /// <param name="radius">The radius of the sphere</param>
        /// <param name="segments">The number of horizontal and vertical sphere segments</param>
        /// <returns>Returns a List of <see cref="Point3D"/> Lists containing the points of the sphere map</returns>
        public static List<List<Point3D>> GenerateSphereMap(double originX, double originY, double originZ, double radius, int segments)
        {
            var circleStack = new List<List<Point3D>>();

            int halfSegments = segments / 2;
            double curZ = originZ + radius, step = radius / halfSegments, curDist = radius;

            //First half
            for(int i = 0; i < halfSegments; i++)
            {
                var curRadius = Math.Sqrt((radius * radius) - (curDist * curDist));
                var points = GenerateCircle(originX, originY, curZ, curRadius, segments);
                //Add the first point to close the circle
                points.Add(points.First());
                circleStack.Add(points);
                curDist -= step;
                curZ -= step;
            }

            //Second half
            for(int i = 0; i <= halfSegments; i++)
            {
                var curRadius = Math.Sqrt((radius * radius) - (curDist * curDist));
                var points = GenerateCircle(originX, originY, curZ, curRadius, segments);
                //Add the first point to close the circle
                points.Add(points.First());
                circleStack.Add(points);
                curDist +=step;
                curZ -= step;
            }

            return circleStack;
        }

        /// <summary>
        /// Generates a polygon from a given line and sweep distance
        /// </summary>
        /// <param name="en1x">The X coordinate of the start of the line</param>
        /// <param name="en1y">The Y coordinate of the start of the line</param>
        /// <param name="en2x">The X coordinate of the end of the line</param>
        /// <param name="en2y">The Y coordinate of the end of the line</param>
        /// <param name="distance">The sweep distance for the polygon</param>
        /// <param name="centerLeftPoint">The center left point on the line</param>
        /// <param name="centerRightPoint">The center right point on the line</param>
        /// <param name="maxZ">The max Z value for the polygon</param>
        /// <returns>Returns the generated <see cref="Polygon"/></returns>
        public static Polygon GetPolyFromLineAndSweepDistance(double en1x, double en1y, double en2x, double en2y, double distance, ref Point3D centerLeftPoint, ref Point3D centerRightPoint, double maxZ)
        {
            var cX = (en1x + en2x) / 2;
            var cY = (en1y + en2y) / 2;
            double brg = 0, length = 0;

            Trig.GetBrgAndDist(en1x, en1y, en2x, en2y, ref brg, ref length, 1);
            brg = brg / (Math.PI / 180);
            double ctx = 0, cty = 0, ltx = 0, lty = 0, rtx = 0, rty = 0;
            double cbx = 0, cby = 0, lby = 0, lbx = 0, rby = 0, rbx = 0;
            Trig.GetCoords(cX, cY, (brg - 90) * (Math.PI / 180), distance / 2, ref ctx, ref cty);
            Trig.GetCoords(cX, cY, (brg + 90) * (Math.PI / 180), distance / 2, ref cbx, ref cby);
            Trig.GetCoords(ctx, cty, (brg + 180) * (Math.PI / 180), length / 2, ref ltx, ref lty);
            Trig.GetCoords(cbx, cby, (brg + 180) * (Math.PI / 180), length / 2, ref lbx, ref lby);
            Trig.GetCoords(ctx, cty, brg * (Math.PI / 180), length / 2, ref rtx, ref rty);
            Trig.GetCoords(cbx, cby, brg * (Math.PI / 180), length / 2, ref rbx, ref rby);

            double clpx = 0, clpy = 0, crpx = 0, crpy = 0;
            Trig.GetCoords(cX, cY, (brg) * (Math.PI / 180), length / 2, ref clpx, ref clpy);
            Trig.GetCoords(cX, cY, (brg + 180) * (Math.PI / 180), length / 2, ref crpx, ref crpy);
            centerLeftPoint = new Point3D(clpx, clpy, 0);
            centerRightPoint = new Point3D(crpx, crpy, 0);

            var poly = new Basic2DPolygon();
            poly.Points.Add(new Point3D(ltx, lty, maxZ));
            poly.Points.Add(new Point3D(lbx, lby, maxZ));
            poly.Points.Add(new Point3D(rbx, rby, maxZ));
            poly.Points.Add(new Point3D(rtx, rty, maxZ));
            return poly;
        }

        /// <summary>
        /// Generates a polygon from a given line and sweep distance
        /// </summary>
        /// <param name="en1x">The X coordinate of the start of the line</param>
        /// <param name="en1y">The Y coordinate of the start of the line</param>
        /// <param name="en2x">The X coordinate of the end of the line</param>
        /// <param name="en2y">The Y coordinate of the end of the line</param>
        /// <param name="distance">The sweep distance for the polygon</param>
        /// <param name="section">The section object to use</param>
        /// <param name="maxZ">The max Z value for the polygon</param>
        /// <returns>Returns the generated <see cref="Polygon"/></returns>
        public static Polygon GetPolyFromLineAndSweepDistance(double en1x, double en1y, double en2x, double en2y, double distance, ref CrossSection section, double maxZ)
        {
            var centerLeftPoint = new Point3D();
            var centerRightPoint = new Point3D();
            var poly = GetPolyFromLineAndSweepDistance(en1x, en1y, en2x, en2y, distance, ref centerLeftPoint, ref centerRightPoint, maxZ);
            section.SectionCenterLeft = centerLeftPoint.Copy();
            section.SectionCenterRight = centerRightPoint.Copy();
            section.SectionCenter = (centerLeftPoint + centerRightPoint) / 2;
            return poly;
        }

        /// <summary>
        /// Adds the x, y and z value to a given vertex array and the r, g and b color to a given color array
        /// </summary>
        /// <param name="vertices">A <see cref="float"/> array containing the vertices</param>
        /// <param name="colors">A <see cref="float"/> array contianing the colors</param>
        /// <param name="index">The current index of the arrays</param>
        /// <param name="offsetX">The X value of the coordinate shift</param>
        /// <param name="offsetY">The Y value of the coordinate shift</param>
        /// <param name="offsetZ">The Z value of the coordinate shift</param>
        /// <param name="x">The X value for the vertex</param>
        /// <param name="y">The Y value for the vertex</param>
        /// <param name="z">The Z value for the vertex</param>
        /// <param name="r">The red channel for the vertex</param>
        /// <param name="g">The green channel for the vertex</param>
        /// <param name="b">The blue channel for the vertex</param>
        private static void AddValue(ref float[] vertices, ref float[] colors, ref int index, float offsetX, float offsetY, float offsetZ, float x, float y, float z, float r, float g, float b)
        {
            vertices[index] = x - offsetX;
            colors[index++] = r;

            vertices[index] = y - offsetY;
            colors[index++] = g;

            vertices[index] = z - offsetZ;
            colors[index++] = b;
        }

        public static bool LegacyBestFitCircle(IEnumerable<Point2D> Points, ref Point2D center, ref double radius)
        {
            int i = 0;
            int n = 0;
            double x = 0, y = 0;


            
            try
            {
                int numlines = 0;
                for (i = 1; i <= Points.Count()+1; i++)
                {
                    numlines = numlines + i;

                }
                i = 0;
                n = Points.Count() * (Points.Count() - 1) * (Points.Count() - 2) / 2;
                double[] g = new double[numlines];
                double[] xa = new double[numlines];
                double[] ya = new double[numlines];
                int[] ptr = new int[numlines];
                double[] cx = new double[n];
                double[] cy = new double[n];


                for (int p1 = 0; p1 < Points.Count() - 1; p1++)
                {
                    for (int p2 = p1 + 1; p2 < Points.Count(); p2++)
                    {
                        if (Points.ElementAt(p1).Y - Points.ElementAt(p2).Y == 0)
                        {
                            g[i] = 1E+20;//double.PositiveInfinity;
                        }
                        else
                        {
                            g[i] = (Points.ElementAt(p2).X - Points.ElementAt(p1).X) / (Points.ElementAt(p1).Y - Points.ElementAt(p2).Y);
                        }
                        xa[i] = Points.ElementAt(p2).X + (Points.ElementAt(p1).X - Points.ElementAt(p2).X) / 2;
                        ya[i] = Points.ElementAt(p2).Y + (Points.ElementAt(p1).Y - Points.ElementAt(p2).Y) / 2;
                        i++;
                    }
                }

                n = 0;
                double tol = 0.1;
                bool finished = false;
                int finish = 0;
                int n2 = 0;
                int n3 = 0;
                int j = 0;

                do
                {
                    int m = -Points.Count();
                    int k = 0;
                    int l = Points.Count() - 2;

                    for (i = 0; i <= Points.Count() - 1; i++)
                    {
                        finish = Points.Count() - 2;
                        n2 = 0; n3 = Points.Count() - 2;
                        j = i - 1;
                        while (j <= m && n3 == 0)
                        {
                            ptr[n2] = j;
                            n2 = n2 + 1;
                            j = j + n3; n3 = n3 - 1;
                        }
                        for (j = k; j <= l; j++)
                        {
                            ptr[n2] = j;
                            n2 = n2 + 1;
                        }
                        k = k + Points.Count() -i- 1; l = l + Points.Count() - i - 2;
                        m = m + Points.Count() - i;

                        for (int p1 = 0; p1 <= finish; p1++)
                        {
                            int intPtrP1 = ptr[p1];
                            double dblG1 = g[intPtrP1];
                            double a1 = Math.Atan(dblG1);
                            for (int p2 = p1 + 1; p2 <= finish; p2++)
                            {
                                int intPtrP2 = ptr[p2];
                                double dblG2 = g[intPtrP2];
                                double a2 = Math.Atan(dblG2);

                                if (Math.Abs(a2 - a1) > tol && Math.Abs(a2 - a1) < (Math.PI - tol))
                                {
                                    x = xa[intPtrP1] * dblG1 - xa[intPtrP2] * dblG2 - ya[intPtrP1] + ya[intPtrP2];
                                    x = x / (dblG1 - dblG2);
                                    y = ya[intPtrP1] * dblG2 - ya[intPtrP2] * dblG1 + (xa[intPtrP2] - xa[intPtrP1]) * dblG1*dblG2;
                                    y = y / (dblG2 - dblG1);

                                    cx[n] = x;
                                    cy[n] = y;
                                    n++;
                                }

                            }
                        }
                    }

                    if (n > 0 | tol < 0.1)
                    {
                        finished = true;
                        break;
                    }
                    tol = 0.00001;
                    n = 0;

                } while (finished == false);

                if (n > 0)
                {
                    double xc = 0, yc = 0;
                    for (i = 0; i < n; i++)
                    {
                        xc = xc + cx[i];
                        yc = yc + cy[i];
                    }
                    center.X = xc / n;
                    center.Y = yc / n;

                    double r = 0;
                    for (i = 0; i < Points.Count(); i++)
                    {
                        double d = MathsHelpers.Vector.FnDistance(center.X, center.Y, Points.ElementAt(i).X, Points.ElementAt(i).Y);
                        r = r + d;
                    }
                    radius = r / Points.Count();

                    return true;
                }
                else
                {
                    radius = 0;
                    return false;
                }
            }
            catch
            {

                return false;
            }

        }
        
        /// <summary>
        /// Attempts to fit a circle to a given list of x and y vals
        /// Taken from: https://stackoverflow.com/questions/44647239/how-to-fit-a-circle-to-a-set-of-points-with-a-constrained-radius
        /// </summary>
        /// <param name="xVals">List of X values for the points</param>
        /// <param name="yVals">List of Y values for the points</param>
        /// <param name="center">A <see cref="Point2D"/> to store the center point of the fitted circle</param>
        /// <param name="radius">A <see cref="double"/> to store the radius of the fitted circle</param>
        public static bool BestFitCircle(IEnumerable<Point2D> points, ref Point2D center, ref double radius)
        {

            //We really need a matrix object for doubles, GlmNet only handles floats
            var lhs = new double[3, 3] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
            var rhs = new double[3, 3] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };

            /*
                         A        B        C       R         R
                G1:  +2*a*x^2 +2*b*x*y  +2*c*x  +2*x^3    +2*x*y^2  = 0
                G2:  +2*a*x*y +2*b*y^2  +2*c*y  +2*y^3    +2*x^2*y  = 0
                G3:  +2*a*x   +2*b*y    +2*c    +2*y^2    +2*x^2    = 0
            */

            if (points.Count() < 3)
                return false;

            for(int i = 0; i < points.Count(); i++)
            {
                var point = points.ElementAt(i);
                var x1 = point.X;
                var x2 = Math.Pow(x1, 2);
                var x3 = Math.Pow(x1, 3);
                var y1 = point.Y;
                var y2 = Math.Pow(y1, 2);
                var y3 = Math.Pow(y1, 3);

                //col 0 = A / col 1 = B / col 2 = C

                //Row 0 = G1
                lhs[0, 0] += 2 * x2;
                lhs[0, 1] += 2 * x1 * y1;
                lhs[0, 2] += 2 * x1;

                rhs[0, 0] -= 2 * x3 + 2 * x1 * y2;

                //Row 1 = G2
                lhs[1, 0] += 2 * x1 * y1;
                lhs[1, 1] += 2 * y2;
                lhs[1, 2] += 2 * y1;

                rhs[1, 0] -= 2 * y3 + 2 * x2 * y1;

                //Row 2 = G3
                lhs[2, 0] += 2 * x1;
                lhs[2, 1] += 2 * y1;
                lhs[2, 2] += 2;

                rhs[2, 0] -= 2 * y2 + 2 * x2;
            }

            var lhsMatrix = Matrix<double>.Build.DenseOfArray(lhs);
            var rhsMatrix = Matrix<double>.Build.DenseOfArray(rhs);

            var solutionMatrix = lhsMatrix.Solve(rhsMatrix);
            //Solve the linear exquation

            var abc = new double[] { solutionMatrix[0, 0], solutionMatrix[1, 0], solutionMatrix[2, 0] };

            center = new Point2D(abc[0] / -2.0, abc[1] / -2.0);
            radius = Math.Sqrt(Math.Abs(Math.Pow(center.X, 2) + Math.Pow(center.Y, 2) - abc[2]));

            return true;
        }

        public static bool InPoly2D(IEnumerable<Point2D> points, double pX, double pY)
        {
            bool isInPoly = false;
            int il = 0;

            //start and enpoints of a line seg;
            double sx = 0;
            double sy = 0;
            double ex = 0;
            double ey = 0;
            int crossings = 0;

            int numLasso = points.Count();

            //if we have less than two points we don't have a polygon
            if (numLasso < 2)
                return false;

            //go through each line segement in the polygon
            for(int i = 0; i < numLasso; i++)
            {
                //get the start and end points of the line relative to origin x,y
                il = i - 1;
                if (il == -1)
                    il = numLasso - 1;

                var start = points.ElementAt(il);
                var end = points.ElementAt(i);

                sx = start.X - pX;
                sy = start.Y - pY;
                ex = end.X - pX;
                ey = end.Y - pY;

                if ((sx == 0 && sy == 0) || (ex == 0 && ey == 0))
                    return true;
               
                //check if the line segment sx, sy - ex, ey stradlles the x axis
                if(ey > 0 && sy <= 0)
                {
                    //calculate the intersection with x axis
                    double ix = (ex * sy - sx * ey) / (double)(sy - ey);

                    //if this intersection is strictly +ve we have a crossing
                    if (ix >= 0)
                        crossings++;
                }

                if(sy > 0 && ey <= 0)
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
        /// Calculates the area of a polygon formed from a list of points. Points must be sorted radially beforehand
        /// </summary>
        /// <param name="points">The list of points that define the polygon</param>
        /// <returns>Returns a <see cref="double"/> containing the area of the polygon</returns>
        public static double CalcAreaOfAPolygon(IEnumerable<Point2D> points)
        {
            var area = 0D;

            if (points == null || points.Count() <= 0)
                return area;

            Point2D curPoint;
            Point2D nextPoint;

            for(int i = 0; i < points.Count() - 1; i++)
            {
                curPoint = points.ElementAt(i);
                nextPoint = points.ElementAt(i + 1);

                area += ((nextPoint.X - curPoint.X) * (nextPoint.Y + curPoint.Y)) / 2;
            }

            //Do last
            curPoint = points.Last();
            nextPoint = points.First();

            area += ((nextPoint.X - curPoint.X) * (nextPoint.Y + curPoint.Y)) / 2;

            return Math.Abs(area);
        }

        public static Point2D CalcCentroid(IEnumerable<Point2D> points, double area)
        {
            var center = new Point2D();


            if (area == 0)
                return center;

            if(points.Count() <= 2)
            {
                center.X = (points.First().X + points.Last().X) / 2;
                center.Y = (points.First().Y + points.Last().Y) / 2;
                return center;
            }

            double xSummation = 0, ySummation = 0, xDiff = 0, yDiff = 0, xSum1 = 0, ySum1 = 0, xSum2 = 0 , ySum2 = 0;

            Point2D curPoint;
            Point2D nextPoint;

            for(int i = 0; i < points.Count() - 1; i++)
            {
                curPoint = points.ElementAt(i);
                nextPoint = points.ElementAt(i + 1);
                xDiff = nextPoint.X - curPoint.X;
                xSum1 = nextPoint.X + curPoint.X;
                xSum2 = 2 * nextPoint.X + curPoint.X;

                yDiff = nextPoint.Y - curPoint.Y;
                ySum1 = nextPoint.Y + curPoint.Y;
                ySum2 = 2 * nextPoint.Y + curPoint.Y;

                xSummation += xDiff * xDiff * ySum2 / 6 + curPoint.X * xDiff * ySum1 / 2;
                ySummation += yDiff * yDiff * xSum2 / 6 + curPoint.Y * yDiff * xSum1 / 2;
            }

            //Do last point
            curPoint = points.Last();
            nextPoint = points.First();

            xDiff = nextPoint.X - curPoint.X;
            xSum1 = nextPoint.X + curPoint.X;
            xSum2 = 2 * nextPoint.X + curPoint.X;

            yDiff = nextPoint.Y - curPoint.Y;
            ySum1 = nextPoint.Y + curPoint.Y;
            ySum2 = 2 * nextPoint.Y + curPoint.Y;

            xSummation += xDiff * xDiff * ySum2 / 6 + curPoint.X * xDiff * ySum1 / 2;
            ySummation += yDiff * yDiff * xSum2 / 6 + curPoint.Y * yDiff * xSum1 / 2;

            //Calc center points
            center.X = -xSummation / area;
            center.Y = ySummation / area;

            return center;
        }

        /// <summary>
        /// Generates the arrays of floats required to draw a series of triangles with transparency in OpenGL
        /// </summary>
        /// <param name="pointsList">A List containing the Lists of <see cref="Point3D"/>s to be turned into a triangle model</param>
        /// <param name="vertices">An array of <see cref="float"/>s to store the vertex points for the triangles</param>
        /// <param name="rgba">An array of <see cref="float"/>s to store the rgba values for the triangles</param>
        /// <param name="r">The Red color channel for the transparent triangles</param>
        /// <param name="g">The Green color channel for the transparent triangles</param>
        /// <param name="b">The Blue color channel for the transparent triangles</param>
        /// <param name="a">The Alpha channel for the transparent triangles</param>
        /// <param name="cX">The X offset for the rendered vertices</param>
        /// <param name="cY">The Y offset for the rendered vertices</param>
        public static void CalcShadedTransparentTriangles(List<List<Point3D>> pointsList, ref float[] vertices, ref float[] rgba, float r, float g, float b, float a, double cX, double cY, bool shadeVertically = true)
        {
            try
            {
                const double deg90 = Math.PI / 2, deg180 = Math.PI, deg270 = Math.PI * 1.5, deg360 = Math.PI * 2;
                const double factor90 = 1 / deg90;

                var numTriangles = (pointsList.Count - 1) * ((pointsList.First().Count - 1) * 2 * 3);
                if (numTriangles <= 0)
                    return;

                vertices = new float[numTriangles * 3];
                rgba = new float[numTriangles * 4];
                int index = 0, tIndex = 0;
                float factor = 1.0f;
                float sR = 0f, sG = 0f, sB = 0f;

                for (int i = 0; i < pointsList.Count - 1; i++)
                {
                    //First element to next element
                    var firstList = pointsList[i];
                    var nextList = pointsList[i + 1];

                    if (firstList.Count != nextList.Count)
                        continue;

                    var triangleCount = firstList.Count * 2;
                    var counter = 0;

                    for (int j = 0; j < triangleCount; j += 2)
                    {
                        if (counter >= firstList.Count - 1)
                            break;

                        Point3D A1 = firstList[counter], A2 = nextList[counter], A3 = firstList[counter + 1];
                        Point3D B1 = firstList[counter + 1], B2 = nextList[counter], B3 = nextList[counter + 1];
                        counter++;

                        //Calc scale for first triangle
                        double brg = 0;
                        if (shadeVertically)
                            brg = Trig.RPC(A1.X, A1.Z, A3.X, A3.Z);
                        else
                            brg = Trig.RPC(A1.X, A1.Y, A3.X, A3.Y);

                        if (brg <= deg90)
                            factor = (float)(1 - (Math.Abs(brg - deg90) * factor90));
                        else if (brg <= deg180)
                            factor = (float)(Math.Abs(brg - deg180) * factor90);
                        else if (brg <= deg270)
                            factor = (float)(1 - (Math.Abs(brg - deg270) * factor90));
                        else if (brg <= deg360)
                            factor = (float)(Math.Abs(brg - deg360) * factor90);

                        sR = (r * 0.5f) + ((r * 0.5f) * factor);
                        sG = (g * 0.5f) + ((g * 0.5f) * factor);
                        sB = (b * 0.5f) + ((b * 0.5f) * factor);

                        //A1
                        vertices[index++] = (float)(A1.X - cX);
                        vertices[index++] = (float)(A1.Y - cY);
                        vertices[index++] = (float)(A1.Z);

                        rgba[tIndex++] = sR;
                        rgba[tIndex++] = sG;
                        rgba[tIndex++] = sB;
                        rgba[tIndex++] = a;

                        //A2
                        vertices[index++] = (float)(A2.X - cX);
                        vertices[index++] = (float)(A2.Y - cY);
                        vertices[index++] = (float)(A2.Z);

                        rgba[tIndex++] = sR;
                        rgba[tIndex++] = sG;
                        rgba[tIndex++] = sB;
                        rgba[tIndex++] = a;

                        //A3
                        vertices[index++] = (float)(A3.X - cX);
                        vertices[index++] = (float)(A3.Y - cY);
                        vertices[index++] = (float)(A3.Z);

                        rgba[tIndex++] = sR;
                        rgba[tIndex++] = sG;
                        rgba[tIndex++] = sB;
                        rgba[tIndex++] = a;

                        //Calc scale for second triangle
                        if (shadeVertically)
                            brg = Trig.RPC(B2.X, B2.Z, B3.X, B3.Z);
                        else
                            brg = Trig.RPC(B2.X, B2.Y, B3.X, B3.Y);

                        if (brg <= deg90)
                            factor = (float)(1 - (Math.Abs(brg - deg90) * factor90));
                        else if (brg <= deg180)
                            factor = (float)(Math.Abs(brg - deg180) * factor90);
                        else if (brg <= deg270)
                            factor = (float)(1 - (Math.Abs(brg - deg270) * factor90));
                        else if (brg <= deg360)
                            factor = (float)(Math.Abs(brg - deg360) * factor90);

                        sR = (r * 0.5f) + ((r * 0.5f) * factor);
                        sG = (g * 0.5f) + ((g * 0.5f) * factor);
                        sB = (b * 0.5f) + ((b * 0.5f) * factor);

                        //B1
                        vertices[index++] = (float)(B1.X - cX);
                        vertices[index++] = (float)(B1.Y - cY);
                        vertices[index++] = (float)(B1.Z);

                        rgba[tIndex++] = sR;
                        rgba[tIndex++] = sG;
                        rgba[tIndex++] = sB;
                        rgba[tIndex++] = a;

                        //B2
                        vertices[index++] = (float)(B2.X - cX);
                        vertices[index++] = (float)(B2.Y - cY);
                        vertices[index++] = (float)(B2.Z);

                        rgba[tIndex++] = sR;
                        rgba[tIndex++] = sG;
                        rgba[tIndex++] = sB;
                        rgba[tIndex++] = a;

                        //B3
                        vertices[index++] = (float)(B3.X - cX);
                        vertices[index++] = (float)(B3.Y - cY);
                        vertices[index++] = (float)(B3.Z);

                        rgba[tIndex++] = sR;
                        rgba[tIndex++] = sG;
                        rgba[tIndex++] = sB;
                        rgba[tIndex++] = a;
                    }
                }
            }
            catch
            {
                vertices = new float[0];
                rgba = new float[0];
                return; 
            }
        }

        /// <summary>
        /// Generates the arrays of floats required to draw a series of opaque triangles in OpenGL
        /// </summary>
        /// <param name="pointsList">A List containing the Lists of <see cref="Point3D"/>s to be turned into a triangle model</param>
        /// <param name="vertices">An array of <see cref="float"/>s to store the vertex points for the trianges</param>
        /// <param name="rgb">An array of <see cref="float"/>s to store the rgb values for the triangles</param>
        /// <param name="r">The Red color channel for the triangles</param>
        /// <param name="g">The Green color channel for the triangles</param>
        /// <param name="b">The Blue color channel for the triangles</param>
        /// <param name="cX">The X offset for the rendered vertices</param>
        /// <param name="cY">The Y offset for the rendered vertices</param>
        public static void CalcShadedTriangles(List<List<Point3D>> pointsList, ref float[] vertices, ref float[] rgb, float r, float g, float b, double cX, double cY, bool shadeVertically = true)
        {
            try
            {
                const double deg90 = Math.PI / 2, deg180 = Math.PI, deg270 = Math.PI * 1.5, deg360 = Math.PI * 2;
                const double factor90 = 1 / deg90;

                var numTriangles = (pointsList.Count - 1) * ((pointsList.First().Count - 1) * 2 * 3);
                if (numTriangles <= 0)
                    return;

                vertices = new float[numTriangles * 3];
                rgb = new float[numTriangles * 3];
                int index = 0;
                float factor = 1.0f;
                float sR = 0f, sG = 0f, sB = 0f;

                for (int i = 0; i < pointsList.Count - 1; i++)
                {
                    List<Point3D> firstList, nextList;

                    //First element to next element
                    firstList = pointsList[i];
                    nextList = pointsList[i + 1];

                    if (firstList.Count != nextList.Count)
                        continue;

                    var triangleCount = firstList.Count * 2;
                    var counter = 0;

                    for (int j = 0; j < triangleCount; j += 2)
                    {
                        if (counter >= firstList.Count - 1)
                            break;

                        Point3D A1 = firstList[counter], A2 = nextList[counter], A3 = firstList[counter + 1];
                        Point3D B1 = firstList[counter + 1], B2 = nextList[counter], B3 = nextList[counter + 1];
                        counter++;

                        //Calc color scale for first triangle
                        double brg = 0;
                        if (shadeVertically)
                            brg = Trig.RPC(A1.X, A1.Z, A3.X, A3.Z);
                        else
                            brg = Trig.RPC(A1.X, A1.Y, A3.X, A3.Y);

                        //Possibly can be optimized a bit, this calc is done for each pointsList when they should all be the same
                        //Need to check if the color scale changes when there is shape rotation before doing the calc only once
                        if (brg <= deg90)
                            factor = (float)(1 - Math.Abs(brg - deg90) * factor90);
                        else if (brg <= deg180)
                            factor = (float)(Math.Abs(brg - deg180) * factor90);
                        else if (brg <= deg270)
                            factor = (float)(1 - Math.Abs(brg - deg270) * factor90);
                        else if (brg <= deg360)
                            factor = (float)(Math.Abs(brg - deg360) * factor90);

                        sR = (r * 0.5f) + ((r * 0.5f) * factor);
                        sG = (g * 0.5f) + ((g * 0.5f) * factor);
                        sB = (b * 0.5f) + ((b * 0.5f) * factor);

                        //A1
                        vertices[index] = (float)(A1.X - cX);
                        rgb[index++] = sR;

                        vertices[index] = (float)(A1.Y - cY);
                        rgb[index++] = sG;

                        vertices[index] = (float)A1.Z;
                        rgb[index++] = sB;

                        //A2
                        vertices[index] = (float)(A2.X - cX);
                        rgb[index++] = sR;

                        vertices[index] = (float)(A2.Y - cY);
                        rgb[index++] = sG;

                        vertices[index] = (float)A2.Z;
                        rgb[index++] = sB;

                        //A3
                        vertices[index] = (float)(A3.X - cX);
                        rgb[index++] = sR;

                        vertices[index] = (float)(A3.Y - cY);
                        rgb[index++] = sG;

                        vertices[index] = (float)A3.Z;
                        rgb[index++] = sB;

                        //Calc color scale for second triangle
                        if (shadeVertically)
                            brg = Trig.RPC(B2.X, B2.Z, B3.X, B3.Z);
                        else
                            brg = Trig.RPC(B2.X, B2.Y, B3.X, B3.Y);

                        if (brg <= deg90)
                            factor = (float)(1 - (Math.Abs(brg - deg90) * factor90));
                        else if (brg <= deg180)
                            factor = (float)(Math.Abs(brg - deg180) * factor90);
                        else if (brg <= deg270)
                            factor = (float)(1 - (Math.Abs(brg - deg270) * factor90));
                        else if (brg <= deg360)
                            factor = (float)(Math.Abs(brg - deg360) * factor90);

                        sR = (r * 0.5f) + ((r * 0.5f) * factor);
                        sG = (g * 0.5f) + ((g * 0.5f) * factor);
                        sB = (b * 0.5f) + ((b * 0.5f) * factor);

                        //B1
                        vertices[index] = (float)(B1.X - cX);
                        rgb[index++] = sR;

                        vertices[index] = (float)(B1.Y - cY);
                        rgb[index++] = sG;

                        vertices[index] = (float)B1.Z;
                        rgb[index++] = sB;

                        //B2
                        vertices[index] = (float)(B2.X - cX);
                        rgb[index++] = sR;

                        vertices[index] = (float)(B2.Y - cY);
                        rgb[index++] = sG;

                        vertices[index] = (float)B2.Z;
                        rgb[index++] = sB;

                        //B3
                        vertices[index] = (float)(B3.X - cX);
                        rgb[index++] = sR;

                        vertices[index] = (float)(B3.Y - cY);
                        rgb[index++] = sG;

                        vertices[index] = (float)B3.Z;
                        rgb[index++] = sB;
                    }
                }
            }
            catch
            {
                vertices = new float[0];
                rgb = new float[0];
                return;
            }
        }

        /// <summary>
        /// Generates an array of line strip points representing a square drawn around a line start and end 
        /// </summary>
        /// <param name="vertices">A <see cref="float"/> array to containing the verticies of the square</param>
        /// <param name="colors">A <see cref="float"/> array to contain the color data of the square</param>
        /// <param name="distance">The distance to draw the square around the provided line</param>
        /// <param name="sx">The X value of the start of the line to draw a square around</param>
        /// <param name="sy">The Y value of the start of the line to draw a square around</param>
        /// <param name="ex">The X value of the end of the line to draw a square around</param>
        /// <param name="ey">The Y value of the end of the line to draw a square around</param>
        /// <param name="mode">The mode to draw the square, 0 is locked to the Z plane and 1 is locked to the XY plane</param>
        /// <param name="zVal">The Z value to begin the square at</param>
        /// <param name="r">A <see cref="float"/> representing the Red color channel of the square</param>
        /// <param name="g">A <see cref="float"/> representing the Green color channel of the square</param>
        /// <param name="b">A <see cref="float"/> representing the Blue color channel of the square</param>
        public static void GenerateSquareLineStripsFromLine(ref float[] vertices, ref float[] colors, double distance, double sx, double sy, double ex, double ey, int mode, double zVal, float r = 0.0f, float g = 0.0f, float b = 0.0f)
        {
            vertices = new float[24];
            colors = new float[24];

            var points = new List<Point3D>();
            double newX = 0, newY = 0;

            if (mode == 0)
            {
                //Operate on the Z plane
                var brg = Trig.RPC(sx, sy, ex, ey);
                brg += (Math.PI / 2);
                Trig.PRC(sx, sy, brg, distance, ref newX, ref newY);
                points.Add(new Point3D(newX, newY, zVal));
                Trig.PRC(sx, sy, brg, -distance, ref newX, ref newY);
                points.Add(new Point3D(newX, newY, zVal));
                Trig.PRC(ex, ey, brg, -distance, ref newX, ref newY);
                points.Add(new Point3D(newX, newY, zVal));
                Trig.PRC(ex, ey, brg, distance, ref newX, ref newY);
                points.Add(new Point3D(newX, newY, zVal));
            }
            else if(mode == 1)
            {
                points.Add(new Point3D(sx, sy, zVal - distance));
                points.Add(new Point3D(ex, ey, zVal - distance));
                points.Add(new Point3D(ex, ey, zVal + distance));
                points.Add(new Point3D(sx, sy, zVal + distance));
            }

            //Convert the points into an array of vertices and colors
            int index = 0, prev = 0;
            for(int i = 0; i < points.Count; i++)
            {
                prev = i - 1;
                if (prev == -1)
                    prev = points.Count - 1;

                var start = points[prev];
                var end = points[i];

                vertices[index] = (float)start.X;
                colors[index++] = r;

                vertices[index] = (float)start.Y;
                colors[index++] = g;

                vertices[index] = (float)start.Z;
                colors[index++] = b;

                vertices[index] = (float)end.X;
                colors[index++] = r;

                vertices[index] = (float)end.Y;
                colors[index++] = g;

                vertices[index] = (float)end.Z;
                colors[index++] = b;
            }
        }

        #endregion

        #region Lines

        /// <summary>
        /// CalculateSplineCoords - given a list of poinhts calulates a splined line of best fit
        /// Added by ES:26.06.19 - roughly based on the routine of the same name in VB SPLINE.bas
        /// </summary>
        /// <param name="points">list of point2D you wish to spline through</param>
        /// <param name="SegmentLength">Interval of output point</param>
        /// <returns>List of Point2D containing the new splined coords</returns>
        public static List<Point2D> CalculateSplineCoords(IEnumerable<Point2D> points, double SegmentLength, ref BackgroundWorker worker)
        {
            List<Point2D> Coords = new List<Point2D>();

            Coords = points.ToList();

            List<Point2D> OutPoints = new List<Point2D>();

            if (Coords.Count >= 3)
            {
                double[] XO = new double [Coords.Count]; double[] X1 = new double[Coords.Count]; double[] X2 = new double[Coords.Count]; double[] X3 = new double[Coords.Count]; double[] Y0 = new double[Coords.Count]; double[] Y1 = new double[Coords.Count]; double[] Y2 = new double[Coords.Count]; double[] Y3 = new double[Coords.Count];
                List<double> Lengths = new List<double>();
                CalculateSplines(Coords, true,Lengths, XO, X1, X2, X3, Y0, Y1, Y2, Y3);
                double totLength = 0;
                Point2D pt = new Point2D();

                foreach (double len in Lengths)
                {
                    totLength = totLength + len;
                }

                if (SegmentLength == 0)
                {
                    SegmentLength = totLength / 10;
                }

                int numsegs = Convert.ToInt32((totLength / SegmentLength) + Coords.Count + 2);

                for (int i = 0; i < Coords.Count-1 ; i++)
                {
                    if (worker.CancellationPending)
                        return Coords;

                    pt = new Point2D(Coords.ElementAt(i).X, Coords.ElementAt(i).Y);
                    OutPoints.Add(pt);
                    double StartL = 0;
                    if (StartL < SegmentLength) { StartL = SegmentLength;}
                    double EndL = Vector.FnDistance(Coords.ElementAt(i).X, Coords.ElementAt(i).Y, Coords.ElementAt(i + 1).X, Coords.ElementAt(i+1).Y);
                    if (EndL < Lengths.ElementAt(i)) { EndL = Lengths.ElementAt(i); }
                    for (double dist = StartL; dist < EndL; dist = dist + SegmentLength)
                    {
                        pt = new Point2D();
               
                        pt.X = XO[i] + X1[i] * dist + X2[i] * dist * dist + X3[i] * dist * dist * dist;
                        pt.Y = Y0[i] + Y1[i] * dist + Y2[i] * dist * dist + Y3[i] * dist * dist * dist;

                        OutPoints.Add(pt);

                        if (OutPoints.Count == 804)
                        {
                            Console.WriteLine ("804");
                        }
                    }
                }

                pt = new Point2D(Coords.ElementAt(Coords.Count - 1).X, Coords.ElementAt(Coords.Count - 1).Y);
                OutPoints.Add(pt);
                return OutPoints;
            }

            return null;

        }

        /// <summary>
        /// CalulateSplines - given a list of Point2D calculates best fit spline
        /// Added by ES:26.06.19 from spline.bas
        /// </summary>
        /// <param name="Coords">List of Point2d you wish to </param>
        /// <param name="fixedeEnds">T/F the spline ends are fixed to the start & end of original data</param>
        /// <param name="Lengths">Distances between original points</param>
        /// <param name="XO"></param>
        /// <param name="X1"></param>
        /// <param name="X2"></param>
        /// <param name="X3"></param>
        /// <param name="YO"></param>
        /// <param name="Y1"></param>
        /// <param name="Y2"></param>
        /// <param name="Y3"></param>
        private static void CalculateSplines(List <Point2D> Coords,bool fixedeEnds,  List<double> Lengths, double[] XO, double[] X1, double[] X2, double [] X3, double[] YO, double[] Y1, double[]Y2, double[]Y3 )
        {
            
            int j = 0;

            //====================================================================================
            //Remove any duplication

            //Removed the foreach to avoid an exception caused by removing a point from a foreach
            for (int i = 0; i < Coords.Count - 1; i++)
            {
                var point1 = Coords[i];
                var point2 = Coords[i + 1];

                var dist = Vector.FnDistance(point1.X, point1.Y, point2.X, point2.Y);
                if (dist == 0)
                {
                    Coords.RemoveAt(i);
                    i--;
                }
                else
                    Lengths.Add(dist);
            }

            //foreach (Point2D pt in Coords)
            //{
            //    if (j < Coords.Count)
            //    {
            //        for (int i = j + 1; i < Coords.Count; i++)
            //        {
            //            Point2D testPt = new Point2D();
            //            testPt = Coords.ElementAt(i);
            //            double dist = Vector.FnDistance(pt.X, pt.Y, testPt.X, testPt.Y);
            //            if (dist == 0)
            //            {
            //                Coords.RemoveAt(i);
            //            }
            //            else
            //            {
            //                Lengths.Add(dist);
            //                break;
            //            }
            //        }
            //    }
            //    j++;
            //}
            //===================================================================================

            double[,] n = new double[ 4, Coords.Count + 1];
            double[,] b = new double[ 4, Coords.Count + 1];
            double[,] U = new double[4, Coords.Count + 1];

            if (fixedeEnds == true)
            {
                n[1, 0] = 1; n[2, 0] = 0.5;
                
                b[0, 0] = (3 / (2 * Lengths.ElementAt(0))) * (Coords.ElementAt(1).X - Coords.ElementAt(0).X);
                b[1, 0] = (3 / (2 * Lengths.ElementAt(0))) * (Coords.ElementAt(1).Y - Coords.ElementAt(0).Y);

                n[ 0, Coords.Count - 1] = 2; n[1,Coords.Count - 1] = 4;

                b[0, Coords.Count - 1] = (6 / Lengths.ElementAt(Lengths.Count - 2)) * (Coords.ElementAt(Coords.Count - 1).X - Coords.ElementAt(Coords.Count - 2).X);
                b[1, Coords.Count - 1] = (6 / Lengths.ElementAt(Lengths.Count - 2)) * (Coords.ElementAt(Coords.Count - 1).Y - Coords.ElementAt(Coords.Count - 2).Y);
            }
            else
            {
                n[1, 0] = 1;n[2,0] = 0.5;
                b[0, 0] = U[0,0];
                b[1, 0] = U[1, 0];

                n[0,Coords.Count - 1] = 0; n[1, Coords.Count - 1] = 0;
                b[0, Coords.Count - 1] = U[0, Coords.Count - 1];
                b[1, Coords.Count - 1] = U[1, Coords.Count - 1];
            }


            for ( j = 1; j <= Coords.Count - 2; j++)
            {
                n[0,j] = Lengths.ElementAt(j);
                n[1,j] = 2 * (Lengths.ElementAt(j) + Lengths.ElementAt(j - 1));
                n[2,j] = Lengths.ElementAt(j - 1);

                //==================================================================
                b[0, j] = (Lengths.ElementAt(j - 1) * Lengths.ElementAt(j - 1)) * (Coords.ElementAt(j + 1).X - Coords.ElementAt(j).X);
                b[0, j] = b[0, j] + (Lengths.ElementAt(j) * Lengths.ElementAt(j)) * (Coords.ElementAt(j).X - Coords.ElementAt(j - 1).X);
                b[0, j] = 3 * b[0, j] / (Lengths.ElementAt(j) * Lengths.ElementAt(j - 1));

                b[1, j] = (Lengths.ElementAt(j - 1) * Lengths.ElementAt(j - 1)) * (Coords.ElementAt(j + 1).Y - Coords.ElementAt(j).Y);
                b[1, j] = b[1, j] + (Lengths.ElementAt(j) * Lengths.ElementAt(j)) * (Coords.ElementAt(j).Y - Coords.ElementAt(j - 1).Y);
                b[1, j] = 3 * b[1, j] / (Lengths.ElementAt(j) * Lengths.ElementAt(j - 1));


                //==================================================================
            }


            for (int i = 1; i < Coords.Count; i++)
            {
                if (n[0, i] != 0)
                {
                    double d = n[1, i - 1] / n[0, i];
                    for (int k = 0; k < 3; k++)
                    {
                        n[k, i] = n[k, i] * d - n[k + 1, i - 1];
                        b[k, i] = b[k, i] * d - b[k, i - 1];
                    }
                    double q = n[1, i];
                    for (int k = 0; k < 3; k++)
                    {
                        n[k,i] = n[k,i] / q;
                        b[k, i] = b[k, i] / q;
                    }

                }

            }




            for (int k = 0; k < 2; k++)

            {
               
                for (j = 1; j <= Coords.Count ; j++)
                {
                    if (n[1, Coords.Count - j ] != 0)
                    {
                        U[k, Coords.Count - j ] = (b[k, Coords.Count - j ] - n[2,Coords.Count - j ] * U[k, Coords.Count+1 - j]) / n[1, Coords.Count - j ];
                    }
                }

            }


            for (j = 0; j <= Coords.Count - 2; j++)
            {
                 
                XO[j] = Coords.ElementAt(j).X; YO[j] = Coords.ElementAt(j).Y;
                X1[j] = U[0, j]; Y1[j] = U[1, j];
                X2[j] = (3 / (Lengths[j] * Lengths[j])) * (Coords.ElementAt(j+1).X-Coords.ElementAt(j).X);
                Y2[j] = (3 / (Lengths[j] * Lengths[j])) * (Coords.ElementAt(j + 1).Y - Coords.ElementAt(j).Y);

                X2[j] = X2[j] - (1 / Lengths[j]) * (U[0, j + 1] + 2 * U[0, j]);

                Y2[j] = Y2[j] - (1 / Lengths.ElementAt(j)) * (U[1, j + 1] + 2 * U[1, j]);
                X3[j] = (-2 / Math.Pow(Lengths.ElementAt(j), 3)) * (Coords.ElementAt(j + 1).X - Coords.ElementAt(j).X);
                Y3[j] = (-2 / Math.Pow(Lengths.ElementAt(j), 3)) * (Coords.ElementAt(j + 1).Y - Coords.ElementAt(j).Y);
                X3[j] = X3[j] + (1 / Math.Pow(Lengths.ElementAt(j), 2)) * (U[0, j + 1] + U[0, j]);
                Y3[j] = Y3[j] + (1 / Math.Pow(Lengths.ElementAt(j), 2)) * (U[1, j + 1] + U[1, j]);

            }

        }

        /// <summary>
        /// Calculates minimum distances between the existing polyline's start/end nodes and the passed points. Reverses the existing list if needed.
        /// Currently used in the DXFImport when importing LWPolylines which contains reversed Arcs (lovely!)
        /// Note: May not have desired results if the newEntityFirst/LastPoint do not already exist in the existingPolyLine in some form.
        /// </summary>
        /// <param name="existingPolyLine">The existing polyline that will be reversed/not reversed if needed.</param>
        /// <param name="newEntityFirstPoint">The first point of the next entity's node list</param>
        /// <param name="newEntityLastPoint">The last point of the next entity's node list</param>
        /// <returns>Returns true if the next entity's nodes needs to be reversed. Returns false if the next entity nodes can be added normally.</returns>
        public static bool PolyLineReversal(ref PolyLine existingPolyLine, DTMPoint newEntityFirstPoint, DTMPoint newEntityLastPoint)
        {
            double existingLast_ToNewFirst = MathsHelpers.Vector.FnDistance(existingPolyLine.Nodes.Last().X, existingPolyLine.Nodes.Last().Y, newEntityFirstPoint.X, newEntityFirstPoint.Y);
            double existingLast_ToNewLast = MathsHelpers.Vector.FnDistance(existingPolyLine.Nodes.Last().X, existingPolyLine.Nodes.Last().Y, newEntityLastPoint.X, newEntityLastPoint.Y);

            double existingFirst_ToNewFirst = MathsHelpers.Vector.FnDistance(existingPolyLine.Nodes.First().X, existingPolyLine.Nodes.First().Y, newEntityFirstPoint.X, newEntityFirstPoint.Y);
            double existingFirst_ToNewLast = MathsHelpers.Vector.FnDistance(existingPolyLine.Nodes.First().X, existingPolyLine.Nodes.First().Y, newEntityLastPoint.X, newEntityLastPoint.Y);


            //Compare the last point of the existing polyline to the first and last point of the new entity
            double existingLastSmallestDist = 0;
            if (existingLast_ToNewFirst < existingLast_ToNewLast)
                existingLastSmallestDist = existingLast_ToNewFirst;
            else
                existingLastSmallestDist = existingLast_ToNewLast;

            //Compare the first point of the existing polyline to the first and last point of the new entity
            double existingFirstSmallestDist = 0;
            if (existingFirst_ToNewFirst < existingFirst_ToNewLast)
                existingFirstSmallestDist = existingFirst_ToNewFirst;
            else
                existingFirstSmallestDist = existingFirst_ToNewLast;

            //Compare smallest distances of previous calcs and decide wether to reverse the existing polyline or not
            if (existingFirstSmallestDist < existingLastSmallestDist)
            {
                var reversedExistingPoly = existingPolyLine.Nodes.Reverse().ToList();
                existingPolyLine.Nodes = new BindingList<DTMPoint>(reversedExistingPoly);
            }

            //Now the existing list is in order we can compare the last point in it against the new entities first and last points
            //might be a smarter way to do this but i cant be arsed so just going to do the comparisons again.

            //Compare the last point of the existing polyline to the first and last point of the new entity
            existingLastSmallestDist = 0;
            existingLast_ToNewFirst = MathsHelpers.Vector.FnDistance(existingPolyLine.Nodes.Last().X, existingPolyLine.Nodes.Last().Y, newEntityFirstPoint.X, newEntityFirstPoint.Y);
            existingLast_ToNewLast = MathsHelpers.Vector.FnDistance(existingPolyLine.Nodes.Last().X, existingPolyLine.Nodes.Last().Y, newEntityLastPoint.X, newEntityLastPoint.Y);

            if (existingLast_ToNewFirst < existingLast_ToNewLast)
                return false;
            else
                return true;
        }

        #endregion
    }
}

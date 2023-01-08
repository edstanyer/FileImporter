using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;

namespace NRG.MathsHelpers
{
    public static class Vector
    {
        #region Legacy

        public static void WeightedCentroid(List<Point3D> Pts)
        {
            //List<Point2D> xlist;
            //List<Point2D> yList;
            ////Point2D vec = new Point2D();
            //if (Pts.Count > 0)
            //{
            //    foreach (Point3D ele in Pts)
            //    {
            //        //Point2D vec.X = ele.X;
            //        //vec.Y = ele.Z;
            //        //xlist.Add vec;
            //    }
            //    //LeastSquaresFit()

            //}

        }


        /// <summary>
        /// LeastSquaresFit - Given a list of points  calculates  the start and end of the best fit vector
        /// </summary>
        /// <param name="Pts">points used in calculation</param>
        /// <param name="StartPt">start of the best fit vector</param>
        /// <param name="EndPt">end of the best fit vector</param>
        /// <returns>True if it works, however there doesn't seem to be a code path that would retrun false</returns>
        public static bool LeastSquaresFit(List<Point2D> Pts, ref Point2D StartPt, ref Point2D EndPt)
        {
            double sx = 0;
            double sy = 0;
            double sxy = 0;
            double sx2 = 0;
            Point2D ele = new Point2D();
            double minx = double.PositiveInfinity;
            double miny = double.PositiveInfinity;
            double maxx = double.NegativeInfinity;
            double maxy = double.NegativeInfinity;
            for (int i = 0; i < Pts.Count; i++)
            {
                ele = Pts.ElementAt(i);
                if (ele.X < minx) minx = ele.X;
                if (ele.Y < miny) miny = ele.Y;
                if (ele.X > maxx) maxx = ele.X;
                if (ele.Y > maxy) maxy = ele.Y;
                sx = sx + ele.X * ele.Y;
                sy = sy + ele.X * ele.X;
                sxy = sxy + ele.X * ele.X;
                sx2 = sx2 + ele.X * ele.X;
            }

            double m = (Pts.Count * sxy - sx * sy) / (Pts.Count * sx2 - sx * sx);
            double b = (sy - m * sx) / Pts.Count;

            double calcY = m * minx + b;

            if ((calcY >= miny - 0.0001f) & (calcY < -maxy + 0.0001f))
            {
                StartPt.X = minx;
                StartPt.Y = calcY;
            }
            else
            {
                if (m > 0)
                {
                    StartPt.Y = miny;
                }
                else
                {
                    StartPt.Y = maxy;
                }


            }
            StartPt.X = (StartPt.Y - b);
            if ((calcY >= miny - 0.1f) & (calcY <= maxy + 0.1f))
            {
                EndPt.X = maxx;
                EndPt.Y = calcY;
            }
            else
            {
                if (m > 0)
                {
                    EndPt.Y = maxy;
                }
                else
                {
                    EndPt.Y = miny;
                }
                EndPt.X = (EndPt.Y - b) / m;
            }

            return true;
        }

        /// <summary>
        /// Check if two numbers are equal to within a given tolerance
        /// </summary>
        /// <param name="A">The first number</param>
        /// <param name="b">The second number</param>
        /// <param name="tol">How far apart the numbers can be while still being equal</param>
        /// <returns>Returns <see cref="true"/> is the numbers are equal</returns>
        public static bool FnEquals(double A, double b, double tol = 0.001)
        {
            if (A == b) { return true; }
            //   return ((double)Math.Abs(((decimal)A - (decimal)b)) <= tol);

            return Math.Abs(A - b) <= tol;
        }


        //Override for below
        public static double FnDistance(Point2D start, Point2D end)
        {
            return FnDistance(start.X, start.Y, end.X, end.Y);
        }

        //Override for below
        public static double FnDistance(Point3D start, Point3D end)
        {
            return FnDistance(start.X, start.Y, end.X, end.Y);
        }

        /// <summary>
        /// Calculates the 2D distance between two vectors
        /// </summary>
        /// <param name="x1">The X value of the first vector</param>
        /// <param name="y1">The Y value of the first vector</param>
        /// <param name="x2">The X value of the second vector</param>
        /// <param name="y2">The Y value of the second vector</param>
        /// <returns>Returns a <see cref="double"/> containing the distance between the two vectors</returns>
        public static double FnDistance(double x1, double y1, double x2, double y2)
        {
            var x = x2 - x1;
            var y = y2 - y1;

            return Math.Sqrt(x * x + y * y);
        }

        /// <summary>
        /// Calculates the 3D distance between two vectors
        /// </summary>
        /// <param name="x1">The X value of the first vector</param>
        /// <param name="y1">The Y value of the first vector</param>
        /// <param name="z1">The Z value of the first vector</param>
        /// <param name="x2">The X value of the second vector</param>
        /// <param name="y2">The Y value of the second vector</param>
        /// <param name="z2">The Z value of the second vector</param>
        /// <returns>Returns a <see cref="double"/> containing the distance between the two vectors</returns>
        public static double Fn3DDistance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            var x = x2 - x1;
            var y = y2 - y1;
            var z = z2 - z1;

            return Math.Sqrt(x * x + y * y + z * z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x1">The X value of the first vector</param>
        /// <param name="y1">The Y value of the first vector</param>
        /// <param name="x2">The X value of the second vector</param>
        /// <param name="y2">The Y value of the second vector</param>
        /// <param name="x3">The X value of the test vector</param>
        /// <param name="y3">The Y value of the test vector</param>
        /// <returns></returns>
        public static double FnPerpendicular(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double a, b, c = 0;
            double perpendicular = 0;

            a = y2 - y1;
            b = x1 - x2;
            c = x2 * y1 - x1 * y2;

            if(FnEquals(x1, x2, 0.0000001) && FnEquals(y1, y2, 0.0000001))
            {
                perpendicular = FnDistance(x1, y1, x3, y3);
            }
            else
            {
                perpendicular = (a * x3 + b * y3 + c) / Math.Sqrt(a * a + b * b);
            }

            return perpendicular;
        }



        /// <summary>
        /// Tests if a point defined by X & Y is to the right of the line defined by X1, Y1 & X2, Y2
        /// </summary>
        /// <param name="X1">start x of line</param>
        /// <param name="Y1">start y of line</param>
        /// <param name="X2">end x of line</param>
        /// <param name="Y2">end y of line</param>
        /// <param name="X">test point x</param>
        /// <param name="Y">text point y</param>
        /// <returns>True if the point is to the right of the line, False else</returns>
        public static bool FNRightOf(double X1, double Y1, double X2, double Y2, double X, double Y)
        {
            bool test = ((Y - Y1) * (X2 - X) < (X - X1) * (Y2 - Y));
            return test;
        }

        public static bool FnOnLine(double x1, double y1, double x2, double y2, double x, double y)
        {
            double dxx1, dyy1, dx12, dy12;
            double ratx, raty;
            dxx1 = x - x1;
            dyy1 = y - y1;
            dx12 = x2 - x1;
            dy12 = y2 - y1;

            if (dyy1 * (x2 - x) == dxx1  * (y2 - y))
            {
                if (dx12 != 0)
                {
                    ratx = dxx1 / dx12;
                    return (ratx >= 0 & ratx <= 1);
                }
                else
                {
                    if (dy12 != 0)
                    {
                        raty = dyy1 / dy12;
                        return (raty >= 0 & raty <= 1);
                    }
                    else
                    {
                        return (x == x1 & y == y1);
                    }
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Ported 19.05.22 TN
        /// Added by John, 27/7/94
        /// /// </summary>
        /// <param name="x1">start x of line</param>
        /// <param name="y1">start y of line</param>
        /// <param name="x2">end x of line</param>
        /// <param name="y2">end y of line</param>
        /// <param name="x">test point x</param>
        /// <param name="y">test point y</param>
        /// <param name="anywhere">true, point may be anywhere on line. false, point must be between x1,y1 and x2,y2</param>
        /// <param name="tol">tolerance. Should be taken as order of magnitude rather than dead exact</param>
        /// <returns></returns>
        public static bool FnOnLine2(double x1, double y1, double x2, double y2, double x, double y, bool anywhere, double tol = 0.001)
        {
            double dxx1, dyy1, dx12, dy12;
            double ratx, raty; //Ratios
            dxx1 = x - x1;
            dyy1 = y - y1;
            dx12 = x2 - x1;
            dy12 = y2 - y1;
            double test1 = Math.Abs(dyy1 * (x2 - x) - dxx1 * (y2 - y));
            double test2 = tol * (Math.Abs(dx12) + Math.Abs(dy12));
            if (test1 <= test2)
            {
                if (anywhere) { return true; }
                else
                {
                    if (Math.Abs(dx12) > Math.Abs(dy12))  //Check its between points
                    { 
                        if (Math.Abs(dx12) > tol)
                        {
                            ratx = dxx1 / dx12;
                            if (ratx >= -Math.Abs(tol / dx12) & ratx <= 1 + Math.Abs(tol / dx12)) { return true; } //y1 = y2
                            else { return false;}
                        }
                        else
                        {
                            if (Math.Abs(x-x1) < tol & Math.Abs(y-y1) < tol) { return true; } // x1 = x2 AND y1 = y2
                            else { return false;  }
                        }
                    }
                    else
                    {
                        //x1 = x2
                        if (Math.Abs(dy12) > tol)
                        {
                            raty = dyy1 / dy12;
                            if (raty >= -Math.Abs(tol / dy12) & raty <= 1 + Math.Abs(tol / dy12)) { return true; }
                            else { return false; }
                        }
                        else
                        {
                            if (Math.Abs(x - x1) < tol & Math.Abs(y - y1) < tol) //x1 = x2 AND y1 = y2
                            { return true; }
                            else { return false; }
                        }
                    }
                }

            }
            else
            {
                if (anywhere)
                {
                    if (Math.Abs(dyy1 * (x2-x1) - dxx1 * (y2-y1)) <= FnDistance(x,y,x2,y2) * tol | Math.Abs(dy12 * (x2-x) - dx12 * (y2-y)) <= FnDistance(x1,y1,x,y) * tol) { return true; }
                    else { return false; }
                }
                else if(FnDistance(x1,y1,x,y) < tol | FnDistance(x2,y2,x,y) < tol) { return true; }
                else { return false; }
            }
        }

        /// <summary>
        /// Determines if a given point lies on a given line denoted by a start and end coordinate. the fixed verion :)
        /// </summary>
        /// <param name="x1">The X value for the line start</param>
        /// <param name="y1">The Y value for the line start</param>
        /// <param name="x2">The X value for the line end</param>
        /// <param name="y2">The Y value for the line end</param>
        /// <param name="x">The X value of the point to test</param>
        /// <param name="y">The Y value of the point to test</param>
        /// <param name="tol">The maximum distance from the line to accept as on the line</param>
        /// <returns>Returns 0 if the point is on the line, -1 if it is to the left of the line and 1 if it is to the right</returns>
        public static int FnOnLine3(double x1, double y1, double x2, double y2, double x, double y, double tol = 0.001)
        {
            double diff = ((x - x1) * (y2 - y)) - ((y - y1) * (x2 - x));
            if ((Math.Abs(diff) / 10) <= tol) //Edited by TN 19.05.22. Previously it was: if (Maths.Abs(diff) < tol * tol) which seemed to be some form of bollocks
                return 0;
            else
                return Math.Sign(diff);
        }

        /// <summary>
        /// Gets the 2D distance between two points
        /// </summary>
        /// <param name="pt1">A <see cref="Point3D"/> representing the first point</param>
        /// <param name="pt2">A <see cref="Point3D"/> representing the second point</param>
        /// <returns>Returns the 2D length between the 2 points</returns>
        public static double Get2DLengthBetweenXYZ(Point3D pt1, Point3D pt2)
        {
            double outputLength = 0d;
            outputLength = Math.Sqrt(Math.Pow(pt2.X - pt1.X, 2) + Math.Pow(pt2.Y - pt1.Y, 2));
            return outputLength;
        }

        /// <summary>
        /// Calculates the normal vector for a given surface defined by 3 points
        /// </summary>
        /// <param name="point1">The first <see cref="Point3D"/> of the surface</param>
        /// <param name="point2">The second <see cref="Point3D"/> of the surface</param>
        /// <param name="point3">The third <see cref="Point3D"/> of the surface</param>
        /// <returns>Returns a <see cref="Point3D"/> containing the normal vector for the surface</returns>
        public static Point3D CalcSurfaceNormal(Point3D point1, Point3D point2, Point3D point3)
        {
            var u = point2 - point1;
            var v = point3 - point1;

            return new Point3D()
            {
                X = (u.Y * v.Z) - (u.Z * v.Y),
                Y = (u.Z * v.X) - (u.X * v.Z),
                Z = (u.X * v.Y) - (u.Y * v.X)
            };
        }

        #endregion

        #region Distance

        /// <summary>
        /// Calculates the 2D distance between two coordinates
        /// </summary>
        /// <param name="x1">The X value for the first coordinate</param>
        /// <param name="y1">The Y value for the first coordinate</param>
        /// <param name="x2">The X value for the first coordinate</param>
        /// <param name="y2">The Y value for the first coordinate</param>
        /// <returns>Returns a <see cref="double"/> containing the 2D distance between the two coordinates</returns>
        public static double Distance(double x1, double y1, double x2, double y2)
        {
            var x = x2 - x1;
            var y = y2 - y1;

            x *= x;
            y *= y;

            return Math.Sqrt(x + y);
        }

        /// <summary>
        /// Calculates the 3D distance bewtween two coordinates
        /// </summary>
        /// <param name="x1">The X value for the first coordinate</param>
        /// <param name="y1">The Y value for the first coordinate</param>
        /// <param name="z1">The Z value for the first coordinate</param>
        /// <param name="x2">The X value for the second coordinate</param>
        /// <param name="y2">The Y value for the second coordinate</param>
        /// <param name="z2">The Z value for the second coordinate</param>
        /// <returns>Returns a <see cref="double"/> containing the 3D distance between the two coordinates</returns>
        public static double Distance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            var x = x2 - x1;
            var y = y2 - y1;
            var z = z2 - z1;

            x *= x;
            y *= y;
            z *= z;

            return Math.Sqrt(x + y + z);
        }

        /// <summary>
        /// Calculates the closet distance between a point and a line between two points
        /// </summary>
        /// <param name="x1">The X value for the first line coordinate</param>
        /// <param name="y1">The Y value for the first line coordinate</param>
        /// <param name="z1">The Z value for the first line coordinate</param>
        /// <param name="x2">The X value for the second line coordinate</param>
        /// <param name="y2">The Y value for the second line coordinate</param>
        /// <param name="z2">The Z value for the second line coordinate</param>
        /// <param name="x">The X value for the point</param>
        /// <param name="y">The Y value for the point</param>
        /// <param name="z">The Z value for the point</param>
        /// <returns>Returns a <see cref="double"/> containing the perpendicular distance of the point to the line</returns>
        public static double ClosestDistanceOnALine(double x1, double y1, double z1, double x2, double y2, double z2, double x, double y, double z)
        {
            var apX = x - x1;
            var apY = y - y1;
            var apZ = z - z1;

            var abX = x2 - x1;
            var abY = y2 - y1;
            var abZ = z2 - z1;

            var dotApAb = DotProduct(apX, apY, apZ, abX, abY, abZ);
            var dotAb = DotProduct(abX, abY, abZ);

            var resX = x1 + dotApAb / dotAb * abX;
            var resY = y1 + dotApAb / dotAb * abY;
            var resZ = z1 + dotApAb / dotAb * abZ;
            return ((resX - x) * (resX - x)) + ((resY - y) * (resY - y)) + ((resZ - z) * (resZ - z));
        }

        /// <summary>
        /// Calculates the dot product of a single vector
        /// </summary>
        /// <param name="x">The X value of the vector</param>
        /// <param name="y">The Y value of the vector</param>
        /// <param name="z">The Z value of the vector</param>
        /// <returns>Returns a <see cref="double"/> containing the dot product of the vector</returns>
        public static double DotProduct(double x, double y, double z)
        {
            return (x * x) + (y * y) + (z * z);
        }

        /// <summary>
        /// Calculates the dot product of a single vector
        /// </summary>
        /// <param name="vector">A <see cref="Point3D"/> containing the vector to find the dot product of</param>
        /// <returns>Returns a <see cref="double"/> containing the dot product of the vector</returns>
        public static double DotProduct(Point3D vector)
        {
            return (vector.X * vector.X) + (vector.Y * vector.Y) + (vector.Z * vector.Z);
        }

        /// <summary>
        /// Calculates the dot product of two vectors
        /// </summary>
        /// <param name="x1">The X value for the first vector</param>
        /// <param name="y1">The Y value for the first vector</param>
        /// <param name="z1">The Z value for the first vector</param>
        /// <param name="x2">The X value for the second vector</param>
        /// <param name="y2">The Y value for the second vector</param>
        /// <param name="z2">The Z value for the second vector</param>
        /// <returns>Returns a <see cref="double"/> containing the dot product of the two vectors</returns>
        public static double DotProduct(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return (x1 * x2) + (y1 * y2) + (z1 * z2);
        }

        /// <summary>
        /// Calculates the dot product of two vectors
        /// </summary>
        /// <param name="vector1">The first vector</param>
        /// <param name="vector2">The second vector</param>
        /// <returns>Returns a <see cref="double"/> containing the dot product of the two vectors</returns>
        public static double DotProduct(Point3D vector1, Point3D vector2)
        {
            return (vector1.X * vector2.X) + (vector1.Y * vector2.Y) + (vector1.Z * vector2.Z);
        }

        /// <summary>
        /// Normalizes the provided vector
        /// </summary>
        /// <param name="vector">A <see cref="Point3D"/> vector to normalize</param>
        /// <returns>Returns a <see cref="Point3D"/> containing the normalized vector</returns>
        public static Point3D Normalize(Point3D vector)
        {
            var dist = Math.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y) + (vector.Z * vector.Z));
            return new Point3D(vector.X / dist, vector.Y / dist, vector.Z / dist);
        }

        /// <summary>
        /// Attempts to calculate the shortest line between two 3D lines. Based off the algorithm disclosed at:
        /// http://paulbourke.net/geometry/pointlineplane/
        /// </summary>
        /// <param name="P1">A <see cref="Point3D"/> representing the start of the first line</param>
        /// <param name="P2">A <see cref="Point3D"/> representing the end of the first line</param>
        /// <param name="P3">A <see cref="Point3D"/> representing the start of the second line</param>
        /// <param name="P4">A <see cref="Point3D"/> representing the end of the second line</param>
        /// <param name="Pa">A <see cref="Point3D"/> to store the start of the shortest line</param>
        /// <param name="Pb">A <see cref="Point3D"/> to store the end of the shortest line</param>
        public static void ShortestLineBetweenTwoLines(Point3D P1, Point3D P2, Point3D P3, Point3D P4, ref Point3D Pa, ref Point3D Pb)
        {
            //Pa = P1 + a * (P2 - P1)
            //Pb = P3 + b * (P4 - P3)

            //dmnop = (xm - xn) * (xo - xp) + (ym - yn) * (yo - yp) + (zm - zn) * (zo - zp)

            //a = ((d1343 * d4321) - (d1321 * d4343)) / ((d2121 * d4343) - (d4321 * d4321))
            //b = (d1343 + a * d4321) / d4343

            var d1343 = ((P1.X - P3.X) * (P4.X - P3.X)) + ((P1.Y - P3.Y) * (P4.Y - P3.Y)) + ((P1.Z - P3.Z) * (P4.Z - P3.Z));
            var d4321 = ((P4.X - P3.X) * (P2.X - P1.X)) + ((P4.Y - P3.Y) * (P2.Y - P1.Y)) + ((P4.Z - P3.Z) * (P2.Z - P1.Z));
            var d1321 = ((P1.X - P3.X) * (P2.X - P1.X)) + ((P1.Y - P3.Y) * (P2.Y - P1.Y)) + ((P1.Z - P3.Z) * (P2.Z - P1.Z));
            var d4343 = ((P4.X - P3.X) * (P4.X - P3.X)) + ((P4.Y - P3.Y) * (P4.Y - P3.Y)) + ((P4.Z - P3.Z) * (P4.Z - P3.Z));
            var d2121 = ((P2.X - P1.X) * (P2.X - P1.X)) + ((P2.Y - P1.Y) * (P2.Y - P1.Y)) + ((P2.Z - P1.Z) * (P2.Z - P1.Z));

            var a = ((d1343 * d4321) - (d1321 * d4343)) / ((d2121 * d4343) - (d4321 * d4321));
            var b = (d1343 + a * d4321) / d4343;

            Pa = P1 + a * (P2 - P1);
            Pb = P3 + b * (P4 - P3);
        }

        #endregion

        #region Scale

        public static Double RoundUpToNearest(Double passednumber, Double roundto)
        {

            // 105.5 up to nearest 1 = 106
            // 105.5 up to nearest 10 = 110
            // 105.5 up to nearest 7 = 112
            // 105.5 up to nearest 100 = 200
            // 105.5 up to nearest 0.2 = 105.6
            // 105.5 up to nearest 0.3 = 105.6

            //if no rounto then just pass original number back
            if (roundto == 0)
            {
                return passednumber;
            }
            else
            {
                return Math.Ceiling(passednumber / roundto) * roundto;
            }
        }
        public static Double RoundDownToNearest(Double passednumber, Double roundto)
        {

            // 105.5 down to nearest 1 = 105
            // 105.5 down to nearest 10 = 100
            // 105.5 down to nearest 7 = 105
            // 105.5 down to nearest 100 = 100
            // 105.5 down to nearest 0.2 = 105.4
            // 105.5 down to nearest 0.3 = 105.3

            //if no rounto then just pass original number back
            if (roundto == 0)
            {
                return passednumber;
            }
            else
            {
                return Math.Floor(passednumber / roundto) * roundto;
            }
        }

        /// <summary>
        /// Calculates the parameters required to scale a point to a given canvas
        /// </summary>
        /// <param name="canvasWidth">The width of the canvas area</param>
        /// <param name="canvasHeight">The height of the canvas area</param>
        /// <param name="worldBounds">The bounds of the coordinates to be displayed</param>
        /// <param name="pixelsPerUnit">The number of pixels required to display a single coordinate unit</param>
        /// <param name="xOffset">The pixel offset for x coordinates</param>
        /// <param name="yOffset">The pixel offset for y coordinates</param>
        /// <param name="padding">The number of pixels between the coordindate system and the edge of the canvas</param>
        public static void CalcScaleParams(int canvasWidth, int canvasHeight, Bounds worldBounds, ref double pixelsPerUnit, ref double xOffset, ref double yOffset, int padding = 0, bool fixedWidth = false)
        {
            //If there is a fixed width we don't try to fit both x and y into the plot area, we just fit x
            if (fixedWidth)
            {
                pixelsPerUnit = (canvasWidth - (padding * 2)) / worldBounds.Size.X;
                yOffset = ((canvasHeight - (padding * 2)) - ((worldBounds.Size.Y) * pixelsPerUnit)) / 2;
                yOffset += padding;
                xOffset += padding;

                return;
            }
            else
            {
                var fracX = (canvasWidth - (padding * 2)) / worldBounds.Size.X;
                var fracY = Math.Max((canvasHeight - (padding * 2)) / worldBounds.Size.Y, 0.1);
                pixelsPerUnit = Math.Min(fracX, fracY);

                yOffset = 0;
                xOffset = 0;

                if (fracX < fracY)
                    yOffset = ((canvasHeight - (padding * 2)) - ((worldBounds.Size.Y) * pixelsPerUnit)) / 2;
                else
                    xOffset = ((canvasWidth - (padding * 2)) - ((worldBounds.Size.X) * pixelsPerUnit)) / 2;

                yOffset += padding;
                xOffset += padding;
            }
        }


        public static void CalcScaledCoords(int canvasWidth, int canvasHeight, double xCoord, double yCoord, Bounds worldBounds, ref double scaledX, ref double scaledY, int padding = 0, double xOffset = 0, double yOffset = 0)
        {
            double frac = 0;
            CalcScaleParams(canvasWidth, canvasHeight, worldBounds, ref frac, ref xOffset, ref yOffset, padding);
            CalcScaledCoords(canvasHeight, xCoord, yCoord, worldBounds, ref scaledX, ref scaledY, frac, xOffset, yOffset);
        }

        /// <summary>
        /// Calculates canvas coordinates from world coordinates
        /// </summary>
        /// <param name="canvasWidth">The width of the canvas area</param>
        /// <param name="canvasHeight">The height of the canvas area</param>
        /// <param name="xCoord">The x coord in world coordinates</param>
        /// <param name="yCoord">The y coord in world coordinates</param>
        /// <param name="worldBounds">The bounds of the coordinates to be displayed</param>
        /// <param name="scaledX">The x coord in canvas coordinates</param>
        /// <param name="scaledY">The y coord in canvas coordinates</param>
        /// <param name="padding">Number of pixels between the coordinate system and the edge of the canvas</param>
        public static void CalcScaledCoords(int canvasWidth, int canvasHeight, double xCoord, double yCoord, Bounds worldBounds, ref double scaledX, ref double scaledY, int padding = 0)
        {
            double frac = 0, xOffset = 0, yOffset = 0;
            CalcScaleParams(canvasWidth, canvasHeight, worldBounds, ref frac, ref xOffset, ref yOffset, padding);
            CalcScaledCoords(canvasHeight, xCoord, yCoord, worldBounds, ref scaledX, ref scaledY, frac, xOffset, yOffset);
        }

        /// <summary>
        /// Calculates canvas coordinates from world coordinates
        /// </summary>
        /// <param name="canvasHeight">The height of the canvas area</param>
        /// <param name="xCoord">The x coord in world coordinates</param>
        /// <param name="yCoord">The y coord in world coordinates</param>
        /// <param name="worldBounds">The bounds of the coordinates to be displayed</param>
        /// <param name="scaledX">The x coord in canvas coordinates</param>
        /// <param name="scaledY">The y coord in canvas coordinates</param>
        /// <param name="pixelsPerUnit">The number of pixels required to display a single coordinate unit</param>
        /// <param name="xOffset">The pixel offset for the x coordinates</param>
        /// <param name="yOffset">The pixel offset for the y coordinates</param>
        public static void CalcScaledCoords(int canvasHeight, double xCoord, double yCoord, Bounds worldBounds, ref double scaledX, ref double scaledY, double pixelsPerUnit, double xOffset, double yOffset)
        {
            scaledX = xOffset + ((xCoord - worldBounds.Min.X) * pixelsPerUnit);
            scaledY = canvasHeight - (yOffset + ((yCoord - worldBounds.Min.Y) * pixelsPerUnit));
        }

        /// <summary>
        /// Calculates world coordinates from canvas coordinates
        /// </summary>
        /// <param name="canvasWidth">The width of the canvas area</param>
        /// <param name="canvasHeight">The height of the canvas area</param>
        /// <param name="scaledX">The x coord in canvas coordinates</param>
        /// <param name="scaledY">The y coord in canvas coordinates</param>
        /// <param name="worldBounds">The bounds of the coordinates to be displayed</param>
        /// <param name="xCoord">The x coord in world coordinates</param>
        /// <param name="yCoord">The y coord in world coordinates</param>
        /// <param name="padding">Number of pixels between the coordinate system and the edge of the canvas</param>
        public static void CalcUnscaledCoords(int canvasWidth, int canvasHeight, double scaledX, double scaledY, Bounds worldBounds, ref double xCoord, ref double yCoord, int padding = 0)
        {
            double frac = 0, xOffset = 0, yOffset = 0;
            CalcScaleParams(canvasWidth, canvasHeight, worldBounds, ref frac, ref xOffset, ref yOffset, padding);
            CalcUnscaledCoords(canvasHeight, scaledX, scaledY, worldBounds, ref xCoord, ref yCoord, frac, xOffset, yOffset);
        }

        /// <summary>
        /// Calculates world coordinates from canvas coordinates
        /// </summary>
        /// <param name="canvasHeight">The height of the canvas area</param>
        /// <param name="scaledX">The x coord in canvas coordinates</param>
        /// <param name="scaledY">The y coord in canvas coordinates</param>
        /// <param name="worldBounds">The bounds of the coordinates to be displayed</param>
        /// <param name="xCoord">The x coord in world coordinates</param>
        /// <param name="yCoord">The y coord in world coordinates</param>
        /// <param name="pixelsPerUnit">The number of pixels required to display a single coordinate unit</param>
        /// <param name="xOffset">The pixel offset for the x coordinates</param>
        /// <param name="yOffset">The pixel offset for the y coordinates</param>
        public static void CalcUnscaledCoords(int canvasHeight, double scaledX, double scaledY, Bounds worldBounds, ref double xCoord, ref double yCoord, double pixelsPerUnit, double xOffset, double yOffset)
        {
            xCoord = ((scaledX + worldBounds.Min.X) / pixelsPerUnit) - xOffset;
            yCoord = (canvasHeight + ((scaledY + worldBounds.Min.Y) / pixelsPerUnit) - yOffset);
        }

        #endregion


#region LengthOfLine
        public static double LengthOfLine(List<DTMPoint> line)
        {
            if (line != null && line.Count > 1)
            {
                double totDist = 0;
                for(int i = 0; i < line.Count-1; i++)
                {
                    totDist += FnDistance(line[i].X, line[i].Y, line[i+1].X, line[i+1].Y);
                }
                return totDist;
            }
            return 0;
        }

        public static double LengthOfLine(List<Point2D> line)
        {
            if (line != null && line.Count > 1)
            {
                double totDist = 0;
                for (int i = 0; i < line.Count - 1; i++)
                {
                    totDist +=  FnDistance(line[i].X, line[i].Y, line[i + 1].X, line[i + 1].Y); 
                }
                return totDist;
            }
            return 0;
        }
        


        public static double LengthOfLine(List<Point3D> line)
        {
            if (line != null && line.Count > 1)
            {
                double totDist = 0;
                for (int i = 0; i < line.Count - 1; i++)
                {
                    totDist +=  FnDistance(line[i].X, line[i].Y, line[i + 1].X, line[i + 1].Y); 
                }
                return totDist;
            }
            return 0;
        }

        #endregion LengthOfLine

        public static bool InterpolateAlongPoly(List<DTMPoint> line)
        {
            bool ok = false;
            if (line.Count > 2)
            {
                double startLevel = line[0].Z;
                double endLevel = line[line.Count-1].Z;
                if (startLevel > -999 && endLevel > -999)
                {
                    double totLen = LengthOfLine(line);
                    if (totLen > 0)
                    {
                        double curdist = FnDistance(line[0].X, line[0].Y, line[1].X, line[1].Y);
                        for (int i = 1; i < line.Count - 1; i++)
                        {
                            line[i].Z = Interpolation.FnInterpolateY(0, startLevel, totLen, endLevel, curdist);
                            curdist += FnDistance(line[i].X, line[i].Y, line[i + 1].X, line[i + 1].Y); ;

                        }

                        ok = true;
                    }
                }

            }
            return ok;
        }

        public static List<DTM> ShiftLineParallel(List<DTMPoint> line, double xOffset , double yOffset)
        {
            if (line != null && line.Count > 1)
            {
                List<DTMPoint> outlist = new List<DTMPoint>();
                for (int i = 0; i < line.Count; i++)
                {
                    
                }

            }
            return null;
        }



        public static double CalcSectionGrade(Point3D startPt, Point3D endPt)
        {

            if (startPt.X != endPt.X)
            {
                return (endPt.X - startPt.X) / Math.Abs(startPt.Y - endPt.Y);            
            }

            return double.PositiveInfinity;
        }

        public static double CalcGrade(Point3D startPt, Point3D endPt)
        {
            double dist = FnDistance(startPt, endPt);
            double heightDiff =  endPt.Z - startPt.Z;

            double grade = heightDiff / dist;

            return grade;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;

namespace NRG.MathsHelpers
{
    public static class Intersection
    {
        #region Line Intersection

        /// <summary>
        /// Linear equation of a straight line 
        /// m describes the slope of the line a c describe the position where the y axis intersects the x axis
        /// please note the Yanks use b instead of c (wankers)
        /// </summary>
        /// <param name="x1">vector start x</param>
        /// <param name="y1">vector start y</param>
        /// <param name="x2">vector end x</param>
        /// <param name="y2">vector end y</param>
        /// <param name="m">slope (grade as decimal) of the line</param>
        /// <param name="c">Y value as the vector passes through X1</param>
        public static void CalcSlopeAndIntercept(double x1, double y1, double x2, double y2, ref double m, ref double c)
        {
            if (x1 == x2 && y1 == y2)
            {
                m = 0;
                c = y1;
            }
            else if (x1 == x2)
            {
                m = double.PositiveInfinity;
                c = double.PositiveInfinity;
            }
            else
            {
                m = (y1 - y2) / (x1 - x2);
                c = y1 - m * x1;
            }
        }

        /// <summary>
        /// A slightly more complex override for IntersectLines. Calcuates the 2D intersection then works out the level(z values, if you like?) depending on input from "LevelFrom"
        /// Note: this isn't a 3D intersection method but could be used for sectioning methods. ie line one is the section vector with no z value and line 2 is the 
        /// edge of a triangle with valid ground levels, then call this with LevelFrom=2.
        /// </summary>
        /// <param name="Line1Start">Start coord of line one</param>
        /// <param name="Line1End">End coord of line one</param>
        /// <param name="Line2Start">Start coord of line 2</param>
        /// <param name="Line2End">End coord of line 2</param>
        /// <param name="LevelFrom">1 = take level from line 1: 2 = take level from line 2: 3 = mean levels for lines 1 & 2...average...median...add 'em together and divide by 2</param>
        /// <returns>3D Intersection point if within the bounds of both lines, null else</returns>
        public static Point3D IntersectLines(Point3D Line1Start, Point3D Line1End, Point3D Line2Start, Point3D Line2End, int LevelFrom = 1)
        {
            double ipx=0, ipy=0;
            //Find the intersection, if one exists, within the bounds of the 2 lines
            if (IntersectLines(Line1Start.X,Line1Start.Y, Line1End.X, Line1End.Y,Line2Start.X,Line2Start.Y, Line2End.X, Line2End.Y,ref ipx,ref ipy ))
            {
                //Get level, Z, altitude, height from the ellipsoid or geoid model...or whatever we're calling verticallity this week.
                double ipz;

                Point3D isect = new Point3D(ipx,ipy,ipz = -999);

                if (LevelFrom == 1)//level from line 1
                    ipz = MathsHelpers.Interpolation.InterpolateZ(Line1Start.X, Line1Start.Y, Line1End.Z, Line1End.X, Line1End.Y, Line1End.Z, MathsHelpers.Vector.FnDistance(Line1Start.X, Line1Start.Y, ipx, ipy));
                else if (LevelFrom == 2)//level from line 2
                    ipz = MathsHelpers.Interpolation.InterpolateZ(Line2Start.X, Line2Start.Y, Line2End.Z, Line2End.X, Line2End.Y, Line2End.Z, MathsHelpers.Vector.FnDistance(Line2Start.X, Line2Start.Y, ipx, ipy));
                else if (LevelFrom == 3)//mean level
                {
                    double ipz1 = MathsHelpers.Interpolation.InterpolateZ(Line1Start.X, Line1Start.Y, Line1End.Z, Line1End.X, Line1End.Y, Line1End.Z, MathsHelpers.Vector.FnDistance(Line1Start.X, Line1Start.Y, ipx, ipy));
                    double ipz2 = MathsHelpers.Interpolation.InterpolateZ(Line2Start.X, Line2Start.Y, Line2End.Z, Line2End.X, Line2End.Y, Line2End.Z, MathsHelpers.Vector.FnDistance(Line2Start.X, Line2Start.Y, ipx, ipy));
                    ipz = (ipz1 + ipz2) / 2;//Wow...complex
                }
                else ipz = -999;
                isect.Z = ipz;
                return isect;
            }       
            return null;
        }


        /// <summary>
        /// Finds the intersection point of two lines
        /// </summary>
        /// <param name="x1">Line 1 start X</param>
        /// <param name="y1">Line 1 stary Y</param>
        /// <param name="x2">Line 1 end X</param>
        /// <param name="y2">Line 1 end Y</param>
        /// <param name="x3">Line 2 start X</param>
        /// <param name="y3">Line 2 start Y</param>
        /// <param name="x4">Line 2 end X</param>
        /// <param name="y4">Line 2 end Y</param>
        /// <param name="ipx">Line intersection X</param>
        /// <param name="ipy">Line intersection Y</param>
        /// <returns>T/F if the lines intersect within line 1</returns>
        public static bool IntersectLines(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4, ref double ipX, ref double ipY)
        {
            double m1 = 0, m2 = 0, c1 = 0, c2 = 0;

            #region Calc Slope and Intercept

            //Calc Slope and Intercept
            if (x1 == x2 && y1 == y2)
            {
                m1 = 0;
                c1 = y1;
            }
            else if (x1 == x2)
            {
                m1 = double.PositiveInfinity;
                c1 = double.PositiveInfinity;
            }
            else
            {
                m1 = (y1 - y2) / (x1 - x2);
                c1 = y1 - m1 * x1;
            }

            //Calc Slope and Intercept
            if (x3 == x4 && y3 == y4)
            {
                m2 = 0;
                c2 = y3;
            }
            else if (x3 == x4)
            {
                m2 = double.PositiveInfinity;
                c2 = double.PositiveInfinity;
            }
            else
            {
                m2 = (y3 - y4) / (x3 - x4);
                c2 = y3 - m2 * x3;
            }

            #endregion

            //Intersect the lines
            ipX = x1;
            ipY = y1;
            if (m1 == m2 || (Math.Abs(m1) > 100000000 && Math.Abs(m2) > 100000000))
                return false;
            else
            {
                if (Math.Abs(m1) > 100000000)
                {
                    ipX = x1;
                    ipY = m2 * ipX + c2;
                }
                else if (Math.Abs(m2) > 100000000)
                {
                    ipX = x3;
                    ipY = m1 * ipX + c1;
                }
                else
                {
                    ipX = (c2 - c1) / (m1 - m2);
                    ipY = m1 * ipX + c1;
                }               

                #region FnInBox

                double right1, left1, top1, bot1;
                if(x1 > x2)
                {
                    right1 = x1;
                    left1 = x2;
                }
                else
                {
                    right1 = x2;
                    left1 = x1;
                }
                if(y1 > y2)
                {
                    top1 = y1;
                    bot1 = y2;
                }
                else
                {
                    top1 = y2;
                    bot1 = y1;
                }

                //Modified by WA 16.08.19: Added equals check to account for floating points - Had one specific shape that would fail to find an intersection by 0.0000000000001....
                if (!((ipX >= left1 || Math.Abs(ipX - left1) < 0.0000001) && (ipX <= right1 || Math.Abs(ipX - right1) < 0.0000001) && (ipY >= bot1 || Math.Abs(ipY - bot1) < 0.0000001) && (ipY <= top1 || Math.Abs(ipY - top1) < 0.0000001)))
                    return false;

                if(x3 > x4)
                {
                    right1 = x3;
                    left1 = x4;
                }
                else
                {
                    right1 = x4;
                    left1 = x3;
                }
                if(y3 > y4)
                {
                    top1 = y3;
                    bot1 = y4;
                }
                else
                {
                    top1 = y4;
                    bot1 = y3;
                }

                //Modified by WA 16.08.19: Added equals check to account for floating points - Had one specific shape that would fail to find an intersection by 0.0000000000001....
                if (!((ipX >= left1 || Math.Abs(ipX - left1) < 0.0000001) && (ipX <= right1 || Math.Abs(ipX - right1) < 0.0000001) && (ipY >= bot1 || Math.Abs(ipY - bot1) < 0.0000001) && (ipY <= top1 || Math.Abs(ipY - top1) < 0.0000001)))
                    return false;

                #endregion

                return true;
            }
        }

        /// <summary>
        /// Finds whether an intersection between two lines exists
        /// </summary>
        /// <param name="lineStart1">A <see cref="Point3D"/> representing the start of the first line</param>
        /// <param name="lineEnd1">A <see cref="Point3D"/> representing the end of the first line</param>
        /// <param name="lineStart2">A <see cref="Point3D"/> representing the start of the second line</param>
        /// <param name="lineEnd2">A <see cref="Point3D"/> representing the end of the second line</param>
        /// <returns>Returns true if there is an intersection</returns>
        public static bool IntersectLines(Point3D lineStart1, Point3D lineEnd1, Point3D lineStart2, Point3D lineEnd2)
        {
            double ipX = 0, ipY = 0;
            return IntersectLines(lineStart1.X, lineStart1.Y, lineEnd1.X, lineEnd1.Y, lineStart2.X, lineStart2.Y, lineEnd2.X, lineEnd2.Y, ref ipX, ref ipY);
        }

        /// <summary>
        /// Given 2 vectors described by Eocc1, Nocc1 & Eocc2, Nooc2 and a WCB for each calcs the intersection point
        /// Note: it is up to the caller to make sure the intersection is within the bounds of the 2 lines...unless the 2 vectors are perfectly 
        /// parallel there will always be an intersection
        /// </summary>
        /// <param name="Eocc">origin of line 1 x</param>
        /// <param name="Nocc">origin of line 1 y</param>
        /// <param name="brg">WCB of line 1</param>
        /// <param name="Eocc2">origin of line 2 x</param> 
        /// <param name="Nocc2">origin of line 2 y</param> 
        /// <param name="brg2">WCB of line 2</param>
        public static Point2D IntersectTwoBrgLines(double Eocc, double Nocc, double brg, double Eocc2, double Nocc2, double brg2)
        {
            double X = -100000000;
            double Y = -100000000;
            double IpX = -100000000;
            double IpY = -100000000;

            if (Vector.FnEquals(brg, Math.PI / 2, 0.00000000001) || Vector.FnEquals(brg, Math.PI * 1.5, 0.00000000001))
            {
                if (Vector.FnEquals(brg2, Math.PI / 2, 0.00000000001) || Vector.FnEquals(brg2, Math.PI * 1.5, 0.00000000001))
                {
                    //MEh
                }
                else
                {
                    IpY = Nocc;
                    Y = Nocc2 - Nocc;
                    X = Y * Math.Tan(brg2);
                    IpX = Eocc2 - X;
                }
            }
            else if (Vector.FnEquals(brg2, Math.PI / 2, 0.00000000001) || Vector.FnEquals(brg2, Math.PI * 1.5, 0.00000000001))
            {
                if (Vector.FnEquals(brg, Math.PI / 2, 0.00000000001) || Vector.FnEquals(brg, Math.PI * 1.5, 0.00000000001))
                {

                }
                else
                {
                    IpY = Nocc2;
                    Y = Nocc - Nocc2;
                    X = Y * Math.Tan(brg);
                    IpX = Eocc - X;
                }
            }
            else if (Math.Abs(Math.Tan(brg) - Math.Tan(brg2)) < 0.000000000000001)
            {

            }
            else
            {
                Y = (Eocc2 - Eocc) - ((Nocc2 - Nocc) * Math.Tan(brg2));
                Y = Y / (Math.Tan(brg) - Math.Tan(brg2));
                X = Y * Math.Tan(brg);
                if (Math.Abs(X) > 100000 || Math.Abs(Y) > 100000)
                {

                }
                else
                {
                    IpX = X + Eocc;
                    IpY = Y + Nocc;
                }
            }

            return new Point2D(IpX, IpY);
        }

        /// <summary>
        /// Determine Where a given line intersects with a plane. Returns null if the line is parallel to the plane
        /// </summary>
        /// <param name="planePoint">A <see cref="Point3D"/> of a point on the plane</param>
        /// <param name="planeNormal">A <see cref="Point3D"/> of the normal of the plane see <seealso cref="Vector.CalcSurfaceNormal(Point3D, Point3D, Point3D)"/> for reference</param>
        /// <param name="linePoint">A <see cref="Point3D"/> of a point on the line</param>
        /// <param name="lineDirection">A <see cref="Point3D"/> vector of the line direction</param>
        /// <returns>Returns null if the line is parallel to the plane otherwise returns a <see cref="Point3D"/> containing the intersection point</returns>
        public static Point3D IntersectLineOnPlane(Point3D planePoint, Point3D planeNormal, Point3D linePoint, Point3D lineDirection)
        {
            var diff = linePoint - planePoint;

            var prod1 = Vector.DotProduct(diff, planeNormal);

            //Check to see if the ray is parallel to the plane and return null if so
            if (Math.Abs(prod1) <= 0.0000001)
                return null;

            var prod2 = Vector.DotProduct(lineDirection, planeNormal);
            var prod3 = prod1 / prod2;

            return linePoint - (lineDirection * prod3);
        }

        /// <summary>
        /// Determines if and where a ray intersects a given plane with a fixed boundary
        /// </summary>
        /// <param name="planePoint1">A <see cref="Point3D"/> of the first corner of the plane</param>
        /// <param name="planePoint2">A <see cref="Point3D"/> of the second corner of the plane</param>
        /// <param name="planePoint3">A <see cref="Point3D"/> of the third corner of the plane</param>
        /// <param name="planePoint4">A <see cref="Point3D"/> of the fourth corner of the plane</param>
        /// <param name="ray">A <see cref="Ray"/> to check for an intersection with the plane</param>
        /// <returns>Returns null if there is no intersection or a <see cref="Point3D"/> of the intersection point</returns>
        public static Point3D IntersectRayOnPlane(Point3D planePoint1, Point3D planePoint2, Point3D planePoint3, Point3D planePoint4, Ray ray)
        {
            var center = (planePoint1 + planePoint2 + planePoint3 + planePoint4) / 4;
            var planeNormal = Vector.CalcSurfaceNormal(planePoint1, planePoint2, planePoint3);

            var rayNormal = Vector.Normalize(ray.Direction);

            var diff = ray.Origin - center;
            var prod1 = Vector.DotProduct(diff, planeNormal);

            //Check to see if the ray is parallel to the plane and return null if so
            if (Math.Abs(prod1) <= 0.0000001)
                return null;

            var prod2 = Vector.DotProduct(ray.Direction, planeNormal);
            var prod3 = prod1 / prod2;

            var intersection = ray.Origin - (ray.Direction * prod3);

            //Check if the intersection point falls within the bounds of the plane
            var bounds = new Bounds();
            bounds.Update(planePoint1);
            bounds.Update(planePoint2);
            bounds.Update(planePoint3);
            bounds.Update(planePoint4);

            if (Math.Abs(intersection.Z - bounds.Min.Z) <= 0.0000001)
                intersection.Z = bounds.Min.Z;
            else if (Math.Abs(intersection.Z - bounds.Max.Z) <= 0.0000001)
                intersection.Z = bounds.Max.Z;

            if (bounds.IsInside(intersection))
            {
                //If the plane is completely flat we need to add another check to determine the intersection point falls within the flat bounds
                if (bounds.Min.Z == bounds.Max.Z)
                {
                    var poly = new Basic2DPolygon();
                    poly.Points.AddRange(new Point3D[] { planePoint1, planePoint2, planePoint3, planePoint4 });
                    if (poly.InPoly2D(intersection.X, intersection.Y, poly.Points))
                        return intersection;
                }
                else
                    return intersection;
            }

            return null;
        }

        /// <summary>
        /// Determines if a given point lies within the bounds of of 2 points
        /// Box runs from upper left to lower right, no rotation is taken into account
        /// </summary>
        /// <param name="fx1">Line start X</param>
        /// <param name="fy1">Line start Y</param>
        /// <param name="fx2">Line end X</param>
        /// <param name="fy2">Line end Y</param>
        /// <param name="iX">Intersection Point X</param>
        /// <param name="iY">Intersection Point Y</param>
        /// <returns></returns>
        public static bool FnInBox(double fx1, double fy1, double fx2, double fy2, double iX, double iY)
        {
            double right1, left1, top1, bot1;
            if (fx1 > fx2)
            {
                right1 = fx1;
                left1 = fx2;
            }
            else
            {
                right1 = fx2;
                left1 = fx1;
            }

            if (fy1 > fy2)
            {
                top1 = fy1;
                bot1 = fy2;
            }
            else
            {
                top1 = fy2;
                bot1 = fy1;
            }

            return ((iX >= left1/* - 0.0009 */&& iX <= right1/* + 0.0009*/) && (iY >= bot1 /*- 0.0009*/ && iY <= top1/* + 0.0009*/));
        }

        /// <summary>
        /// Determines if a given point lies within the bounds of of 2 points within tolerance of 1mm
        /// Box runs from upper left to lower right, no rotation is taken into account
        /// </summary>
        /// <param name="fx1">Easting of start of segement</param>
        /// <param name="fy1">Northing of start of segment</param>
        /// <param name="fx2">Easting of start of segment</param>
        /// <param name="fy2">Northing of start of segment</param>
        /// <param name="iX">Easting of interest</param>
        /// <param name="iY">Northing of interest</param>
        /// <returns>True if intersection exists within the line segment</returns>
        public static bool FnInBox1(double fx1, double fy1, double fx2, double fy2, double iX, double iY)
        {
            bool isInBox = false;
            double Right1;
            double left1;
            double top1;
            double bot1;

            if (fx1 > fx2)
            {
                Right1 = fx1;
                left1 = fx2;
            }
            else
            {
                Right1 = fx2;
                left1 = fx1;
            }

            if (fy1 > fy2)
            {
                top1 = fy1;
                bot1 = fy2;
            }
            else
            {
                top1 = fy2;
                bot1 = fy1;
            }
            isInBox = ((iX >= left1 - 0.0009d && iX <= Right1 + 0.0009d) && (iY >= bot1 - 0.0009d && iY <= top1 + 0.0009d));

            return isInBox;
        }

        /// <summary>
        /// Determines if a given point lies within the bounds of of 2 points within tolerance of input
        /// Box runs from upper left to lower right, no rotation is taken into account
        /// </summary>
        /// <param name="fx1">Easting of start of segement</param>
        /// <param name="fy1">Northing of start of segment</param>
        /// <param name="fx2">Easting of start of segment</param>
        /// <param name="fy2">Northing of start of segment</param>
        /// <param name="iX">Easting of interest</param>
        /// <param name="iY">Northing of interest</param>
        /// <param name="tol">input tolerance </param>
        public static bool FnInBox2(double fx1, double fy1, double fx2, double fy2, double iX, double iY, double tol)
        {
            bool isInBox = false;
            double Right1;
            double left1;
            double top1;
            double bot1;
            tol = Math.Abs(tol);

            if (fx1 > fx2)
            {
                Right1 = fx1;
                left1 = fx2;
            }
            else
            {
                Right1 = fx2;
                left1 = fx1;
            }

            if (fy1 > fy2)
            {
                top1 = fy1;
                bot1 = fy2;
            }
            else
            {
                top1 = fy2;
                bot1 = fy1;
            }
            isInBox = ((iX >= left1 - tol && iX <= Right1 + tol) && (iY >= bot1 - tol && iY <= top1 + tol));

            return isInBox;
        }

        /// <summary>
        /// Determines if a given point lies within the bounds of 4 points 
        /// Box runs from upper left to lower right...not that it reallay matters
        /// In this instance rotation is catered for
        /// </summary>
        /// <param name="fx1">Easting of start of segement</param>
        /// <param name="fy1">Northing of start of segment</param>
        /// <param name="fx2">Easting of start of segment</param>
        /// <param name="fy2">Easting of start of segment</param>
        /// <param name="fx3">Northing of start of segment</param>
        /// <param name="fy3">Northing of start of segment</param>
        /// <param name="fx4">Easting of start of segment</param>
        /// <param name="fy4">Northing of start of segment</param>
        /// <param name="iX">Easting of interest</param>
        /// <param name="iY">Northing of interest</param>
        public static bool FnInBox3(double fx1, double fy1, double fx2, double fy2, double fx3, double fy3, double fx4, double fy4, double iX, double iY)
        {
            double left1, left2, left3, left4;
            left1 = Vector.FnOnLine3(fx1, fy1, fx2, fy2, iX, iY, 0.0001);
            left2 = Vector.FnOnLine3(fx2, fy2, fx3, fy3, iX, iY, 0.0001);
            left3 = Vector.FnOnLine3(fx3, fy3, fx4, fy4, iX, iY, 0.0001);
            left4 = Vector.FnOnLine3(fx4, fy4, fx1, fy1, iX, iY, 0.0001);
            if (Math.Abs(left1 + left2 + left3 + left4) == 4)
                return true;
            else
                return false;
        }



        /// <summary>
        /// Trim the line outside the box's perimeter so only the line inside the box remains. Box runs from top left to bottom right.
        /// </summary>
        /// <param name="topLeftX"></param>
        /// <param name="topLeftY"></param>
        /// <param name="bottomRightX"></param>
        /// <param name="bottomRightY"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns><see langword="true"/> if the lines were trimmed, <see langword="false"/> if lines were not trimmed.</returns>
        public static bool TrimLinesToFitBox(double topLeftX, double topLeftY, double bottomRightX, double bottomRightY, ref double x1, ref double y1, ref double x2, ref double y2)
        {

            bool point1inside = NRG.MathsHelpers.Intersection.FnInBox(topLeftX, topLeftY, bottomRightX, bottomRightY, x1, y1);
            bool point2inside = NRG.MathsHelpers.Intersection.FnInBox(topLeftX, topLeftY, bottomRightX, bottomRightY, x2, y2);

            double returnX1, returnY1, returnX2, returnY2;

            if (point1inside & point2inside)
            {
                return false; //Both points are inside the box, nothing to trim.
            }
            else if (point1inside & !point2inside)
            {
                //Point 1 stays the same, point2 is replaced for intersection point1
                IntersectLineThroughBox(topLeftX, topLeftY, bottomRightX, bottomRightY, x1,y1,x2,y2, out returnX1, out returnY1, out returnX2, out returnY2);
                x2 = returnX1;
                y2 = returnY1;
                return true;
            }
            else if (!point1inside & point2inside)
            {
                //Point 2 stays the same, point1 is replaced for intersection point1
                IntersectLineThroughBox(topLeftX, topLeftY, bottomRightX, bottomRightY, x1, y1, x2, y2, out returnX1, out returnY1, out returnX2, out returnY2);
                x1 = returnX1;
                y1 = returnY1;
                return true;
            }
            else if (!point1inside & !point2inside)
            {
                //Both points are replaced by both intersection points
                IntersectLineThroughBox(topLeftX, topLeftY, bottomRightX, bottomRightY, x1, y1, x2, y2, out returnX1, out returnY1, out returnX2, out returnY2);
                x1 = returnX1;
                y1 = returnY1;
                x2 = returnX2;
                y2 = returnY2;
                return true;
            }
            return true;
        }
        /// <summary>
        /// Return the resulting intersection points between a line and a rectangle. Can return 0, 1 or 2 intersection points
        /// </summary>
        /// <param name="topLeftX"></param>
        /// <param name="topLeftY"></param>
        /// <param name="bottomRightX"></param>
        /// <param name="bottomRightY"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="ip1x"></param>
        /// <param name="ip1y"></param>
        /// <param name="ip2x"></param>
        /// <param name="ip2y"></param>
        /// <returns></returns>
        public static bool IntersectLineThroughBox(double topLeftX, double topLeftY, double bottomRightX, double bottomRightY, double x1, double y1, double x2, double y2, out double ip1X, out double ip1Y, out double ip2X, out double ip2Y)
        {
            double intersectionX = 0;
            double intersectionY = 0;

            ip1X = ip1Y = ip2X = ip2Y = 0;

            bool ip1found = false;

            //There can be 0, 1 or 2 intersection points

            if (IntersectLines(x1, y1, x2, y2, topLeftX, topLeftY, bottomRightX, topLeftY, ref intersectionX, ref intersectionY)) //Top Left to Top Right
            { 
                ip1found = true;
                ip1X = intersectionX;
                ip1Y = intersectionY;
            }
            if (IntersectLines(x1, y1, x2, y2, topLeftX, topLeftY, topLeftX, bottomRightY, ref intersectionX, ref intersectionY))//Top Left to Bottom Left
            { 
                if (ip1found) //ip1 already found, this must be second ip
                {
                    //Assign ip2 values and return
                    ip2X = intersectionX;
                    ip2Y = intersectionY;
                    return true;
                }
                else
                {
                    ip1found = true;
                    ip1X = intersectionX;
                    ip1Y = intersectionY;
                }
            }
            if (IntersectLines(x1, y1, x2, y2, bottomRightX, bottomRightY, topLeftX, bottomRightY, ref intersectionX, ref intersectionY))//Bottom Right to Bottom Left
            {
                if (ip1found) //ip1 already found, this must be second ip
                {
                    //Assign ip2 values and return
                    ip2X = intersectionX;
                    ip2Y = intersectionY;
                    return true;
                }
                else
                {
                    ip1found = true;
                    ip1X = intersectionX;
                    ip1Y = intersectionY;
                }
            }
            if (IntersectLines(x1, y1, x2, y2, bottomRightX, bottomRightY, bottomRightX, topLeftY, ref intersectionX, ref intersectionY))//Bottom Right to Top Right
            {
                if (ip1found) //ip1 already found, this must be second ip
                {
                    //Assign ip2 values and return
                    ip2X = intersectionX;
                    ip2Y = intersectionY;
                    return true;
                }
                else
                {
                    ip1found = true;
                    ip1X = intersectionX;
                    ip1Y = intersectionY;
                }
            }

            return ip1found; //If false, line doesn't intersect with box at all.
        }

        /// <summary>
        /// Returns a list of intersection points. Intersections between the test line and each line within the polygon (infinite distances).
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static List<Point2D> IntersectionsThroughPoly(List<Point2D> poly, double x1, double y1, double x2, double y2)
        {
            List<Point2D> intersectionPoints = new List<Point2D>();

            //Check each line within the poly for intersections with the test line
            for(int i = 0; i < poly.Count; i++)
            {
                Point2D ip = new Point2D();
                if (poly[i] == poly[poly.Count - 1]) //Last point in the list
                {
                    if (poly[i] == poly[0])
                    {
                        break; //Last point of the list is the same as the first. We have all the checks we need so we can exit here
                    }
                    //The last point needs to loop back to the first point to close the polyline
                    double brgPolyLine = MathsHelpers.Trig.RPC(poly[i].X, poly[i].Y, poly[0].X, poly[0].Y);
                    double brgTestLine = MathsHelpers.Trig.RPC(x1, y1, x2, y2);
                    ip = IntersectTwoBrgLines(poly[i].X, poly[i].Y, brgPolyLine, x1, y1, brgTestLine);
                }
                else 
                {
                    double brgPolyLine = MathsHelpers.Trig.RPC(poly[i].X, poly[i].Y, poly[i + 1].X, poly[i + 1].Y);
                    double brgTestLine = MathsHelpers.Trig.RPC(x1, y1, x2, y2);
                    ip = IntersectTwoBrgLines(poly[i].X, poly[i].Y, brgPolyLine, x1, y1, brgTestLine);
                }

                //Check ip is inside the box made up of the test line and the polyline we're getting an intersection from
                if (FnInBox1(x1, y1, x2, y2, ip.X, ip.Y))
                { intersectionPoints.Add(ip); }
            }
            return intersectionPoints;
        }

        /// <summary>
        /// Return a list of lines that lay either inside or outside a polygon.
        /// </summary>
        /// <param name="poly">List of polygon nodes</param>
        /// <param name="x1">Easting of the test line's start</param>
        /// <param name="y1">Northing of the test line's start</param>
        /// <param name="x2">Easting of the test line's end</param>
        /// <param name="y2">Northing of the test line's end</param>
        /// <param name="linesInsidePoly"> true to return lines only inside the polygon. false to return lines only outside the polygon</param>
        /// <returns></returns>
        public static List<Tuple<Point2D, Point2D>> TrimLinesByPoly(List<Point2D> poly, double x1, double y1, double x2, double y2, bool linesInsidePoly = true)
        {
            //Get list of intersection points from where the test line crosses a line witihn the polygon
            List<Point2D> ipList = new List<Point2D>();
            ipList.Add(new Point2D(x1, y1)); //Add the first point of the test line
            ipList.AddRange(IntersectionsThroughPoly(poly, x1, y1, x2, y2)); //Add all the found insersection points
            ipList.Add(new Point2D(x2, y2)); //Add the end point of the rest line
            //Sort points in order of distance from one end of the start line (dooesn't really matter, as long as they're in some sort of order)
            ipList = NRG.Models.SearchingAndSorting.BubbleSortSortPointsByVector(new Point2D(x1,y1), ipList);
            List<Tuple<Point2D, Point2D>> Lines = new List<Tuple<Point2D, Point2D>>();

            
            
            //Loop through all intersection points
            //(starting from start point of the test line, ending with the end point of the test line
            //Stop when we get to the last point (there are no points for it to compare against)
            for (int i = 0; i < ipList.Count -1; i++)
            {
                //Get the centre point between a pair of intersection points
                double centreX = (ipList[i].X + ipList[i + 1].X) / 2;
                double centreY = (ipList[i].Y + ipList[i + 1].Y) / 2;

                //Check wether the centre point of the two intersection points is within the polyline.
                if (linesInsidePoly && MathsHelpers.Geometry.InPoly2D(poly, centreX, centreY))
                {
                    Lines.Add(Tuple.Create(ipList[i], ipList[i+1]));
                }
                else if (!linesInsidePoly && !MathsHelpers.Geometry.InPoly2D(poly, centreX, centreY))
                {
                    Lines.Add(Tuple.Create(ipList[i], ipList[i + 1]));
                }
            }

            return Lines;
        }

        public static List<Tuple<Point2D, Point2D>> TrimLinesByPoly(List<DTMPoint> dtmpolylist, double x1, double y1, double x2, double y2, bool linesInsidePoly = true)
        {
            List<Point2D> point2Dlist = new List<Point2D>();

            //Convert the outline shape into a list of point2Ds
            foreach(var dtmpoint in dtmpolylist)
            {
                Point2D pt = new Point2D(dtmpoint.X, dtmpoint.Y);
                point2Dlist.Add(pt);
            }

            return TrimLinesByPoly(point2Dlist, x1,y1,x2,y2,linesInsidePoly);
        }

        #endregion

        #region Circle Intersection

        /// <summary>
        /// Determines whether a line intersects an arc and returns the intersection points if it does
        /// </summary>
        /// <param name="ArcStartPt">A <see cref="Point2D"/> representing the start X and Y of the arc</param>
        /// <param name="ArcEndPt">A <see cref="Point2D"/> representing the end X and Y of the arc</param>
        /// <param name="Radius">The radius of the arc</param>
        /// <param name="LineStart">A <see cref="Point2D"/> representing the start X and Y of the line</param>
        /// <param name="brg">The bearing of the line</param>
        /// <param name="IPS">A List of <see cref="Point2D"/> to contain the intersection points</param>
        /// <returns>Returns true if there is an intersection between the line and the arc</returns>
        public static bool LineIntersectsArc(Point2D ArcStartPt, Point2D ArcEndPt, double Radius, Point2D LineStart, double brg, ref List<Point2D> IPS)
        {
            Point2D cent = new Point2D();
            Point2D ip1 = new Point2D(), ip2 = new Point2D();
            ip1.X = double.PositiveInfinity;
            ip2.X = double.PositiveInfinity;
            bool ok = false;
            cent = Geometry.CircleCentre(ArcStartPt, ArcEndPt, Radius);

            ok = IntersectLineAndCircle(LineStart, brg, cent.X, cent.Y, Radius, ref ip1, ref ip2);
            if (ok == false)
            {
                return false;
            }
            else
            {
                if (Radius >= 0)
                {
                    if (Vector.FnOnLine3(ArcStartPt.X, ArcStartPt.Y, ArcEndPt.X, ArcEndPt.Y, ip1.X, ip1.Y) <= 0)
                    {
                        IPS.Add(ip1);
                    }
                    if (Vector.FnOnLine3(ArcStartPt.X, ArcStartPt.Y, ArcEndPt.X, ArcEndPt.Y, ip2.X, ip2.Y) <= 0)
                    {
                        IPS.Add(ip2);
                    }
                }
                else
                {
                    if (Vector.FnOnLine3(ArcStartPt.X, ArcStartPt.Y, ArcEndPt.X, ArcEndPt.Y, ip1.X, ip1.Y) >= 0)
                    {
                        IPS.Add(ip1);
                    }
                    if (Vector.FnOnLine3(ArcStartPt.X, ArcStartPt.Y, ArcEndPt.X, ArcEndPt.Y, ip2.X, ip2.Y) >= 0)
                    {
                        IPS.Add(ip2);
                    }
                }
                return IPS.Count > 0;
            }

        }

        /// <summary>
        /// Determines whether there is a vertical intersection between a line and a circle
        /// </summary>
        /// <param name="ox">The X value of the Line start</param>
        /// <param name="cx">Circle Centre X coords</param>
        /// <param name="cy">Circle Centre Y coords</param>
        /// <param name="rad">Circle Radius</param>
        /// <param name="ipy1">A <see cref="double"/> to contain the first intersection Y value</param>
        /// <param name="ipy2">A <see cref="double"/> to contain the second intersection Y value</param>
        /// <returns>Returns true if there is a vertical intersection between the line and circle else returns false</returns>
        public static bool IntersectVerticalLineAndCircle(double ox, double cx, double cy, double rad, ref double ipy1, ref double ipy2)
        {
            double discriminent = rad * rad - (ox - cx) * (ox - cx);

            if (discriminent < 0)
            {
                return false;
            }
            else
            {
                ipy1 = cy + Math.Sqrt(discriminent);
                ipy2 = cy - Math.Sqrt(discriminent);
                return true;
            }
        }

        /// <summary>
        /// LineIntersectsArc - giver and arc described by start coords, end coords and radius calculates line intersection points if any
        /// </summary>
        /// <param name="ArcStartPt">start of the arc</param>
        /// <param name="ArcEndPt">end of the arc</param>
        /// <param name="Radius">radius of the arc</param>
        /// <param name="LineStart">start of the line</param>
        /// <param name="LineEnd">end of the line</param>
        /// <param name="IPS">list containing intersection point - either 1 or 2</param>
        /// <returns>True if ips found, false else</returns>
        public static bool LineIntersectsArc(Point2D ArcStartPt, Point2D ArcEndPt, double Radius, Point2D LineStart, Point2D LineEnd, ref List<Point2D> IPS)
        {
            Point2D cent = new Point2D();
            Point2D ip1 = new Point2D(), ip2 = new Point2D();
            ip1.X = double.PositiveInfinity;
            ip2.X = double.PositiveInfinity;
            bool ok = false;
            //cent = Geometry.CircleCentre(ArcStartPt, ArcEndPt, Radius);

            double brg = 0, dist = 0;
            Trig.GetBrgAndDist(ArcStartPt.X, ArcStartPt.Y, ArcEndPt.X, ArcEndPt.Y, ref brg, ref dist);

            //Calc center point
            var oppo = Math.Sqrt((Radius * Radius) - ((dist / 2) * (dist / 2)));
            double cx = 0, cy = 0;
            var clx = (ArcStartPt.X + ArcEndPt.X) / 2;
            var cly = (ArcStartPt.Y + ArcEndPt.Y) / 2;

            if (Vector.FnEquals(Radius, (dist / 2)))
            {
                cx = clx;
                cy = cly;
            }
            else
            {
                if (Radius < 0)
                    Trig.GetCoords(clx, cly, brg - (Math.PI / 2), oppo, ref cx, ref cy);
                else
                    Trig.GetCoords(clx, cly, brg + (Math.PI / 2), oppo, ref cx, ref cy);
            }
            cent = new Point2D(cx, cy);

            ok = IntersectLineAndCircle(LineStart, LineEnd, cent.X, cent.Y, Radius, ref ip1, ref ip2);
            if (ok == false)
            {
                return false;
            }
            else
            {
                if (Radius >= 0)
                {
                    if (Vector.FnOnLine3(ArcStartPt.X, ArcStartPt.Y, ArcEndPt.X, ArcEndPt.Y, ip1.X, ip1.Y) <= 0)
                    {
                        IPS.Add(ip1);
                    }
                    if (Vector.FnOnLine3(ArcStartPt.X, ArcStartPt.Y, ArcEndPt.X, ArcEndPt.Y, ip2.X, ip2.Y) <= 0)
                    {
                        IPS.Add(ip2);
                    }
                }
                else
                {
                    if (Vector.FnOnLine3(ArcStartPt.X, ArcStartPt.Y, ArcEndPt.X, ArcEndPt.Y, ip1.X, ip1.Y) >= 0)
                    {
                        IPS.Add(ip1);
                    }
                    if (Vector.FnOnLine3(ArcStartPt.X, ArcStartPt.Y, ArcEndPt.X, ArcEndPt.Y, ip2.X, ip2.Y) >= 0)
                    {
                        IPS.Add(ip2);
                    }
                }
                return IPS.Count > 0;
            }

        }

        /// <summary>
        /// IntersectlinetoCircle
        /// </summary>
        /// <param name="pt1"> start of the line</param>
        /// <param name="brg"> WCB to the end of the line</param>
        /// <param name="cx"> circle centre X coord</param>
        /// <param name="cy"> circle centre Y coord</param>
        /// <param name="radius"> circle radius</param>
        /// <param name="ip1"> first intersection point</param>
        /// <param name="ip2"> second intersection point</param>
        /// <returns>True if there's intersection false else</returns>
        public static bool IntersectLineAndCircle(Point2D pt1, double brg, double cx, double cy, double radius, ref Point2D ip1, ref Point2D ip2)
        {
            const double ZERO_ACCURACY = 0.00000047;


            if (Math.Abs(brg) < ZERO_ACCURACY/* | Math.Abs(brg) - Math.PI < ZERO_ACCURACY | Math.Abs(brg) + Math.PI < ZERO_ACCURACY | Math.Abs(brg) - 2 * Math.PI < ZERO_ACCURACY*/)
            {
                ip1.X = pt1.X;
                ip2.X = pt1.X;
                double ipy1 = 0;
                double ipy2 = 0;
                if (IntersectVerticalLineAndCircle(pt1.X, cx, cy, radius, ref ipy1, ref ipy2))
                {
                    ip1.Y = ipy1;
                    ip2.Y = ipy2;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                double m = Math.Tan(Math.PI / 2 - brg);
                double c = pt1.Y - m * pt1.X;
                return IntersectLineAndCircle(m, c, cx, cy, radius, ref ip1, ref ip2);
            }
        }

        /// <summary>
        /// Determines whether there is an intersection between a line and a circle
        /// </summary>
        /// <param name="pt1">A <see cref="Point2D"/> representing the start of the line</param>
        /// <param name="pt2">A <see cref="Point2D"/> representing the end of the line</param>
        /// <param name="cx">The Circle Centre X</param>
        /// <param name="cy">The Circle Centre Y</param>
        /// <param name="radius">The Circle Radius</param>
        /// <param name="ip1">A <see cref="Point2D"/> to contain the first intersection point</param>
        /// <param name="ip2">A <see cref="Point2D"/> to contain the second intersection point</param>
        /// <returns>Returns true if there is an intersection between the line and the circle else returns false</returns>
        public static bool IntersectLineAndCircle(Point2D pt1, Point2D pt2, double cx, double cy, double radius, ref Point2D ip1, ref Point2D ip2)
        {
            double brg = 0; double d = 0;

            MathsHelpers.Trig.RPC(pt1.X, pt1.Y, pt2.X, pt2.Y, ref brg, ref d);
            return IntersectLineAndCircle(pt1, brg, cx, cy, radius, ref ip1, ref ip2);

        }

        /// <summary>
        /// IntersectLineAndCircle
        /// </summary>
        /// <param name="m">m element of line equation</param>
        /// <param name="c">c of the equation</param>
        /// <param name="cx"> circle centre X coord</param>
        /// <param name="cy"> circle centre Y coord</param>
        /// <param name="radius"> circle radius</param>
        /// <param name="ip1"> first intersection</param>
        /// <param name="ip2"> second intersection</param>
        /// <returns>True if there's any intersection false else</returns>
        public static bool IntersectLineAndCircle(double m, double c, double cx, double cy, double radius, ref Point2D ip1, ref Point2D ip2)
        {
            double discriminent = ((1 + Math.Pow(m, 2)) * Math.Pow(radius, 2)) - Math.Pow((m * cx + (c - cy)), 2);
            if (discriminent < 0)
            {
                return false;
            }
            else
            {
                ip1.X = (cx - m * (c - cy) + Math.Sqrt(discriminent)) / (1 + Math.Pow(m, 2));
                ip1.Y = m * ip1.X + c;
                ip2.X = (cx - m * (c - cy) - Math.Sqrt(discriminent)) / (1 + Math.Pow(m, 2));
                ip2.Y = m * ip2.X + c;
                return true;
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;

namespace NRG.MathsHelpers
{
    public static class Trig
    {
        #region Legacy

        /// <summary>
        /// Calculates the angle between two bearings
        /// </summary>
        /// <param name="a">The first bearing</param>
        /// <param name="b">The second bearing</param>
        /// <returns>Returns the angle between the two bearings in radians</returns>
        public static double FnAngle(double a, double b)
        {
            double answer = 0, TWOPI = Math.PI * 2;
            answer = a - b;
            if (answer > TWOPI) { answer = answer - TWOPI; }
            if (answer < 0) { answer = answer + TWOPI; }

            // answer = ((answer / TWOPI) - (int)((double)((answer / TWOPI)))) * TWOPI;

            return answer;
        }

        /// <summary>
        /// Calculates the angel between two bearings
        /// </summary>
        /// <param name="A">The first bearing</param>
        /// <param name="b">The second bearing</param>
        /// <returns>Returns the angle between the two bearings in radians</returns>
        public static double FnSmallAngle(double A, double b)
        {
            double answer = 0, TWOPI = Math.PI * 2;

            answer = A - b;
            if (answer > TWOPI) { answer = answer - TWOPI; }
            if (answer < 0) { answer = answer + TWOPI; }
            if (answer > Math.PI) { answer = TWOPI - answer; }
            return answer;
        }

        /// <summary>
        /// Polar to Rectangular - finds the coordinate at a given bearing and distance from a starting coordinate
        /// </summary>
        /// <param name="east">The X value of the start coordinate</param>
        /// <param name="north">The Y value of the start coordinate</param>
        /// <param name="brg">The bearing of the new coordinate from the start coordinate</param>
        /// <param name="dist">The distance of the new coordinate from the old coordinate</param>
        /// <param name="easta">The X value of the new coordinate</param>
        /// <param name="northa">The Y value of the new coordinate</param>
        /// <param name="scalefactor">The scale factor to use</param>
        public static void PRC(double east, double north, double brg, double dist, ref double easta, ref double northa, double scalefactor = 1.0)
        {
            double newdist = 0, X = 0, Y = 0;
            if (scalefactor == 0) { scalefactor = 1; }

            newdist = dist * scalefactor;
            if (brg == Math.PI / 2)
            {
                X = newdist;
            }
            else if (brg == 3 * (Math.PI / 2))
            {
                X = -newdist;
            }
            else
            {
                X = Math.Sin(brg) * newdist;
            }
            if (brg == 0)
            {
                Y = newdist;
            }
            else if (brg == Math.PI)
            {
                Y = -newdist;
            }
            else
            {
                Y = Math.Cos(brg) * newdist;
            }
            easta = east + X;
            northa = north + Y;
        }

        /// <summary>
        /// Polar to Rectangular - finds the coordinate at a given bearing and distance from a starting coordinate
        /// </summary>
        /// <param name="east">The X value of the start coordinate</param>
        /// <param name="north">The Y value of the start coordinate</param>
        /// <param name="brg">The bearing of the new coordinate from the start coordinate</param>
        /// <param name="dist">The distance of the new coordinate from the start coordinate</param>
        /// <param name="scalefactor">The scale factor to use</param>
        /// <returns>Returns a <see cref="Point2D"/> containing the new coordinate</returns>
        public static Point2D PRC(double east, double north, double brg, double dist, double scalefactor = 1.0)
        {
            double x = 0, y = 0;
            PRC(east, north, brg, dist, ref x, ref y, scalefactor);
            return new Point2D(x, y);
        }

        /// <summary>
        /// Rectangular to Polar - finds the bearing and distance between two coordinates
        /// </summary>
        /// <param name="Eocc">The X value for the first coordinate</param>
        /// <param name="Nocc">The Y value of the first coordinate</param>
        /// <param name="er">The X value of the second coordinate</param>
        /// <param name="nr">The Y value of the second coordinate</param>
        /// <param name="brg">A <see cref="double"/> to store the bearing between the two coordinates</param>
        /// <param name="dist">A <see cref="double"/> to store the distance between the two coordinates</param>
        /// <param name="scaleFactor">The scalefactor to use</param>
        public static void RPC(double Eocc, double Nocc, double er, double nr, ref double brg, ref double dist, double scaleFactor = 1)
        {

            double X, Y;

            if (scaleFactor <= 0) { scaleFactor = 1; }

            X = er - Eocc;
            Y = nr - Nocc;

            bool calcBrg = true;

            if (Y == 0 & X >= 0)
            {
                brg = Math.PI / 2;
                calcBrg = false;
            }
            if (Y == 0 & X < 0)
            {
                brg = 1.5 * Math.PI;
                calcBrg = false;
            }

            if (calcBrg == true)
            {
                brg = Math.Atan(X / Y) - Math.PI;
                if (Y > 0) { brg = brg + Math.PI; }
                if (brg < 0) { brg = brg + (2 * Math.PI); }
            }

            dist = Math.Sqrt((X * X) + (Y * Y));
            dist = dist / scaleFactor;
        }

        /// <summary>
        /// Rectangular to Polar - finds the bearing and distance between two coordinates
        /// </summary>
        /// <param name="Eocc">The X value for the first coordinate</param>
        /// <param name="Nocc">The Y value for the first coordinate</param>
        /// <param name="er">The X value of the second coordinate</param>
        /// <param name="nr">The Y value of the second coordinate</param>
        /// <param name="scaleFactor">The scalefactor to use</param>
        /// <returns>Returns a <see cref="double"/> containing the bearing between the two coordinates</returns>
        public static double RPC(double Eocc, double Nocc, double er, double nr, double scaleFactor = 1)
        {
            double X, Y;
            double brg = 0;
            if (scaleFactor <= 0) { scaleFactor = 1; }

            X = er - Eocc;
            Y = nr - Nocc;

            bool calcBrg = true;

            if (Y == 0 & X >= 0)
            {
                brg = Math.PI / 2;
                calcBrg = false;
            }
            if (Y == 0 & X < 0)
            {
                brg = 1.5 * Math.PI;
                calcBrg = false;
            }

            if (calcBrg == true)
            {
                brg = Math.Atan(X / Y) - Math.PI;
                if (Y > 0) { brg = brg + Math.PI; }
                if (brg < 0) { brg = brg + (2 * Math.PI); }
            }

            return brg;
        }

        /// <summary>
        /// Rotate a 2D coordinate around an origin in a clockwise direction
        /// </summary>
        /// <param name="x">X coord to rotate</param>
        /// <param name="y">Y coord to rotate</param>
        /// <param name="clockWiseAngle">Clockwise angle to rotate by</param>
        /// <param name="originX">The X coord to rotate about</param>
        /// <param name="originY">The Y coord to rotate about</param>
        /// <returns>Returns a <see cref="Point2D"/> containing the rotated point</returns>
        public static Point2D RotateXYClockWiseAroundOrigin(double x, double y, double clockWiseAngle, double originX, double originY)
        {
            var outputPoint = new Point2D();

            outputPoint.X = originX + (Math.Cos(clockWiseAngle) * (x - originX)) + (Math.Sin(clockWiseAngle) * (y - originY));
            outputPoint.Y = originY - (Math.Sin(clockWiseAngle) * (x - originX)) + (Math.Cos(clockWiseAngle) * (y - originY));

            return outputPoint;
        }

        /// <summary>
        /// /rotate a 2d coordinate around an origin in an counter clockwise direction...an' that
        /// /Ported by ES:23.03.19 - orginal VB code found in MATHSLIB.bas. Function name RotateXYAroundAB
        /// </summary>
        /// <param name="x">X coord to rotate</param>
        /// <param name="y">Y coord to rotate</param>
        /// <param name="AntiClockWiseAngle">Anti clockwise angle to rotate by</param>
        /// <param name="originX">The X coord to rotate about</param>
        /// <param name="originY">The Y coord to rotate about</param>
        /// <returns>Returns a <see cref="Point2D"/> containing the rotated point</returns>
        public static Point2D RotateXYAroundOrigin(double x, double y, double AntiClockWiseAngle, double originX, double originY)
        {
            var outputPoint = new Point2D();

            outputPoint.X = originX + (Math.Cos(AntiClockWiseAngle) * (x - originX)) - (Math.Sin(AntiClockWiseAngle) * (y - originY));
            outputPoint.Y = originY + (Math.Sin(AntiClockWiseAngle) * (x - originX)) + (Math.Cos(AntiClockWiseAngle) * (y - originY));

            return outputPoint;
        }

        /// <summary>
        /// Calculates the distance and offset of a point from a line determined by a start x, y and end x, y
        /// </summary>
        /// <param name="x1">The X value for the line start</param>
        /// <param name="y1">The Y value for the line start</param>
        /// <param name="x2">The X value for the line end</param>
        /// <param name="y2">The Y value for the line end</param>
        /// <param name="px">The X value for the point</param>
        /// <param name="py">The Y value for the point</param>
        /// <param name="distance">A <see cref="double"/> to store the distance of the point along the line</param>
        /// <param name="offset">A <see cref="double"/> to store the offset of the point from the line</param>
        public static void DistanceAndOffsetFromLine(double x1, double y1, double x2, double y2, double px, double py, ref double distance, ref double offset)
        {
            DistanceAndOffsetFromLine(x1, y1, RPC(x1, y1, x2, y2), px, py, ref distance, ref offset);
        }

        /// <summary>
        /// Calculates the distance and offset of a point from a line determined by a start X, Y and Bearing
        /// </summary>
        /// <param name="originX">The X value for the line start</param>
        /// <param name="originY">The Y value for the line start</param>
        /// <param name="bearing">The bearing for the line</param>
        /// <param name="px">The X value for the point</param>
        /// <param name="py">The Y value for the points</param>
        /// <param name="distance">A <see cref="double"/> to store the distance of the point along the line</param>
        /// <param name="offset">A <see cref="double"/> to store the offset of the point from the line</param>
        public static void DistanceAndOffsetFromLine(double originX, double originY, double bearing, double px, double py, ref double distance, ref double offset)
        {
            double brg = 0;
            double dist = 0;
            double theta = 0;

            RPC(originX, originY, px, py, ref brg, ref dist, 1);
            if (Vector.FnDistance(originX, originY, px, py) <= 0.001)
            {
                distance = 0;
                offset = 0;
                return;
            }

            theta = brg - bearing;
            distance = Math.Cos(theta) * dist;
            offset = Math.Sin(theta) * dist;
        }

        /// <summary>
        /// Calculates the distance and offset of a point from a line determied by a start X, Y and End X, Y
        /// </summary>
        /// <param name="startX">The X value for the line start</param>
        /// <param name="startY">The Y value for the line start</param>
        /// <param name="endX">The X value for the line end</param>
        /// <param name="endY">The Y value for the line end</param>
        /// <param name="radius">The radius of the line. A radius of 0 indicates a line otherwise a curve</param>
        /// <param name="pX">The X value for the point</param>
        /// <param name="pY">The Y value for the point</param>
        /// <param name="distance">A <see cref="double"/> to store the distance of the point along the line</param>
        /// <param name="offset">A <see cref="double"/> to store the offset of the point from the line</param>
        public static void DistanceAndOffsetFromLine(double startX, double startY, double endX, double endY, double radius, double pX, double pY, ref double distance, ref double offset)
        {
            //Added  by WA 27.06.19 Handles Dist and Offset for Lines (Radius = 0)
            if(radius == 0)
            {
                double tBrg = 0, tDist = 0;
                Trig.RPC(startX, startY, endX, endY, ref tBrg, ref tDist);
                Trig.DistanceAndOffsetFromLine(startX, startY, tBrg, pX, pY, ref distance, ref offset);
                return;
            }

            double brg = 0, entryBrg = 0, exitBrg = 0, length = 0, dist = 0, theta = 0, a = 0, r = 0;

            //Added By: WA 18.06.19 Testing if this allows for correct offset calculation with reversed lines
            if (radius < 0)
            {
                var temp = startX;
                startX = endX;
                endX = temp;

                temp = startY;
                startY = endY;
                endY = temp;

                r = radius * -1;
            }
            else
                r = radius;

            CalcEntryAndExitBrg(startX, startY, endX, endY, 0, 0, r, ref entryBrg, ref exitBrg, ref length);//Added 0 and 0 as parameters. If this if used at all, this will need rethinking TN 03/08/22

            RPC(startX, startY, endX, endY, ref brg, ref dist);

            //Calc center point
            var oppo = Math.Sqrt((r * r) - (dist / 2) * (dist / 2));
            double cx = 0, cy = 0;
            var clx = (startX + endX) / 2;
            var cly = (startY + endY) / 2;

            if (Vector.FnEquals(r, (dist / 2)))
            {
                cx = clx;
                cy = cly;
            }
            else
                PRC(clx, cly, brg + (Math.PI / 2), oppo, ref cx, ref cy, 1);

            RPC(cx, cy, pX, pY, ref brg, ref dist);

            if (r < 0)
            {
                a = entryBrg + (Math.PI / 2);
                if (a > 2 * Math.PI)
                    a -= (2 * Math.PI);

                theta = (a + (2 * Math.PI)) - brg;
                offset = r + dist;
            }
            else
            {
                a = entryBrg - (Math.PI / 2);
                if (a < -0.000001)
                    a += (Math.PI * 2);

                theta = (brg + (2 * Math.PI)) - a;
                offset = r - dist;
            }

            if (Vector.FnEquals(a, brg, 0.000000001))
                distance = 0;
            else
            {
                if (theta >= (Math.PI * 2))
                    theta -= (Math.PI * 2);
                distance = Math.Abs(r * theta);
            }

            if(radius < 0)
            {
                var l = CalcLineLength(startX, startY, endX, endY, r);
                distance = l - distance;
            }
        }
       
        /// <summary>
        /// Calculates the length of a given line or arc
        /// </summary>
        /// <param name="startX">The X coordinate of the line start</param>
        /// <param name="startY">The Y coordinate of the line start</param>
        /// <param name="endX">The X coordinate of the line end</param>
        /// <param name="endY">The Y coordinate of the line end</param>
        /// <param name="radius">Optional radius representing an arc</param>
        /// <returns>Returns a <see cref="double"/> containing the line length</returns>
        public static double CalcLineLength(double startX, double startY, double endX, double endY, double radius = 0)
        {
            var dist = Vector.FnDistance(startX, startY, endX, endY);

            if (radius != 0)
            {
                if (Vector.FnEquals(dist, 0))
                    return (radius * 2) * Math.PI;
                else
                    return radius * (2 * Math.Asin(dist / (2 * radius)));
            }
            else
                return dist;
        }

        /// <summary>
        /// Given the 2D coordinates of the start and end of a curve returns the
        /// Start bearing, end bearing and arc length 
        /// </summary>
        /// <param name="x1">arc start X</param>
        /// <param name="y1">arc start Y</param>
        /// <param name="x2">arc end X</param>
        /// <param name="y2">arc end Y</param>
        /// <param name="radius">arc radius</param>
        /// <param name="entryBrg">returned entry bearing</param>
        /// <param name="exitBrg">retuned exit bearing</param>
        /// <param name="length">returned arc length</param>
        public static void CalcEntryAndExitBrg(double x1, double y1, double x2, double y2, double cpX, double cpY, double radius,  ref double entryBrg, ref double exitBrg, ref double length)
        {
            double brg = 0, d = 0, da = 0;
            RPC(x1, y1, x2, y2, ref brg, ref d);

            if (radius == 0)
            {
                entryBrg = brg;
                exitBrg = brg;
                length = d;
                return;
            }

            //Commented oiut 03/08/22 TN
            //var val = (d / (2 * radius));

            //if (val > 1)
            //    val = 1;

            //da = Math.Asin(val);

            double brg1 = RPC(cpX, cpY, x1, y1);
            double brg2 = RPC(cpX, cpY, x2, y2);


                da = Math.Abs(NRG.MathsHelpers.Trig.FnSmallAngle(brg1, brg2) / 2);

                //da = NRG.MathsHelpers.Trig.FnSmallAngle(brg2, brg1) / 2;
            if (radius < 0)
            {
                da = 0 - da;
            }



            entryBrg = brg - da;
            if (entryBrg < 0)
                entryBrg += (Math.PI * 2);

            exitBrg = brg + da;
            if (exitBrg > (2 * Math.PI))
                exitBrg -= (2 * Math.PI);

            length = Math.Abs(radius) * 2 * (Math.Asin(d / (2 * Math.Abs(radius))));
        }

        /// <summary>
        /// Calculates the coordinates of a given chainage value on a curve element
        /// </summary>
        /// <param name="Chainage">The Chainage value of the point to calculate</param>
        /// <param name="ChOrig">The Chainage Origin of the curve element</param>
        /// <param name="EastOrig">The Easting Origin of the curve element</param>
        /// <param name="NorthOrig">The Northing Origin of the curve element</param>
        /// <param name="BrgOrig">The Bearing Origin</param>
        /// <param name="lastRad">The radius of the last element</param>
        /// <param name="thisRad">The radius of the current element</param>
        /// <param name="nextRad">The radius of the next element</param>
        /// <param name="thisRL">The length of the spiral</param>
        /// <param name="east">A <see cref="double"/> to contain the Easting of the curve at the given chainage</param>
        /// <param name="north">A <see cref="double"/> to contain the Northing of the curve at the given chainage</param>
        /// <param name="tang">A <see cref="double"/> to contain the tangent to the curve at the given chainage</param>
        /// <param name="r">A <see cref="double"/> to contain the radius of the curve at the given chainage</param>
        public static void CoordsOnCurve(double Chainage, double ChOrig, double EastOrig, double NorthOrig, double BrgOrig, double lastRad, double thisRad, double nextRad, double thisRL, out double east, out double north, out double tang, out double r)
        {
            double radi, Distance, bearing;
            double tangent, j, l, m;
            double EastO, northO;

            tang = 0;
            east = 0;
            north = 0;
            r = 0;

            Distance = Chainage - ChOrig;

            //straight
            if ((thisRad == 0) && (thisRL == 0))
            {
                bearing = BrgOrig;
                tangent = bearing;
                r = 0;

            }
            //curve
            else if (thisRL == 0)
            {
                j = Distance / (thisRad * 2);
                bearing = BrgOrig + j;
                tangent = BrgOrig + (2 * j);
                Distance = (2 * thisRad) * Math.Sin(j);
                r = thisRad;
            }
            //sprial
            else
            {
                if (Distance == 0)
                {
                    radi = double.PositiveInfinity;
                }
                else
                {
                    radi = Math.Abs(thisRL / Distance);
                }

                m = Distance / (2 * radi);
                j = (m / 3) + ((m * m * m) / 105) + ((m * m * m * m * m) / 5997);
                j = Math.Abs(j);
                j = Math.Atan(j);
                tangent = (Distance * Distance) / (2 * Math.Abs(thisRL));
                tangent = FnAngle(tangent, 0);

                if (Chainage > ChOrig)
                {
                    if (nextRad < 0)
                    {
                        j = -j;
                        tangent = -tangent;
                        r = -radi;
                    }
                    else
                    {
                        r = radi;
                    }
                }
                else
                {
                    if (lastRad != 0)
                    {
                        if (lastRad > 0)
                        {
                            j = -j;
                            tangent = -tangent;
                            r = radi;
                        }
                        else
                        {
                            r = -radi;
                        }
                    }
                    else
                    {
                        if (nextRad > 0)
                        {
                            j = -j;
                            tangent = -tangent;
                            r = radi;
                        }
                        else
                        {
                            r = -radi;
                        }
                    }
                }

                bearing = BrgOrig + j;
                tangent = BrgOrig + tangent;
                tangent = FnAngle(tangent, 0);
                l = Distance;
                Distance = l - ((l * l * l) / (40 * (radi * radi))) + ((l * l * l * l * l) / (3456 * (radi * radi * radi * radi)));
                Distance = Distance / Math.Cos(j);
            }

            EastO = EastOrig;
            northO = NorthOrig;
            double oEast = 0; double oNorth = 0;
            PRC(EastO, northO, bearing, Distance, ref oEast, ref oNorth, 1);
            east = oEast;
            north = oNorth;
            tang = tangent;
            while (tang > 2 * Math.PI)
            {
                tang = tang - 2 * Math.PI;
            }
        }

        public static void Geometry2Coords(double Chstart, double ChOrigin, double eastOrigin, double northOrigin, double BrgOrigin, double radius, double rl, double prevRadius, double nextRadius, double nextChstart, ref HorizontalElementType eletype, ref double eastStart, ref double northStart, ref double RadiusStart, ref double length, ref bool newele, ref double eaststart2, ref double northStart2)
        {
            double tang;

            if ((rl == 0) && ((radius == 0) || (Math.Abs(radius) > 100000000)))
            {
                eletype = 0;
                eastStart = eastOrigin;
                northStart = northOrigin;
                RadiusStart = 0;

            }
            else if (rl == 0)
            {
                eletype = HorizontalElementType.Arc;
                eastStart = eastOrigin;
                northStart = northOrigin;
                RadiusStart = radius;
                if (nextChstart != 0)
                {
                    //if  (!MathsHelpers.Vector.FnEquals(nextChstart - Chstart , Math.Abs(radius) * Math.PI, 0.01) && (nextChstart - Chstart > Math.Abs(radius) * Math.PI))
                    if (nextChstart - Chstart > Math.Abs(radius) * 2)//Math.PI)
                    {
                        newele = true;
                        BrgOrigin = BrgOrigin + Math.PI;
                        if (BrgOrigin > 2 * Math.PI) { BrgOrigin = BrgOrigin - 2 * Math.PI; }
                        PRC(eastStart, northStart, BrgOrigin - Math.PI / 2, 2 * radius, ref eaststart2, ref northStart2, 1);
                    }
                }
            }
            else
            {
                eletype = HorizontalElementType.Spiral;
                CoordsOnCurve(Chstart, ChOrigin, eastOrigin, northOrigin, BrgOrigin, prevRadius, radius, nextRadius, rl, out eastStart, out northStart, out tang, out RadiusStart);
                if (Math.Abs(RadiusStart) > 100000000)
                {
                    RadiusStart = 0;
                }
                if (nextChstart == 0)
                {
                    length = Math.Abs(rl / RadiusStart);
                }
                else
                {
                    length = Math.Abs(nextChstart - Chstart);
                }
            }
        }


        /// <summary>
        /// FNasn returns the arc sine of x. Sorts out all potential div by zero shite
        /// that can occur due to M$'s lack of precision in floating point calculations.
        /// </summary>
        /// <param name="X"></param>
        /// <returns>Arc sine of the value X</returns>
        public static double FNasn(double X)
        {
            if (MathsHelpers.Vector.FnEquals(Math.Abs(X), 1, 0.00000000000001f))
            {
                return Math.Sign(X) * Math.PI / 2;  
            }
            else
            {
                return Math.Atan(X / Math.Sqrt(-X * X + 1));    
            }
            
        }

        public static double FNBearingToAngle(double brg)
        {
            return ((450 - (brg * 180 / Math.PI)) % 360) / 180 * Math.PI;
        }





        #endregion

        #region Conversion

        /// <summary>
        /// Converts degrees to radians
        /// </summary>
        /// <param name="deg">The angle to convert in degrees</param>
        /// <returns>Returns a <see cref="double"/> containing the angle in radians</returns>
        public static double DegToRad(double deg)
        {
            return (deg * Math.PI / 180);
        }

        /// <summary>
        /// Converts radians to degrees
        /// </summary>
        /// <param name="rad">The angle to convert in radians</param>
        /// <returns>Returns a <see cref="double"/> containing the angle in degrees</returns>
        public static double RadToDeg(double rad)
        {
            return (rad / Math.PI) * 180;
        }

        /// <summary>
        /// Converts degrees to gradians
        /// </summary>
        /// <param name="deg">The angle to convert in degrees</param>
        /// <returns>Returns a <see cref="double"/> containing the angle in gradians</returns>
        public static double DegToGrad(double deg)
        {
            return deg * (10D / 9D);
        }

        /// <summary>
        /// Converts gradians to degrees
        /// </summary>
        /// <param name="grad">The angle to convert in gradians</param>
        /// <returns>Returns a <see cref="double"/> containing the angle in degrees</returns>
        public static double GradToDeg(double grad)
        {
            return grad * (9D / 10D);
        }

        /// <summary>
        /// Converts radians to gradians
        /// </summary>
        /// <param name="rad">The angle to convert in radians</param>
        /// <returns>Returns a <see cref="double"/> containing the angle in gradians</returns>
        public static double RadToGrad(double rad)
        {
            return (rad / Math.PI) * 200;
        }

        /// <summary>
        /// Converts gradians to radians
        /// </summary>
        /// <param name="grad">The angle to convert in gradians</param>
        /// <returns>Returns a <see cref="double"/> containing the angle in radians</returns>
        public static double GradToRad(double grad)
        {
            return grad * (Math.PI / 200);
        }

        /// <summary>
        /// Converts decimal degrees to degrees, minutes, seconds
        /// </summary>
        /// <param name="deg">The angle to convert in degrees</param>
        /// <returns>Returns a <see cref="string"/> containing the angle in degrees, minutes, seconds</returns>
        public static string DegToDMS(double deg)
        {
            //Sanity check for the values
            while (deg > 360)
                deg -= 360;

            while (deg < 0)
                deg += 360;

            double d = 0;
            if (deg < 0)
                d = Math.Ceiling(deg);
            else
                d = Math.Floor(deg);

            double mTemp = ((Math.Abs(deg) - Math.Abs(d)) * 60);
            double m = Math.Floor(mTemp);
            double s = ((mTemp - m) * 60);
            return new string('0', 3 - d.ToString().Count()) + d + "'" + new string('0', 2 - m.ToString().Count()) + m + "'" + new string('0', 2 - Math.Floor(Math.Round(s, 2)).ToString().Count()) + Math.Round(s, 2);
        }

        /// <summary>
        /// Convertes an angle in radians to degrees, minutes and seconds
        /// </summary>
        /// <param name="rad">The angle to convert in radians</param>
        /// <returns>Returns a <see cref="string"/> containing the angle in degrees, minutes and seconds</returns>
        public static string RadToDMS(double rad)
        {
            return DegToDMS(RadToDeg(rad));
        }

        /// <summary>
        /// Converts degrees, minutes, seconds to decimal degrees
        /// </summary>
        /// <param name="dms">the angle to convert in degrees, minutes, seconds</param>
        /// <returns>Returns a <see cref="double"/> containing the angle in decimal degrees</returns>
        public static double DMSToDeg(string dms)
        {
            try
            {
                var vals = dms.Split('\'');
                var degrees = vals[0];
                var mintues = vals[1];
                var seconds = vals[2];

                if (Convert.ToDouble(degrees) < 0 || degrees.Contains("-"))
                    return -1 * Math.Round(Math.Abs(Convert.ToDouble(degrees)) + (Convert.ToDouble(mintues) / 60) + (Convert.ToDouble(seconds) / 3600), 8);
                else
                    return Math.Round(Convert.ToDouble(degrees) + (Convert.ToDouble(mintues) / 60) + (Convert.ToDouble(seconds) / 3600), 8);
            }
            catch
            {
                return -999;
            }
        }

        /// <summary>
        /// Converts degrees, minutes and seconds to radians
        /// </summary>
        /// <param name="dms">The angle to convert to radians</param>
        /// <returns>Returns a <see cref="double"/> containing the angle in decimal degrees</returns>
        public static double DMSToRad(string dms)
        {
            return DegToRad(DMSToDeg(dms));
        }

        #endregion

        #region General

        /// <summary>
        /// Calculates a set of new coordinates based on input coordinates, bearing and distance
        /// </summary>
        /// <param name="x">The X value for the input coordinate</param>
        /// <param name="y">The Y value for the input coordinate</param>
        /// <param name="brg">The bearing from the old coordinate to the new coordinate</param>
        /// <param name="dist">The distance from the old coordinate to the new coordinate</param>
        /// <param name="nX">A <see cref="double"/> to store the X value for the new coordinate</param>
        /// <param name="nY">A <see cref="double"/> to store the Y value for the new coordinate</param>
        /// <param name="scaleFactor">The scalefactor to use</param>
        public static void GetCoords(double x, double y, double brg, double dist, ref double nX, ref double nY, double scaleFactor = 1.0)
        {
            PRC(x, y, brg, dist, ref nX, ref nY, scaleFactor);
        }

        /// <summary>
        /// Calculates a set of new coordinates based on input coordinate, bearing and distance and returns these coordinates
        /// in the form of a <see cref="Point2D"/>
        /// </summary>
        /// <param name="x">The X value for the input coordinate</param>
        /// <param name="y">The Y value for the input coordinate</param>
        /// <param name="brg">The bearing from the old coordinate to the new coordinate</param>
        /// <param name="dist">The distance from the old coordinate to the new coordinate</param>
        /// <param name="scaleFactor">The scalefactor to use</param>
        /// <returns>Returns a <see cref="Point2D"/> containing the new x and y value</returns>
        public static Point2D GetCoords(double x, double y, double brg, double dist, double scaleFactor = 1.0)
        {
            double nX = 0, nY = 0;
            PRC(x, y, brg, dist, ref nX, ref nY, scaleFactor);

            return new Point2D(nX, nY);
        }

        /// <summary>
        /// Calculates the bearing between two coordinates
        /// </summary>
        /// <param name="x1">The X value for the first coordinate</param>
        /// <param name="y1">The Y value for the first coordinate</param>
        /// <param name="x2">The X value for the second coordinate</param>
        /// <param name="y2">The Y value for the second coordinate</param>
        /// <param name="scaleFactor">The scalefactor to use</param>
        /// <returns>Return a <see cref="double"/> containing the bearing between the two coordinates</returns>
        public static double GetBrg(double x1, double y1, double x2, double y2, double scaleFactor = 1.0)
        {
            return RPC(x1, y1, x2, y2, scaleFactor);
        }

        /// <summary>
        /// Calculates the bearing and distance between two points
        /// </summary>
        /// <param name="x1">The X value for the first coordinate</param>
        /// <param name="y1">The Y value for the first coordinate</param>
        /// <param name="x2">The X value for the second coordinate</param>
        /// <param name="y2">The Y value for the seoncd coordinate</param>
        /// <param name="brg">A <see cref="double"/> to store the bearing between the two coordinates</param>
        /// <param name="dist">A <see cref="double"/> to store the distance between the two coordinates</param>
        /// <param name="scaleFactor">The scalefactor to use</param>
        public static void GetBrgAndDist(double x1, double y1, double x2, double y2, ref double brg, ref double dist, double scaleFactor = 1.0)
        {
            RPC(x1, y1, x2, y2, ref brg, ref dist, scaleFactor);
        }

        /// <summary>
        /// Calculates the center point for an arc between two points with a given radius
        /// </summary>
        /// <param name="startX">The X value for the start of the Arc</param>
        /// <param name="startY">The Y value for the start of the Arc</param>
        /// <param name="endX">The X value for the end of the Arc</param>
        /// <param name="endY">The Y value for the end of the Arc</param>
        /// <param name="radius">The Radius for the Arc. Negative for anti-clockwise</param>
        /// <param name="centrePointSide">Side of the start-end line that the centre point lays. True for left, false for right</param>
        /// <param name="cX">a <see cref="double"/> to store the center X value for the Arc</param>
        /// <param name="cY">a <see cref="double"/> to store the center Y value for the Arc</param>
        public static void CalcCenterOfArc(double startX, double startY, double endX, double endY, double radius, ref double cX, ref double cY, bool centrePointSide = true)
        {
            double brg = 0, dist = 0;
            //GetBrgAndDist(startX, startY, endX, endY, ref brg, ref dist);
            RPC(startX, startY, endX, endY, ref brg, ref dist);

            //Calc center point
            var oppo = Math.Sqrt((radius * radius) - ((dist / 2) * (dist / 2)));
            cX = 0;
            cY = 0;

            var clx = (startX + endX) / 2;
            var cly = (startY + endY) / 2;


            if(Vector.FnEquals(radius, (dist / 2))) //centre point is on the line
            {
                cX = clx;
                cY = cly;
            }
            else
            {
                if (centrePointSide) //If we want to centre point to the left of the line, rotate brg 90degrees left
                { 
                    GetCoords(clx, cly, brg - (Math.PI / 2), oppo, ref cX, ref cY); 
                }
                else//else rotate it 90degrees right
                { 
                    GetCoords(clx, cly, brg + (Math.PI / 2), oppo, ref cX, ref cY); 
                }
            }
        }

        public static void CalcCenterOfArc2(double startX, double startY, double endX, double endY, double radius, ref double centX, ref double centY)
        {
            double q = Math.Sqrt(Math.Pow((endX - startX), 2) + Math.Pow((endY - startY), 2));

            double y3 = (startY + endY) / 2;

            double x3 = (startX + endX) / 2;

            double r = Math.Abs(radius);

            double basex = Math.Sqrt(Math.Pow(r, 2) - Math.Pow((q / 2), 2)) * (startY - endY) / q; //calculate once
            double basey = Math.Sqrt(Math.Pow(r, 2) - Math.Pow((q / 2), 2)) * (endX - startX) / q; //calculate once

            if (radius <= 0)
            {
                centX = x3 + basex; //center x of circle 1
                centY = y3 + basey; //center y of circle 1
            }
            else
            {
                centX = x3 - basex; //center x of circle 2
                centY = y3 - basey; //center y of circle 2
            }
        }

        public static List<Point2D> CalcVerticesOfArc(double centreX, double centreY, double startBrg, double endBrg, double radius, double nodeCount = 100)
        {
            if (nodeCount <= 1) { nodeCount = 2; } //rational input check. stepCount of 2 will still look weird.

            double bearing1 = 0;
            double totalBearing = 0;

            bool clockwise = true;
            if (radius < 0) { clockwise = false; }

            if (clockwise)
            {
                if (startBrg > endBrg)
                {
                    double a1 = (Math.PI * 2) - startBrg;
                    totalBearing = a1 + endBrg;
                }
                else
                {
                    totalBearing = endBrg - startBrg;
                }
            }
            else
            {
                if (startBrg > endBrg)
                {
                    totalBearing = startBrg - endBrg;
                }
                else
                {
                    double a2 = (Math.PI * 2) - endBrg;
                    totalBearing = a2 + startBrg;
                }

            }

            double stepBearing = totalBearing / (nodeCount - 1); //-1 here so we get the step count and not the node count (which is one less...)

            if (!clockwise) { stepBearing = -Math.Abs(stepBearing); } //Invert increment if anti-clockwise

            List<Point2D> ArcNodes = new List<Point2D>();
            for(int i = 0; i < nodeCount; i++)
            {
                double vertX = 0; double vertY = 0;
                double nodeBrg = AddAngle(startBrg, (i * stepBearing));
                PRC(centreX, centreY, nodeBrg , Math.Abs(radius) ,ref vertX, ref vertY);
                ArcNodes.Add(new Point2D(vertX, vertY));
            }

            return ArcNodes;
        }

        public static double AddAngle(double angle1, double angle2)
        {
            var angle = angle1 + angle2;
            if (angle < 0)
                angle += Math.PI * 2;
            else if (angle > Math.PI * 2)
                angle -= Math.PI * 2;

            return angle;
        }

        /// <summary>
        /// Validates the bearing value
        /// </summary>
        /// <param name="brg"></param>
        /// <returns></returns>
        public static double CheckBearing(double brg)
        {
            if (brg > Math.PI * 2) { brg -= Math.PI * 2; }
            else if (brg < 0) { brg += Math.PI * 2 ; }

            return brg;
        }
        /// <summary>
        /// Given 3 points calculates the radius that fits between them
        /// </summary>
        /// XY coords of input points
        /// <param name="X1"></param>
        /// <param name="Y1"></param>
        /// <param name="X2"></param>
        /// <param name="Y2"></param>
        /// <param name="X3"></param>
        /// <param name="Y3"></param>
        /// <param name="cx">output circle centre x</param>
        /// <param name="cy">output circle centre Y</param>
        /// <param name="r">output radius</param>
        /// <returns>T = all OK F = points co-linear of duplicate</returns>
        public static bool RadiusOfPoints(double X1, double Y1, double X2, double Y2, double X3, double Y3, ref double cx, ref double cy, ref double r)
        {
            double xDelta_a, yDelta_a, xDelta_b, yDelta_b;
            double aSlope, bSlope;
            if (Math.Abs(Vector.FnPerpendicular(X1, Y1, X2, Y2, X3, Y3)) <= 0.0001 || Vector.FnDistance(X1, Y1, X2, Y2) <= 0.001 || Vector.FnDistance(X1, Y1, X3, Y3) <= 0.001 || Vector.FnDistance(X3, Y3, X2, Y2) <= 0.001)
            {
                //points are co-linear or 2 of the points are duplicated - no circle
                return false;
            }
            xDelta_a = X2 - X1; yDelta_a = Y2 - Y1;
            xDelta_b = X3 - X2; yDelta_b = Y3 - Y2;
            aSlope = yDelta_a / xDelta_a; bSlope = yDelta_b / xDelta_b;
            cx = (aSlope * bSlope * (Y1 - Y3) + bSlope * (X1 + X2) - aSlope * (X2 + X3)) / (2 * (bSlope - aSlope));
            cy = -1 * (cx - (X1 + X2) / 2) / aSlope + (Y1 + Y2) / 2;
            r = Vector.FnDistance(cx, cy, X1, Y1);

            return true;
        }

        #endregion
    }
}

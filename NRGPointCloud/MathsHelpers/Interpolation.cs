using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;

namespace NRG.MathsHelpers
{
    public static class Interpolation
    {
        #region Line Interpolation

        /// <summary>
        /// Interpolation routine added from Paul's VB code.
        /// Given 2 3D vectors and a required height, finds the interpolated X & Y for said height.
        /// Really handy routine, but used mostly in parallel line, volume calcs & contouring
        /// This is heavily re-factored, but original VB code can be found in drawCon32.BAS
        /// </summary>
        /// <param name="X1"></param> |
        /// <param name="Y1"></param> |
        /// <param name="Z1"></param> |Usual start and end vector stuff
        /// <param name="X2"></param> |
        /// <param name="Y2"></param> |
        /// <param name="Z2"></param> |
        /// <param name="Height"> This is the height or Z value we're looking to achieve on the line</param>
        /// <returns>2D Vector describing the position on the line, but null if bollocks</returns>
        public static Point2D InterpolateOntoLine(double X1, double Y1, double Z1, double X2, double Y2, double Z2, double Height)
        {
            if (Z1 == Z2 | (Height < Z1 & Height < Z2) | (Height > Z1 & Height > Z2))
            {
                return null; //height is outside bounds 
            }
            else
            {
                double hzDist = Vector.FnDistance(X1, Y1, X2, Y2); //horizontal distance between 1 & 2
                Point2D p = new Point2D();
                if (hzDist == 0)
                {
                    //1 & 2 are vertically above each other
                    p.X = X1;
                    p.Y = Y1;
                }
                else
                {
                    double pointDist = ((Height - Z1) / (Z2 - Z1)) * hzDist; //hz distance from point 1 to the required vertical height on our vector
                    p.X = ((pointDist / hzDist) * (X2 - X1)) + X1; //by similar triangles computer
                    p.Y = ((pointDist / hzDist) * (Y2 - Y1)) + Y1; //point x & y
                }
                //now the original code had a thing that retuned gradient in the parameter list and I'm not sure why the caller couldn't 
                //sort this out itself, but if implementation is needed please feel free to un-comment and do something with it.
                /*if (Z2 != Z1)
                {
                    double gradient = hzDist / Math.Abs(Z2 - Z1);
                }
                else
                {
                    double gradient = 1E+20;
                }*/
                return p;

            }
        }

        /// <summary>
        /// Function name is a little misleading, as what it's actually used most for is interpolating Z (I know, right?)
        /// I've just read back the comment above and don't know why I didn't sort this years ago, what it actually does
        /// is interpolate a value in 2D, so imagine a graph...
        /// 
        ///             x2,y2
        ///               /| ^
        ///              / |  
        ///             /  | |
        ///            /   |
        ///           /    | |
        ///          /     |Return + y1 (so if x1 = 0, y1 = 10, x2 = 10, y2 = 20, x3 = 15: return = 15)
        ///         /      | |
        ///        /       |
        ///       /        | |
        ///      /   x3    | ^
        ///     /<-------->
        ///   x1,y1
        ///   
        /// So, in the upshot of this revalation I've created a new function called InterpolateZ which should be below this
        /// function. 
        /// </summary>
        /// <param name="X1">start vector X</param>
        /// <param name="Y1">start vector Y</param>
        /// <param name="X2">end vector X</param>
        /// <param name="Y2">end vector Y</param>
        /// <param name="X3">distance along vector</param>
        /// <returns>the 'y' value of the x3 distance along the vector</returns>
        public static double FnInterpolateY(double X1, double Y1, double X2, double Y2, double X3)
        {
            double output;
            double DistBetween1and2, DistBetween1and3, HeightDiffBetween1and2;

            DistBetween1and2 = X2 - X1;
            DistBetween1and3 = X3 - X1;
            HeightDiffBetween1and2 = Y2 - Y1;

            if (DistBetween1and2 == 0)
                return output = (Y1 + Y2) / 2;
            else
                output = Y1 + HeightDiffBetween1and2 * DistBetween1and3 / DistBetween1and2;

            return output;
        }

        /// <summary>
        /// Please see comments for FNInterpolateY
        /// Pass in the start and end of a 3d vector then the distance alomg it, and the return is the Z value for that distance
        /// </summary>
        /// <param name="X1">start x</param>
        /// <param name="Y1">start y</param>
        /// <param name="Z1">startz</param>
        /// <param name="X2">end x</param>
        /// <param name="Y2">end y</param>
        /// <param name="Z2">end z</param>
        /// <param name="DistanceAlongVector">distance along the vector to calc the return</param>
        /// <returns></returns>
        /// Tested: No
        public static double InterpolateZ(double X1, double Y1, double Z1, double X2, double Y2, double Z2, double DistanceAlongVector)
        {
            return FnInterpolateY(0, Z1, MathsHelpers.Vector.FnDistance(X1, Y1, X2, Y2), Z2, DistanceAlongVector);
        }

        public static Point3D InterpolateAlongLine(Point3D startLine, Point3D endLine, double dist)
        {
            double brg = MathsHelpers.Trig.RPC(startLine.X, startLine.Y, endLine.X, endLine.Y);
            Point2D pt = new Point2D();
            pt = MathsHelpers.Trig.PRC(startLine.X, startLine.Y, brg, dist, 1);
            Point3D ptout = new Point3D();
            ptout.X = pt.X;
            ptout.Y = pt.Y;
            ptout.Z = InterpolateZ(startLine.X, startLine.Y, startLine.Z, endLine.X, endLine.Y, endLine.Z, dist);
            return ptout;
        }

        #endregion

        #region Shape Interpolation

        /// <summary>
        /// Morphs two shapes by collecting a series of radial shape points and then interpolating the resulting points to find the morphed shape
        /// </summary>
        /// <param name="originX">The center X value for the radial selection</param>
        /// <param name="originY">The center Y value for the radial selection</param>
        /// <param name="angleStep">The angle interval for the radial selection in radians</param>
        /// <param name="firstShape">The first shape to morph/></param>
        /// <param name="curShape">The second shape to morph</param>
        /// <param name="startCh">The start chainage for the first shape</param>
        /// <param name="endCh">The start chainage for the current shape</param>
        /// <param name="curCh">The current chainage value (should be between start and end chainage)</param>
        /// <returns>Returns a new <see cref="Shape"/> containg the interpolated shape points as elements</returns>
        public static Shape InterpolateShape(double originX, double originY, double angleStep, Shape firstShape, Shape curShape, double startCh, double endCh, double curCh)
        {
            //There is no step size
            if (angleStep <= 0)
                return null;

            //Create the bearing list
            double brg = 0.00000047;
            var brgList = new List<double>();
            const double twoPI = Math.PI * 2;

            for (double i = 0; i < twoPI; i += angleStep)
                brgList.Add(Math.Round(i, 6));

            //Now find the brg of each tangment point in the firstShape and add it to the list
            foreach(var element in firstShape.Elements)
            {
                brg = Trig.RPC(firstShape.ShapeCentre.X, firstShape.ShapeCentre.Y, element.StartX, element.StartY);
                //if (!brgList.Contains(brg))
                    brgList.Add(brg);
            }

            //Add the brg of the last element endX & endY for firstShape
            var ele = firstShape.Elements.LastOrDefault();
            brg = Trig.RPC(firstShape.ShapeCentre.X, firstShape.ShapeCentre.Y, ele.EndX, ele.EndY);
            //if (!brgList.Contains(brg))
                brgList.Add(brg);

            //Now find the brg of each tangment point in the curShape and add it to the list
            foreach(var element in curShape.Elements)
            {
                brg = Trig.RPC(curShape.ShapeCentre.X, curShape.ShapeCentre.Y, element.StartX, element.StartY);
              //  if (!brgList.Contains(brg))
                    brgList.Add(brg);
            }

            //Add the brg of the last element endX & endY for curShape
            ele = curShape.Elements.LastOrDefault();
            brg = Trig.RPC(curShape.ShapeCentre.X, curShape.ShapeCentre.Y, ele.EndX, ele.EndY);
            //if (!brgList.Contains(brg))
                brgList.Add(brg);

            //There are no bearings
            if (brgList.Count <= 0)
                return null;

            //Sort the bearings, Asc / Desc shouldn't really matter
            brgList.Sort();

            //Create new shape
            var newShape = new Shape();
            newShape.Layers = curShape.Layers;
            newShape.Gauge = curShape.Gauge;

            newShape.LayerName = curShape.LayerName;
            newShape.Elements = new List<ShapeElement>();

            var firstShapeBrg = firstShape.ExplodeShapeByBearing(firstShape.ShapeCentre.X, firstShape.ShapeCentre.X, brgList);
            var curShapeBrg = curShape.ExplodeShapeByBearing(curShape.ShapeCentre.X, curShape.ShapeCentre.Y, brgList);

            //Interpolate the shape points
            var distBetweenCh = endCh - startCh;
            var chFactor = (curCh - startCh) / (distBetweenCh);

            var newShapePoints = new List<Point2D>(firstShapeBrg.Count);
            if (firstShapeBrg.Count == curShapeBrg.Count)
            {
                for (int i = 0; i < firstShapeBrg.Count; i++)
                {
                    var offset = curShapeBrg[i] - firstShapeBrg[i];
                    newShapePoints.Add(firstShapeBrg[i] + (offset * chFactor));
                }
            }

            //Add new shape elements
            for(int i = 0; i < newShapePoints.Count - 1; i++)
            {
                var start = newShapePoints[i];
                var end = newShapePoints[i + 1];

                newShape.Elements.Add(new ShapeElement()
                {
                    StartX = start.X,
                    StartY = start.Y,
                    EndX = end.X,
                    EndY = end.Y,
                    Radius = 0,
                    Layer = 0
                });
            }

            //Add last element
            var startPoint = newShapePoints.LastOrDefault();
            var endPoint = newShapePoints.FirstOrDefault();

            newShape.Elements.Add(new ShapeElement()
            {
                StartX = startPoint.X,
                StartY = startPoint.Y,
                EndX = endPoint.X,
                EndY = endPoint.Y,
                Radius = 0,
                Layer = 0
            });

            newShape.FileName = System.IO.Path.GetFileName(firstShape.FileName) + " - " + System.IO.Path.GetFileName(curShape.FileName);

            return newShape;
        }

        #endregion
    }
}

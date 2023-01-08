using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;

namespace NRG.MathsHelpers
{
    public static class Triangle
    {
        #region Area

        /// <summary>
        /// Plan (2d) area of a triangle - Pass in the vertices and it kindly returns the area
        /// Ported by ES:13.05.19. Original VB code found in TRIANGL2.BAS
        /// </summary>
        /// <param name="X1">The X value for the first triangle vertex</param>
        /// <param name="Y1">The Y value for the first triangle vertex</param>
        /// <param name="X2">The X value for the second triangle vertex</param>
        /// <param name="Y2">The Y value for the second triangle vertex</param>
        /// <param name="X3">The X value for the third triangle vertex</param>
        /// <param name="Y3">The Y value for the third triangle vertex</param>
        /// <returns>Returns a <see cref="double"/> containing the plan area for the triangle</returns>
        public static double CalcTriPlanArea(double X1, double Y1, double X2, double Y2, double X3, double Y3)
        {
            return -0.5 * ((X2 - X1) * (Y2 + Y1) + (X3 - X2) * (Y3 + Y2) + (X1 - X3) * (Y1 + Y3));
        }

        /// <summary>
        /// Surface (3D) area of a triangle, as above pass in vertices and returns the area  
        /// Ported by ES::13.05.19. Original VB code found in TRIANGL2.BAS
        /// Notes: can't help thinking there's a better or more efficient way of doing this.
        /// </summary>
        /// <param name="X1">The X value for the first triangle vertex</param>
        /// <param name="Y1">The Y value for the first triangle vertex</param>
        /// <param name="Z1">The Z value for the first triangle vertex</param>
        /// <param name="X2">The X value for the second triangle vertex</param>
        /// <param name="Y2">The Y value for the second triangle vertex</param>
        /// <param name="Z2">The Z value for the second triangle vertex</param>
        /// <param name="X3">The X value for the third triangle vertex</param>
        /// <param name="Y3">The Y value for the third triangle vertex</param>
        /// <param name="Z3">The Z value for the third triangle vertex</param>
        /// <returns>Retruns a <see cref="double"/> containing the surface area for the triangle</returns>
        public static double CalcTriSurfaceArea(double X1, double Y1, double Z1, double X2, double Y2, double Z2, double X3, double Y3, double Z3)
        {
            double a = Math.Sqrt((X1 - X2) * (X1 - X2) + (Y1 - Y2) * (Y1 - Y2) + (Z1 - Z2) * (Z1 - Z2));  //length of each side of the triangle
            double b = Math.Sqrt((X2 - X3) * (X2 - X3) + (Y2 - Y3) * (Y2 - Y3) + (Z2 - Z3) * (Z2 - Z3));
            double c = Math.Sqrt((X3 - X1) * (X3 - X1) + (Y3 - Y1) * (Y3 - Y1) + (Z3 - Z1) * (Z3 - Z1));
            double s = (a + b + c) / 2;
            return Math.Sqrt(Math.Abs(s * (s - a) * (s - b) * (s - c)));
        }

        public static Point2D TriangleCentre(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            Point2D ptout = new Point2D();
            ptout.X = (x1 + x2 + x3) / 3;
            ptout.Y = (y1 + y2 + y3) / 3;

            return ptout;
        }

        #endregion
    }
}

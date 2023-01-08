using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Services;
using NRG.Models;
using System.Drawing;

namespace NRG.Volumes


{
    /// <summary>
    /// 
    /// </summary>
    class VolumeVector
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; } //Represents finished ground level
        public double ZB { get; set;} //Represents original ground level
    }

    public class VolumeResult
    {
        public double Cut { get; set; }
        public double Fill { get; set; }
		public double CutSurfaceArea { get; set; }
		public double FillSurfaceArea { get; set; }
		public double CutPlanArea { get; set; }
		public double FillPlanArea { get; set; }
		public string Filename { get; set; }

	}

	public class VolumeReportData //Added by ES:17.03.21

	{
		public VolumeResult Result = new VolumeResult();
		public int NumberOfPoints { get; set; }
		public int NumberOfTriangles { get; set; }
		public Bitmap Image;
	}


	class VolumeTriangle
        /* CCW triangle
                  v3
                  /\
                 /  \
                /    \  
             v1/______\v2 
             
        */
   
    {
		public VolumeVector Vector1 = new VolumeVector(); //{ get; set; }
        public VolumeVector Vector2 = new VolumeVector(); //{ get; set; }
		public VolumeVector Vector3 = new VolumeVector(); //{ get; set; }

		public VolumeResult GetCutAndFill()
        {

            double totalVolume = 0;
            VolumeResult v = new VolumeResult();

            double surfaceArea =Math.Abs( MathsHelpers.Triangle.CalcTriSurfaceArea(Vector1.X, Vector1.Y, Vector1.Z, Vector2.X, Vector2.Y, Vector2.Z, Vector3.X, Vector3.Y, Vector3.Z));

            double planArea = Math.Abs( MathsHelpers.Triangle.CalcTriPlanArea(Vector1.X, Vector1.Y, Vector2.X, Vector2.Y, Vector3.X, Vector3.Y));

            totalVolume = Math.Abs( planArea * (((Vector1.Z-Vector1.ZB)+ (Vector2.Z - Vector2.ZB) + (Vector3.Z - Vector3.ZB)) /3));
            
            
			//return v;
			//==========================================================================================================
			//not so special cases for when a triangle is totally in cut or totally in fill...
			if (MathsHelpers.Vector.FnEquals(Vector1.Z - Vector1.ZB, 0) && MathsHelpers.Vector.FnEquals(Vector2.Z - Vector2.ZB, 0) && MathsHelpers.Vector.FnEquals(Vector3.Z - Vector3.ZB, 0))
			{
				return v; //zero
			}
			else if (Vector1.Z - Vector1.ZB >= 0 & Vector2.Z - Vector2.ZB >= 0 & Vector3.Z - Vector3.ZB >= 0) //All fill
			{
				v.Fill = totalVolume;
                v.FillPlanArea = planArea;
                v.FillSurfaceArea = surfaceArea;
				return v;
			}

			else if (Vector1.Z - Vector1.ZB <= 0 & Vector2.Z - Vector2.ZB <= 0 & Vector3.Z - Vector3.ZB <= 0) //All cut
			{
				v.Cut = totalVolume;
                v.CutPlanArea = planArea;
                v.CutSurfaceArea = surfaceArea;
				return v;
			}
            //==========================================================================================================

            //OK, I knew we wouldn't get that lucky! The triangle is in both cut and fill. I'm going to wing this for the time
            //being, without refering to the VB code - 'cos that's the way I roll.
            //We use vector intersections to work out which proportion is in cut and fill.
            //Unfortunately this is going to get a shit load more complicated...but for the time being:


            double diff1 = Vector1.Z - Vector1.ZB;
            double diff2 = Vector2.Z - Vector2.ZB;
            double diff3 = Vector3.Z - Vector3.ZB;

            Point2D p1 = new Point2D();
            Point2D p2 = new Point2D();
            Point2D p3 = new Point2D();
			double z1=0, z2=0, z3=0;
            bool isFill = false;
            double avgDepth = 0;


            //We need the odd one out...

            if ((diff1 >= 0 & diff2 >= 0)| (diff1 <= 0 & diff2 <= 0))
            {
                //vector  3  is the odd one out
                p1 = NRG.MathsHelpers.Interpolation.InterpolateOntoLine(Vector3.X, Vector3.Y, diff3, Vector1.X, Vector1.Y, diff1,0.0f);
                p3 = NRG.MathsHelpers.Interpolation.InterpolateOntoLine(Vector3.X, Vector3.Y, diff3, Vector2.X, Vector2.Y, diff2, 0.0f);
                p2.X = Vector3.X;
                p2.Y = Vector3.Y;
                isFill= diff1 <= 0 & diff2 <= 0;
                avgDepth = Math.Abs(diff3) / 3;
				z3 = diff3;
            }
            else if ((diff2 >=0 & diff3 >=0) | (diff2 <= 0 & diff3 <= 0))
            {
                //vector 1 is the odd one out
                p1 = NRG.MathsHelpers.Interpolation.InterpolateOntoLine(Vector1.X, Vector1.Y, diff1, Vector3.X, Vector3.Y, diff3, 0.0f);
                p3 = NRG.MathsHelpers.Interpolation.InterpolateOntoLine(Vector1.X, Vector1.Y, diff1, Vector2.X, Vector2.Y, diff2, 0.0f);
                p2.X = Vector1.X;
                p2.Y = Vector1.Y;
                isFill= diff2 <= 0 & diff3 <= 0;
                avgDepth = Math.Abs(diff1) / 3;
				z1 = diff1;
            }
            else if ((diff1 >=0 & diff3 >=0) | (diff1 <= 0 & diff3 <= 0))
            {
                //vector 2 is the odd one out
                p1 = NRG.MathsHelpers.Interpolation.InterpolateOntoLine(Vector2.X, Vector2.Y, diff2, Vector1.X, Vector1.Y, diff1, 0.0f);
                p3 = NRG.MathsHelpers.Interpolation.InterpolateOntoLine(Vector2.X, Vector2.Y, diff2, Vector3.X, Vector3.Y, diff3, 0.0f);
                p2.X = Vector2.X;
                p2.Y = Vector2.Y;
                isFill = diff1 <= 0 & diff3 <= 0;
                avgDepth = Math.Abs(diff2) / 3;
				z2 = diff2;
            }


			double partialPlanArea = Math.Abs(MathsHelpers.Triangle.CalcTriPlanArea(p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y));
			double partialSurfArea = Math.Abs(MathsHelpers.Triangle.CalcTriSurfaceArea(p1.X, p1.Y,z1, p2.X, p2.Y,z2, p3.X, p3.Y,z3));

			double partialVol =partialPlanArea * avgDepth;

			double ratio = Math.Abs(partialVol / totalVolume);

			if (isFill)
			{
				v.Fill = Math.Abs(partialVol);
				v.Cut = Math.Abs(totalVolume) - Math.Abs(partialVol);
				v.FillPlanArea = partialPlanArea;
				v.CutPlanArea = Math.Abs(planArea) - Math.Abs(v.FillPlanArea);
				v.FillSurfaceArea = partialSurfArea;
				v.CutSurfaceArea = Math.Abs(surfaceArea) - Math.Abs (v.FillSurfaceArea);

			}
			else
			{
				v.Cut = Math.Abs(partialVol);
				v.Fill = Math.Abs(totalVolume) - Math.Abs(partialVol);
				v.CutPlanArea = partialPlanArea;
				v.FillPlanArea = Math.Abs(planArea) - Math.Abs( v.FillPlanArea);
				v.CutSurfaceArea = partialSurfArea;
				v.FillSurfaceArea = Math.Abs(surfaceArea) - Math.Abs(v.CutPlanArea);
			}
			return v;
        }

        


    }

    
}

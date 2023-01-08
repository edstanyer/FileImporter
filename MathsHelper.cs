using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG;
using static C1.Util.Win.Win32;
using static FileImporter.DataTypes;

namespace FileImporter
{
    public static class MathsHelper
    {

        /// <summary>
        /// ROHeightAndDist - Added by ES:02.01.23
        /// </summary>
        /// <param name="Alt">altidutde of the centre of the origin (eg. centre of a total staion )</param>
        /// <param name="TargetHeight"> height from the tip of the detail pole to the centre of reflector (0 for tape and retros)</param>
        /// <param name="VCR">Vertical angle read from instrument</param>
        /// <param name="SlopeDist">3 dimensional distance from centre of insrument to target</param>
        /// <param name="Height">Output height difference to instrument collumation ot the target</param>
        /// <param name="Dist">Output horizontal distance between instrument and target</param>
        public static void ROHeightAndDist(double Alt, double TargetHeight, double VCR, double SlopeDist, out double Height, out double Dist)
        {
            Height = Alt + TargetHeight - (Math.Cos(VCR) * SlopeDist);
            Dist = Math.Abs(Math.Sin(VCR) * SlopeDist);

        }

        public static Tuple<int , TripletCoordinate> CalcResection(TripletCoordinate Stn1, PolarObservation Obs1, TripletCoordinate Stn2, PolarObservation Obs2)
        {


            ROHeightAndDist(Stn1.Z , Obs1.TargetHeight, Obs1.VerticalAngle, Obs1.SlopeDistance, out double Ht1, out double D1);
            ROHeightAndDist(Stn2.Z, Obs2.TargetHeight, Obs2.VerticalAngle, Obs2.SlopeDistance, out double Ht2, out double D2);

            int ROIs = 1; double brg = 0, dist = 0, ac = 0, ROd  = 0, ROsd = 0, ROvcr = Math.PI/2, ROe = 0, ROn = 0, fz = 0;

            if (D1 < D2 || D2 == 0)
            {
                ROIs = 1;
                NRG.MathsHelpers.Trig.RPC(Stn1.X, Stn1.Y, Stn2.X, Stn2.Y, ref brg, ref dist);
                ac = Obs1.HorizontalAngle - Obs2.HorizontalAngle;
                ROd = D1;
                ROsd = Obs1.SlopeDistance;
                ROvcr = Obs1.VerticalAngle;
                fz = Stn1.Z;
            }
            else
            {
                ROIs = 2;
                NRG.MathsHelpers.Trig.RPC(Stn2.X, Stn2.Y, Stn1.X, Stn1.Y, ref brg, ref dist);
                ac = Obs2.HorizontalAngle - Obs1.HorizontalAngle;
                ROd = D2;
                ROsd = Obs2.SlopeDistance;  
                ROvcr = Obs2.VerticalAngle;
                fz = Stn2.Z;    
            }

            double d = NRG.MathsHelpers.Vector.FnDistance(Stn1.X, Stn1.Y, Stn2.X, Stn2.Y);

            double ia = (Math.Sin(ac) / d) * ROd;
            ia = Math.Asin(ia);
            ia = Math.PI - ia - ac;


            double deltaY = (Math.Cos(ia + brg)) * ROd;
            double deltaX = (Math.Sin(ia + brg)) * ROd;

            double fx = deltaX + ROe;
            double fy = deltaY + ROn;

            double b = 0;
            double RoHeight = 0,  Hdist = 0;
            if (ROIs== 1)
            {
                NRG.MathsHelpers.Trig.RPC(fx, fy, Stn2.X, Stn2.Y, ref b, ref d);
                ROHeightAndDist(fz, Obs2.TargetHeight, Obs2.VerticalAngle, Obs2.SlopeDistance, out  RoHeight, out  Hdist);
            }
            else
            {
                NRG.MathsHelpers.Trig.RPC(fx, fy, Stn1.X, Stn1.Y, ref b, ref d);
                ;
                ROHeightAndDist(fz, Obs1.TargetHeight, Obs1.VerticalAngle, Obs1.SlopeDistance, out RoHeight, out Hdist);

            }

            return Tuple.Create(ROIs, new TripletCoordinate(deltaX + ROe, ROn + deltaY, fz)); //check this - not happy with the calculated level.
        }

    }
}

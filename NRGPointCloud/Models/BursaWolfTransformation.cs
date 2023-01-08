using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Services;
using NRG.MathsHelpers;
using NRG.Models;


namespace NRG.Models


{

    public class CommonPoint
    {

        public double OriginX { get; set; }
        public double OriginY { get; set; }
        public double OriginZ { get; set; }

        public double ArbitraryX { get; set; }
        public double ArbitraryY { get; set; }
        public double ArbitraryZ { get; set; }

        public double ResidualX { get;  set; }
        public double ResidualY { get;  set; }
        public double ResidualZ { get;  set; }
    }

    //OK folks, I give you a fully fledged C# implementation of the Bursa Wolf 7 step 3 dimensional conformal transformation algorithm
    //No need to thank me, 'cos I know that's not going to happen anyway

    //This should facilitate transforming point clouds to ground control points (hmm why would you want to do that?)
    //well it'll probably mean about 4 grand a licence if we can get it working

    //Pass in control points with orgin and destination coords using AddControlPoint then call InitialiseTransformation which does all the heavy lifting.




    public class BursaWolfTransformation
    {

        #region Unittest
        public void CreateUnitTest()
        {

            double x = 0;
            double y = 0;
            double z = 0;

            BursaWolfTransformation bwf = new BursaWolfTransformation();

            bwf.AddControlPoint(10.0f, 10.0f, 0.0f, 10.5f, 10.0f, 10);
            bwf.AddControlPoint(15.0f, 15.0f, 0.0f, 15.5f, 10.0f, 10);
            bwf.AddControlPoint(20.0f, 10.0f, 0.0f, 20.5f, 10.0f, 10);
            bwf.InitialiseTransformation();
            bwf.TransformCoordinate(10.0f, 10.0f, 0.0f, ref x, ref y, ref z);
        }

        #endregion


        #region module level variables
        private bool Initialised = false;
        private List<CommonPoint> ControlPoints = new List<CommonPoint>();
        private double[] param = new double[8];
        private double s0 = double.PositiveInfinity;
        private double[] norm = new double[29]; //29? cool story bro
        private RotationMatrixStruct RotMat;

        public double ResultRoll = 0;
        public double ResultPitch = 0;
        public double ResultYaw = 0;


        //public double ResultOmega = 0;
        //public double ResultPhi = 0;
        //public double ResultKappa = 0;
        //public double ResultTx = 0;
        //public double ResultTy = 0;
        //public double ResultTz = 0;








        #endregion

        private struct RotationMatrixStruct
        {
            public double m11 { get; set; }
            public double m12 { get; set; }
            public double m13 { get; set; }
            public double m21 { get; set; }
            public double m22 { get; set; }
            public double m23 { get; set; }
            public double m31 { get; set; }
            public double m32 { get; set; }
            public double m33 { get; set; }
            public double so { get; set; }
            public double sp { get; set; }
            public double sk { get; set; }
            public double co { get; set; }
            public double cp { get; set; }
            public double ck { get; set; }
            public double st { get; set; }
            public double ss { get; set; }
            public double sa { get; set; }
            public double ct { get; set; }
            public double cs { get; set; }
            public double ca { get; set; }
        }


        //Results for display - I think I've got this right!
        public double ResultScale(){return param[1];}
        public double ResultOmega() { return param[2]; }
        public double ResultPhi() { return param[3]; }
        public double ResultKappa() { return param[4]; }
        public double ResultTranslationX() { return param[5]; }
        public double ResultTranslationY() { return param[6]; }
        public double ResultTranslationZ() { return param[7]; }


        #region helpers
        private int INDUT(int i, int j)
        { return ((((j) * ((j) - 1)) / 2) + i); }


        private double SQRIT(double x)
        { return x * x; }

        private double HyPot(double A, double B)
        { return Math.Sqrt(A * A + B * B); }

        #endregion

        #region matrix stuff
        /// <summary>
        /// RotationMatrixTSA - Creates a rotation matrix for quaternion
        /// </summary>
        /// <param name="Tilt"></Yaw>
        /// <param name="Swing"></pitch>
        /// <param name="Azimuth"></roll
        /// <returns> a RotationMatrixStruct</returns>
        //for the record; tilt, swing and azimuth equate to roll, pitch and yaw in modern jargon. I've left the terminology 
        //as-is because people in the past were cleverer than me and, also, generally British or Austrian, so obviously superior to our U.S. cousins

        RotationMatrixStruct RotationMatrixTSA(double Tilt, double Swing, double Azimuth)
        {
            RotationMatrixStruct rm = new RotationMatrixStruct();
            #region Trig Stuff
            rm.st = Math.Sin(Tilt);
            rm.ct = Math.Cos(Tilt);
            rm.ss = Math.Sin(Swing);
            rm.cs = Math.Cos(Swing);
            rm.sa = Math.Sin(Azimuth);
            rm.ca = Math.Cos(Azimuth);
            #endregion
            #region Matrix Computation Elements
            rm.m11 = -rm.ca * rm.cs - rm.sa * rm.ct * rm.ss;
            rm.m12 = rm.sa * rm.cs - rm.ca * rm.ct * rm.ss;
            rm.m13 = -rm.st * rm.ss;
            rm.m21 = rm.ca * rm.ss - rm.sa * rm.ct * rm.cs;
            rm.m22 = -rm.sa * rm.ss - rm.ca * rm.ct * rm.cs;
            rm.m23 = -rm.st * rm.cs;
            rm.m31 = -rm.sa * rm.st;
            rm.m32 = -rm.ca * rm.st;
            rm.m33 = rm.ct;
            #endregion

            ResultPitch = -Tilt;
            ResultRoll = Swing;
            ResultYaw = Azimuth;

            return rm;
        }

        RotationMatrixStruct RotationMatrixOPK(double omega, double phi, double kappa)
        {
            RotationMatrixStruct rm = new RotationMatrixStruct();
            #region Trig Stuff
            rm.so = Math.Sin(omega);
            rm.co = Math.Cos(omega);
            rm.sp = Math.Sin(phi);
            rm.cp = Math.Cos(phi);
            rm.sk = Math.Sin(kappa);
            rm.ck = Math.Cos(kappa);
            #endregion
            #region Matrix Computation Elements
            rm.m11 = rm.cp * rm.ck;
            rm.m12 = rm.so * rm.sp * rm.ck + rm.co * rm.sk;
            rm.m13 = -rm.co * rm.sp * rm.ck + rm.so * rm.sk;
            rm.m21 = -rm.cp * rm.sk;
            rm.m22 = -rm.so * rm.sp * rm.sk + rm.co * rm.ck;
            rm.m23 = rm.co * rm.sp * rm.sk + rm.so * rm.ck;
            rm.m31 = rm.sp;
            rm.m32 = -rm.so * rm.cp;
            rm.m33 = rm.co * rm.cp;
            #endregion
            return rm;

        }


        #endregion

        public void AddControlPoint(double OriginX, double OriginY, double OriginZ, double DestinationX, double DestinationY, double DestinationZ)
        {
            CommonPoint pt = new CommonPoint();
            pt.OriginX = OriginX;
            pt.OriginY = OriginY;
            pt.OriginZ = OriginZ;
            pt.ArbitraryX = DestinationX;
            pt.ArbitraryY = DestinationY;
            pt.ArbitraryZ = DestinationZ;
            ControlPoints.Add(pt);
        }

        public DTMPoint GetControlResidual(int idx = -1)
        {
            if (idx >= 0 && idx < ControlPoints.Count)
            {
                CommonPoint pt = ControlPoints.ElementAt(idx);
                DTMPoint ptout = new DTMPoint();
                ptout.X = pt.ResidualX;
                ptout.Y = pt.ResidualY;
                ptout.Z = pt.ResidualZ;
                return ptout;
            }
            return null;
        }

        public bool InitialiseTransformation()
        {
            if (ControlPoints == null)
            {
                return false;
            }
            if (ControlPoints.Count < 3)
            {
                return false;
            }


            //RotationMatrixStruct RotMat = new RotationMatrixStruct();
            RotMat = new RotationMatrixStruct();//ES:27.09.20
            InitalApproximation();
            int iter = 0;
            double s0old = 0;
            double[] rhs = new double[8];
            int converge = 0;
            int diverge = 0;
            do
            {
                iter++;
                s0old = s0;
                RotMat= RotationMatrixOPK(param[2], param[3], param[4]);
                s0 = FormNormals(ref norm,ref rhs);
                if (s0 < 0.0001)
                {
                    break;
                }
                if (Math.Abs(s0old - s0) < 0.0001)
                {
                    converge = 1;
                }
                else if (s0 > s0old)
                {
                    diverge = 1;
                }
                Solve(ref norm, ref rhs, 7, converge|diverge); //flag is supposed to be or
                AddCorrections(rhs);
            } while ((converge==0 & diverge == 0) | (converge== 1 & diverge==1)); //condition is xor

            return true;
           


        }

        public void TransformCoordinate(double x, double y, double z,ref  double xout, ref double yout,ref double zout)
        {
            //the original VB code would have called RotationMatrixOPK here but this should have already been done and stored in a module level variable RotmMat

            //calc partial derivitive for the vector
            double[,] AI = new double[4, 8];
            FormAI(ref AI,  param[1],x,y,z);
            xout = param[1] * AI[1, 1] + param[5];
            yout = param[1] * AI[2, 1] + param[6];
            zout = param[1] * AI[3, 1] + param[7];
        }

        private void Solve(ref double[] A, ref double[] B,   int n, int invflag)

        {
            double[] s = new double[n];

            //forward elimination 
            for (int piv = 1; piv <= n; piv++)
            {
                for (int i = piv + 1; i <= n; i++)
                {
                    for (int j = i; j <= n; j++)
                    {
                        A[INDUT(i, j)] = A[INDUT(i, j)] - (A[INDUT(piv, i)] * A[INDUT(piv, j)] / A[INDUT(piv, piv)]);
                    }//j
                    B[i] = B[i] - (A[INDUT(piv, i)] * B[piv] / A[INDUT(piv, piv)]);
                }//i
            }//piv
            //Back substitution and inverse (if requested)
            for (int piv = n; piv > 0; piv--)
            {
                for (int j = piv + 1; j < n+1; j++)
                {
                    B[piv] = B[piv] - (A[INDUT(piv, j)] * B[j]);
                }//j
                B[piv] = B[piv] / (A[INDUT(piv, piv)]);
                if (invflag != 0)
                {
                    for (int j = piv + 1; j < n; j++)
                    {
                        s[j] = 0.0f;
                        for (int i = piv + 1; i <= j; i++)
                        {
                            s[j] = s[j] - (A[INDUT(piv, i)] * A[INDUT(i, j)]);
                        }//i

                        for (int i = j + 1; i <= n; i++)
                        {
                            s[j] = s[j] - (A[INDUT(piv, i)] * A[INDUT(j, i)]);
                        }//i
                        s[j] = s[j] / (A[INDUT(piv, piv)]);
                    }//j
                    double r = 1.0f;
                    for (int j = piv + 1; j < n; j++)
                    {
                        r= r - (A[INDUT(piv, j)] * s[j]);
                        A[INDUT(piv, j)] = s[j];
                    }//j
                    A[INDUT(piv, piv)] = r / A[INDUT(piv, piv)];
                }//endif
            }//piv
        }//end method

        private double FormNormals(ref double[] norm,ref double[] rhs)
        {

            double[,] AI = new double [4,8];
            double[] li = new double[4];
            double sumres2 = 0;

            for (int i = 1; i < 8; i++)
            {
                rhs[i] = 0;
                for (int j = i; j < 8; j++)
                {
                    norm[INDUT(i, j)] = 0;
                }
            }

            for (int pt = 0; pt < ControlPoints.Count; pt++)
            {

                //Really need to sort this
                //Form partial derivative terms for Taylor's series approximation 
                FormAI(ref AI, param[1], ControlPoints.ElementAt(pt).ArbitraryX, ControlPoints.ElementAt(pt).ArbitraryY, ControlPoints.ElementAt(pt).ArbitraryZ);
                li[1] = ControlPoints.ElementAt(pt).OriginX - param[1] * AI[1, 1] - param[5];
                ControlPoints.ElementAt(pt).ResidualX = -li[1];
                li[2] = ControlPoints.ElementAt(pt).OriginY - param[1] * AI[2, 1] - param[6];
                ControlPoints.ElementAt(pt).ResidualY = -li[2];
                li[3] = ControlPoints.ElementAt(pt).OriginZ - param[1] * AI[3, 1] - param[7];
                ControlPoints.ElementAt(pt).ResidualZ = -li[3];

                //Accumulate sum of squares of residuals
                sumres2 = sumres2 + (li[1] * li[1] + li[2] * li[2] + li[3] * li[3]);

                //Add contributions from this point to normal equations
                for (int i = 1; i < 8; i++)
                {
                    for (int k = 1; k < 4; k++)
                    {
                        rhs[i] = rhs[i] + (AI[k, i] * li[k]);
                    }//k

                    for (int j = i; j < 8; j++)
                    {
                        for (int k = 1; k < 4; k++)
                        {
                            norm[INDUT(i, j)] = norm[INDUT(i, j)] + (AI[k,i] * AI[k,j]);
                        }//k
                    }//j
                }//i
            }//pt

            return Math.Sqrt(sumres2 / (3 * ControlPoints.Count - 7));
        }

        private void FormAI(ref double[,] AI, double scle,double x, double y, double z)
        {
            // Calculate partial derivative terms for the point

            AI[1, 1] = RotMat.m11 * x + RotMat.m21 * y + RotMat.m31 * z;
            AI[2, 1] = RotMat.m12 * x + RotMat.m22 * y + RotMat.m32 * z;
            AI[3, 1] = RotMat.m13 * x + RotMat.m23 * y + RotMat.m33 * z;
            AI[1, 2] = 0.0f;
            AI[2, 2] = -scle * AI[3, 1];
            AI[3, 2] = scle * AI[2, 1];
            AI[1, 3] = scle * (-RotMat.sp * RotMat.ck * x + RotMat.sp * RotMat.sk * y + RotMat.cp * z);
            AI[2, 3] = scle * (RotMat.so * RotMat.cp * RotMat.ck * x - RotMat.so * RotMat.cp * RotMat.sk * y + RotMat.so * RotMat.sp * z);
            AI[3, 3] = scle * (-RotMat.co * RotMat.cp * RotMat.ck * x + RotMat.co * RotMat.cp * RotMat.sk * y - RotMat.co * RotMat.sp * z);
            AI[1, 4] = scle * (RotMat.m21 * x - RotMat.m11 * y);
            AI[2, 4] = scle * (RotMat.m22 * x - RotMat.m12 * y);
            AI[3, 4] = scle * (RotMat.m23 * x - RotMat.m13 * y);
            AI[1, 5] =1.0f;
            AI[2, 5] = 0.0f;
            AI[3, 5] = 0.0f;
            AI[1, 6] = 0.0f;
            AI[2, 6] = 1.0f;
            AI[3, 6] = 0.0f;
            AI[1, 7] = 0.0f;
            AI[2, 7] = 0.0f;
            AI[3, 7] = 1.0f;

        }

        private void AddCorrections(double[] rhs)
        {
            for (int i = 1; i < 8; i++)
            {
                param[i] = param[i] + rhs[i];
            }
        }

        /// <summary>
        /// InitialApproximation taken from https://www.asprs.org/wp-content/uploads/pers/1996journal/jan/1996_jan_79-83.pdf
        /// Can't beat a bit of light reading
        /// </summary>
        private void InitalApproximation()
        {
            #region declare variables

            double dsq12 = 0;
            double dsq13 = 0;
            double dsq23 = 0;
            double c2 = 0;
            double a2 = 0;
            double b2 = 0;
            double h2 = 0;
            double maxaltsq = 0;
            int pt1 = 0;
            int pt2 = 0;
            int pt3 = 0;
            double distcon = 0;
            double distarb = 0;
            double sumscale = 0;
            int numscale = 0;
            #endregion

            for (int i = 0; i <= ControlPoints.Count - 2; i++)
            {
                for (int j = i + 1; j < ControlPoints.Count; j++)
                {
                    distcon = Math.Sqrt(SQRIT(ControlPoints.ElementAt(i).OriginX - ControlPoints.ElementAt(j).OriginX) + SQRIT(ControlPoints.ElementAt(i).OriginY - ControlPoints.ElementAt(j).OriginY) + SQRIT(ControlPoints.ElementAt(i).OriginZ - ControlPoints.ElementAt(j).OriginZ));
                    distarb = Math.Sqrt(SQRIT(ControlPoints.ElementAt(i).ArbitraryX - ControlPoints.ElementAt(j).ArbitraryX) + SQRIT(ControlPoints.ElementAt(i).ArbitraryY - ControlPoints.ElementAt(j).ArbitraryY) + SQRIT(ControlPoints.ElementAt(i).ArbitraryZ - ControlPoints.ElementAt(j).ArbitraryZ));
                    sumscale = sumscale + distcon / distarb;
                    numscale++;
                }
            }

			if (sumscale == double.PositiveInfinity) { sumscale = 1; } //added by ES:12.10.20 - stop div by zero
            param[1] = sumscale / numscale;

            for (int ind1 = 0; ind1 < ControlPoints.Count - 2; ind1++)
            {
                for (int ind2 = ind1+1; ind2 < ControlPoints.Count-1;ind2++)
                {
                    dsq12 = SQRIT(ControlPoints.ElementAt(ind1).OriginX-ControlPoints.ElementAt(ind2).OriginX) + SQRIT(ControlPoints.ElementAt(ind1).OriginY-ControlPoints.ElementAt(ind2).OriginY) + SQRIT(ControlPoints.ElementAt(ind1).OriginZ - ControlPoints.ElementAt(ind2).OriginZ);
                    for (int ind3 = ind2+1; ind3 < ControlPoints.Count; ind3++)
                    {
                        dsq13 = SQRIT(ControlPoints.ElementAt(ind1).OriginX - ControlPoints.ElementAt(ind3).OriginX) + SQRIT(ControlPoints.ElementAt(ind1).OriginY- ControlPoints.ElementAt(ind3).OriginY) + SQRIT(ControlPoints.ElementAt(ind1).OriginZ- ControlPoints.ElementAt(ind3).OriginZ);
                        dsq23 = SQRIT(ControlPoints.ElementAt(ind2).OriginX - ControlPoints.ElementAt(ind3).OriginX) + SQRIT(ControlPoints.ElementAt(ind2).OriginY - ControlPoints.ElementAt(ind3).OriginY) + SQRIT(ControlPoints.ElementAt(ind2).OriginZ - ControlPoints.ElementAt(ind3).OriginZ);

                        if ((dsq12 >= dsq13) & (dsq12 >= dsq23))
                        {
                            c2 = dsq12;
                            a2 = dsq13;
                            b2 = dsq23;
                        }//endif
                        else
                        {
                            if (((dsq13 >= dsq12) && (dsq13 >= dsq23)))
                            {
                                c2 = dsq13;
                                a2 = dsq12;
                                b2 = dsq23;
                            }
                            else
                            {
                                c2 = dsq23;
                                a2 = dsq12;
                                b2 = dsq13;

                            }	
                        }//endelse
                        h2 = (2 * (c2 * (a2 + b2) + a2 * b2) - a2 * a2 - b2 * b2 - c2 * c2) / (4 * c2);
                        if (h2 < 0)
                        {
                            return;       
                        }//endif
                        if (h2 > maxaltsq)
                        {
                            pt1 = ind1;
                            pt2 = ind2;
                            pt3 = ind3;
                            maxaltsq = h2;
                        }//endif
                    }//ind3
                }//ind2
            }//ind1

            //Compute coefficients of the equation of a plane through the 3 points...this should be good! LOLZ
            double aarb = ControlPoints.ElementAt(pt1).ArbitraryY * ControlPoints.ElementAt(pt2).ArbitraryZ + ControlPoints.ElementAt(pt2).ArbitraryY * ControlPoints.ElementAt(pt3).ArbitraryZ + ControlPoints.ElementAt(pt3).ArbitraryY * ControlPoints.ElementAt(pt1).ArbitraryZ - ControlPoints.ElementAt(pt1).ArbitraryY * ControlPoints.ElementAt(pt3).ArbitraryZ - ControlPoints.ElementAt(pt2).ArbitraryY* ControlPoints.ElementAt(pt1).ArbitraryZ- ControlPoints.ElementAt(pt3).ArbitraryY* ControlPoints.ElementAt(pt2).ArbitraryZ;
            double barb = -ControlPoints.ElementAt(pt1).ArbitraryX * ControlPoints.ElementAt(pt2).ArbitraryZ - ControlPoints.ElementAt(pt2).ArbitraryX * ControlPoints.ElementAt(pt3).ArbitraryZ - ControlPoints.ElementAt(pt3).ArbitraryX * ControlPoints.ElementAt(pt1).ArbitraryZ + ControlPoints.ElementAt(pt1).ArbitraryX * ControlPoints.ElementAt(pt3).ArbitraryZ + ControlPoints.ElementAt(pt2).ArbitraryX * ControlPoints.ElementAt(pt1).ArbitraryZ + ControlPoints.ElementAt(pt3).ArbitraryX * ControlPoints.ElementAt(pt2).ArbitraryZ;
            double carb = ControlPoints.ElementAt(pt1).ArbitraryX * ControlPoints.ElementAt(pt2).ArbitraryY + ControlPoints.ElementAt(pt2).ArbitraryX * ControlPoints.ElementAt(pt3).ArbitraryY + ControlPoints.ElementAt(pt3).ArbitraryX * ControlPoints.ElementAt(pt1).ArbitraryY - ControlPoints.ElementAt(pt1).ArbitraryX * ControlPoints.ElementAt(pt3).ArbitraryY - ControlPoints.ElementAt(pt2).ArbitraryX * ControlPoints.ElementAt(pt1).ArbitraryY - ControlPoints.ElementAt(pt3).ArbitraryX * ControlPoints.ElementAt(pt2).ArbitraryY;
            double acon = ControlPoints.ElementAt(pt1).OriginY * ControlPoints.ElementAt(pt2).OriginZ + ControlPoints.ElementAt(pt2).OriginY * ControlPoints.ElementAt(pt3).OriginZ + ControlPoints.ElementAt(pt3).OriginY * ControlPoints.ElementAt(pt1).OriginZ - ControlPoints.ElementAt(pt1).OriginY * ControlPoints.ElementAt(pt3).OriginZ - ControlPoints.ElementAt(pt2).OriginY * ControlPoints.ElementAt(pt1).OriginZ - ControlPoints.ElementAt(pt3).OriginY * ControlPoints.ElementAt(pt2).OriginZ;
            double bcon = -ControlPoints.ElementAt(pt1).OriginX * ControlPoints.ElementAt(pt2).OriginZ - ControlPoints.ElementAt(pt2).OriginX * ControlPoints.ElementAt(pt3).OriginZ - ControlPoints.ElementAt(pt3).OriginX * ControlPoints.ElementAt(pt1).OriginZ + ControlPoints.ElementAt(pt1).OriginX * ControlPoints.ElementAt(pt3).OriginZ + ControlPoints.ElementAt(pt2).OriginX * ControlPoints.ElementAt(pt1).OriginZ + ControlPoints.ElementAt(pt3).OriginX * ControlPoints.ElementAt(pt2).OriginZ;
            double ccon = ControlPoints.ElementAt(pt1).OriginX * ControlPoints.ElementAt(pt2).OriginY + ControlPoints.ElementAt(pt2).OriginX * ControlPoints.ElementAt(pt3).OriginY + ControlPoints.ElementAt(pt3).OriginX * ControlPoints.ElementAt(pt1).OriginY - ControlPoints.ElementAt(pt1).OriginX * ControlPoints.ElementAt(pt3).OriginY - ControlPoints.ElementAt(pt2).OriginX * ControlPoints.ElementAt(pt1).OriginY - ControlPoints.ElementAt(pt3).OriginX * ControlPoints.ElementAt(pt2).OriginY;

            //**everything good to here**

            //Wow - I should have stayed at school longer.

            //Compute tilt & azimuth of plane in arbitrary system
            double arbtilt = Math.Atan2(carb, HyPot(aarb, barb)) + Math.PI / 2;
            double arbaz = Math.Atan2(aarb, barb);

            //Compute tilt & azimuth of plane in control system
            double contilt = Math.Atan2(ccon, HyPot(acon, bcon)) + Math.PI/ 2;
            double con_az = Math.Atan2(acon, bcon);

            //Compute the corresponding rotation matrices with swing = 0 - again that was fucking emotional
            RotationMatrixStruct ArbRotMat = RotationMatrixTSA(arbtilt, 0, arbaz);
            RotationMatrixStruct ConRotMat = RotationMatrixTSA(contilt, 0, con_az);

            //Rotate arbitrary and control coordinates for points 1 and 2
            //to get a line in the arbitrary system and the corresponding
            //line in the control system.  Don't need to rotate Z because
            //with these rotations all of the z values must be the same.
            double xarb1 = ArbRotMat.m11 * ControlPoints.ElementAt(pt1).ArbitraryX + ArbRotMat.m12 * ControlPoints.ElementAt(pt1).ArbitraryY + ArbRotMat.m13 * ControlPoints.ElementAt(pt1).ArbitraryZ;
            double yarb1 = ArbRotMat.m21 * ControlPoints.ElementAt(pt1).ArbitraryX + ArbRotMat.m22 * ControlPoints.ElementAt(pt1).ArbitraryY + ArbRotMat.m23 * ControlPoints.ElementAt(pt1).ArbitraryZ;
            double xarb2 = ArbRotMat.m11 * ControlPoints.ElementAt(pt2).ArbitraryX + ArbRotMat.m12 * ControlPoints.ElementAt(pt2).ArbitraryY + ArbRotMat.m13 * ControlPoints.ElementAt(pt2).ArbitraryZ;
            double yarb2 = ArbRotMat.m21 * ControlPoints.ElementAt(pt2).ArbitraryX + ArbRotMat.m22 * ControlPoints.ElementAt(pt2).ArbitraryY + ArbRotMat.m23 * ControlPoints.ElementAt(pt2).ArbitraryZ;
            double xcon1 = ConRotMat.m11 * ControlPoints.ElementAt(pt1).OriginX + ConRotMat.m12 * ControlPoints.ElementAt(pt1).OriginY + ConRotMat.m13 * ControlPoints.ElementAt(pt1).OriginZ;
            double ycon1 = ConRotMat.m21 * ControlPoints.ElementAt(pt1).OriginX + ConRotMat.m22 * ControlPoints.ElementAt(pt1).OriginY + ConRotMat.m23 * ControlPoints.ElementAt(pt1).OriginZ;
            double xcon2 = ConRotMat.m11 * ControlPoints.ElementAt(pt2).OriginX + ConRotMat.m12 * ControlPoints.ElementAt(pt2).OriginY + ConRotMat.m13 * ControlPoints.ElementAt(pt2).OriginZ;
            double ycon2 = ConRotMat.m21 * ControlPoints.ElementAt(pt2).OriginX + ConRotMat.m22 * ControlPoints.ElementAt(pt2).OriginY + ConRotMat.m23 * ControlPoints.ElementAt(pt2).OriginZ;

            //Get swing by subtracting azimuths
            double azimuthcon = Math.Atan2(xcon2 - xcon1, ycon2 - ycon1);
            double azimutharb = Math.Atan2(xarb2 - xarb1, yarb2 - yarb1);
            double swing = azimuthcon - azimutharb;
            ArbRotMat= RotationMatrixTSA (arbtilt, swing, arbaz);

            //Now compute (ConRotMat:transpose * ArbRotMat):transpose
            //This is overall rotation matrix
            RotationMatrixStruct FullRotMat = new RotationMatrixStruct();

            FullRotMat.m11 = ConRotMat.m11 * ArbRotMat.m11 + ConRotMat.m21 * ArbRotMat.m21 + ConRotMat.m31 * ArbRotMat.m31;
            FullRotMat.m21 = ConRotMat.m11 * ArbRotMat.m12 + ConRotMat.m21 * ArbRotMat.m22 + ConRotMat.m31 * ArbRotMat.m32;
            FullRotMat.m31 = ConRotMat.m11 * ArbRotMat.m13 + ConRotMat.m21 * ArbRotMat.m23 + ConRotMat.m31 * ArbRotMat.m33;
            FullRotMat.m12 = ConRotMat.m12 * ArbRotMat.m11 + ConRotMat.m22 * ArbRotMat.m21 + ConRotMat.m32 * ArbRotMat.m31;
            FullRotMat.m22 = ConRotMat.m12 * ArbRotMat.m12 + ConRotMat.m22 * ArbRotMat.m22 + ConRotMat.m32 * ArbRotMat.m32;
            FullRotMat.m32 = ConRotMat.m12 * ArbRotMat.m13 + ConRotMat.m22 * ArbRotMat.m23 + ConRotMat.m32 * ArbRotMat.m33;
            FullRotMat.m13 = ConRotMat.m13 * ArbRotMat.m11 + ConRotMat.m23 * ArbRotMat.m21 + ConRotMat.m33 * ArbRotMat.m31;
            FullRotMat.m23 = ConRotMat.m13 * ArbRotMat.m12 + ConRotMat.m23 * ArbRotMat.m22 + ConRotMat.m33 * ArbRotMat.m32;
            FullRotMat.m33 = ConRotMat.m13 * ArbRotMat.m13 + ConRotMat.m23 * ArbRotMat.m23 + ConRotMat.m33 * ArbRotMat.m33;

            //Compute omega, phi, kappa from rotation matrix
            param[3] = Math.Asin(FullRotMat.m31); //phi

            if (Math.Abs(FullRotMat.m31) < 1)
            {
                param[2] = Math.Atan2(-FullRotMat.m32, FullRotMat.m33); //omega
                param[4] = Math.Atan2(-FullRotMat.m21, FullRotMat.m11); //kappa
            }
            else
            {
                //omega and kappa are undefined, so erm, y'know...define them
                param[2] = 0.0;  //omega 
                param[4] = Math.Atan2(FullRotMat.m12, FullRotMat.m22);  //kappa
            }

            //Compute average Tx, Ty, and Tz using all common points
            double txsum = 0;
            double tysum = 0;
            double tzsum = 0;
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                txsum = txsum + (ControlPoints.ElementAt(i).OriginX - param[1] * (FullRotMat.m11 * ControlPoints.ElementAt(i).ArbitraryX + FullRotMat.m21 * ControlPoints.ElementAt(i).ArbitraryY + FullRotMat.m31 * ControlPoints.ElementAt(i).ArbitraryZ));
                tysum = tysum + (ControlPoints.ElementAt(i).OriginY - param[1] * (FullRotMat.m12 * ControlPoints.ElementAt(i).ArbitraryX + FullRotMat.m22 * ControlPoints.ElementAt(i).ArbitraryY + FullRotMat.m32 * ControlPoints.ElementAt(i).ArbitraryZ));
                tzsum = tzsum + (ControlPoints.ElementAt(i).OriginZ - param[1] * (FullRotMat.m13 * ControlPoints.ElementAt(i).ArbitraryX + FullRotMat.m23 * ControlPoints.ElementAt(i).ArbitraryY + FullRotMat.m33 * ControlPoints.ElementAt(i).ArbitraryZ));
            }

            param[5] = txsum / ControlPoints.Count;
            param[6] = tysum / ControlPoints.Count;
            param[7] = tzsum / ControlPoints.Count;

        }//end method


    }

}





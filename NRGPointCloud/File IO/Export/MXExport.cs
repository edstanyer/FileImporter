using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using NRG.Models;
using NRG.Import;

namespace NRG.File_IO.Export


{

    /*
     *      New email passwords:

            Nigel – G0kd3$J5md&l40VD

            NRG - @x38NgL$!ms6k0B4

            NB: everything that looks like and 0 is zero.

    
     */


    public static class MXExport
	{//TODO need to give option to attach a code to uncoded points (or if the point has a label but the string isn't a feature in the features manager)

		public static bool WriteMXFile(DTM Model, string Filename, List<Alignment> Alignments = null)
		{

            
            if (string.IsNullOrWhiteSpace(Filename) || (Model == null || Model.Points.Count <= 0) && Alignments==null) { return false; }

            List<Alignment> ExportAlignments = new List<Alignment>();
            if (Alignments != null && Alignments.Count > 0)
            {
                ExportAlignmentSelector dlg = new ExportAlignmentSelector(Alignments);
                DialogResult res = dlg.ShowDialog();
                if (res == DialogResult.Cancel)
                {
                    return false;
                }
                else
                {
                        
                    ExportAlignments = dlg.GetSelectedAlignments();
                       
                }
                dlg.Dispose();
            }

            using (StreamWriter writer = new StreamWriter(Filename))
            {

                writer.WriteLine("GENIO " + Path.GetFileNameWithoutExtension(Filename));

                //writer.WriteLine("001FORMAT(3F12.3)");
                //writer.WriteLine("003,ORDR,4=1,1,2,3");

                if (Model != null)
                {
                    var keys = Model.LineFeatures.Keys.ToList();

                    foreach (var k in keys) //loop through each uniques feature code
                    {
                        LineFeatureCollection lns;

                        Model.LineFeatures.TryGetValue(k, out lns);
                        foreach (var ln in lns.FeatureItems)
                        {
                            foreach (var l in ln.Value)
                            {
                                if (l.Code != "")
                                {
                                    writer.WriteLine("001FORMAT(3F12.3)");
                                    writer.WriteLine("003,ORDR,4=1,1,2,3");

                                    string[] arr = l.Code.Split('/');
                                    if (arr != null && arr.Count() > 0)
                                    {
                                        string lab = arr[0];
                                        if (lab.Length > 4) { lab.Substring(0, 3); }
                                        if (lab.Length < 4) { lab = lab.PadRight(4, ' '); }
                                        writer.WriteLine("080," + lab + ",7=3");
                                        foreach (DTMPoint pt in l.Points)
                                        {
                                            string e = pt.X.ToString("0.000").PadLeft(12, ' ');
                                            string n = pt.Y.ToString("0.000").PadLeft(12, ' ');
                                            string a = pt.Z.ToString("0.000").PadLeft(12, ' ');

                                            writer.WriteLine(e + n + a);
                                            if (pt == l.Points.Last())
                                            {
                                                double enl = 0;
                                                string dum = enl.ToString("0.000").PadLeft(12, ' ');
                                                writer.WriteLine(dum + dum + dum);

                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //alignments
                if (ExportAlignments != null && ExportAlignments.Count > 0)
                {
                    OutputAlignments(writer, ExportAlignments);

                }


                if (Model != null)
                {
                    //Triangles - if there are any
                    if (Model.Triangles != null && Model.Triangles.Count > 0)
                    {

                        OutputTriangles(Model, writer);

                    }
                }
                   
                writer.WriteLine("999");
                writer.WriteLine("999");
                writer.WriteLine("FINISH");

                writer.Close();
                return true;
            }
            return false;

		}

        private static void OutputAlignments(StreamWriter writer, List<Alignment> Alignments)
        {
            if (Alignments == null || Alignments.Count <= 0) { return; }

            foreach (Alignment alg in Alignments)
            {
                List<GStringElement> MXString = new List<GStringElement>();
                string header = "001FORMAT(3F12.3/F12.3,F12.9,F12.3/F12.9,F12.9,4A4)";
                string order = "003,ORDR,4 = 1,1,2,3,4,5,6";

                MXString = alg.ExtractMXAlignment();
                writer.WriteLine(header);
                writer.WriteLine("003,ORDR,4 = 1,1,2,3,4,5,6");
                writer.WriteLine("080," + alg.StringName + ",7=12");
                foreach (GStringElement ele in MXString)
                {
                    string field1 = ele.Coord.X.ToString("0.000").PadLeft(12);
                    string field2 = ele.Coord.Y.ToString("0.000").PadLeft(12);
                    string field3 = ele.Coord.Z.ToString("0.000").PadLeft(12);
                    writer.WriteLine(field1 + field2 + field3);
                    field1 = ele.Chainage.ToString("0.000").PadLeft(12);
                    field2 = ele.Bearing.ToString("0.000000000").PadLeft(12);
                    if (ele.Radius == 0 ||Math.Abs(ele.Radius) > 999999.999)
                    {
                        ele.Radius = 999999.999;
                         
                    }
                    field3 = ele.Radius.ToString("0.000").PadLeft(12);
                    writer.WriteLine(field1 + field2 + field3);

                    field1 = ele.Grade.ToString("0.000000000").PadLeft(12);
                    field2 = ele.Mval.ToString("0.000000000").PadLeft(12);
                    string field4 = "";
                    field4 = ele.HCode.PadRight(4);
                    string field5 = "";
                    if (ele.VCode == null) { ele.VCode = ""; }
                    field5 = ele.VCode.PadRight(4);
                    writer.WriteLine(field1 + field2 + field4 + field5);

                }
                writer.WriteLine("       0.000       0.000       0.000");
                writer.WriteLine("       0.000       0.000       0.000");
                writer.WriteLine("               0.000       0.000                ");
            }
                
        }


        /// <summary>
        /// OutputTriangles - outputs triangles from DTM object to MX GENIO 090 record
        /// </summary>
        /// <param name="model">DTM Object</param>
        /// <param name="writer">Stream Writer object</param>
        private static void OutputTriangles(DTM model, StreamWriter writer)
        {
            if (model != null && model.Triangles != null && model.Triangles.Count > 0 && writer != null)
            {
                writer.WriteLine("001FORMAT(9I8/6A4,I8/5(3D23.17/),2D23.17)");
                writer.WriteLine("090, TP00, TRIN, 7709," + model.Triangles.Count.ToString());

                string field1 = "";
                string field2 = "";
                string field3 = "";
                string field4 = "";
                string field5 = "";
                string field6 = "";
                string field7 = "";
                string field8 = "";
                string field9 = "";
                List<Triangle> modelTriangles = model.Triangles.ToList();
                foreach (Triangle tri in model.Triangles)
                {
                    if (tri.Surface.ID == 0) { continue; }
                    

                    int adj1 = modelTriangles.IndexOf(tri.FindAdjacentTriangle(2))+1;
                    int adj2 = modelTriangles.IndexOf(tri.FindAdjacentTriangle(1))+1;
                    int adj3 = modelTriangles.IndexOf(tri.FindAdjacentTriangle(3))+1;

                    field1 = adj1.ToString().PadLeft(8);
                    field2 = adj2.ToString().PadLeft(8);
                    field3 = adj3.ToString().PadLeft(8);
                    field4 = "        0";
                    field5 = "        0"; 
                    field6 = "        0";
                    field4 = "        0";
                    field5 = "        0";
                    field6 = "        0";
                    field7 = "        0";
                    field8 = "        0";
                    field9 = "        0";
                    writer.WriteLine(field1 + field2 + field3 + field4 + field5 + field6 + field7 + field8 + field9);
                    writer.WriteLine("                               0");
                    field1 = CheckNegative("0" + tri.Point2.X.ToString(".00000000000000000E+00"));
                    field2 = CheckNegative("0" + tri.Point2.Y.ToString(".00000000000000000E+00"));
                    field3 = CheckNegative("0" + tri.Point2.Z.ToString(".00000000000000000E+00"));
                    writer.WriteLine(field1 + field2 + field3);
                    field1 = CheckNegative("0" + tri.Point1.X.ToString(".00000000000000000E+00"));
                    field2 = CheckNegative("0" + tri.Point1.Y.ToString(".00000000000000000E+00"));
                    field3 = CheckNegative("0" + tri.Point1.Z.ToString(".00000000000000000E+00"));
                    writer.WriteLine(field1 + field2 + field3);
                    field1 = CheckNegative("0" + tri.Point3.X.ToString(".00000000000000000E+00"));
                    field2 = CheckNegative("0" + tri.Point3.Y.ToString(".00000000000000000E+00"));
                    field3 = CheckNegative("0" + tri.Point3.Z.ToString(".00000000000000000E+00"));
                    writer.WriteLine(field1 + field2 + field3);
                    writer.WriteLine("0.00000000000000000D+000.00000000000000000D+000.00000000000000000D+00");
                    writer.WriteLine("0.00000000000000000D+000.00000000000000000D+000.00000000000000000D+00");
                    writer.WriteLine("0.00000000000000000D+000.00000000000000000D+00");
                }
                

            }

            


            
        
        
        }
        private static string CheckNegative(string inputString)
        {

            return inputString.Replace("0-", "-");

		}
	}
}

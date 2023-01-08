using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using NRG.Services;

using System.Xml;
using System.Windows.Forms;
using System.ComponentModel;



namespace NRG.File_IO.Export
{


    public static class XMLExport
    {

        //Given a DTM and optionally a list of alignments outputs data to landXML
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ModelToWrite">DTM to output</param>
        /// <param name="projectName">Current Project</param>
        /// <param name="filename">File to sqirt the data into</param>
        /// <param name="Alignments">List of type alignments to output - which alignments will have to be chosen externally</param>
        /// <returns>T/F if it worked or not</returns>
        
        public static bool CreateLandXMLFile(DTM ModelToWrite, string projectName,  string filename = "",List<Alignment> Alignments=null)
        {
            string xmlVersion = "<?xml version = " + StringHandling.WrapWithQuotes("1.0") + "?>";

			#region legitimacy checks 
			//check that we have an instantiated models and it contains at least one point - otherwise fuck it off
			if ((ModelToWrite == null || ModelToWrite.Points == null || ModelToWrite.Points.Count <= 0) && Alignments != null)
			{
				MessageBox.Show("The file cannot be saved as there is no data.", "LandXML File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
			//Quick sanity check on the filename
			if (filename == null || filename == "")
			{
				MessageBox.Show("The file cannot be saved as the file name is invalid.", "LandXML File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;

			}

			
			foreach (char c in Path.GetInvalidPathChars())
			{
				if (filename.Contains(c))
				{
					MessageBox.Show("The file cannot be saved as the file name is invalid.", "LandXML File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return false;

				}
			}
            #endregion legitimacy checks 

            //start thinking about  writing the doc...

            using (StreamWriter writer = new StreamWriter(filename))
            {

                #region Header data -> static text
                writer.WriteLine (xmlVersion);
                
                WriteXMLInfo(writer);
                WriteUnits(writer);
                WriteProject(writer, projectName);
                WriteApplication(writer);
                #endregion Header data -> static text

                #region Alignments
                WriteAlignments(writer, Alignments);
                #endregion Alignments

                #region surfaces
                if (ModelToWrite != null)
                {
                    writer.WriteLine(CreateIndent(2) + "<Surfaces>");
                    WriteTIN(writer, ModelToWrite);
                    writer.WriteLine(CreateIndent(2) + "</Surfaces>");
                }
                #endregion surfaces

                //Finish things off by closing the master tag
                writer.WriteLine( "</LandXML>"); //close main / master tag

                //And finally close the file
                writer.Close();//added by ES:26.05.22
                
            }

            return true;//got here so we must have written the file
        }




        #region write verbage - not going to comment this lot for the time being - all self-explanitary and should remain static
        private static void WriteXMLInfo(StreamWriter writer)
        {
            writer.WriteLine(CreateAttrib(0, "<LandXML xmls", "http://www.landxml.org/schema/LandXML-1.0"));
            writer.WriteLine(CreateAttrib(0, "xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance"));
            writer.WriteLine(CreateAttrib(3, "xsi:schemaLocation", "http://www.landxml.org/schema/LandXML-1.0 http://www.landxml.org/schema/LandXML-1.0/LandXML-1.0.xsd"));
            writer.WriteLine(CreateAttrib(3, "date", DateTime.Now.ToString("yyyy-MM-dd")) + CreateAttrib(3, "time", DateTime.Now.ToString("hh-mm-ss")) + CreateAttrib(3, "version", "1.0") + CreateAttrib(3, "language", "English") + CreateAttrib(3, "readOnly", "false") + ">");

        }


        private static void WriteUnits(StreamWriter writer, bool metric = true)
        {
            writer.WriteLine("  <Units>");
            if (metric)
            {
                writer.WriteLine("     <Metric" + CreateAttrib(5, " areaUnit", "squareMeter") + CreateAttrib(5, "linearUnit", "meter") + CreateAttrib(5, "volumeUnit ", "cubicMeter") + CreateAttrib(5, "temperatureUnit ", "celsius") + CreateAttrib(5, "pressureUnit", "milliBars") + CreateAttrib(5, "angularUnit ", "decimal dd.mm.ss") + CreateAttrib(5, "directionUnit ", "decimal dd.mm.ss") + "></Metric>");
            }
            writer.WriteLine("  </Units>");
        }

        private static void WriteProject(StreamWriter writer, string projectName)
        {
            writer.WriteLine("  <Project " + (CreateAttrib(0, "name", projectName) + "></Project>"));

        }

        private static void WriteApplication(StreamWriter writer)
        {
            writer.WriteLine("  <Application" + CreateAttrib(0, " name", "NRG Survey System") + CreateAttrib(0, "desc", "LandXML Export") + CreateAttrib(0, "manufacturer", "NRG Surveys Ltd.") + CreateAttrib(0, "version", "2022.0") + CreateAttrib(0, "manufacturerURL", "https://www.nrgsurveys.co.uk/") + CreateAttrib(0, "timeStamp", "2022-01-12T00:10:34" + DateTime.Now.ToString("yyyy-MM-dd") + "T00:" + DateTime.Now.ToString("hh:mm")) + "></Application>");
        }

        #endregion write verbage


        #region TIN Model   
        public static void WriteTIN(StreamWriter writer, DTM ModelToWrite)
        {

            if (ModelToWrite != null && ModelToWrite.Triangles.Count > 0)
            {
                writer.WriteLine(CreateIndent(4) + "<Surface " + CreateAttrib(0, "name", ModelToWrite.Name) + CreateAttrib(0, "desc", "Ground Model") + ">");
                GetTINInfo(ModelToWrite, out double planArea, out double surfaceArea, out double minElevation, out double maxElevation);
                writer.WriteLine(CreateIndent(6) + "<Definition " + CreateAttrib(0, "area2DSurf", planArea.ToString()) + CreateAttrib(0, "area3DSurf", surfaceArea.ToString()) + CreateAttrib(0, "elevMin", minElevation.ToString("0.00000")) + CreateAttrib(0, "elevMax", maxElevation.ToString("0.00000")+">"));
                writer.WriteLine(CreateIndent(8) + "<Pnts>");
                WriteTINPoints(writer, ModelToWrite);
                writer.WriteLine(CreateIndent(8) + "</Pnts>");
                writer.WriteLine(CreateIndent(8) + "<Faces>");
                WriteTINFaces(writer, ModelToWrite);
                writer.WriteLine(CreateIndent(8) + "</Faces>");

                writer.WriteLine(CreateIndent(6) + "</Definition>");


                writer.WriteLine(CreateIndent(4) + "</Surface>");

            }

        }

        public static void WriteTINPoints(StreamWriter writer, DTM ModelToWrite)
        {
            if (ModelToWrite != null && ModelToWrite.Points.Count > 0)
            {
                for (int i = 0; i < ModelToWrite.Points.Count; i++)
                {
                    writer.WriteLine(CreateIndent(10) + "<P " + CreateAttrib(0, "id", i.ToString()) + ">" + ModelToWrite.Points[i].X.ToString("0.00000") + " " + ModelToWrite.Points[i].Y.ToString("0.00000") + " " + ModelToWrite.Points[i].Z.ToString("0.00000") + "</P>");
                }
            }
        }

        public static void WriteTINFaces(StreamWriter writer, DTM ModelToWrite)
        {
            if (ModelToWrite != null && ModelToWrite.Triangles != null && ModelToWrite.Triangles.Count > 0)
            {
                foreach (Triangle t in ModelToWrite.Triangles)
                {
                    if (t.Surface.Contour)
                    {
                        writer.WriteLine(CreateIndent(10) + "<F>" + ModelToWrite.Points.IndexOf(t.Point1) + " " + ModelToWrite.Points.IndexOf(t.Point2) + " " + ModelToWrite.Points.IndexOf(t.Point3) + "</F>");
                    }
                }
            }
        }


        private static void GetTINInfo(DTM ModelToWrite, out double PlanArea, out double SurfaceArea, out double MinElevation, out double MaxElevation)
        {

            MinElevation = double.PositiveInfinity;
            MaxElevation = double.NegativeInfinity;
            PlanArea = 0;
            SurfaceArea = 0;
            if (ModelToWrite != null && ModelToWrite.Triangles.Count > 0)
            {

                foreach (Triangle t in ModelToWrite.Triangles)
                {
                    PlanArea  += t.PlanArea();
                    SurfaceArea  += t.SurfaceArea();
                    double z = t.MinHeight();
                    if (z < MinElevation)
                    {
                        MinElevation = z;
                    }
                    z = t.MaxHeight();
                    if (z > MaxElevation)
                    {
                        MaxElevation = z;
                    }

                }
            }
            
        }
        #endregion TIN Model


        #region alignments - horizontal and vertical
        private static void WriteAlignments(StreamWriter writer, List <Alignment> alignments )
        {
            if (alignments != null)
            {
                writer.WriteLine("  <Alignments" + CreateAttrib(0, " name", "Geometric Alignments")+">");
                foreach (Alignment alg in alignments)
                {

                    double totLen = alg.Horizontal.MaxChainage - alg.Horizontal.MinChainage;
                    double startCh= alg.Horizontal.MinChainage;
                    writer.WriteLine("    <Alignment" + CreateAttrib(0, " name", alg.StringName) + CreateAttrib(0, " length", totLen.ToString("0.00000")) + CreateAttrib(0, "staStart", startCh.ToString("0.00000")) + CreateAttrib(0, " desc", "") + ">");
                    writer.WriteLine("         <CoordGeom>");
                    WriteHz(writer, alg.Horizontal);
                    writer.WriteLine("         </CoordGeom>");
                    if (alg.Vertical != null && alg.Vertical.Elements.Count > 0)
                    {
                        writer.WriteLine("         <Profile " + CreateAttrib(0,"name", alg.StringName) + ">");
                        writer.WriteLine("           <ProfAlign "+ CreateAttrib(0, "Profile - name", alg.StringName) + ">" );
      
                        WriteVert(writer, alg.Vertical);
                        writer.WriteLine("           </ProfAlign>");
                        writer.WriteLine("         </Profile>");
                    }
                    writer.WriteLine("    </Alignment>");
                }
                writer.WriteLine("  </Alignments>");


            }
           
        }

        private static void WriteVert(StreamWriter writer, Models.VerticalAlignment vert)
        {

            //Remember an alignment doesn't have to have vertical data so check we have something legit before attempting to write it to file.
            //Strictly these checks should already have been done by the calling function in order that the tag header is written, but belt and braces.
            if (vert != null || vert.Elements.Count > 0)
            {
                for (int i = 0; i < vert.Elements.Count; i++)
                {
                    Models.VerticalElement ele = vert.Elements[i];
                    if (i == 0 || i == vert.Elements.Count - 1 || ele.Radius == 0)
                    {
                        //it's the start element, last element or a vertical intersection point -> regardless output is the same

                        writer.WriteLine("               <PVI>" + ele.Chainage.ToString("0.0000") + " " + ele.Level.ToString("0.00000") +   "</ PVI >");
                    }
                    else
                    {

                        Models.VerticalElement nextele = vert.Elements[i + 1];// don;t think this can fall out of the bounds of the array 

                        double brg = 0;
                        double dist = 0;    

                        NRG.MathsHelpers.Trig.RPC(ele.Chainage, ele.Level, nextele.Chainage, nextele.Level,ref  brg, ref  dist, 1.0f);
                        double da = NRG.MathsHelpers.Trig.FNasn(dist / (2 * ele.Radius));//deflection angle baby...yeah

                        double brg1 = 0, brg2 = 0;  

                        if (ele.Radius > 0)
                        {
                            brg1 = brg + da;
                            brg2 = brg - da;
                        }
                        else {
                            brg1 = brg + da;
                            brg2 = brg - da;
                        }
                        double arclen = Math.Abs(NRG.MathsHelpers.Geometry.ArcLength(new Point2D(ele.Chainage, ele.Level), new Point2D(nextele.Chainage, nextele.Level), ele.Radius));
                        double ipx = 0, ipy = 0;
                        Point2D ip =  NRG.MathsHelpers.Intersection.IntersectTwoBrgLines(ele.Chainage, ele.Level, brg1, nextele.Chainage, nextele.Level, brg2);
                        //vertical circular curve
                        writer.WriteLine("               <CircCurve " + CreateAttrib(0 ,"length", arclen.ToString("0.00000") + " " + CreateAttrib(0, "radius", Math.Abs(ele.Radius).ToString("0.00000")) + ">" + ip.X.ToString("0.00000") + " " + ip.Y.ToString("0.00000")  + "</CircCurve>"));

                    }
                }
                
                return;
            }   
            
            


        }
        
        private static void WriteHz(StreamWriter writer, Models.HorizontalAlignment hz)
        {

            //---------------------------------------------------------------
            //fake something for the end in case we need it.....
            HorizontalElement endele = new HorizontalElement();
            HorizontalElement prevele = null;
            if (hz.Elements.Count > 1)
            {
                prevele = hz.Elements.ElementAt(hz.Elements.Count - 2); 
            }
            Point2D outvec = new Point2D();
            double tang = 0;
            double rad = 0;
            bool ok = false;
            hz.Coords(hz.MaxChainage, hz.Elements.ElementAt(hz.Elements.Count-1), prevele, hz.Elements.Last(), ref outvec, ref tang, ref rad, ref ok);
            if (ok)
            {
                endele.East = outvec.X;
                endele.North = outvec.Y;
                endele.BrgOrigin = tang;
                endele.Chainage = hz.MaxChainage;
                endele.ChainageOrigin = hz.MaxChainage;

            }
            double curbrg = 0;

            //---------------------------------------------------------------
            foreach (HorizontalElement ele in hz.Elements)
            {
                HorizontalElement nextele = new HorizontalElement();
                prevele = new HorizontalElement();
                
                if (ele != null)
                {
                   
                    prevele = getPreviousElement(hz, ele);
                    nextele = getNextElement(hz, ele);

                    if (nextele == null) { continue; }

                    //if (nextele == null) { nextele = endele; }
                    double elelen = 0;
                    if (nextele != null)
                    {
                        elelen = nextele.Chainage - ele.Chainage;
                    }

                    string thisLen = (elelen).ToString("0.00000");
                    if (ele.ElementType == HorizontalElementType.Straight)// && elelen > 0)
                    {
                        double lineBrg = MathsHelpers.Trig.RPC(ele.East, ele.North, nextele.East, nextele.North);

                        writer.WriteLine("           <Line " + CreateAttrib(0, "dir", XMLBearing( lineBrg)) + CreateAttrib(0, " length", thisLen) + ">");
                        writer.WriteLine("             <Start>" + ele.North.ToString("0.00000") + " " + ele.East.ToString("0.000000") + "</Start>");
                        writer.WriteLine("             <End> " + nextele.North.ToString("0.00000") + " " + nextele.East.ToString("0.000000") + " </End>");
                        writer.WriteLine("            </Line> ");
                    }
                    else
                    {
                        double brg1 = 0;
                        double brg2 = 0;    
                        double rad1 = 0;
                        double rad2 = 0;
                        Point2D vec1 = new Point2D(0, 0);   
                        Point2D vec2 = new Point2D(0, 0);  
                        hz.Coords(ele.Chainage, ele, prevele, nextele, ref vec1, ref brg1, ref rad1, ref ok);
                        hz.Coords(nextele.Chainage, nextele, ele, getNextElement(hz, nextele), ref vec2, ref brg2, ref rad2, ref ok);

                        double eleLen = nextele.Chainage - ele.Chainage; //should this come from the element properties?
                        string rottext = "cw";

                        if (rad1 < 0 || rad2 < 0) 
                        {
                            rottext = "ccw";
                        }

                        rad1 = Math.Abs(rad1);
                        rad2 = Math.Abs(rad2);
                      

                        
                        
                        if (ele.ElementType == HorizontalElementType.Arc)
                        { //circular curve

                            double centBrg = ele.BrgOrigin;
                            centBrg = centBrg + Math.PI / 2;
                            if (centBrg > Math.PI * 2)
                            {
                                centBrg = centBrg - Math.PI * 2;
                            }

                            Point2D pi = MathsHelpers.Trig.PRC(ele.East, ele.North, centBrg, ele.Radius);


                            writer.WriteLine("           <Curve" + CreateAttrib(0, " length", elelen.ToString("0.000000")) + CreateAttrib(0, " crvType", "arc") + CreateAttrib(0, " dirStart", XMLBearing(ele.BrgOrigin)) + CreateAttrib(0, " dirEnd",XMLBearing(brg2)) + CreateAttrib(0, " rot", rottext) + CreateAttrib(0," radius", ele.Radius.ToString("0.000000")) + ">");
                            writer.WriteLine("             <Start>" + ele.North.ToString("0.000000") + " " + ele.East.ToString("0.000000") + "</Start>");
                            writer.WriteLine("             <Centre> " + pi.Y.ToString("0.000000") + " " + pi.X.ToString("0.000000") + " </Centre>");
                            writer.WriteLine("             <End> " + nextele.North.ToString("0.00000") + " " + nextele.East.ToString("0.000000") + " </End>");
                            writer.WriteLine("           </Curve> ");
                        }
                        else
                        { //spiral
                            if (nextele != null) //can't end on a spiral...
                            {
                                string radstart = "INF";
                                string radend = "INF";

                                rad1 = Math.Abs(rad1);
                                rad2 = Math.Abs(rad2);

                                if (rad1 > 0 && rad1 < 100000) { radstart = rad1.ToString("0.000000"); }
                                if (rad2 > 0 && rad2 < 100000) { radend = rad2.ToString("0.000000"); }

                                Point2D pi = MathsHelpers.Intersection.IntersectTwoBrgLines(ele.East, ele.North, brg1, nextele.East, nextele.North, brg2);
                                
                                Point2D pi2 = MathsHelpers.Intersection.IntersectTwoBrgLines(vec1.X, vec1.Y, brg1, vec2.X, vec2.Y, brg2);

                                Point2D pi3 = MathsHelpers.Intersection.IntersectTwoBrgLines(ele.EastOrigin, ele.NorthOrigin, ele.BrgOrigin, nextele.EastOrigin, nextele.NorthOrigin, nextele.BrgOrigin);


                                if (pi != null)
                                {
                                    writer.WriteLine("           <Spiral" + CreateAttrib(0, " length", elelen.ToString("0.000000")) + CreateAttrib(0, " radiusStart", radstart) + CreateAttrib(0, " radiusEnd", radend) + CreateAttrib(0, " rot", rottext) + CreateAttrib(0, " spiType", "clothoid") + ">");
                                    writer.WriteLine("             <Start>" + ele.North.ToString("0.000000") + " " + ele.East.ToString("0.000000") + "</Start>");
                                    writer.WriteLine("             <PI> " + pi.Y.ToString("0.000000") + " " + pi.X.ToString("0.000000") + " </PI>");
                                    writer.WriteLine("             <End> " + nextele.North.ToString("0.00000") + " " + nextele.East.ToString("0.000000") + " </End>");
                                    writer.WriteLine("           </Spiral> ");
                                }
                            }                  
                        }
                    }

                }

            }
        }

       
        private static HorizontalElement getNextElement(Models.HorizontalAlignment  hz, HorizontalElement ele)
        {
            if (ele != null)
            {
                int idx = hz.Elements.IndexOf(ele)+1;
                if (idx < hz.Elements.Count )
                {
                    return hz.Elements.ElementAt(idx);
                }
            }
            
            return null;
        }

        private static HorizontalElement getPreviousElement(Models.HorizontalAlignment hz, HorizontalElement ele)
        {
            if (ele != null)
            {
                int idx = hz.Elements.IndexOf(ele)-1;
                if (idx >=0)
                {
                    return hz.Elements.ElementAt(idx);
                }
            }

            return null;
        }


        #endregion alignment stuff
      

        #region helpers
        /// <summary>
        /// Added by ES:22.02.22 - take an angle / bearing in radians and converts it to the strange XML angle format
        /// </summary>
        /// <param name="ang">value to convert in radians</param>
        /// <returns>string containing XML angle in the format ddd.mmssss (no leading zeros)</returns>
        private static string XMLBearing(double ang)
        {
            double deg = ang * 180 / Math.PI;

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



            string degString = d.ToString();
            string minString = String.Format("{0:00}", m);
            string secString = String.Format("{0:00.00}", s);
            secString = secString.Replace(".", "");

            return degString + "." + minString + secString;
        }


        private static string CreateAttrib(int numIndent, string id, string value)
        {
            return id + "=" + StringHandling.WrapWithQuotes(value) + " ";
        }

        private static string CreateIndent(int number)
        {
            string outString = "";
            outString = outString.PadLeft(number);
            return outString;
        } //unused for some reason - ES:30.05.22 although it's my fault
        #endregion helpers


    }




}

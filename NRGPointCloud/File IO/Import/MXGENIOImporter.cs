using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NRG.Models;



namespace NRG.Import
{
    public class MXGENIOImporter
    {
        public enum GENIO_FORMAT_TYPE
        {
            NRG_3D,
            MX_3D,
            NRG_5D,
            MX_5D,
            NRG_6D,
            MX_6D,
            NRG_12D,
            MX_12D,
            MX_TRIANGLES,
            IGNORE
        }

        /// <summary>
        /// InterpretFormatString - this might get very complicated, but I've gone through the original code and writing a Fortran interpreter
        /// seems very complicated for the number of scenarios we're likely to encounter.
        /// </summary>
        /// <param name="formatStr">001 string from the file</param>
        /// <param name="orderStr">003 string, optional, but could well affect the outcome, depending on the formatStr</param>
        /// <returns></returns>
        private GENIO_FORMAT_TYPE InterpretFormatString(string formatStr, string orderStr = "")
        {
            //=============================================================================================================================
            //horrible hack to fix issue #95
            if ((formatStr.TrimEnd() == "001FORMAT(6F12.3)" || formatStr.TrimEnd() == "001FORMAT (6F12.3)") && orderStr.Contains("003ORDR               1         1         2       3"))
            {
                formatStr = "001FORMAT(3F12.3)";
            }
            //=============================================================================================================================

            switch (formatStr.Trim())
            {
                case "001FORMAT(3F12.3)":
                    return GENIO_FORMAT_TYPE.NRG_3D;
                case "001FORMAT(3D23.17)":
                    if (orderStr == "")
                    {
                        return GENIO_FORMAT_TYPE.MX_3D;
                    }
                    else
                    {
                        if (orderStr.Contains("003,ORDR,4=1,1,2,3"))
                        {
                            if (orderStr.Contains("003,ORDR,4=1,1,2,3,4,5,6"))
                                return GENIO_FORMAT_TYPE.MX_6D;
                            else if (orderStr.Contains("003,ORDR,4=1,1,2,3,4,5"))
                                return GENIO_FORMAT_TYPE.MX_5D;
                            else
                                return GENIO_FORMAT_TYPE.MX_3D;
                        }
                        else
                        {
                            return GENIO_FORMAT_TYPE.IGNORE;
                        }
                    }
                case "001FORMAT(3D23.17/3D23.17/2D23.17,4A4)":
                    return GENIO_FORMAT_TYPE.MX_12D;
                case "001FORMAT(3F12.3/F12.3,F12.9,F12.3/F12.9,F12.9,4A4)":
                    return GENIO_FORMAT_TYPE.NRG_12D;
                case "001FORMAT(9I8/6A4,I8/5(3D23.17/),2D23.17)":
                    return GENIO_FORMAT_TYPE.MX_TRIANGLES;
                default:
                    return GENIO_FORMAT_TYPE.IGNORE;
            }

        }


        //Moved to Models.GeneralHelpers

        //public class ModelImport
        //{
        //    public List<DTM> Models = new List<DTM>();
        //    public List<Alignment> Geometry = new List<Alignment>();
        //}


        /// <summary>
        /// General MX GENIO importer
        /// </summary>
        /// <param name="fileName">input file</param>
        /// <returns>a list of models from the GENIO file if valid / null else</returns>
        /// 
        /// Notes: This is nowhere near a port of the original code, which was a lot more complex
        /// I've taken this approach with, hopefully, the benefit of hindsight. The orignal was effectively 
        /// a Fortran interpreter with lots of special cases built in to overcome minor issues we had over the years.
        /// This is a more rough and ready implementation.
        /// Problems: While I / we have a good understanding of GENIO there are a lot of devs out there that play a 
        /// little bit fast and loose with the format. The most spectacular are a couple of C3D plugins. The first 
        /// has contradictory order and format statements and the second just outputs a load of junk between each string
        /// 
        /// This will be a WIP for the forseeable future and I think will need to be heavilly refactored at some point.
        /// 
        ///MX GENIO manual can be found at: https://communities.bentley.com/cfs-file/__key/telligent-evolution-components-attachments/00-5922-01-00-00-03-70-10/MOSSV10-user-Manual.pdf
        
        public GeneralHelpers.ModelImport ImportGENIO(string fileName)
        {
            GENIO_FORMAT_TYPE currentFormat = new GENIO_FORMAT_TYPE();
            currentFormat = GENIO_FORMAT_TYPE.IGNORE;
            string currentFormatString = "001FORMAT(3D23.17)";
            string currentLabel = "";
            string currentSubRef = "";
            bool validData = false;
            bool endString = false; //added as a resolution to issue #105
            string currentModelName = "";

            DTM currentModel = new DTM();
            GeneralHelpers.ModelImport modelsInFile = new GeneralHelpers.ModelImport(); 

            #region alignment geomentry declarations
            List<MXMString> MStrings = new List<MXMString>(); //6D Geometry
            List<MXGString> GStrings = new List<MXGString>(); //12D Geometry
            MXMString CurrentM = new MXMString();
            MXGString CurrentG = new MXGString();

            var points = new Dictionary<string, DTMPoint>();

            List<IndexedTriangle> tris = new List<IndexedTriangle>();

            #endregion


            if (!File.Exists(fileName))
                return null;
            using (var reader = new StreamReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                string line = "";
                while (line != null && line != "999"&& line !="FINISH" &&  reader.EndOfStream == false && (line = reader.ReadLine().ToUpper()) != null)
                {
                    //Added by ES:27.10.21
                    line = line.TrimStart(' ');
                    line = line.TrimStart();


					//if (line == "999")
					//{
					//	Console.WriteLine("End of model");
					//	if (currentModel != null)
					//	{
					//		modelsInFile.Models.Add(currentModel); //keep an eye on this as I'm not sure how it got missed - I think it's something to do with there only being one model in the file and no triangles or geometry
					//	}
					//}


					if (line.Contains("001FORMAT")) { validData = true; } //I think we have to have at least one format statement
                    if (line.Contains("080,"))
                    {
                        validData = true;

                    }
                    if (line == "") continue;
                    if (line.Contains("GENIO")) { currentModelName = line.Substring(5).Trim(); continue; }
                    if (validData)
                    {
                        if (line.Contains("FORMAT") == true)
                        {
                            currentFormat = InterpretFormatString(line.Trim());
                            currentFormatString = line;
                            endString = false;
                        }
                        else if (line.Contains("003,ORDR") == true)
                        {
                            currentFormat = InterpretFormatString(currentFormatString, line.Trim());
                            endString = false;
                        }
                        else
                        {
                            if (currentFormat != GENIO_FORMAT_TYPE.IGNORE)
                            {

                                if (line.Contains("080,") == true) //should be 081 also?
                                {
                                    var arr = line.Split(',');
                                    currentLabel = arr[1].Trim() + "/ST";
                                    currentSubRef = arr[2].Trim();
                                    endString = false; //beginning of a new string related to issues #105
                                    if (currentFormat == GENIO_FORMAT_TYPE.MX_12D || currentFormat == GENIO_FORMAT_TYPE.NRG_12D)
                                    {
                                        CurrentM = null;
                                        CurrentG = new MXGString();
                                        CurrentG.Name = currentLabel.Replace("/ST", "");
                                    }
                                    else if (currentFormat == GENIO_FORMAT_TYPE.MX_6D || currentFormat == GENIO_FORMAT_TYPE.MX_6D)
                                    {
                                        CurrentG = null;
                                        CurrentM = new MXMString();
                                        CurrentM.Name = currentLabel.Replace("/ST", "");
                                    }
                                    else
                                    {
                                        CurrentM = null; CurrentG = null;
                                    }

                                }

                                else if ((line.Contains("090,") == true))
                                {
                                    //mx triangles
                                    currentFormat = GENIO_FORMAT_TYPE.MX_TRIANGLES;
                                    points = new Dictionary<string, DTMPoint>();
                                    tris = new List<IndexedTriangle>();
                                }
                                else if (endString && line.Trim() != "999")

                                    continue; //an old one has ended, but a new one hasn't begun issue #105

                                else
                                {
                                    DTMPoint newpoint = new DTMPoint();
                                    if (line == "999")
                                    {
                                        //end of model

                                        if (currentFormat == GENIO_FORMAT_TYPE.MX_TRIANGLES )
                                        {

                                            if (points != null)
                                            {
                                                //for (int i = 0; i < points.Count; i++)
                                                //{

                                                //    DTMPoint pt = new DTMPoint();
                                                //    points.TryGetValue(i, out pt);
                                                //    pt.PointID = i.ToString();
                                                //    pt.ID = i;
                                                //    currentModel.Points.Add(pt);
                                                //}


                                                //foreach (var pt in points)
                                                //{
                                                //    currentModel.Points.Add(pt.Value);
                                                //}



                                                TriangleSurface s0 = currentModel.GetSurface(0);
                                                TriangleSurface s1 = currentModel.GetSurface(1);

                                                currentModel.BuildPointsDictionary();

                                                foreach (IndexedTriangle tr in tris)
                                                {

                                                    DTMPoint p1 = new DTMPoint();
                                                    DTMPoint p2 = new DTMPoint();
                                                    DTMPoint p3 = new DTMPoint();

                                                    //int idx1 = 0;
                                                    //int idx2 = 0;
                                                    //int idx3 = 0;

                                                    points.TryGetValue(tr.Point2, out p1);
                                                    points.TryGetValue(tr.Point1, out p2);
                                                    points.TryGetValue(tr.Point3, out p3);

                                                    #region match existing point or add new point to model
                                                    var point1 = currentModel.MatchPointFromPointsDictionary(p1.X, p1.Y, p1.Z, true, "Default", false);
                                                    var point2 = currentModel.MatchPointFromPointsDictionary(p2.X, p2.Y, p2.Z, true, "Default", false);
                                                    var point3 = currentModel.MatchPointFromPointsDictionary(p3.X, p3.Y, p3.Z, true, "Default", false);
                                                    #endregion

                                                    if (MathsHelpers.Vector.FNRightOf(point1.X, point1.Y, point3.X, point3.Y, point2.X, point2.Y) == false)
                                                    {

                                                        points.TryGetValue(tr.Point1, out p1);
                                                        points.TryGetValue(tr.Point2, out p2);
                                                        points.TryGetValue(tr.Point3, out p3);

                                                        #region match existing point or add new point to model
                                                        point1 = currentModel.MatchPointFromPointsDictionary(p1.X, p1.Y, p1.Z, true, "Default", false);
                                                        point2 = currentModel.MatchPointFromPointsDictionary(p2.X, p2.Y, p2.Z, true, "Default", false);
                                                        point3 = currentModel.MatchPointFromPointsDictionary(p3.X, p3.Y, p3.Z, true, "Default", false);
                                                        #endregion

                                                    }
                                                    else
                                                    {
                                                        points.TryGetValue(tr.Point3, out p1);
                                                        points.TryGetValue(tr.Point2, out p2);
                                                        points.TryGetValue(tr.Point1, out p3);

                                                        #region match existing point or add new point to model
                                                        point1 = currentModel.MatchPointFromPointsDictionary(p1.X, p1.Y, p1.Z, true, "Default", false);
                                                        point2 = currentModel.MatchPointFromPointsDictionary(p2.X, p2.Y, p2.Z, true, "Default", false);
                                                        point3 = currentModel.MatchPointFromPointsDictionary(p3.X, p3.Y, p3.Z, true, "Default", false);
                                                        #endregion
                                                    }

                                                    if (point1.Z < -998 || point2.Z < -998 || point3.Z < -998)
                                                    {
                                                        Triangle tri = new Triangle(s0, point1, point2, point3);
                                                        //s0.AddTriangle(tri);
                                                        currentModel.Triangles.Add(tri);
                                                    }
                                                    else
                                                    {
                                                        Triangle tri = new Triangle(s1, point1, point2, point3);
                                                        //s1.AddTriangle(tri);
                                                        currentModel.Triangles.Add(tri);
                                                    }

                                                }
                                            }

                                        }

                                        currentModel.Name = currentModelName;
                                        modelsInFile.Models.Add(currentModel);
                                        currentModel = new DTM();
                                        currentFormat = GENIO_FORMAT_TYPE.IGNORE;
                                        validData = false;
                                        line = reader.ReadLine();//.TrimStart();
                                    }
                                    else if (currentFormat == GENIO_FORMAT_TYPE.MX_3D || currentFormat == GENIO_FORMAT_TYPE.MX_5D || currentFormat == GENIO_FORMAT_TYPE.MX_6D || currentFormat == GENIO_FORMAT_TYPE.MX_12D|| currentFormat == GENIO_FORMAT_TYPE.NRG_12D )
                                    {
                                        //D23.17 - standard MX output format for 3d 5d 6d and 12d strings (apart from the geometry stuff, but more on this later)

                                        #region commented out parsing code
                                        //newpoint.X = Math.Abs(double.Parse(line.Substring(0, 23).Replace("D+", "E+")));
                                        //newpoint.Y = double.Parse(line.Substring(23, 23).Replace("D+", "E+"));

                                        ////===========================================================================================
                                        ////Horrible if condition added by ES:28.04.20 - there must be a be a better way than this...
                                        ////Fixes Issue #103
                                        //if (line.Substring(46, 23).Contains("D-") || line.Substring(46, 23).Contains("E-"))
                                        //{
                                        //    newpoint.Z = double.Parse(line.Substring(46, 23).Replace("D-", "E-"));
                                        //}
                                        //else
                                        //{
                                        //    newpoint.Z = double.Parse(line.Substring(46, 23).Replace("D+", "E+"));
                                        //}
                                        //===========================================================================================
                                        #endregion commented out parsing code
                                        
                                        newpoint.PointLabel = currentLabel;
                                        
                                        MXTripplet trip = new MXTripplet(currentFormat, line);
                                        if (trip.ParsedOK == false) { continue; }
                                        newpoint.X = Math.Abs(trip.Value1);
                                        newpoint.Y = trip.Value2;
                                        newpoint.Z = trip.Value3;
                                        
                                        if (currentFormat == GENIO_FORMAT_TYPE.MX_5D || currentFormat == GENIO_FORMAT_TYPE.MX_6D || currentFormat == GENIO_FORMAT_TYPE.MX_12D || currentFormat == GENIO_FORMAT_TYPE.NRG_12D|| currentFormat == GENIO_FORMAT_TYPE.NRG_12D)
                                        {

                                            
                                             line = reader.ReadLine().TrimStart(); 
                                            
                                            if (currentFormat == GENIO_FORMAT_TYPE.MX_5D)
                                                continue;//for now
                                            trip.Parse(line);

                                            if (currentFormat == GENIO_FORMAT_TYPE.MX_6D)
                                            {

                                                MStringElement ele = new MStringElement();
                                                ele.Coord = newpoint;
                                                ele.Chainage = trip.Value1; ele.Bearing = trip.Value2; ele.Radius = trip.Value3;
                                                CurrentM.Elements.Add(ele);
                                            }
                                            else if (currentFormat == GENIO_FORMAT_TYPE.MX_12D)
                                            {
                                                GStringElement ele = new GStringElement();
                                                ele.Coord = newpoint;
                                                ele.Chainage = trip.Value1; ele.Bearing = trip.Value2; ele.Radius = trip.Value3;
                                                line = reader.ReadLine().TrimStart();//12d shit
                                                ele.HCode = line.Substring(46, 4);
                                                ele.VCode = line.Substring(50, 4);
                                                if (double.TryParse(line.Substring(23, 23).Replace("D", "E"), out double mval))
                                                {
                                                    ele.Mval = mval;
                                                }
                                                CurrentG.Elements.Add(ele);

                                            }
                                            else if (currentFormat == GENIO_FORMAT_TYPE.NRG_12D)
                                            {
                                                GStringElement ele = new GStringElement();
                                                //ele.Coord.X = Math.Abs(double.Parse(line.Substring(0, 12)));
                                                //ele.Coord.Y = double.Parse(line.Substring(12, 12));
                                                //ele.Coord.Z = double.Parse(line.Substring(24, 12));
                                                //newpoint = ele.Coord;
                                                ele.Coord = newpoint;
                                                //line = reader.ReadLine();
                                                ele.Chainage = Math.Abs(double.Parse(line.Substring(0, 12)));
                                                ele.Bearing = double.Parse(line.Substring(12, 12));
                                                ele.Radius = double.Parse(line.Substring(24, 12));
                                                line = reader.ReadLine().TrimStart();//12d shit
                                                ele.HCode = line.Substring(24, 4);
                                                ele.VCode = line.Substring(28, 4);
                                                if (double.TryParse(line.Substring(12, 12).Replace("D", "E"), out double mval))
                                                {
                                                    ele.Mval = mval;
                                                }
                                                CurrentG.Elements.Add(ele);


                                            }

                                        }
                                    }
                                    else if (currentFormat == GENIO_FORMAT_TYPE.MX_TRIANGLES)
                                    {

                                        string dmp = "";
                                        dmp = reader.ReadLine();


                                        int idx1 = 0;
                                        int idx2 = 0;
                                        int idx3 = 0;

                                        int.TryParse(line.Substring(0, 8), out idx1);
                                        int.TryParse(line.Substring(8, 8), out idx2);
                                        int.TryParse(line.Substring(16, 8), out idx3);

                                        //idx1--;
                                        //idx2--;
                                        //idx3--;

                                        line = reader.ReadLine().TrimStart();
                                        MXTripplet trip = new MXTripplet(currentFormat, line);

                                        DTMPoint pt1 = new DTMPoint();
                                        pt1.X = Math.Abs(trip.Value1);
                                        pt1.Y = trip.Value2;
                                        pt1.Z = trip.Value3;

                                        line = reader.ReadLine().TrimStart();
                                        MXTripplet trip2 = new MXTripplet(currentFormat, line);

                                        DTMPoint pt2 = new DTMPoint();
                                        pt2.X = Math.Abs(trip2.Value1);
                                        pt2.Y = trip2.Value2;
                                        pt2.Z = trip2.Value3;

                                        line = reader.ReadLine().TrimStart();
                                        MXTripplet trip3 = new MXTripplet(currentFormat, line);

                                        DTMPoint pt3 = new DTMPoint();
                                        pt3.X = Math.Abs(trip3.Value1);
                                        pt3.Y = trip3.Value2;
                                        pt3.Z = trip3.Value3;

                                        string key1 = pt1.X.ToString() + pt1.Y.ToString() + pt1.Z.ToString();
                                        string key2 = pt2.X.ToString() + pt2.Y.ToString() + pt2.Z.ToString();
                                        string key3 = pt3.X.ToString() + pt3.Y.ToString() + pt3.Z.ToString();


                                        points[key1] = pt1;
                                        points[key2] = pt2;
                                        points[key3] = pt3;

                                        int surfIDX = 0;
                                        TriangleSurface surf = new TriangleSurface();

                                        if (pt1.Z != -999 && pt2.Z != -999 && pt3.Z != -999)
                                        {
                                            surf = currentModel.GetSurface(1);
                                            surfIDX = 1;
                                        }
                                        else
                                        {
                                            surf = currentModel.GetSurface(0);
                                            surfIDX = 0;
                                        }


                                        //============================================================
                                        //ES:27.06.20
                                        IndexedTriangle tri = new IndexedTriangle(key1, key2, key3, surfIDX);
                                        tris.Add(tri);
                                        //============================================================


                                        //var tri = new Triangle(surf, pt1, pt2, pt3);

                                        //surf.Triangles.Add(tri);
                                        //currentModel.Triangles.Add(tri);

                                        //Clear the clag at the end of the triangle
                                        dmp = reader.ReadLine();
                                        dmp = reader.ReadLine();
                                        dmp = reader.ReadLine();

                                        endString = false;

                                    }
                                    else if (currentFormat == GENIO_FORMAT_TYPE.NRG_3D)
                                    {
                                        //F12.3 - NRG  output format, Fixed 12 chars with 3 decimal places - loved by all who export to Trimble as it halves the file size
                                        newpoint.PointLabel = currentLabel;

                                        MXTripplet t = new MXTripplet(GENIO_FORMAT_TYPE.NRG_3D, line);

                                        newpoint.X = Math.Abs(t.Value1);//Math.Abs(double.Parse(line.Substring(0, 12)));
                                        newpoint.Y = t.Value2;//double.Parse(line.Substring(12, 24));
                                        newpoint.Z = t.Value3;//double.Parse(line.Substring(26, 38));

                                        

                                    }
                                    

                                    if (currentFormat != GENIO_FORMAT_TYPE.MX_TRIANGLES && currentFormat != GENIO_FORMAT_TYPE.IGNORE)
                                    {
                                        if (newpoint.X != 0 && newpoint.Y != 0)
                                        {
                                            if (newpoint.Y <= 0 || endString)
                                            {
                                                newpoint.PointLabel = newpoint.PointLabel + "/ST";
                                                newpoint.Y = Math.Abs(newpoint.Y);
                                                endString = false; //unmark the discontinuity this now effectively becomes a new string in DTM Map's eyes.
                                            }
                                            if (currentFormat != GENIO_FORMAT_TYPE.MX_12D && currentFormat != GENIO_FORMAT_TYPE.NRG_12D)
                                            {
                                                currentModel.Points.Add(newpoint);
                                            }
                                            //=======================================================================

                                            //ensure that no /ST exists on subsequent points - relataed to issue #105
                                            //not keen on this, as it doesn't feel overly efficient.
                                            if (currentLabel.Contains("/ST"))
                                            {
                                                currentLabel = currentLabel.Replace("/ST", "");
                                            }
                                            //=======================================================================
                                        }
                                        else
                                        {
                                            if (CurrentG != null && CurrentG.Elements.Count > 0)
                                            {
                                                //var directoryPath = ApplicationSettings.Window.ProjectPath + "//" + "Alignment Strings" + "//" + name + "//";

                                                //if (!Directory.Exists(directoryPath))
                                                //    Directory.CreateDirectory(directoryPath);

                                                //var filePath = directoryPath + name + ".nst";
                                                //if (File.Exists(filePath))
                                                //alg.SaveNST();
                                                Alignment alg = new Alignment(CurrentG);
                                                modelsInFile.Alignments.Add(alg);

                                            }
                                            else if (CurrentM != null)
                                            {

                                            }
                                            endString = true;
                                        }
                                    }
                                    else
                                    {
                                        endString = false;
                                    }

                                }
                            }

                        }

                    }
                }
            }

            //Set the save flag for all imports
            //foreach (DTM model in modelsInFile.Models)
            //{
            //    //model.
            //    model.SaveRequired = true;
            //    model.Properties.ModelName = model.Name;
            //    model.Properties.SourceFile = fileName;
            //    model.Properties.SourceFileType = "MX GENIO";
                
            //}
            return modelsInFile;
        }
    }


    public class MXMString
    {
        public string Name = "";
        public List<MStringElement> Elements = new List<MStringElement>();
    }

    public class MXGString
    {
        public string Name = "";
        public List<GStringElement> Elements = new List<GStringElement>();
    }



    public class MStringElement
    {
        public DTMPoint Coord = new DTMPoint();
        public double Chainage { get; set; }
        public double Bearing { get; set; }
        public double Radius { get; set; }
    }

    public class GStringElement
    {
        public DTMPoint Coord = new DTMPoint();
        public double Chainage { get; set; }
        public double Bearing { get; set; }
        public double Radius { get; set; }
        public double Grade{ get; set; }

        public double Mval { get; set; }  //MVal = 10000 / vertical radius  
        public string HCode { get; set; } //*4
        public string VCode { get; set; } //*4
    }

    //Save this for later
    public class MXTripplet
    {
        public MXGENIOImporter.GENIO_FORMAT_TYPE Format { get; set; }
        public double Value1 { get; set; }
        public double Value2 { get; set; }
        public double Value3 { get; set; }

        public bool ParsedOK = false;

        public MXTripplet(MXGENIOImporter.GENIO_FORMAT_TYPE format, string Inputline)
        {
            Format = format;
            if (Parse(Inputline))
            {
               ParsedOK = true;
            };
        }


        public bool Parse(string InputLine)
        {
            try
            {
                if (this.Format == MXGENIOImporter.GENIO_FORMAT_TYPE.MX_12D || this.Format == MXGENIOImporter.GENIO_FORMAT_TYPE.MX_6D || this.Format == MXGENIOImporter.GENIO_FORMAT_TYPE.MX_5D || this.Format == MXGENIOImporter.GENIO_FORMAT_TYPE.MX_3D || this.Format == MXGENIOImporter.GENIO_FORMAT_TYPE.MX_TRIANGLES)
                {
                    string ps = InputLine.Replace("D", "E");

                    if (double.TryParse(ps.Substring(0, 23), out double v1) && double.TryParse(ps.Substring(23, 23), out double v2) && double.TryParse(ps.Substring(46, 23), out double v3))
                    {
                        Value1 = v1; Value2 = v2; Value3 = v3;
                        return true;
                    }
                }
                else if (this.Format == MXGENIOImporter.GENIO_FORMAT_TYPE.NRG_12D)
                {
                    string ps = InputLine.Replace("D", "E");

                    if (double.TryParse(ps.Substring(0, 12), out double v1) && double.TryParse(ps.Substring(12, 12), out double v2) && double.TryParse(ps.Substring(24, 12), out double v3))
                    {
                        Value1 = v1; Value2 = v2; Value3 = v3;
                        return true;
                    }

                }
                else if (this.Format == MXGENIOImporter.GENIO_FORMAT_TYPE.NRG_3D)
                {
                    string ps = InputLine;
                    //if (ps.le)
                    if (double.TryParse(ps.Substring(0, 12), out double v1) && double.TryParse(ps.Substring(12, 12), out double v2) && double.TryParse(ps.Substring(24,12 ), out double v3))
                    {
                        Value1 = v1; Value2 = v2; Value3 = v3;
                        return true;
                    }

                }
            }
            catch (Exception ex)
            {
                string errorMessage = "An error ocurred while attempting to parse point / coordinate data from the GENIO file." + System.Environment.NewLine + System.Environment.NewLine + "The error:" + ex.Message + " was thrown processing following data string:" + System.Environment.NewLine + System.Environment.NewLine + InputLine + " This coordinate will be ignored";
                System.Windows.Forms.MessageBox.Show(errorMessage, "Input Data Error", System.Windows.Forms.MessageBoxButtons.OK);
            }
            return false;
        }

    }

}
public class IndexedTriangle 
{
    public string Point1 { get; set; }
    public string Point2 { get; set; }
    public string Point3 { get; set; }
    public int Surface { get; set; }
    public IndexedTriangle(string pt1, string pt2, string pt3, int surface)
    {
        this.Point1 = pt1;
        this.Point2 = pt2;
        this.Point3 = pt3;
        this.Surface = surface;
    }

}

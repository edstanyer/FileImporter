using C1.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SiteSurvey;

namespace FileImporter
{


    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("FileImporter.GeomaxImporter")]
    public class GeomaxImporter : ImporterInterop
    {
        public enum TargetType
        {
            Prism = 0,
            ReflectorLess = 1,
            Tape = 2
        }

        public enum AimingMode
        {
            None = 0,
            Manual = 1,
            ATR = 2,
            Lock = 3
        }

        public enum CoordinateOptions
        { 
            General = 0,
            GPF = 1
        }

        private const string FORMAT_ERR = "The file appears to be in the incorrect format. Please check your data and retry this operation.";

        private const string FILE_INCOMPLETE = "Data in file appears incomplete and no usable information can be imported.";

        private FileHeader Header = new FileHeader();

        private List<TopoGraphicPoint> TopoGraphicPoints = new List<TopoGraphicPoint>();

        private IDictionary<string, TopoGraphicPoint> ControlStations = new Dictionary<string, TopoGraphicPoint>();

        private List<StationSetup> Setups = new List<StationSetup>();

        private List<PolarObservation> PolarOberservations = new List<PolarObservation>();

        private void WritePointsToGPF(string CoordDirectory,string filename)
        {

            if (!filename.ValidFile())
            { 
                

              
                SaveFileDialog sfd = new SaveFileDialog();
                if (Directory.Exists(CoordDirectory))
                {
                    sfd.InitialDirectory = CoordDirectory;  

                }
                sfd.Filter = "Ground Plot Files (*.gpf) | *.gpf";
            }
        }

        public bool ImportGeomaxRAW(string FileName = "", string CoordDirectory, string ObsDirectory)
        {
          

            


            if (!FileName.ValidFile())
            {

                OpenFileDialog ofd = new OpenFileDialog();

                ofd.Filter = ofd.Filter = "Geomax Raw Data Files|*.raw;*.raw;*.raw";
                string initDir = Settings.Default.GeomaxImportDirectory;
                if (Directory.Exists(initDir))
                {
                    ofd.InitialDirectory = initDir;
                }
                if (ofd.ShowDialog() != DialogResult.OK)
                {
                    MessageBox.Show("File selection cancelled.", "File Open", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                else
                {
                    FileName = ofd.FileName;
                    initDir = Path.GetDirectoryName(FileName);
                    Settings.Default.GeomaxImportDirectory = initDir;
                }
            }

            if (!FileName.ValidFile() == true)
            {
                return false;
            }
            List<string> lines;

            #region Open the file and check we have something remotely valid to parse
            try
            {
                lines = File.ReadAllLines(FileName).ToList();
            }
            catch (Exception e)
            {
                //Oops, that didn't work
                MessageBox.Show("An error occured while attempting to read the file." + Environment.NewLine + "Error Description: " + e.Message + Environment.NewLine + "Please check the file is not open in another application and retry this operation.", "Geomax Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;

            }
            if (lines.Count() == 0)
            {
                //empty file? Unlikely but plead not guilty in true NRG fashion.
                MessageBox.Show("There was no data found in the file. Please check the file has not become corrupt and retry this operation.", "Geomax Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            #endregion Open the data

            #region parse


            //I think header has to be the first line in the file and is obligitory according to the manual

            if (Header.ParseHeader(lines[0]))
            {

                lines.RemoveAt(0); //Finished with this now, so remove to make later parsing easier.
            }
            else
            {
                MessageBox.Show(FORMAT_ERR, "Error Reading Header", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (lines.Count < 1)
            {
                MessageBox.Show(FILE_INCOMPLETE, "GeoMax Raw Import");
                return false;
            }

            //read the rest in a loop...

            StationSetup setup = null;
            TopoGraphicPoint pt = new TopoGraphicPoint();
            PolarObservation obs = new PolarObservation();
            foreach (string line in lines)
            {
                string ln = line.ToUpper();//trivial, but makes parsing more certain

                switch (ln.Substring(0, 3))
                {

                    case "PNT":
                        pt = new TopoGraphicPoint();
                        if (pt.ParseTopoPoint(ln))
                        {
                            TopoGraphicPoints.Add(pt);
                            if (pt.PointClass == "TPS_STATION")
                            {
                                if (!ControlStations.ContainsKey(pt.PointNumber))
                                {
                                    ControlStations.Add(pt.PointNumber, pt);
                                }
                            }

                        }
                        break;
                    case "TPS"://total station observation
                        
                        obs = new PolarObservation();
                        obs.ParseObs(ln);
                        if (Setups != null && Setups.Count > 0 && obs != null)
                        { 
                            setup = Setups.Last();
                            if (setup != null) 
                            {
                                setup.DetailShots.Add(obs);
                                
                            }
                        }

                        break;
                    case "STB":

                        OccupiedPosition occ = new  OccupiedPosition(line);

                        TopoGraphicPoint op = FindPoint(occ.PointNumber);

                        if (op != null)
                        {
                            occ.Easting = op.Easting;
                            occ.Northing = op.Northing;
                            occ.Level = op.Level;
                            setup = new StationSetup(occ);
                            Setups.Add(setup);
                        }




                        break;

                }
            }
            #endregion parse

            

            //finally report info to the user...
            GeomaxResultsDlg dlg = new GeomaxResultsDlg();

            dlg.AddResultText("General File Information:");
            dlg.AddResultText("File Version - " + Header.FileVersion);
            dlg.AddResultText("File Date - " + Header.Date);
            dlg.AddResultText("File Time - " + Header.Time);
            dlg.AddResultText("Survey Crew - " + Header.Crew);
            dlg.AddResultText("Notes - " + Header.Notes);
            dlg.AddResultText("");
            dlg.AddResultText("Control Stations:");

            foreach (KeyValuePair<string, TopoGraphicPoint> entry in ControlStations)
            {
                dlg.AddResultText(entry.Key + " - " + "Easting:" + entry.Value.Easting.ToString("0.000") + " Northing:" + entry.Value.Northing.ToString("0.000") + "Level:" + entry.Value.Level.ToString("0.000"));
            }

            dlg.AddResultText("");

            if (TopoGraphicPoints != null && TopoGraphicPoints.Count > 0)
            {
                dlg.AddResultText("Topographic Points:");

                foreach (TopoGraphicPoint p in TopoGraphicPoints)
                {
                    dlg.AddResultText(p.ToString(CoordinateOptions.General));
                }


            }

            dlg.AddResultText("");
            int i = 0;

            dlg.AddResultText("RAW Observations:");


            if (Setups != null && Setups.Count > 0)
            {
                foreach (StationSetup ss in Setups)
                {
                    i++;

                    dlg.AddResultText("Setup " + i.ToString());
                    dlg.AddResultText("Occupied: " + ss.Occupied.PointNumber);
                    foreach (PolarObservation o in ss.DetailShots)
                    {
                        dlg.AddResultText("Raw Observation: " + o.PointNumber);
                        dlg.AddResultText("Raw Observation: " + o.ToString());
                    }


                }
            }

            dlg.ShowDialog();
            return true;

        }

        /// <summary>
        //    /// FindPoint added by ES:31.12.22. Find a match to specified point number, first in control stations then failing that in the Topo point coordinates
        //    /// </summary>
        //    /// <param name="PointNumber point number to find,"></param>
        //    /// <returns><matching point if point number found, false otheriwse</returns>
        
        private TopoGraphicPoint FindPoint(string PointNumber)
        {
            TopoGraphicPoint pt = null;

            //easy one first (and probably the most likely result) - check the control stations list...
            if (ControlStations != null)
            {
                if (ControlStations != null && ControlStations.Count > 0)
                {
                    if (ControlStations.TryGetValue(PointNumber, out pt))
                    {
                        return pt;
                    }
                }

            }

            //nope, nothing, try the points list. This will have to be a simple iteration as it's just a list"

            foreach (TopoGraphicPoint p in TopoGraphicPoints)
            {
                if (PointNumber.Trim().ToUpper() == p.PointNumber.Trim().ToUpper())
                {
                    //got match
                    return p;
                }
            }

            //found nothing so return null and leave it to caller to deal with
            return null;
        }
        
        #region polar observation class - total station data with polar HA, VA, SD coords
        private class PolarObservation
        {
            public string Date { private set; get; }
            public string Time { private set; get; }
            public double InstrumentHeight { private set; get; }
            public string StationName { private set; get; }
            public string PointNumber { private set; get; } //refered to as point name in the documentation, but seems to be akin to point number. So for consistency's sake... 
            public string Code { private set; get; }

            public double HorizontalAngle { private set; get; }
            public double VerticalAngle { private set; get; }
            public double SlopeDistance { private set; get; }

            public double TargetHeight { private set; get; }
            public TargetType Reflector { private set; get; }
            public double PrismConstant { private set; get; }   
            public AimingMode Aiming { private set; get; }
            public string Description { private set; get; }
            public bool ParseObs(string Line)
            {
                if (!String.IsNullOrWhiteSpace(Line) && Line.Length > 0)
                {
                    List<string> fields = Line.Split(',').ToList();
                    foreach (string f in fields)
                    {
                        if (f.Length > 2)
                        {
                            switch (f.ToUpper().Substring(0, 2))
                            {
                                case "DT":
                                    Date = f.Substring(2);
                                    break;
                                case "HM":
                                    Time = f.Substring(2);
                                    break;
                                case "IH":
                                    InstrumentHeight = f.Substring(2).ToDouble();
                                    break;
                                case "ST":
                                    StationName= f.Substring(2);
                                    break;
                                case "PT":
                                    PointNumber = f.Substring(2);
                                    break;
                                case "CD":
                                    Code= f.Substring(2);
                                    break;
                                case "HA":
                                    HorizontalAngle = f.Substring(2).ToDouble();
                                    break;
                                case "VA":
                                    VerticalAngle= f.Substring(2).ToDouble();   
                                    break;
                                case "SD":
                                    SlopeDistance= f.Substring(2).ToDouble();   
                                    break;
                                case "TH":
                                    TargetHeight= f.Substring(2).ToDouble();    
                                    break;
                                case "RT":
                                    Reflector = (TargetType)f.Substring(2).ToInt();
                                    break;
                                case "PK":
                                    PrismConstant= f.Substring(2).ToDouble();
                                    break;
                                case "AM":
                                    Aiming = (AimingMode)f.Substring(2).ToInt(); 
                                    break;
                                case "NT":
                                    Description = f.Substring(2).ToString();    
                                    break;
                            }
                        }
                    }
                    return true;
                }
                return false;
            }

            public string ToString()
            {
                return "";
            }
        }
        #endregion polar observation class

       
        #region TopoGraphic Point class - general survey stuff with cartesian coords.
        /// <summary>
        /// Survey point - position is cartesian (X,Y,Z) no raw obs.
        /// </summary>
        private class TopoGraphicPoint
        {

            public string PointNumber { private set; get; }
            public string Code{ private set; get; }
            public double Easting{ private set; get; }
            public double Northing{ private set; get; }
            public double Level{ private set; get; }
            public string PointClass{ private set; get; }
            public string Description { private set; get; }
            public string Date { private set; get; }
            public string Time{ private set; get; }

            public string ToString(CoordinateOptions opts)
            {
                string txt = "";

                if (opts == CoordinateOptions.General)
                {
                    txt =  PointNumber + "," + Easting.ToString("0.000")+ "," + Northing.ToString("0.000") + "," + Level.ToString("0.000") + "," + Code + "," + "Class: " + PointClass +  "," +  "Description: " + Description + "," +"Date :" + Date + "," + "Time: " + Time;
                }

                return txt;
            }



            public bool ParseTopoPoint(string Line)
            {
                if (!String.IsNullOrWhiteSpace(Line) && Line.Length > 0)
                {
                    List <string> fields = Line.Split(',').ToList();
                    foreach (string f in fields)
                    { 
                        if (f.Length > 2)
                        {
                            switch (f.ToUpper().Substring(0, 2))
                            {
                                case "NM":
                                    PointNumber= f.Substring(2).ToUpper(); 
                                    break;
                                case "CD":
                                    Code = f.Substring(2).ToUpper();
                                    break;
                                case "ET":
                                    Easting = f.Substring(2).ToDouble();
                                    break;
                                case "NO":
                                    Northing = f.Substring(2).ToDouble();
                                    break;
                                case "EL":
                                    Level = f.Substring(2).ToDouble();
                                    break;
                                case "TP"://point class? documentation is unclear on what this is
                                    PointClass = f.Substring(2);    
                                    break;
                                case "NT": //description? although NT is used everywhere else for notes - same diffence I suppose!
                                    Description = f.Substring(2);   
                                    break;
                                case "DT":
                                    Date= f.Substring(2);
                                    break;
                                case "HM":
                                    Time= f.Substring(2);   
                                    break;
                            }
                        }
                    }
                    return true;
                }
                return false;

            }

        }
        #endregion TopoGraphic Point class


        #region file header / general job information
        /// <summary>
        /// General GeoMax job data - first line in JOB file. Has to be there otherwise da file format police is gonna get you!
        /// </summary>
        private class FileHeader
        {
            public string FileVersion { private set;  get; } 
            public string Date { private set; get; }
            public string Time { private set; get; }
            public string JobName { private set; get;}
            public string Crew { private set; get; }
            public string Notes { private set; get; }

            public bool ParseHeader(string Line)
            {
                if (!String.IsNullOrWhiteSpace(Line) && Line.Length >0) //standard validity checks
                {
                    Line = Line.ToUpper();//trivial, but makes parsing more certain

                    string[] fields = Line.Split(',');

                    if (fields.Length >= 1)
                    {

                        if (fields[0] == "JOB")
                        {
                            //Looks like a duck, quacks like a duck...etc.
                            foreach (string f in fields)
                            {
                                switch (f.Substring(0, 2))
                                {
                                    case "FV":
                                        FileVersion = f.Substring(2);
                                        break;

                                    case "DT":
                                        Date = f.Substring(2);
                                        break;

                                    case "HM":
                                        Time = f.Substring(2);
                                        break;
                                    case "NM":
                                        JobName = f.Substring(2);
                                        break;
                                    case "CR":
                                        Crew = f.Substring(2);
                                        break;
                                    case "NT":
                                        Notes = f.Substring(2);
                                        break;

                                }
                            }
                            return true;
                        }

                    }
                }

                return false;
            }
        }
        #endregion file header / general job information

        private class StationSetup
        {
            public OccupiedPosition Occupied{ private set; get; }
            public TopoGraphicPoint Reference{ private set; get; }
            public List<PolarObservation> DetailShots = new List<PolarObservation>();

            public StationSetup(OccupiedPosition occ)
            {
                Occupied = occ;

            }
        }

        private class OccupiedPosition
        {
            public string PointNumber = "";
            public double Easting = 0;
            public double Northing = 0;
            public double Level = 0;
            public double InstrumentHeight = 0;// instrument height for occupied, target height for referce

            public OccupiedPosition(string Line)
            {
                if (!String.IsNullOrWhiteSpace(Line) && Line.Length > 0)
                {
                    List<string> fields = Line.Split(',').ToList();
                    if (fields.Count > 5)
                    {
                        foreach (string f in fields)
                        {
                            if (f.ToUpper().Trim() == "STB")
                            {
                                PointNumber = fields[4].Substring(2).ToUpper().Trim();
                                InstrumentHeight = fields[5].ToDouble();
                                
                            }
                        }
                    }
                }
            }
        }
 
    }
}

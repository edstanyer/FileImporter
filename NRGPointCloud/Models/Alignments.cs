using NRG.Import;
using NRG.MathsHelpers;
using NRG.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ComponentModel;


namespace NRG.Models

{

	


	//Code is taken from Charles DTX with some minor adjustments made by WA.

	public enum ProfileType
    {
        Road,
        Rail
    }

    public enum NstType
    {
        NONE,
        HORIZONTAL,
        LEFTVERTICAL,
        RIGHTVERTICAL,
        VERTICALPOSITION,
        CANTS,
        LEFTCARRIAGEWAY,
        RIGHTCARRIAGEWAY
    }

    public enum HorizontalElementType
    {
        Straight,
        Arc,
        Spiral
    }

    public enum VerticalElementType
    {
        Straight,
        Parabola
    }

    public class AlignmentProperties
    {



        [CategoryAttribute("General File Info"), DescriptionAttribute("Individual model name within the file")]
        public string AlignmentName { get; set; }
        [CategoryAttribute("General File Info"), DescriptionAttribute("File imported")]
        public string SourceFile { get; set; }
        [CategoryAttribute("General File Info"), DescriptionAttribute("Source file type")]
        public string SourceFileType { get; set; }
        [CategoryAttribute("Entity Info"), DescriptionAttribute("Number horizontal elements")]
        public string HorizontalElements { get; set; }
        [CategoryAttribute("Entity Info"), DescriptionAttribute("Horizontal length")]
        public string HorizontalLength { get; set; }
        //[CategoryAttribute("Entity Info"), DescriptionAttribute("Number of triangles in model")]
        //public string TriangleCount { get; set; }
        [CategoryAttribute("Bounds"), DescriptionAttribute("Minimum X")]
        public string MinX { get; set; }
        [CategoryAttribute("Bounds"), DescriptionAttribute("Minimum Y")]
        public string MinY { get; set; }
        [CategoryAttribute("Bounds"), DescriptionAttribute("Maximum X")]
        public string MaxX { get; set; }
        [CategoryAttribute("Bounds"), DescriptionAttribute("Maximum Y")]
        public string MaxY { get; set; }
    }

    public static class AlignmentHelper
    {

       

        /// <summary>
        /// FindNearestAlignment added by ES:05.06.22
        /// Given a list of NRG alignment and coorinate finds the closest alignment in terms of offset of the coordinate represented by X & Y
        /// </summary>
        /// <param name="X">X coordinate of the query point</param>
        /// <param name="Y">Y coordinate of the query point</param>
        /// <param name="alignments"> list of NRG alignments to search</param>
        /// <param name="Chainage">If an alignment is found then Chainage on that alignment of the query point</param>
        /// <param name="Offset">If an alignment is found then Offset from that alignment of the query point</param>
        /// <returns>the closest alignment to the query point if one is found. If an alignment isn't found you get null</returns>
        public static Alignment FindNearestAlignment(double X, double Y, List<Alignment> alignments, out double Chainage, out double Offset)
        {

            Offset = double.PositiveInfinity;

            double ch = 0;
            double offs = Offset;

            if (alignments != null && alignments.Count >0)
            {
                Alignment ao = null;
                foreach(Alignment alg in alignments)
                {
                    AlignmentVector5 cao = new AlignmentVector5();
                    if (alg.Horizontal.GetChainageAndOffset(X, Y, ref cao))
                    {
                        if (Math.Abs(cao.offset) < Math.Abs(offs))
                        {
                            ch= cao.chainage;
                            offs = cao.offset;
                            ao = new Alignment();   
                            ao = alg;
                        }

                            
                    }
                }
                if (ao != null && ao.Horizontal != null && ao.Horizontal.Elements.Count > 0)
                {
                    Chainage = ch; Offset = offs;
                    return ao;
                }
            }

            //nothing found
            Chainage = 0; Offset = 0;
            return null;
        }

        /// <summary>
        /// FindNearestAlignment added by ES:05.06.22
        /// Overides the above if the call isn't worried about having the chainage and offset
        /// </summary>
        /// <param name="X">X coordinate of the query point</param>
        /// <param name="Y">Y coordinate of the query point</param>
        /// <param name="alignments"> list of NRG alignments to search</param>
        /// <returns>the closest alignment to the query point if one is found. If an alignment isn't found you get null</returns>

        public static Alignment FindNearestAlignment(double X, double Y, List<Alignment> alignments)
        { 
            double ch = 0, offs = double.PositiveInfinity;
            return FindNearestAlignment(X, Y, alignments, out ch, out offs);
        }


        /// <summary>
        /// CreateAlignmentList - added by ES:06.06.22
        /// Can't find another of these that is very easy to use - call this to return a list containing all the project alignments
        /// </summary>
        /// <returns>List of alignments from the project if there are any or NULL if there's none</returns>
        public static List<Alignment> CreateAlignmentList(string projectPath)
        {
            if (Directory.Exists(projectPath) == false) { return null; }

            List<Alignment> alignments = new List<Alignment>();

            List<string> alignmentList = Helpers.GetAlignmentList(projectPath + "\\" + "Alignment Strings" + "\\");


            foreach (string algstr in alignmentList)
            {
                Alignment alg = new Alignment();
                alg.LoadNST(projectPath + "\\" + "Alignment Strings" + "\\" + algstr + "\\" + algstr + ".NST");
                alignments.Add(alg);

            }

            if (alignments != null && alignments.Count > 0)
            {
                return alignments;
            }
            else
            {
                return null;
            }
        
        }

        /// <summary>
        /// CalcOrigins - Calulates the geometric origins of an alignment given in coordinates
        /// Needed for coordinate / chainage calculations. 
        /// </summary>
        /// <param name="element"></param> Horizontal element of the alignment you wish to use 
        /// <param name="nextElement"></param> Following element is needed for Transition / spiral calculations
        public static void CalcOrigins(ref HorizontalElement element, HorizontalElement nextElement)
        {
            double brg = 0, chord = 0;
            bool retry = false;
            double dist = 0;

            do
            {


                retry = false;
                if (element.ElementType == HorizontalElementType.Straight)
                {
                    element.ChainageOrigin = element.Chainage;
                    element.RL = 0;
                    element.RadiusOrigin = 0;
                    double bo = 0, el = 0;
                    Trig.RPC(element.East, element.North, nextElement.East, nextElement.North, ref bo, ref el, 1);
                    element.BrgOrigin = bo;
                    element.Length = el;
                    element.EastOrigin = element.East;
                    element.NorthOrigin = element.North;
                }
                else if (element.ElementType == HorizontalElementType.Arc)
                {

                    //This arc stuff has been significantly changed by ES:08.11.22 - the orginal is in the original stuff region below
                    //but to be honest this is mostly not needed as the pure geometry is generally read in from file anyhow
                    //I the think there was a general misunderstanding when this was implemented.

                    //This coded at least allows this app and original NRG software to sing from the same hymm sheet
                    element.ChainageOrigin = element.Chainage;
                    element.RadiusOrigin = element.Radius;
                    element.RL = 0;
                    
                    
                    

                    double cx = 0,  cy = 0;
                    if (element.CenterPointSet)
                    {
                        

                            cx = element.CenterEast;
                        cy = element.CenterNorth;
                    }
                    else
                    {
                        Trig.RPC(element.East, element.North, nextElement.East, nextElement.North, ref brg, ref chord, 1);
                        if (MathsHelpers.Vector.FnEquals(chord, 2 * Math.Abs(Math.Abs(element.Radius))))
                        {
                            Trig.PRC(element.East, element.North, brg, Math.Abs(element.Radius), ref cx, ref cy);
                        }
                        else
                        {
                            MathsHelpers.Trig.CalcCenterOfArc2(element.East, element.North, nextElement.East, nextElement.North, element.Radius, ref cx, ref cy);
                        }
                    }
                    double en = 0, ex = 0, len = 0;
                    MathsHelpers.Trig.CalcEntryAndExitBrg(element.East, element.North, nextElement.East, nextElement.North, cx, cy, element.Radius, ref en, ref ex, ref len);
                    element.BrgOrigin = en;
                    element.Length = len;
                    element.EastOrigin = element.East;
                    element.NorthOrigin = element.North;


                    #region comment out original
                    //}
                    //else if (chord > 2 * Math.Abs(element.Radius))
                    //{
                    //    element.BrgOrigin = brg;
                    //    element.Length = chord;
                    //    element.RadiusOrigin = 0;
                    //}
                    //else
                    //{
                    //    double deflect = Math.Asin(chord / (2 * element.Radius));
                    //    element.BrgOrigin = brg - deflect;
                    //    element.Length = 2 * deflect * element.Radius;

                    //}
                    #endregion commented out original code



                }
                else //spiral
                {
                    if (Math.Abs(nextElement.Radius) < 0.0001) { nextElement.Radius = 1E+20; }
                    if (Math.Abs(element.Radius) < 0.0001) { element.Radius = 1E+20; }


                    //===========================================================================================
                    //Added by ES:14.11.22
                    if (element.RL != 0)
                    {
                        element.Length = Math.Abs(element.RL / element.Radius);
                    }
                    //===========================================================================================

                    if (Vector.FnEquals(nextElement.Radius, element.Radius))
                    {
                        element.ElementType = HorizontalElementType.Arc;
                        retry = true;
                    }
                    else if (Vector.FnEquals(element.Length, 0))
                    {
                        element.RadiusOrigin = 0; element.RL = 0; element.BrgOrigin = 0; element.EastOrigin = element.East; element.NorthOrigin = element.North; element.ChainageOrigin = element.Chainage;
                    }
                    else
                    {

                        

                        double L2 = -element.Length * element.Radius / (element.Radius - nextElement.Radius);
                        double L1 = L2 + element.Length;
                        if (L2 != 0)
                        {
                            element.RL = Math.Abs(L2 * nextElement.Radius);
                        }
                        else
                        {
                            element.RL = Math.Abs(L1 * element.Radius);
                        }

                        double Yb = L2 * L2 / (6D * nextElement.Radius) - L2 * L2 * L2 * L2 / (336D * nextElement.Radius * nextElement.Radius * nextElement.Radius) + L2 * L2 * L2 * L2 * L2 * L2 / (42240D * nextElement.Radius * nextElement.Radius * nextElement.Radius * nextElement.Radius * nextElement.Radius) - L2 * L2 * L2 * L2 * L2 * L2 * L2 * L2 / (9676800D * nextElement.Radius * nextElement.Radius * nextElement.Radius * nextElement.Radius * nextElement.Radius * nextElement.Radius * nextElement.Radius);
                        double Ya = L1 * L1 / (6D * element.Radius) - L1 * L1 * L1 * L1 / (336D * element.Radius * element.Radius * element.Radius) + L1 * L1 * L1 * L1 * L1 * L1 / (42240D * element.Radius * element.Radius * element.Radius * element.Radius * element.Radius) - L1 * L1 * L1 * L1 * L1 * L1 * L1 * L1 / (9676800D * element.Radius * element.Radius * element.Radius * element.Radius * element.Radius * element.Radius * element.Radius);
                        double Yab = Yb - Ya;
                        Trig.RPC(element.East, element.North, nextElement.East, nextElement.North, ref brg, ref dist, 1);
                        double Angle = Math.Asin(Yab / dist);
                        element.BrgOrigin = brg - Angle;
                        double X = L2 - Math.Pow(L2, 3) / (40D * Math.Pow(nextElement.Radius, 2)) + Math.Pow(L2, 5) / (3456D * Math.Pow(nextElement.Radius, 4)) - Math.Pow(L2, 7) / (599040D * Math.Pow(nextElement.Radius, 6));
                        double Y = Math.Pow(L2, 2) / (6D * nextElement.Radius) - Math.Pow(L2, 4) / (336D * Math.Pow(nextElement.Radius, 3)) + Math.Pow(L2, 6) / (42240D * Math.Pow(nextElement.Radius, 5)) - Math.Pow(L2, 8) / (9676800D * Math.Pow(nextElement.Radius, 7));
                        double e = Math.Sin(element.BrgOrigin) * X; double n = Math.Cos(element.BrgOrigin) * X;
                        double e2 = Math.Cos(element.BrgOrigin) * Y; double n2 = Math.Sin(element.BrgOrigin) * Y;
                        element.EastOrigin = nextElement.East + e - e2;
                        element.NorthOrigin = nextElement.North + n + n2;
                        element.RadiusOrigin = element.Radius;
                        if (Math.Abs(element.RadiusOrigin) > 100000000) { element.RadiusOrigin = 0; }
                        element.ChainageOrigin = element.Chainage + L2 + element.Length;
                    }
                }
            } while (retry == true);
        }


        public static void CalcBearingErrors(Alignment alg)
        {
            if (alg == null || alg.Horizontal == null || alg.Horizontal.Elements == null || alg.Horizontal.Elements.Count == 0) { return; }

            for (int i = 0; i < alg.Horizontal.Elements.Count - 1; i++)
            {

                HorizontalElement currentElement = alg.Horizontal.Elements[i];
                HorizontalElement nextElement = alg.Horizontal.Elements[i + 1];

                double x1 = currentElement.EastOrigin;
                double y1 = currentElement.NorthOrigin;
                double x2 = nextElement.EastOrigin;
                double y2 = nextElement.NorthOrigin;

                double eb = 0;
                double exb = 0;
                double length = 0;

                //Find exit bearing of the current element
                double currentExitBearing = 0;
                switch (currentElement.ElementType)
                {

                    case HorizontalElementType.Straight:
                        exb = NRG.MathsHelpers.Trig.RPC(x1, y1, x2, y2);
                        break;
                    case HorizontalElementType.Arc:
                        NRG.MathsHelpers.Trig.CalcEntryAndExitBrg(x1, y1, x2, y2, currentElement.CenterEast, currentElement.CenterNorth, currentElement.Radius, ref eb, ref exb, ref length);
                        break;
                    case HorizontalElementType.Spiral:
                        double tang = 0;
                        //alg.Horizontal.CoordsAtChAndOffset(nextElement.Chainage, 0, out tang);
                        alg.Horizontal.CoordsAtChAndOffset(nextElement.Chainage - 0.001, 0, out tang);
                        
                        exb = tang;
                        break;
                }
                currentExitBearing = exb;

                //Find the entry bearing of the next element
                double nextEntryBearing = 0;
                switch (nextElement.ElementType)
                {
                    case HorizontalElementType.Straight:
                        eb = nextElement.BrgOrigin;
                        break;
                    case HorizontalElementType.Arc:
                        eb = nextElement.BrgOrigin;
                        break;
                    case HorizontalElementType.Spiral:
                        eb = nextElement.BrgOrigin;
                        break;
                }
                nextEntryBearing = eb;

                //get the difference
                double brgError = NRG.MathsHelpers.Trig.FnSmallAngle(currentExitBearing, nextEntryBearing);

                //set that as current element BearingError
                nextElement.BrgError = brgError;
                
            }
        }

    }

    public class Alignment
    {
        #region Properties

        public string StringName = "";
        public string StringFile = "";
        public List<Point3D> Coords = new List<Point3D>();
        public HorizontalAlignment Horizontal { get; set; }
        public VerticalAlignment Vertical { get; set; }


        public CollectionOfAllChainageMarkers MarkerCollection = new CollectionOfAllChainageMarkers();


        public RailwayCant RailwayProfile { get; set; }
        public RoadCrossFall RoadProfile { get; set; }
        public ProfileType ProfileType { get; set; }
        public Rendering.SharedRenderItem RenderItem { get; set; }
        public Point3D ModelOffset { get; set; }
        public AlignmentProperties Properties = new AlignmentProperties();


        public Bounds HorzontalBounds =  null;
        #endregion

        #region Setup

        public Alignment()
        {
            Horizontal = new HorizontalAlignment();
            Vertical = new VerticalAlignment();
            RailwayProfile = new RailwayCant();
            RoadProfile = new RoadCrossFall();
        }

        public Alignment(MXGString GString)
        {
            Horizontal = new HorizontalAlignment();
            Vertical = new VerticalAlignment();
            RailwayProfile = new RailwayCant();
            RoadProfile = new RoadCrossFall();
            foreach (GStringElement ele in GString.Elements)
            {
                #region horizontal
                if (ele.HCode != "")
                {
                    if (ele.HCode.Substring(0, 3).ToUpper() != "CAL")
                    {
                        string ss = ele.HCode.Substring(0, 1).ToUpper();
                        if (ss == "P" || ss == "T" || ss == "C" || ss == "S")
                        {
                            //valid horizontal element
                            HorizontalElement hz = new HorizontalElement();
                            ss = ele.HCode.Substring(1, 1).ToUpper();

                            hz.Chainage = ele.Chainage;
                            hz.ChainageOrigin = ele.Chainage;
                            hz.BrgOrigin = ele.Bearing;
                            hz.East = ele.Coord.X;
                            hz.North = ele.Coord.Y;
                            hz.Radius = ele.Radius;
                            hz.EastOrigin = ele.Coord.X;
                            hz.NorthOrigin = ele.Coord.Y;
                            hz.RadiusOrigin = ele.Radius;

                            hz.ElementType = HorizontalElementType.Straight;
                            if (hz.Radius > 999999) { hz.Radius = 0; }
                            if (ss == "S")
                            {
                                hz.ElementType = HorizontalElementType.Spiral;
                            }
                            else if (hz.Radius != 0)
                            {
                                hz.ElementType = HorizontalElementType.Arc;
                            }


                            Horizontal.AddElement(hz);

                        }
                    }
                }
                #endregion horizontal

                #region vertical
                if (ele.VCode != "")
                {
                    string ss = ele.VCode.Substring(0, 1).ToUpper();
                    if (ss == "P" || ss == "T")
                    {
                        VerticalElement v = new VerticalElement();
                        v.Chainage = ele.Chainage;
                        v.Level = ele.Coord.Z;
                        if (Math.Abs(ele.Mval) < 0.01)
                            v.Radius = 0;
                        else
                            v.Radius = 10000 / ele.Mval;

                        Vertical.AddElement(v);

                    }
                }
                #endregion vertical




            }
            if (Horizontal != null)
            {
                if (Horizontal.Elements.Count > 1)
                {
                    for (int i = 0; i < Horizontal.Elements.Count - 2; i++)
                    {

                        HorizontalElement thisEle = Horizontal.Elements[i];
                        HorizontalElement nextEle = Horizontal.Elements[i + 1];
                        thisEle.Length = nextEle.Chainage - thisEle.Chainage;

                        AlignmentHelper.CalcOrigins(ref thisEle, nextEle);
                    }
                }
            }
            this.Horizontal.Elements.RemoveAt(this.Horizontal.Elements.Count - 1);
            this.StringName = GString.Name;
        }

        public double TotalLength()
        {
            if (Horizontal is null || Horizontal.Elements.Count < 2) { return 0; }
            HorizontalElement ele1 = Horizontal.Elements.ElementAt(0);
            HorizontalElement ele2 = Horizontal.Elements.Last();
            return ele2.Chainage - ele1.Chainage;

        }

        public Alignment(List<MStringElement> MString)
        {
            Horizontal = new HorizontalAlignment();
            Vertical = new VerticalAlignment();
            RailwayProfile = new RailwayCant();
            RoadProfile = new RoadCrossFall();

        }



        /// <summary>
        /// Converts the provided line into an alignment
        /// </summary>
        /// <param name="line"></param>
        public Alignment(Line line)
        {
            Horizontal = new HorizontalAlignment();
            Vertical = new VerticalAlignment();

            if (line != null && line.Points != null && line.Points.Count > 0)
            {
                //Add first point
                var firstPoint = line.Points.First();
                var hzElement = new HorizontalElement();
                var vtElement = new VerticalElement();

                //Horizontal Element
                hzElement.Chainage = 0;
                hzElement.East = firstPoint.X;
                hzElement.North = firstPoint.Y;
                hzElement.ElementType = HorizontalElementType.Straight;
                hzElement.Radius = 0;
                Horizontal.AddElement(hzElement);

                //Vertical Element
                vtElement.Chainage = 0;
                vtElement.Level = firstPoint.Z;
                vtElement.ElementType = VerticalElementType.Straight;
                vtElement.Radius = 0;
                Vertical.AddElement(vtElement);

                for (int i = 1; i < line.Points.Count; i++)
                {
                    var currentPoint = line.Points[i];
                    var lastHzElement = Horizontal.Elements.Last();
                    var lastVtElement = Vertical.Elements.Last();
                    hzElement = new HorizontalElement();
                    vtElement = new VerticalElement();

                    //Horzontal Element
                    hzElement.Chainage = lastHzElement.Chainage + Vector.FnDistance(lastHzElement.East, lastHzElement.North, currentPoint.X, currentPoint.Y);
                    hzElement.East = currentPoint.X;
                    hzElement.North = currentPoint.Y;
                    hzElement.ElementType = HorizontalElementType.Straight;
                    hzElement.Radius = 0;
                    Horizontal.AddElement(hzElement);

                    //Vertical Element
                    vtElement.Chainage = hzElement.Chainage;
                    vtElement.Level = currentPoint.Z;
                    vtElement.ElementType = VerticalElementType.Straight;
                    vtElement.Radius = 0;
                    Vertical.AddElement(vtElement);
                }
            }
        }


        public BindingList<DTMPoint> GetPoints(double interval = 0.1)
        {
            double startch = this.Horizontal.MinChainage;
            double endch = this.Horizontal.MaxChainage;
            BindingList<DTMPoint> outlist = new BindingList<DTMPoint>();

            for (double i = startch; i <= endch; i += interval)
            {

                Point2D pt = this.Horizontal.CoordsAtChAndOffset(i, 0);
                DTMPoint pttoadd = new DTMPoint();
                pttoadd.PointLabel = this.StringName.Replace(" ", "");
                if (i == startch)
                { pttoadd.PointLabel += "/ST"; }
                pttoadd.X = pt.X;
                pttoadd.Y = pt.Y;
                pttoadd.Z = -999;
                if (Vertical != null)
                {
                    pttoadd.Z = this.Vertical.GradeLevel(i);
                }
                outlist.Add(pttoadd);
            }
            return outlist;
        }

        /// <summary>
        /// loadFile - Reads the new alignment file format
        /// </summary>
        /// <param name="filename"></param> File path to read...duh
        public void LoadFile(string filename = "")
        {
            string txt = "";
            int i = 0;
            string[] Arr;
            string[] splitChars = { "=", "," };
            double elementParam = 0;
            HorizontalElement ele = new HorizontalElement();
            HorizontalElement thisEle = new HorizontalElement();
            HorizontalElement lastEle = new HorizontalElement();
            HorizontalElement nextEle = new HorizontalElement();
            VerticalElement vEle = new VerticalElement();

            if (filename == "")
            {
                filename = FileIO.LoadFileDialog("NAF");
            }

            if (filename != "" && File.Exists(filename))
            {
                StringFile = filename;
                var f = Path.GetFileName(filename);
                StringName = f.Substring(0, f.Length - 4);
                var asciiReader = FileIO.LoadAsciiToStringList(filename, false);
                for (i = 0; i < asciiReader.Count; i++)
                {
                    txt = asciiReader[i].ToString();
                    if (txt.Length > 2)
                    {
                        if (txt.Substring(0, 2) != "//")
                        {
                            Arr = txt.Split(splitChars, StringSplitOptions.None);
                            if (Arr[0].ToUpper() == "HORIZONTAL ELEMENT")
                            {
                                ele = new HorizontalElement();
                                if (Arr[1].ToUpper() == "STRAIGHT")
                                {
                                    ele.ElementType = HorizontalElementType.Straight;
                                }
                                else if (Arr[1].ToUpper() == "ARC")
                                {
                                    ele.ElementType = HorizontalElementType.Arc;
                                }
                                else if (Arr[1].ToUpper() == "SPIRAL")
                                {
                                    ele.ElementType = HorizontalElementType.Spiral;
                                }
                                if (double.TryParse(Arr[2], out elementParam)) { ele.Chainage = elementParam; }

                                if (double.TryParse(Arr[3], out elementParam)) { ele.East = elementParam; }
                                if (double.TryParse(Arr[4], out elementParam)) { ele.North = elementParam; }
                                if (double.TryParse(Arr[5], out elementParam)) { ele.Radius = elementParam; }
                                if (double.TryParse(Arr[6], out elementParam)) { ele.Length = elementParam; }
                                Horizontal.AddElement(ele);
                            }
                            else if (Arr[0].ToUpper() == "VERTICAL ELEMENT")
                            {
                                vEle = new VerticalElement();

                                if (Arr[1].ToUpper() == "STRAIGHT")
                                {
                                    vEle.ElementType = VerticalElementType.Straight;
                                }
                                else if (Arr[1].ToUpper() == "PARABOLA")
                                {
                                    vEle.ElementType = VerticalElementType.Parabola;
                                }

                                if (double.TryParse(Arr[2], out elementParam)) { vEle.Chainage = elementParam; }
                                if (double.TryParse(Arr[3], out elementParam)) { vEle.Level = elementParam; }
                                if (double.TryParse(Arr[4], out elementParam)) { vEle.Radius = elementParam; }
                                Vertical.AddElement(vEle);
                            }
                            else if (Arr[0].ToUpper() == "CANT")
                            {
                                CantElement cant = new CantElement();
                                if (double.TryParse(Arr[2], out elementParam)) { cant.Chainage = elementParam; }
                                if (double.TryParse(Arr[3], out elementParam)) { cant.Cant = elementParam; }

                            }


                        }
                    }
                }

                if (Horizontal != null)
                {
                    if (Horizontal.Elements.Count > 1)
                    {
                        for (i = 0; i < Horizontal.Elements.Count - 1; i++)
                        {
                            thisEle = Horizontal.Elements[i];
                            nextEle = Horizontal.Elements[i + 1];
                            AlignmentHelper.CalcOrigins(ref thisEle, nextEle);
                        }
                    }
                }
            }
        }

        public void LoadNAF(string filename = "")
        {
            string txt = "";
            int i = 0;
            string[] Arr;
            string[] splitChars = { "=", "," };
            double elementParam = 0;
            HorizontalElement ele = new HorizontalElement();
            HorizontalElement thisEle = new HorizontalElement();
            HorizontalElement lastEle = new HorizontalElement();
            HorizontalElement nextEle = new HorizontalElement();
            VerticalElement vEle = new VerticalElement();

            if (filename == "")
            {
                filename = FileIO.LoadFileDialog("NAF");
            }

            if (filename != "" && File.Exists(filename))
            {
                StringFile = filename;
                var f = Path.GetFileName(filename);
                StringName = f.Substring(0, f.Length - 4);
                var asciiReader = FileIO.LoadAsciiToStringList(filename, false);
                for (i = 0; i < asciiReader.Count; i++)
                {
                    txt = asciiReader[i].ToString();
                    if (txt.Length > 2)
                    {
                        if (txt.Substring(0, 2) != "//")
                        {
                            Arr = txt.Split(splitChars, StringSplitOptions.None);
                            if (Arr[0].ToUpper() == "HORIZONTAL ELEMENT")
                            {
                                ele = new HorizontalElement();
                                if (Arr[1].ToUpper() == "STRAIGHT")
                                {
                                    ele.ElementType = HorizontalElementType.Straight;
                                }
                                else if (Arr[1].ToUpper() == "ARC")
                                {
                                    ele.ElementType = HorizontalElementType.Arc;
                                }
                                else if (Arr[1].ToUpper() == "SPIRAL")
                                {
                                    ele.ElementType = HorizontalElementType.Spiral;
                                }
                                if (double.TryParse(Arr[2], out elementParam)) { ele.Chainage = elementParam; }

                                if (double.TryParse(Arr[3], out elementParam)) { ele.East = elementParam; }
                                if (double.TryParse(Arr[4], out elementParam)) { ele.North = elementParam; }
                                if (double.TryParse(Arr[5], out elementParam)) { ele.Radius = elementParam; }
                                if (double.TryParse(Arr[6], out elementParam)) { ele.Length = elementParam; }
                                Horizontal.AddElement(ele);
                            }
                            else if (Arr[0].ToUpper() == "VERTICAL ELEMENT")
                            {
                                vEle = new VerticalElement();

                                if (Arr[1].ToUpper() == "STRAIGHT")
                                {
                                    vEle.ElementType = VerticalElementType.Straight;
                                }
                                else if (Arr[1].ToUpper() == "PARABOLA")
                                {
                                    vEle.ElementType = VerticalElementType.Parabola;
                                }
                                if (double.TryParse(Arr[2], out elementParam)) { vEle.Chainage = elementParam; }
                                if (double.TryParse(Arr[3], out elementParam)) { vEle.Level = elementParam; }
                                if (double.TryParse(Arr[4], out elementParam)) { vEle.Radius = elementParam; }
                                Vertical.AddElement(vEle);
                            }
                        }
                    }
                }

                if (Horizontal != null)
                {
                    if (Horizontal.Elements.Count > 1)
                    {
                        for (i = 0; i < Horizontal.Elements.Count - 1; i++)
                        {
                            thisEle = Horizontal.Elements[i];
                            nextEle = Horizontal.Elements[i + 1];
                            AlignmentHelper.CalcOrigins(ref thisEle, nextEle);
                        }
                    }
                }
            }
        }

        public void LoadNST(string filename = "")
        {
            //Temp list to load old stuff into before calc
            List<NstHorizontalAlignment> horizontalList = new List<NstHorizontalAlignment>();
            List<NstVerticalAlignment> verticalList = new List<NstVerticalAlignment>();
            List<NstCant> cantList = new List<NstCant>();

            string txt = "";
            int i = 0;
            string[] splitChars = { "=", "," };
            double elementParam = 0;
            HorizontalElement ele = new HorizontalElement();
            HorizontalElement thisEle = new HorizontalElement();
            HorizontalElement lastEle = new HorizontalElement();
            HorizontalElement nextEle = new HorizontalElement();
            VerticalElement vEle = new VerticalElement();

            if (filename == "")
            {
                filename = FileIO.LoadFileDialog("NST");
            }

            if (filename != "" && File.Exists(filename))
            {
                StringFile = filename;
                var f = Path.GetFileName(filename);
                StringName = f.Substring(0, f.Length - 4);
                var asciiReader = FileIO.LoadAsciiToStringList(filename, false);
                NstType currentProcess = NstType.NONE;

                for (i = 0; i < asciiReader.Count; i++)
                {
                    txt = asciiReader[i].ToString().TrimEnd().TrimStart();

                    //Do if type is horizontal
                    if (!txt.Contains(@"[") && !txt.Contains(";") && currentProcess == NstType.HORIZONTAL)
                    {
                        var lineData = txt.Split(',');
                        FileIO.TrimStrings(ref lineData);
                        NstHorizontalAlignment hData = new NstHorizontalAlignment();

                        if (double.TryParse(lineData[0], out elementParam)) { hData.ChStart = elementParam; }
                        if (double.TryParse(lineData[1], out elementParam)) { hData.ChOrigin = elementParam; }
                        if (double.TryParse(lineData[2], out elementParam)) { hData.Easting = elementParam; }
                        if (double.TryParse(lineData[3], out elementParam)) { hData.Northing = elementParam; }
                        if (double.TryParse(lineData[4], out elementParam)) { hData.Bearing = elementParam; }
                        if (double.TryParse(lineData[5], out elementParam)) { hData.Radius = elementParam; }
                        if (double.TryParse(lineData[6], out elementParam)) { hData.Rl = elementParam; }

                        horizontalList.Add(hData);
                    }

                    if (!txt.Contains(@"[") && !txt.Contains(";") && currentProcess == NstType.LEFTVERTICAL)
                    {
                        var lineData = txt.Split(',');
                        FileIO.TrimStrings(ref lineData);
                        NstVerticalAlignment vData = new NstVerticalAlignment();

                        if (double.TryParse(lineData[0], out elementParam)) { vData.ChStart = elementParam; }
                        if (double.TryParse(lineData[1], out elementParam)) { vData.Level = elementParam; }
                        if (double.TryParse(lineData[2], out elementParam)) { vData.Radius = elementParam; }

                        verticalList.Add(vData);
                    }

                    if (!txt.Contains(@"[") && !txt.Contains(";") && currentProcess == NstType.CANTS)
                    {
                        //WA 13.05.19 updated to support new RailwayCant 
                        var lineData = txt.Split(',');

                        if (lineData.Length <= 2)
                            continue;

                        ProfileType = ProfileType.Rail;

                        FileIO.TrimStrings(ref lineData);

                        var cant = new CantElement();
                        if (double.TryParse(lineData[0], out elementParam))
                            cant.Chainage = elementParam;

                        if (double.TryParse(lineData[1], out elementParam))
                            cant.Cant = elementParam;
                        else
                            cant.Cant = 0;

                        RailwayProfile.AddCant(cant);

                        //var lineData = txt.Split(',');
                        //FileIO.TrimStrings(ref lineData);
                        //NstCant cData = new NstCant();

                        //if (double.TryParse(lineData[0], out elementParam)) { cData.ChStart = elementParam; }

                        //for (int idx = 1; idx < lineData.Length; idx++)
                        //{
                        //    if (double.TryParse(lineData[idx], out elementParam)) { cData.Cant.Add(elementParam); }
                        //}

                        //cantList.Add(cData);
                    }

                    //WA 14.05.19 Added to support road XFall
                    if (!txt.Contains(@"[") && !txt.Contains(";") && (currentProcess == NstType.LEFTCARRIAGEWAY || currentProcess == NstType.RIGHTCARRIAGEWAY))
                    {
                        var lineData = txt.Split(',');

                        if (lineData.Length <= 2)
                            continue;

                        ProfileType = ProfileType.Road;

                        FileIO.TrimStrings(ref lineData);

                        var xFallElement = new CrossFallElement();
                        if (double.TryParse(lineData[0], out elementParam))
                            xFallElement.Chainage = elementParam;

                        for (int j = 1; j < lineData.Length; j++)
                        {
                            if (double.TryParse(lineData[j], out elementParam))
                                xFallElement.Values.Add(elementParam);
                            else
                                xFallElement.Values.Add(0);
                        }

                        RoadProfile.AddElement(xFallElement, currentProcess == NstType.LEFTCARRIAGEWAY);
                    }

                    if (txt.Contains(@"["))
                        currentProcess = NstType.NONE;

                    if (!txt.Contains(";") && txt.Equals(@"[CANTS]"))
                    {
                        currentProcess = NstType.CANTS;
                    }

                    if (!txt.Contains(";") && txt.Equals(@"[LEFT VERTICAL ALIGNMENT]"))
                    {
                        currentProcess = NstType.LEFTVERTICAL;
                    }

                    if (!txt.Contains(";") && txt.Equals(@"[HORIZONTAL ALIGNMENT]"))
                    {
                        currentProcess = NstType.HORIZONTAL;
                    }

                    if (!txt.Contains(";") && txt.Equals(@"[VERTICAL ALIGNMENT POSITION]"))
                    {
                        currentProcess = NstType.NONE;
                    }

                    if (!txt.Contains(";") && txt.Equals(@"[LEFT CARRIAGEWAY]"))
                    {
                        currentProcess = NstType.LEFTCARRIAGEWAY;
                    }

                    if (!txt.Contains(";") && txt.Equals(@"[RIGHT CARRIAGEWAY]"))
                    {
                        currentProcess = NstType.RIGHTCARRIAGEWAY;
                    }
                }

                //============================================================================================

                ////process horizontal alignment
                //double lastrad = 0;
                //double eastStart = 0;
                //double northStart = 0;
                //double radiusStart = 0;
                //double length = 0;
                //bool newEle = false;
                //double eastStart2 = 0;
                //double northStart2 = 0;


                //HorizontalElementType eleType = new HorizontalElementType();
                //var hList = horizontalList.ToArray();
                //for (int hIdx = 0; hIdx < hList.Length; hIdx++)
                //{
                //    if (hIdx > 1)
                //    {
                //        lastrad = horizontalList[hIdx].Radius;
                //    }

                //    if (hIdx == (hList.Length - 1))
                //    {
                //        Trig.Geometry2Coords(hList[hIdx].ChStart, hList[hIdx].ChOrigin, hList[hIdx].Easting, hList[hIdx].Northing, hList[hIdx].Bearing, hList[hIdx].Radius, hList[hIdx].Rl, lastrad, hList[hIdx].Radius, hList[hIdx].ChStart, ref eleType, ref eastStart, ref northStart, ref radiusStart, ref length, ref newEle, ref eastStart2, ref northStart2);
                //    }
                //    else
                //    {
                //        Trig.Geometry2Coords(hList[hIdx].ChStart, hList[hIdx].ChOrigin, hList[hIdx].Easting, hList[hIdx].Northing, hList[hIdx].Bearing, hList[hIdx].Radius, hList[hIdx].Rl, lastrad, hList[hIdx + 1].Radius, hList[hIdx + 1].ChStart, ref eleType, ref eastStart, ref northStart, ref radiusStart, ref length, ref newEle, ref eastStart2, ref northStart2);
                //    }

                //    Horizontal.AddElement(new HorizontalElement
                //    {
                //        Chainage = hList[hIdx].ChStart,
                //        ChainageOrigin = hList[hIdx].ChOrigin,
                //        ElementType = eleType,
                //        North = northStart,
                //        East = eastStart,
                //        Radius = radiusStart,
                //        Length = length,
                //        RL = hList[hIdx].Rl,


                //    }) ;

                //    if (newEle)
                //    {
                //        Horizontal.AddElement(new HorizontalElement
                //        {
                //            Chainage = hList[hIdx].ChStart,
                //            ChainageOrigin = hList[hIdx].ChOrigin,
                //            ElementType = eleType,
                //            North = northStart2,
                //            East = eastStart2,
                //            Radius = radiusStart,
                //            Length = length,
                //            RL = hList[hIdx].Rl,

                //        });
                //    }
                //}

                //============================================================================================

                #region hz elements
                foreach (NstHorizontalAlignment hzele in horizontalList)
                {
                    ele = new HorizontalElement();
                    ele.East = hzele.Easting;
                    ele.North = hzele.Northing;
                    ele.Radius = hzele.Radius;
                    ele.BrgOrigin = hzele.Bearing;
                    ele.RL = hzele.Rl;
                    ele.EastOrigin = hzele.Easting;
                    ele.NorthOrigin = hzele.Northing;
                    ele.Chainage = hzele.ChStart;
                    ele.ChainageOrigin = hzele.ChStart;
                    ele.ChainageOrigin = hzele.ChOrigin;
                    ele.ElementType = HorizontalElementType.Straight;
                    if (ele.RL != 0)
                    {
                        ele.ElementType = HorizontalElementType.Spiral;

                    }
                    else if (ele.Radius != 0)
                    {
                        ele.ElementType = HorizontalElementType.Arc;
                    }
                    Horizontal.AddElement(ele);
                }
                #endregion hz elements

                //process vertical alignments
                var vList = verticalList.ToArray();
                for (int vIdx = 0; vIdx < vList.Length; vIdx++)
                {
                    Vertical.AddElement(new VerticalElement
                    {
                        Chainage = vList[vIdx].ChStart,
                        Level = vList[vIdx].Level,
                        Radius = vList[vIdx].Radius,
                        //WA : 02.07.19 Added vertical element type
                        ElementType = vList[vIdx].Radius == 0 ? VerticalElementType.Straight : VerticalElementType.Parabola
                    });
                }


                #region origin calcs commented out. We're reading directly from an NRG alignment all required data is in the file!
                //calc some origin stuff
                //if (Horizontal != null)
                //{
                //    if (Horizontal.Elements.Count > 1)
                //    {
                //        for (i = 0; i < Horizontal.Elements.Count; i++)
                //        {
                //            //Added by WA 28.08.19 Handles the last element
                //            if (i == Horizontal.Elements.Count - 1)
                //            {
                //                var element = Horizontal.Elements[i];
                //                element.EastOrigin = element.East;
                //                element.NorthOrigin = element.North;

                //                if (i > 0)
                //                {
                //                    var preEle = Horizontal.Elements[i - 1];
                //                    double brg = 0, dist = 0;
                //                    Trig.RPC(preEle.EastOrigin, preEle.NorthOrigin, element.EastOrigin, element.NorthOrigin, ref brg, ref dist);
                //                    element.BrgOrigin = brg;
                //                    element.Length = dist;
                //                }
                //                continue;
                //            }

                //            thisEle = Horizontal.Elements[i];
                //            nextEle = Horizontal.Elements[i + 1];
                //            AlignmentHelper.CalcOrigins(ref thisEle, nextEle);
                //        }
                //    }
                //
                #endregion origin calcs
            }
        }

        public void LineToString(List<Point3D> linePoints, string filePath)
        {
            StringFile = filePath;
            StringName = Path.GetFileNameWithoutExtension(filePath);

            Horizontal = new HorizontalAlignment();
            Vertical = new VerticalAlignment();

            double startCh = 0;

            for (int i = 0; i < linePoints.Count; i++)
            {
                //Add each point to the list or horizontal elements
                var curPoint = linePoints[i];
                var curHzElement = new HorizontalElement();
                var curVtElement = new VerticalElement();

                //Vertical element
                curVtElement.Chainage = startCh;
                curVtElement.ElementType = VerticalElementType.Straight;
                curVtElement.Level = curPoint.Z;
                curVtElement.Radius = 0;

                Vertical.AddElement(curVtElement);

                //Horizontal element
                curHzElement.Chainage = startCh;
                curHzElement.ChainageOrigin = startCh;
                curHzElement.ElementType = HorizontalElementType.Straight;
                curHzElement.East = curPoint.X;
                curHzElement.North = curPoint.Y;
                curHzElement.RL = 0;
                curHzElement.Radius = 0;

                //If the point is not last add the bearing and increment the chainage
                if (i < linePoints.Count - 1)
                {
                    var nextPoint = linePoints[i + 1];
                    var dist = Vector.FnDistance(curPoint.X, curPoint.Y, nextPoint.X, nextPoint.Y);
                    var brg = Trig.RPC(curPoint.X, curPoint.Y, nextPoint.X, nextPoint.Y);
                    curHzElement.BrgOrigin = brg;
                    startCh += dist;
                }

                //Add the vertical element
                Horizontal.AddElement(curHzElement);
            }

            if (Horizontal != null)
            {
                if (Horizontal.Elements.Count > 1)
                {
                    for (int i = 0; i > Horizontal.Elements.Count; i++)
                    {
                        if (i == Horizontal.Elements.Count - 1)
                        {
                            var element = Horizontal.Elements[i];
                            element.EastOrigin = element.East;
                            element.NorthOrigin = element.North;

                            if (i > 0)
                            {
                                var preEle = Horizontal.Elements[i - 1];
                                double brg = 0, dist = 0;
                                Trig.RPC(preEle.EastOrigin, preEle.NorthOrigin, element.EastOrigin, element.NorthOrigin, ref brg, ref dist);
                                element.BrgOrigin = brg;
                                element.Length = dist;
                            }
                            continue;
                        }

                        var thisEle = Horizontal.Elements[i];
                        var nextEle = Horizontal.Elements[i + 1];
                        AlignmentHelper.CalcOrigins(ref thisEle, nextEle);
                    }
                }
            }
        }

        /// <summary>
        /// saveFile - Saves the new alignment file format
        /// </summary>
        /// <param name="filename"></param> File path to write too
        /// 
        public void SaveFile(string filename = "")
        {
            if (filename == "")
            {
                filename = filename = FileIO.SaveFileDialog("*.NAF", "");
            }

            if (filename != "")
            {
                string outputData = "";
                string len = "";

                outputData = "//NRG Alignment" + Environment.NewLine;
                outputData = outputData + "//Horizontal Alignment=Element Type, Chainage, Easting, Northing, Radius, Spiral Length" + Environment.NewLine;
                foreach (HorizontalElement hz in Horizontal.Elements)

                {
                    if (hz.ElementType == HorizontalElementType.Spiral) { len = hz.Length.ToString(); } else { len = "0"; }
                    outputData = outputData + "Horizontal Element=" + hz.ElementType.ToString() + "," + hz.Chainage.ToString() + "," + hz.East.ToString() + "," + hz.North.ToString() + "," + hz.Radius.ToString() + "," + len + Environment.NewLine;
                }
                var asciiWriter = FileIO.SaveStringToFile(outputData, filename);
            }
        }

        public void SaveNST(string filename, bool IsShapeFile = false)
        {
            if (filename != "")
            {
                string outputData = "";
                string len = "";
                if (!IsShapeFile)
                    outputData = "FORMAT - NRG NEW STRING FILE VERSION 1.0\r\n\r\n" + "[HORIZONTAL ALIGNMENT]\r\n;CH START     CH ORIGIN     EASTING       NORTHING      BEARING       RADIUS        RL\r\n";
                else
                    outputData = ";LEN START     LEN ORIGIN     X Y      BEARING RADIUS        RL" + Environment.NewLine;
                foreach (HorizontalElement hz in Horizontal.Elements)
                {
                    if (hz.ElementType == HorizontalElementType.Spiral)
                        len = hz.Length.ToString();
                    else
                        len = "0";

                    if (hz.RadiusOrigin >= 1E+20)
                        hz.RadiusOrigin = 9999999.9990;
                    if (hz == Horizontal.Elements[Horizontal.Elements.Count - 1])
                    {
                        outputData = outputData + hz.Chainage.ToString("0.0000") + " ,      " + hz.ChainageOrigin.ToString("0.0000") + " ,      " + hz.EastOrigin.ToString("0.0000") + " ,               " + hz.NorthOrigin.ToString("0.0000") + " ,               " + hz.BrgOrigin.ToString("0.000000000") + " ,               " + hz.RadiusOrigin.ToString("0.0000") + " ,      ";
                        outputData += hz.RL.ToString("0.0000") + "\r\n";
                    }
                    else
                    {
                        outputData = outputData + hz.Chainage.ToString("0.0000") + " ,      " + hz.ChainageOrigin.ToString("0.0000") + " ,      " + hz.EastOrigin.ToString("0.0000") + " ,               " + hz.NorthOrigin.ToString("0.0000") + " ,               " + hz.BrgOrigin.ToString("0.000000000") + " ,               " + hz.RadiusOrigin.ToString("0.0000") + " ,      ";
                        outputData += hz.RL.ToString("0.0000") + "\r\n";
                    }
                }


                if (Vertical != null)
                {
                    if (Vertical.Elements != null)
                    {
                        outputData += "\r\n[LEFT VERTICAL ALIGNMENT]\r\n" + ";CH START     LEVEL         RADIUS\r\n";
                        foreach (VerticalElement vt in Vertical.Elements)
                        {
                            outputData += vt.Chainage.ToString("0.0000") + " ,      " + vt.Level.ToString("0.0000") + " ,      " + vt.Radius.ToString("0.0000") + "\r\n";
                        }
                        outputData += "\r\n[VERTICAL ALIGNMENT POSITION]\r\n" + "LOW RAIL = FALSE\r\n" + ";CH START     LEFT ALIGN POS              LEFT EDGE POS RIGHT ALIGN POS             RIGHT EDGE POS, LEFT LEVEL OFFSET, RIGHT LEVEL OFFSET\r\n\r\n";

                        outputData += "[LEFT CARRIAGEWAY]\r\n" + ";CH START     XFALL 1       WIDTH 1       XFALL 2       WIDTH 2       XFALL 3       WIDTH 3       XFALL 4       WIDTH 4       XFALL 5       WIDTH 5\r\n\r\n";
                        outputData += "[RIGHT CARRIAGEWAY]\r\n" + ";CH START     XFALL 1       WIDTH 1       XFALL 2       WIDTH 2       XFALL 3       WIDTH 3       XFALL 4       WIDTH 4       XFALL 5       WIDTH 5\r\n\r\n";
                        outputData += "[LEFT VERGE]\r\n" + ";CH START     XFALL 1       WIDTH 1       RESOIL 1      XFALL 2       WIDTH 2       RESOIL 2      XFALL 3       WIDTH 3       RESOIL 3      XFALL 4       WIDTH 4       RESOIL 4      XFALL 5       WIDTH 5       RESOIL 5\r\n\r\n";
                        outputData += "[CENTRE VERGE]\r\n" + ";CH START     XFALL 1       WIDTH 1       RESOIL 1      XFALL 2       WIDTH 2       RESOIL 2      XFALL 3       WIDTH 3       RESOIL 3      XFALL 4       WIDTH 4       RESOIL 4      XFALL 5       WIDTH 5       RESOIL 5\r\n\r\n";
                        outputData += "[RIGHT VERGE]\r\n" + ";CH START     XFALL 1       WIDTH 1       RESOIL 1      XFALL 2       WIDTH 2       RESOIL 2      XFALL 3       WIDTH 3       RESOIL 3      XFALL 4       WIDTH 4       RESOIL 4      XFALL 5       WIDTH 5       RESOIL 5\r\n\r\n";
                        outputData += "[LEFT BATTER]\r\n" + ";CH START     XFALL 1       WIDTH 1       RESOIL 1      XFALL 2       WIDTH 2       RESOIL 2      XFALL 3       WIDTH 3       RESOIL 3      XFALL 4       WIDTH 4       RESOIL 4      XFALL 5       WIDTH 5       RESOIL 5\r\n\r\n";
                        outputData += "[RIGHT BATTER]\r\n" + ";CH START     XFALL 1       WIDTH 1       RESOIL 1      XFALL 2       WIDTH 2       RESOIL 2      XFALL 3       WIDTH 3       RESOIL 3      XFALL 4       WIDTH 4       RESOIL 4      XFALL 5       WIDTH 5       RESOIL 5\r\n\r\n";
                        outputData += "[LINE FILE]";
                    }
                }

                System.IO.FileInfo f = new System.IO.FileInfo(filename);

                f.Directory.Create();
                File.WriteAllText(filename, outputData);

            }
        }

        #endregion

        #region Methods

       

        /// <summary>
        /// ExtractMXAlignment - produces MX 12 (G String phnar) dimensional data from an NRG string
        /// This is ported straight from the VB code (more or less) and for some reason it's taken me most of the day - if it's not full of bugs I WILL BE STAGGERED
        /// </summary>
        /// <returns>List of GString Elements</returns>
        public List<GStringElement> ExtractMXAlignment()
        {
            if (this.Horizontal != null && this.Horizontal.Elements.Count > 0)
            {
                List<GStringElement> Elements = new List<GStringElement>();
                List<double> Chainages = new List<double>();
                double lastM = 0;
                foreach (HorizontalElement h in Horizontal.Elements)
                {
                    Chainages.Add(h.Chainage);
                }
                if (Vertical != null && Vertical.Elements != null && Vertical.Elements.Count > 0)
                {
                    foreach (VerticalElement v in Vertical.Elements)
                    {
                        Chainages.Add(v.Chainage);
                    }
                }
                Chainages.Sort();
                HorizontalElement Lastele = null;
                HorizontalElement Thisele = null;
                HorizontalElement Nextele = null;
                double lastCh = double.NegativeInfinity;

                foreach (double ch in Chainages)
                {
                    if (ch - lastCh < 0.0001) { continue; }
                    GStringElement thisMXEle = new GStringElement();
                    thisMXEle.Chainage = ch;
                    Point2D crd = new Point2D();
                    double brg = 0;
                    double rad = 0;
                    bool ok = false;
                    int vertIdx = -1;
                    this.FindHzElements(ch, out Thisele, out Lastele, out Nextele);
                    
                    if (this.Vertical != null && this.Vertical.Elements!= null && this.Vertical.Elements.Count > 0)
                    {
                        vertIdx = this.Vertical.LocateVerticalElement(ch);
                    }

                    if (Thisele != null)
                    {
                        this.Horizontal.Coords(ch, Thisele, Lastele, Nextele, ref crd, ref brg, ref rad, ref ok);

                        if (ok)
                        {
#region - Horizontal Bits
                            HorizontalElement ele = new HorizontalElement();

                            thisMXEle.Coord.X = crd.X;
                            thisMXEle.Coord.Y = crd.Y;

                            thisMXEle.Bearing = brg;
                            thisMXEle.Radius = rad;
                            ele = Thisele;
                            if (MathsHelpers.Vector.FnEquals(ch, Thisele.Chainage,0.0001))
                            {
                                ele = Thisele;
                            }
                            if (Thisele != Horizontal.Elements.First())
                            {
                                if (Math.Abs(ch-Lastele.Chainage) < 0.0001) 
                                {
                                    ele = new HorizontalElement();
                                    ele = Lastele;
                                }
                            }
                            if (Thisele != Horizontal.Elements.Last())
                            {
                                if (Math.Abs(ch - Nextele.Chainage) < 0.0001)
                                {
                                    ele = new HorizontalElement();
                                    ele = Nextele;
                                }
                            }
                            HorizontalElement prevele = new HorizontalElement();
                            if (ele != Horizontal.Elements.First())
                            {
                                prevele = Horizontal.Elements[Horizontal.Elements.IndexOf(ele)-1];
                            }
                            thisMXEle.HCode = HorizontalMXCode(ele, prevele);
							#endregion
#region - Vertical Bits
							//Have to be careful here as there isn't necessarily a vertical alignment 
							if (vertIdx >= 0 && this.Vertical != null && this.Vertical.Elements != null && this.Vertical.Elements.Count > 0 && vertIdx < this.Vertical.Elements.Count)
                            {
                                VerticalElement vele = new VerticalElement();
                                vele = this.Vertical.Elements[vertIdx];

                                double lev = this.Vertical.GradeLevel(ch);
                                thisMXEle.Coord.Z = lev;
                                VerticalElement eletocalc = new VerticalElement();
                                eletocalc = vele;
                                if (Math.Abs(ch - vele.Chainage) < 0.0001)
                                {
                                    eletocalc = vele;
                                }
                                if (vele != this.Vertical.Elements.First())
                                {
                                    VerticalElement lastVertele = Vertical.Elements[vertIdx - 1];
                                    if (Math.Abs(ch - lastVertele.Chainage) < 0.0001)
                                    {
                                        eletocalc = new VerticalElement();
                                        eletocalc = this.Vertical.Elements[vertIdx - 1];
                                    }
                                }

                                
                                if (vele != this.Vertical.Elements.Last())
                                {
                                    VerticalElement nextVertele = this.Vertical.Elements[vertIdx + 1];

                                    if (Math.Abs(ch - nextVertele.Chainage) < 0.0001)
                                    {
                                        eletocalc = new VerticalElement();
                                        eletocalc = this.Vertical.Elements[vertIdx + 1];
                                        
                                    }
                                }


                                VerticalElement prevVele = new VerticalElement();
                                int thisVidx = this.Vertical.Elements.IndexOf(eletocalc);
                                if (thisVidx > 0)
                                {
                                    prevVele = this.Vertical.Elements[thisVidx - 1];
                                }
                                thisMXEle.VCode = VerticalMXCode(eletocalc, prevVele);

                                double grad= CalcVerticalGradeAtTangentPoint(eletocalc);

                                if (eletocalc.Radius == 0)
                                {
                                    thisMXEle.Mval = 0;
                                }
                                else 
                                {
                                    thisMXEle.Mval = 10000 / eletocalc.Radius;   
                                }
                                lastM = thisMXEle.Mval;
                                thisMXEle.Grade = grad;

                                #endregion

                            }
                            Elements.Add(thisMXEle);  //After all that, stick in the list
						}
                    }
                    lastCh = ch;
                }
                return Elements;
            }

            return null;
        }

        public string HorizontalMXCode(HorizontalElement ThisEle, HorizontalElement LastEle)
        {

            if (ThisEle != null)
            {
                if (ThisEle == this.Horizontal.Elements.First())
                {
                    if (ThisEle.Radius == 0)
                    {
                        return "PBT";
                    }
                    else
                    {
                        return "PBC";
                    }

                }
                else if (ThisEle == this.Horizontal.Elements.Last())
                {
                    if (LastEle.Radius == 0)
                    {
                        return "PAT";
                    }
                    else
                    {
                        return "PAC";
                    }
                }
                else if (LastEle.Radius == 0 && LastEle.RL == 0)
                {
                    if (ThisEle.RL == 0)
                    {
                        return "PC";
                    }
                    else
                    {
                        return "TS";
                    }
                }
                else if (LastEle.RL == 0)
                {
                    if (ThisEle.Radius == 0)
                    {
                        return "PT";
                    }
                    else if (ThisEle.RL == 0)
                    {
                        return "PCC";
                    }
                    else
                    {
                        return "CS";
                    }
                }
                else
                {
                    if (ThisEle.Radius == 0)
                    {
                        return "ST";
                    }
                    else if (ThisEle.RL == 0)
                    {
                        return "SC";
                    }
                    else
					{
                        return "SS";

					}
                }
            }
            return "";
        }

        public string VerticalMXCode(VerticalElement thisEle, VerticalElement lastEle)
        {

            if (thisEle != null)
            {
                if (thisEle == this.Vertical.Elements.First())
                {
                    if (thisEle.Radius == 0 || Math.Abs(thisEle.Radius) > 99999999)
                    {
                        return "PBT";
                    }
                    else
                    {
                        return "PBC";
                    }
                }
                else if (thisEle == this.Vertical.Elements.Last())
                {
                    if (thisEle.Radius == 0 || Math.Abs(thisEle.Radius) > 99999999)
                    {
                        return "PAT";
                    }
                    else
                    {
                        return "PAC";
                    }
                }
                else if (lastEle.Radius == 0 || Math.Abs(lastEle.Radius) > 99999999)
                {
                    if (thisEle.Radius == 0 || Math.Abs(thisEle.Radius) > 99999999)
                    {
                        return "TT";
                    }
                    else
                    {
                        return "PC";
                    }
                }
                else
                {
                    if (thisEle.Radius == 0 || Math.Abs(thisEle.Radius) > 99999999)
                    {
                        return "PRC";
                    }
                    else
                    {
                        return "PCC";
                    }
                }

            }

            return "";
        }

        public double CalcVerticalGradeAtTangentPoint(VerticalElement thisEle)
        {
            double grade = 0;
            double g1 = 0;
            if (this.Vertical != null && this.Vertical.Elements.Count > 0)
            {
                VerticalElement ele = thisEle;
                if (ele == this.Vertical.Elements.First())
                {
                    ele = new VerticalElement();
                    ele = this.Vertical.Elements[1];

                }
                if (ele != this.Vertical.Elements.First())
                {
                    double y = 0;
                    VerticalElement lastEle = this.Vertical.Elements[this.Vertical.Elements.IndexOf(ele)];
                    double l = ele.Chainage - lastEle.Chainage;
                    if (lastEle.Radius == 0)
                    {
                        y = 0;
                    }
                    else
                    {
                        y = l * l / (2 * lastEle.Radius);
                    }
                    if (l != 0)
                    {
                        g1 = (ele.Level - y - lastEle.Level) / l;
                        double a = (g1 * l / 2) + lastEle.Level;
                        grade = (ele.Level - a) / (l / 2);
                    }
                    else
                    {
                        grade = 0;
                    }
                }
                if (thisEle == this.Vertical.Elements.First()) { grade = g1; }
            }
            
            return grade;
        }

        /*
 

         
         */

        public string Directory()
        {
            if (File.Exists(StringFile) == false)
            return "";

            return StringFile.Replace(StringName + ".nst", "");
        }

        /// <summary>
        /// Determines whether a given chainage value can be found on an alignment
        /// </summary>
        /// <param name="chainage">The chainage value to check</param>
        /// <returns>Returns true if the chainage is on the alignment</returns>
        public bool IsOnHzAlignment(double chainage)
        {
            if (Horizontal == null)
                return false;

            return (chainage >= Horizontal.MinChainage && chainage <= Horizontal.MaxChainage);
        }

        public void FindHzElements(double Chainage, out HorizontalElement ThisElement, out HorizontalElement LastElement, out HorizontalElement NextElement)
        {
            ThisElement = null;
            LastElement = null;
            NextElement = null;

            int i = -1;
            foreach (HorizontalElement hz in this.Horizontal.Elements)
            {
                i++;
                if (Chainage >= hz.Chainage)
                {
                    //should be this one by rights. But get the silly situations out of the way first...
                    if (this.Horizontal.Elements.Count == 1)
                    {
                        ThisElement = hz;
                        return;
                    }
                    else 
                    {
                        if (i < Horizontal.Elements.Count-1 && Chainage >= hz.Chainage && Chainage <= this.Horizontal.Elements[i + 1].Chainage)
                        {
                            ThisElement = hz;
                            NextElement = this.Horizontal.Elements[i + 1];
                            if (i > 0)
                            {
                                LastElement = this.Horizontal.Elements[i - 1];
                            }
                            return;
                        }
                    
                    }
                }
            }
            
        }

        /// <summary>
        /// Calculates the extents of the horizontal alignment
        /// </summary>
        /// <returns>Returns A <see cref="Bounds"/> representing the extents of the horizontal alignment</returns>
        public Bounds CalcHzExtents()
        {
            var minMax = new Bounds();

            HorizontalElement ele = new HorizontalElement();

            //Just giving it something to test off in the first place
            minMax.Min.X = Horizontal.Elements[0].East;
            minMax.Min.Y = Horizontal.Elements[0].North;
            minMax.Max.X = Horizontal.Elements[0].East;
            minMax.Max.Y = Horizontal.Elements[0].North;

            for (int i = 0; i < Horizontal.Elements.Count; i++)
            {
                ele = Horizontal.Elements[i];
                if (ele.East < minMax.Min.X) { minMax.Min.X = ele.East; }
                if (ele.North < minMax.Min.Y) { minMax.Min.Y = ele.North; }
                if (ele.East > minMax.Max.X) { minMax.Max.X = ele.East; }
                if (ele.North > minMax.Max.Y) { minMax.Max.Y = ele.North; }
            }

            //Todo calc bounds for alignment - last point needed
            return minMax;
        }


        public void GenerateChainageMarkers(double Interval, double Offset, double Flagsize)
        {
            if (Horizontal.Elements == null || Horizontal.Elements.Count <= 0) { return; }

            CollectionOfAllChainageMarkers outMarkers = new CollectionOfAllChainageMarkers();

            double startCh = Horizontal.Elements.First().Chainage;
            double endCh = Horizontal.Elements.Last().Chainage;

            for (double ch = startCh; ch <= endCh; ch += Interval   )
            { 
                
                ChainageMarker marker = new ChainageMarker();
                marker = CalcChainageMarker(ch, Offset, Flagsize);
                if (marker != null)
                { 
                    outMarkers.Markers.Add(marker);
                }

            }

            MarkerCollection = outMarkers;

        }

        
        //Added by ES:28.07.22
        public ChainageMarker CalcChainageMarker(double Ch, double Offset, double Flagsize)
        {
            List<Point2D> outlist = new List<Point2D>();
            if (Horizontal != null && Horizontal.Elements != null && Horizontal.Elements.Count > 0)
            {
                double tang = 0;
                double algTang = 0;

                Point2D algPos1 = Horizontal.CoordsAtChAndOffset(Ch,-Math.Abs(Offset), out algTang);
                Point2D algPos2 = Horizontal.CoordsAtChAndOffset(Ch, Math.Abs(Offset), out algTang);
                if (algPos1 != null && algPos2 != null)
                {
                    Point2D algPos = new Point2D((algPos1.X + algPos2.X)/2, (algPos1.Y + algPos2.Y) / 2);
                    if (algPos != null)
                    {
                        ChainageMarker mark = new ChainageMarker();

                        if (mark.GenerateMarker(algPos, algTang, Offset, Flagsize))
                        {
                            return mark;
                        }
                    }
                }
            }

            return null;

        }

        public List<Point3D> ThrupennyBit(double step = 1)
        {
            if (Horizontal != null && Horizontal.Elements != null && Horizontal.Elements.Count > 0) //hz elements null ref check added by ES:12.07.22
            { 
                List<Point3D> outlist = new List<Point3D>();

                foreach (HorizontalElement ele in this.Horizontal.Elements)
                {
                    HorizontalElement nextele = getNextElement(ele);
                    if (nextele == null)
                    {
                        break;
                    }
                    if (ele.ElementType == HorizontalElementType.Straight)
                    {
                        double z = this.Vertical.GradeLevel(ele.Chainage);

                        outlist.Add(new Point3D(ele.East, ele.North, z));

                        if (nextele != null)
                        {
                            z = this.Vertical.GradeLevel(nextele.Chainage);

                            outlist.Add(new Point3D(nextele.East, nextele.North));  
                        }
                    }
                    else
                    {
                        double curch = ele.Chainage;
                        double z = this.Vertical.GradeLevel(curch);

                        outlist.Add(new Point3D(ele.East, ele.North, z));
                        if (ele != Horizontal.Elements.Last())
                        {
                            do
                            {
                                curch += step;
                                Point2D p = Horizontal.CoordsAtChAndOffset(curch, 0);

                                z = this.Vertical.GradeLevel(curch);

                                outlist.Add(new Point3D(p.X, p.Y,z));
                            }
                            while (curch <= nextele.Chainage);
                        }
                    }
                }


                if (outlist.Count < 10000)
                {
                    Console.WriteLine("");
                }
                return outlist;
                
            }
            return null;
        
        }

        private HorizontalElement getNextElement(HorizontalElement ele)
        {
            int idx = Horizontal.Elements.IndexOf(ele);
            if (idx < Horizontal.Elements.Count-1)
                return Horizontal.Elements[idx+1];
            else
                return null;
        }

        //This function is doubling or tripaling some coordinates , on straigt and arc, needs to check or go foward 2 at a time
        public void CalcDrawCoords(List<Point3D> coords, double step = 0.1)
        {
            coords = new List<Point3D>();
            coords = ThrupennyBit(step);
            return;

            //WA 06.06.18 Added check to prevent infinite looping
            if (step <= 0)
                step = 0.1;

            if (Horizontal != null && Horizontal.Elements != null && Horizontal.Elements.Count > 0) //hz elements null ref check added by ES:12.07.22
            {
                int i = 0;
                double curCh = 0;
                double nextCH = 0;
                double tang = 0;
                double r = 0;
                bool OK = false;
                Point3D vec = new Point3D();
                Point2D thisCoord = new Point2D();
                Point2D nextCoord = new Point2D();

                HorizontalElement thisEle = new HorizontalElement();
                HorizontalElement lastEle = new HorizontalElement();
                HorizontalElement nextEle = new HorizontalElement();

                for (i = 0; i < Horizontal.Elements.Count - 1; i++)
                {
                    if (Horizontal.Elements[i].ElementType == HorizontalElementType.Straight)
                    {
                        vec = new Point3D();
                        vec.X = Horizontal.Elements[i].East;
                        vec.Y = Horizontal.Elements[i].North;
                        vec.Z = Vertical.GradeLevel(Horizontal.Elements[i].Chainage);
                        coords.Add(vec);
                        //WA 25.05.18 Removed Duplicate coord entry


                    }
                    else
                    {
                        if (i == 0)
                        {
                            lastEle = null;
                        }
                        else
                        {
                            thisEle = Horizontal.Elements[i];
                            lastEle = Horizontal.Elements[i - 1];
                            nextEle = Horizontal.Elements[i + 1];
                            curCh = Horizontal.Elements[i].Chainage;
                            vec = new Point3D();
                            //WA 25.05.18 Removed Duplicate coord entry

                            //vec.X = horizontal.elements[i].East;
                            //vec.Y = horizontal.elements[i].North;
                            //vec.Z = vertical.GradeLevel(horizontal.elements[i].Chainage);
                            //coords.Add(vec);
                            do
                            {
                                nextCH = curCh + step;
                                Horizontal.Coords(curCh, thisEle, lastEle, nextEle, ref thisCoord, ref tang, ref r, ref OK);
                                vec = new Point3D();
                                vec.X = thisCoord.X;
                                vec.Y = thisCoord.Y;
                                vec.Z = Vertical.GradeLevel(curCh);
                                coords.Add(vec);
                                Horizontal.Coords(nextCH, thisEle, lastEle, nextEle, ref nextCoord, ref tang, ref r, ref OK);
                                vec = new Point3D();
                                vec.X = nextCoord.X;
                                vec.Y = nextCoord.Y;
                                vec.Z = Vertical.GradeLevel(curCh);
                                coords.Add(vec);
                                //WA 25.05.18 added step again to remove duplicate coords

                                curCh = nextCH + step;
                            }
                            while (curCh + step < Horizontal.Elements[i + 1].Chainage);
                        }
                    }
                }

                //WA 16.07.19 Add Last point
                if(Horizontal.Elements.Count >= 1)
                {
                    var last = Horizontal.Elements.Last();
                    vec = new Point3D();
                    vec.X = last.East;
                    vec.Y = last.North;
                    vec.Z = Vertical.GradeLevel(last.Chainage);
                    coords.Add(vec);
                }
            }
        }

        /// <summary>
        /// Creates a fake two element horizontal alightment, for use on manual single cross section
        /// </summary>
        /// <param name="pt1"></param>The first point
        /// <param name="pt2"></param>And Unsuprising second point
        public void CreateHorizontalFromPoints(Point3D pt1, Point3D pt2)
        {
            HorizontalElement ele1 = new HorizontalElement();
            HorizontalElement ele2 = new HorizontalElement();

            ele1.ElementType = HorizontalElementType.Straight;
            ele1.Chainage = 0;
            ele1.East = pt1.X;
            ele1.North = pt1.Y;
            ele1.Radius = 0f;
            ele1.Length = Vector.Get2DLengthBetweenXYZ(pt1, pt2);

            ele2.ElementType = HorizontalElementType.Straight;
            ele2.Chainage = 1;
            ele2.East = pt2.X;
            ele2.North = pt2.Y;
            ele2.Radius = 0f;
            ele2.Length = 0f;

            Horizontal.AddElement(ele1);
            Horizontal.AddElement(ele2);

            AlignmentHelper.CalcOrigins(ref ele1, ele2);
        }

        /// <summary>
        /// Finds the road / rail cant at a given chainage
        /// </summary>
        /// <param name="chainage">The chainage to find the cant for</param>
        /// <returns>Returns a <see cref="double"/> containing the cant at the given chainage</returns>
        public double GetCantAtChainage(double chainage)
        {
            switch(ProfileType)
            {
                default:
                    return 0;
                case ProfileType.Road:
                    return RoadProfile.GetCantAt(chainage);
                case ProfileType.Rail:
                    return RailwayProfile.GetCantAt(chainage);                    
            }
        }

        /// <summary>
        /// Converts a rail cant (expressed as a height diff) to a road crossfall (grade percentage)
        /// </summary>
        public static double CantToXFall(double cant, double gauge = 1.507)
        {
            return Math.Tan(Math.Asin(cant / gauge)) * 100;
        }

        /// <summary>
        /// Converts a road crossfall (grade percentage) to a rail cant (expressed as a height diff)
        /// </summary>
        public static double XFallToCant(double xFall, double gauge = 1.507)
        {
            return Math.Sin(Math.Atan(xFall / 100)) * gauge;
        }

        /// <summary>
        /// Calculates what the plan road width would be given a rail cant (expressed as a height diff) and gauge (slope distance)
        /// effectively the adjacent of the triangle where the cant is opposite and the gauge is hypotenuse
        /// </summary>
        public static double RoadWidthFromCant(double cant, double gauge = 1.507)
        {
            return Math.Cos(Math.Atan(CantToXFall(cant, gauge) / 100)) * gauge;
        }

        /// <summary>
        /// Calculates what the plan road width would be given a road crossfall (grade percentage) and gauge (slope distance)
        /// effectively the adjacent of the triangle where the cant is opposite and the gauge is hypotenuse
        /// </summary>
        public static double RoadWidthFromXFall(double xFall, double gauge = 1.507)
        {
            return Math.Cos(Math.Atan(xFall / 100)) * gauge;
        }

		public AlignmentDetails GetDetails()
		{

			AlignmentDetails det = new AlignmentDetails();

			det.HorizontalLength = this.Horizontal.MaxChainage - this.Horizontal.MinChainage;
			det.NumberElements = Horizontal.Elements.Count;
			foreach (HorizontalElement hz in Horizontal.Elements)
			{
				
				if (hz.ElementType == HorizontalElementType.Arc)
				{
					if (hz.Radius > det.MaximumRadius)
						det.MaximumRadius = hz.Radius;
					if (hz.Radius < det.MinimumRadius)
						det.MinimumRadius = hz.Radius;
				}
				else if (hz.ElementType == HorizontalElementType.Spiral)
				{
					if (hz.Length > det.MaximumSpriralLength)
						det.MaximumSpriralLength = hz.Length;
					if (hz.Length < det.MinimumSprialLength)
						det.MinimumSprialLength = hz.Length;
				}
				
			}

			return det;

		}

        #endregion
    }

    public class HorizontalAlignment
    {
        #region Properties

        public List<HorizontalElement> Elements { get { return PrivateElements; } }

        private List<HorizontalElement> PrivateElements { get; set; }

        public ChainageEquationSet ChainageEquations = new ChainageEquationSet(); //added by ES:25.03.22

        public double MaxChainage
        {
            get
            {
                if (Elements.Count <= 0)
                    return 0;

                return Elements.LastOrDefault().Chainage;
            }
        }

        public double MinChainage
        {
            get
            {
                if (Elements.Count <= 0)
                    return 0;

                return Elements.FirstOrDefault().Chainage;
            }
        }

        #endregion

        #region Methods


        public bool GetSectionExtents(Point3D CursorPosition, out Point3D StartPoint,out Point3D EndPoint)
        {

            StartPoint = new Point3D(0,0,0);
            EndPoint = new Point3D(0, 0, 0);

            AlignmentVector5 vec = new AlignmentVector5();

            if (this.GetChainageAndOffset(CursorPosition, ref vec))
            {
                Point2D ept = this.CoordsAtChAndOffset(vec.chainage, vec.offset);
                StartPoint.X = ept.X;
                StartPoint.Y = ept.Y;

                Point2D spt = this.CoordsAtChAndOffset(vec.chainage, -vec.offset);
                EndPoint.X = spt.X;
                EndPoint.Y = spt.Y;
                return true;
            
            }

            return false;
        }


        /// <summary>
        /// AddElement - Add a new horizontal element to the alignment. 
        /// Once the new element is added to the list, the whole list is
        /// sorted by chainage
        /// </summary>
        /// <param name="element"></param> new element to add
        public void AddElement(HorizontalElement element)
        {
            if (PrivateElements == null)
            {
                //fuck all there so instantiate
                PrivateElements = new List<HorizontalElement>();
            }

            //Add to the array thing
            PrivateElements.Add(element);
            //sort by chainage...still undecided on this, what if the design is in reverse order. Software option?
            PrivateElements.Sort((x, y) => x.Chainage.CompareTo(y.Chainage));
            //Console.WriteLine(privateElements[0].Chainage);
        }

        /// <summary>
        /// Coords - Stupid name retained for posterity. Calculates the 2D coords(E & N) of a given chainage
        /// </summary>
        /// <param name="Chainage"></param> Chainage (meterage) along the alignemnt (ie geometric distance from the start)
        /// <param name="element"></param> Hz element used for calculation
        /// <param name="previousElement"></param> Hz element which precedes Element
        /// <param name="nextElement"></param> Obvs
        /// <param name="outVector"></param> The Easting & Northing in an nrgVector2
        /// <param name="tang"></param> Tangent bearing at this chainage
        /// <param name="r"></param>Radius at this position
        /// <param name="OK"></param> bool true if it worked, false else.
        public void Coords(double Chainage, HorizontalElement element, HorizontalElement previousElement, HorizontalElement nextElement, ref Point2D outVector, ref double tang, ref double r, ref bool OK)
        {
            double radi = 0, Distance = 0, bearing = 0;
            double tangent = 0, j = 0, l = 0, m = 0;
            double EastO = 0, NorthO = 0;
            double outX = 0, outY = 0;
            Distance = Chainage - element.ChainageOrigin;

            if (element.ElementType == HorizontalElementType.Straight)
            {
                bearing = element.BrgOrigin;
                tangent = bearing;
                r = 0;
            }
            else if (element.ElementType == HorizontalElementType.Arc)
            {
                j = Distance / (element.Radius * 2D);
                bearing = element.BrgOrigin + j;
                tangent = element.BrgOrigin + (2D * j);
                Distance = (2 * element.Radius) * Math.Sin(j);
                r = element.Radius;
            }
            else //spiral
            {
                if (Distance == 0)
                {
                    radi = 1E+38;
                }
                else
                {
                    radi = Math.Abs(element.RL / Distance);
                }
                m = Distance / (radi * 2D);
                j = (m / 3) + ((m * m * m) / 105D) + ((m * m * m * m * m) / 5997D);
                j = Math.Abs(j);
                j = Math.Atan(j);
                tangent = (Distance * Distance) / (2D * Math.Abs(element.RL));
                tangent = Trig.FnAngle(tangent, 0);
                if (Chainage > element.ChainageOrigin)
                {
                    //Modified By WA: 20/08/2019 - fixing issue with not being able to get coords exactly on the last alignment
                    if(nextElement == null)
                    {
                        OK = false;
                        return;
                    }
                    else if (nextElement.Radius < 0)
                    {
                        j = -j; tangent = -tangent; r = -radi;
                    }
                    else
                    {
                        r = radi;
                    }
                }
                else if (previousElement != null)
                {
                    if (previousElement.ElementType == HorizontalElementType.Straight || previousElement.RadiusOrigin > 999999)
                    {
                        //spiral follows straight
                        if (element.RadiusOrigin < 0)
                        {
                            j = -j; tangent = -tangent; r = radi;
                        }
                        else
                        {
                            r = -radi;
                        }
                    }
                    else
                    {
                        if (previousElement.RadiusOrigin > 0)
                        {
                            j = -j; tangent = -tangent; r = radi;
                        }
                        else
                        {
                            r = -radi;
                        }
                    }
                }
                else
                {
                    //shouldn't be able to get here first element is a spiral and the radius is before the spiral origin
                    j = 0; tangent = 0; r = 1E+38;
                }
                bearing = element.BrgOrigin + j;
                tangent = element.BrgOrigin + tangent;
                tangent = Trig.FnAngle(tangent, 0);

                l = Distance;

                Distance = l - ((l * l * l) / (40D * (radi * radi))) + ((l * l * l * l * l) / (3456D * (radi * radi * radi * radi)));
                Distance = Distance / Math.Cos(j);

            }

            EastO = element.EastOrigin; NorthO = element.NorthOrigin;
            Trig.PRC(EastO, NorthO, bearing, Distance, ref outX, ref outY, 1);
            outVector.X = outX; outVector.Y = outY;
            tang = tangent;
            OK = true;
        }

       
        /// <summary>
        /// getChainageAndOffset - wrapper for compare. Identifys the horizontal element in which a given coordinate
        /// lies, then calculates the chainage & offset
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="OutVector">Vector5 containing Chainage, offset, radius, tangent bearing and element index
        /// <returns></returns> bool true if it worked</param>
        /// <returns></returns>
        public bool GetChainageAndOffset(double x, double y, ref AlignmentVector5 OutVector)
        {
            Point2D pt = new Point2D(x, y);
            return GetChainageAndOffset(pt, ref OutVector);
            
        }

        /// <summary>
        /// getChainageAndOffset - wrapper for compare. Identifys the horizontal element in which a given coordinate
        /// lies, then calculates the chainage & offset
        /// </summary>
        /// <param name="inVector"></param> Vector3 containing the desired coordinate
        /// <param name="OutVector"></param> Vector5 containing Chainage, offset, radius, tangent bearing and element index
        /// <returns></returns> bool true if it worked
        public bool GetChainageAndOffset(Point3D inVector, ref AlignmentVector5 OutVector)
        {
            if (inVector == null) { return false; }

            Point2D pt = new Point2D(inVector.X, inVector.Y);
            return GetChainageAndOffset(pt, ref OutVector);

        }

        /// <summary>
        /// getChainageAndOffset - wrapper for compare. Identifys the horizontal element in which a given coordinate
        /// lies, then calculates the chainage & offset
        /// </summary>
        /// <param name="inVector"></param> Vector2 containing the desired coordinate
        /// <param name="outVector"></param> Vector5 containing Chainage, offset, radius, tangent bearing and element index
        /// <returns></returns> bool true if it worked
        public bool GetChainageAndOffset(Point2D inVector, ref AlignmentVector5 outVector)
        {
            int i = 0;
            int lastele = 0;
            double offset = 1E+38;

            AlignmentVector5 testVector = new AlignmentVector5();
            if (PrivateElements != null)
            {
                offset = double.PositiveInfinity;
                for (i = 0; i < PrivateElements.Count - 1; i++)
                {

                    testVector = new AlignmentVector5();

                    if (Compare(inVector, i, testVector, lastele))
                    {
                        if ((Math.Abs(testVector.offset) < Math.Abs(offset)))
                        {
                            outVector = testVector;
                            outVector.element = i;
                            offset = outVector.offset;
                            outVector.chainage = ChainageEquations.Transform(outVector.chainage);
                        }
                    }
                }
            }

            if (offset < 1E+38)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        

        /// <summary>
        /// CoordsAtChAndOffset - Returns the coordinate at a given chainage and offset
        /// </summary>
        /// <param name="ch"></param>Required chainage
        /// <param name="off"></param>Required offset
        /// <returns>Vector2 containing the Easting and Northing</returns> 
        public Point2D CoordsAtChAndOffset(double ch, double off)
        {
            double tang = 0, r = 0, bearing = 0, east = 0, north = 0;
            bool OK = false;
            int i = 0;

            ch = ChainageEquations.UnTransform(ch);//REALLY not sure about this ES:29.03.22

            Point2D res = new Point2D();
            HorizontalElement ele = new HorizontalElement(); //test branch
            HorizontalElement previousele = new HorizontalElement();
            HorizontalElement nextele = new HorizontalElement();

            if (PrivateElements != null)
            {
                for (i = 0; i < PrivateElements.Count - 1; i++)
                {
                    ele = PrivateElements[i + 1];
                    if (ch < ele.Chainage)
                    {
                        break;
                    }
                }

                if (i <= PrivateElements.Count - 1)
                {
                    if (i > 0)
                    {
                        previousele = PrivateElements[i - 1];
                    }
                    else
                    {
                        previousele = null;
                    }

                    if (i == PrivateElements.Count - 1)
                        nextele = null;
                    else
                        nextele = PrivateElements[i + 1];
                    ele = PrivateElements[i];

                    Coords(ch, ele, previousele, nextele, ref res, ref tang, ref r, ref OK);
                }
            }

            if (OK == true)
            {
                bearing = tang + Math.PI / 2;
                if (bearing > 2 * Math.PI) { bearing = bearing - 2 * Math.PI; }
                Trig.PRC(res.X, res.Y, bearing, off, ref east, ref north, 1);
                res.X = east;
                res.Y = north;
                return res;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// CoordsAtChAndOffset - Returns the coordinate at a given chainage and offset
        /// </summary>
        /// <param name="ch"></param>Required chainage
        /// <param name="off"></param>Required offset
        /// <param name="tang">Tangent of the chainage element</param>
        /// <returns>Vector2 containing the Easting and Northing</returns>
        public Point2D CoordsAtChAndOffset(double ch, double off, out double tang)
        {
            double r = 0, bearing = 0, east = 0, north = 0;
            tang = 0;
            bool OK = false;
            int i = 0;

            //==============================================================
            //Debug code.
            if (off != 0)
            {
                Console.WriteLine("");
            }
            //=============================================================


            Point2D res = new Point2D();
            HorizontalElement ele = new HorizontalElement();
            HorizontalElement previousele = new HorizontalElement();
            HorizontalElement nextele = new HorizontalElement();

            ch = ChainageEquations.UnTransform(ch);//REALLY not sure about this ES:29.03.22


            if (PrivateElements != null)
            {
                for (i = 0; i < PrivateElements.Count - 1; i++)
                {
                    ele = PrivateElements[i + 1];
                    if (ch <= ele.Chainage)
                    {
                        break;
                    }
                }

                if (i < PrivateElements.Count - 1)
                {
                    if (i > 0)
                    {
                        previousele = PrivateElements[i - 1];
                    }
                    else
                    {
                        previousele = null;
                    }
                    nextele = PrivateElements[i + 1];
                    ele = PrivateElements[i];

                    Coords(ch, ele, previousele, nextele, ref res, ref tang, ref r, ref OK);
                }
            }

            if (OK == true)
            {
                bearing = tang + Math.PI / 2;
                if (bearing > 2 * Math.PI) { bearing = bearing - 2 * Math.PI; }
                Trig.PRC(res.X, res.Y, bearing, off, ref east, ref north, 1);
                res.X = east;
                res.Y = north;
                return res;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Compare - Name retained from VB6 code. Probaly the most confusing proc name I've ever come across.
        /// Given a horizontal alignment element, Compare calulates the distance along and offset from the element
        /// Passes back a Vector5 containing chainage, offset, tangent bearing, radius and element index.
        /// Generally called from GetChainageAndOffset.
        /// </summary>
        /// <param name="inVector"></param> Vector2 containing the coordinates of the position to calulate
        /// <param name="ele"></param> index of the hz element
        /// <param name="outVector"></param> vector5 containing chainage, offset, radius and tangent bearing
        /// <param name="lastEle"></param> unused in ported code. Might need in future.
        /// <returns></returns>
        public bool Compare(Point2D inVector, int ele, AlignmentVector5 outVector, int lastEle)
        {
            //=========================================================
            //valid element?
            if (PrivateElements == null) { lastEle = ele; ; return false; }
            if (ele < 0 || ele >= PrivateElements.Count - 1) { lastEle = ele; ; return false; }
            //=========================================================
            double theta2 = 0;
            double A = 0, X = 0;
            double brg = 0, dist = 0, theta = 0;
            double tang = 0, r = 0;
            bool OK = false;
            double EastCentre = 0, NorthCentre = 0;
            bool bollocks = false;
            bool found = false;
            Point2D Position = new Point2D();

            HorizontalElement element = new HorizontalElement();
            element = PrivateElements[ele];
            HorizontalElement nextElement = new HorizontalElement();
            nextElement = PrivateElements[ele + 1];

            //get the geometric origin of spiral / clothoid curves
            //CalcOrigins(ref element, nextElement);

            HorizontalElement previousElement = new HorizontalElement();
            if (ele > 0)
            {
                previousElement = PrivateElements[ele - 1];
            }
            else
            {
                previousElement = null;
            }

            if (element.ElementType == HorizontalElementType.Straight)
            {
                Trig.RPC(element.East, element.North, inVector.X, inVector.Y, ref brg, ref dist, 1);
                theta = brg - element.BrgOrigin;
                outVector.chainage = (Math.Cos(theta) * dist) + element.ChainageOrigin;
                outVector.offset = Math.Sin(theta) * dist;
                outVector.radius = 0;
                outVector.bearing = brg;
            }
            else if (element.ElementType == HorizontalElementType.Arc)
            {
                double Ecentre = element.EastOrigin + (Math.Sin(element.BrgOrigin + Math.PI / 2) * element.Radius);
                double Ncentre = element.NorthOrigin + (Math.Cos(element.BrgOrigin + Math.PI / 2) * element.Radius);
                Trig.RPC(Ecentre, Ncentre, inVector.X, inVector.Y, ref brg, ref dist, 1);
                if (element.Radius < 0)
                {
                    A = element.BrgOrigin + Math.PI / 2; if (A > 2 * Math.PI) { A = A - 2 * Math.PI; }
                    theta = (A + (2 * Math.PI)) - brg;
                    outVector.offset = element.Radius + dist;
                }
                else
                {
                    A = element.BrgOrigin - Math.PI / 2; if (A < -0.000001) { A = A + 2 * Math.PI; }
                    theta = (brg + (2 * Math.PI)) - A;
                    outVector.offset = element.Radius - dist;
                }
                if (Vector.FnEquals(A, brg, 0.00000001))
                {
                    outVector.chainage = element.ChainageOrigin;
                }
                else
                {
                    if (theta >= Math.PI) { theta = theta - Math.PI * 2; }
                    outVector.chainage = element.ChainageOrigin + (Math.Abs(element.Radius) * theta);
                }
                outVector.radius = element.Radius;
                
            }
            else //spiral
            {
                int maxtried = 0, tried = 0;
                double diff = 0, diff2 = 0;

                //minor differences due to rounding or the origins here - keep an eye!!
                Trig.RPC(element.EastOrigin, element.NorthOrigin, inVector.X, inVector.Y, ref brg, ref dist, 1);

                theta = brg - element.BrgOrigin;
                X = Math.Cos(theta) * dist;
                outVector.chainage = element.ChainageOrigin + X;

                if (Math.Abs(element.RL) == 0) { maxtried = 10; }
                else if (Math.Abs(element.RL) < 100) { maxtried = 60; }
                else if (Math.Abs(element.RL) >= 100 && Math.Abs(element.RL) <= 500) { maxtried = 50; }
                else if (Math.Abs(element.RL) > 500 && Math.Abs(element.RL) <= 1000) { maxtried = 40; }
                else if (Math.Abs(element.RL) > 1000 && Math.Abs(element.RL) <= 10000) { maxtried = 30; }
                else if (Math.Abs(element.RL) > 10000 && Math.Abs(element.RL) <= 50000) { maxtried = 20; }
                else { maxtried = 10; }

                diff = 10000000000;

                do
                {
                    diff2 = diff;
                    //call coords here
                    Coords(outVector.chainage, element, previousElement, nextElement, ref Position, ref tang, ref r, ref OK);
                    outVector.radius = r;
                    brg = tang + Math.PI / 2D;
                    Trig.PRC(Position.X, Position.Y, brg, r, ref EastCentre, ref NorthCentre, 1);
                    Trig.RPC(EastCentre, NorthCentre, inVector.X, inVector.Y, ref brg, ref dist, 1);
                    if (r < 0)
                    {
                        outVector.offset = r + dist;
                    }
                    else
                    {
                        outVector.offset = r - dist;
                    }

                    if (r < 0)
                    {
                        theta2 = Trig.FnSmallAngle(brg, (tang + Math.PI / 2D));
                    }
                    else
                    {
                        theta2 = Trig.FnSmallAngle(brg, (tang - Math.PI / 2D));
                    }
                    diff = theta2 * -r;

                    if (Math.Abs(theta2) < 0.00000001 || Math.Abs(diff) < 0.005)
                    {
                        found = true;
                    }
                    else
                    {
                        outVector.chainage = outVector.chainage + diff;
                        found = false;
                        if ((outVector.chainage + Math.Abs(diff) * (maxtried - tried)) < element.Chainage) { found = true; }
                        if ((outVector.chainage - Math.Abs(diff) * (maxtried - tried)) > nextElement.Chainage) { found = true; }

                    }

                    if (Math.Abs(diff) > Math.Abs(diff2))
                    {
                        outVector.chainage = outVector.chainage - diff - 2 * diff2;
                        outVector.bearing = theta2;
                        diff = -diff2;
                    }
                    tried++;

                }
                while (found == false && tried <= maxtried);

                if (found == false) { bollocks = true; }
            }

            if (nextElement != null)
            {

                if ((outVector.chainage < nextElement.Chainage && outVector.chainage >= element.Chainage) || MathsHelpers.Vector.FnEquals(element.Chainage, outVector.chainage))
                {
                    found = true;
                }
                else if (previousElement == null)
                {
                    if (outVector.chainage < nextElement.Chainage && outVector.chainage >= element.Chainage)
                    {
                        found = true;
                    }
                    else
                    {
                        found = false;
                    }
                }
                else
                {
                    found = false;
                }
            }

            if (bollocks == true)
            {
                found = false;
            }
            else
            {
                Point2D pt = new Point2D();
                bool ok = new bool();

                double brg2 = 0;
                double r2 = 0;

                Coords(outVector.chainage,element , this.Elements.ElementAt(lastEle), nextElement, ref pt,   ref brg2  ,ref r2,ref ok);
                outVector.bearing = brg2;
            }
            //lastEle = ele;
            
            return found;
        }

        public List<Point2D> CreateThrupennyBit(double interval)
        {
            double thisCh = this.MinChainage;
            List<Point2D> outlist = new List<Point2D>();
            for (double curch = this.MinChainage; curch <= this.MaxChainage; curch = curch + interval)
            {
                Point2D pt = this.CoordsAtChAndOffset(curch, 0.0f);
                outlist.Add(pt);
            }
            return outlist;
        }

        #endregion
    }

    public class VerticalAlignment
    {
        #region Properties

        public List<VerticalElement> Elements { get { return PrivateElements; } }

        private List<VerticalElement> PrivateElements { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// AddElement - Add a new horizontal element to the alignment. 
        /// Once the new element is added to the list, the whole list is
        /// sorted by chainage
        /// </summary>
        /// <param name="element"></param> new element to add
        public void AddElement(VerticalElement element)
        {

            if (PrivateElements == null)
            {
                // instantiate
                PrivateElements = new List<VerticalElement>();
            }

            //Add to the array 
            PrivateElements.Add(element);
            //sort by chainage
            PrivateElements.Sort((x, y) => x.Chainage.CompareTo(y.Chainage));
            // Console.WriteLine(privateElements[0].Chainage);
        }

        public int LocateVerticalElement(double chainage)
        {
            int i = 0;
            VerticalElement ele = new VerticalElement();
            if (PrivateElements == null)
            {
                return -1;

            }
            else
            {
                for (i = 0; i < PrivateElements.Count - 1; i++)
                {
                    if (chainage >= PrivateElements[i].Chainage && chainage <= PrivateElements[i + 1].Chainage) { return i; }
                }
                return -1;
            }
        }

        /// <summary>
        /// GradeLevel - Calculate the vertical alignment level at a given chainage.
        /// </summary>
        /// <param name="chainage">Chainage in metres along the alignment that we want to interogate</param>
        /// <returns>Returns the level of the vertical alignment</returns>
        public double GradeLevel(double chainage)
        {
            return GradeLevel(chainage, out double temp);
        }

        /// <summary>
        /// GradeLevel - calculate the vertical aligmment level at a given chainage
        /// </summary>
        /// <param name="chainage">Chainage in metres along the alignment that we want to interogate</param>
        /// <param name="vAngle">The vertical angle of the alignment at the given chainage</param>
        /// <returns>Returns the level of the vertical alignment</returns>
        public double GradeLevel(double chainage, out double vAngle)
        {
            int idx = -1;
            double x = 0, l = 0, Y = 0, h = 0, g = 0, m = 0;
            double grade = 0;
            VerticalElement ele = new VerticalElement();
            VerticalElement nextEle = new VerticalElement();
            vAngle = 0;
            if (PrivateElements == null)
            {
                return -999;
            }
            else if (PrivateElements.Count == 0)
            {
                return -999;
            }

            idx = LocateVerticalElement(chainage);
            if (idx < 0 || idx >= PrivateElements.Count - 1)
            {
                if (ele.Chainage > chainage)
                {
                    ele = new VerticalElement();
                    ele = PrivateElements[0];
                }
                else
                {
                    ele = new VerticalElement();
                    ele = PrivateElements[PrivateElements.Count - 1];
                }
                return ele.Level;
            }
            else if (idx < PrivateElements.Count - 1)
            {
                ele = PrivateElements[idx];
                nextEle = PrivateElements[idx + 1];

                if (nextEle.Chainage >= ele.Chainage)
                {
                    x = chainage - ele.Chainage;
                    l = nextEle.Chainage - ele.Chainage;
                    if (l != 0)
                    {
                        if (ele.ElementType == VerticalElementType.Straight)
                        {
                            double ht = Interpolation.FnInterpolateY(ele.Chainage, ele.Level, nextEle.Chainage, nextEle.Level, chainage);
                            if (NRG.MathsHelpers.Vector.FnEquals(ele.Level, nextEle.Level))
                            {
                                vAngle = 0;
                            }
                            else
                            {
                                vAngle = -Math.Atan((nextEle.Level - ele.Level) / l);
                            }    

                            return ht;
                        }   
                        else
                        {
                            Y = l * l / (2 * ele.Radius);
                            h = nextEle.Level - Y;
                            g = (h - ele.Level) / l;
                            h = x * x / (2 * ele.Radius);
                            m = (x * g) + ele.Level;

                            double ht = m + h;

                            if (MathsHelpers.Vector.FnEquals(x, 0))
                            {
                                grade = g;
                            }
                            else
                            {
                                grade = g + Math.Tan(x / ele.Radius);
                            }
                            if (MathsHelpers.Vector.FnEquals(grade, 0))
                            {
                                vAngle = Math.PI / 2;
                            }
                            else
                            {
                                vAngle = -Math.Atan(grade);
                            }
                            return ht;
                        }
                    }
                    else
                    {
                        vAngle = 0;
                        return ele.Level;
                    }
                }
                else if (Math.Abs(nextEle.Chainage - ele.Chainage) < 0.001)
                {
                    vAngle = 0;
                    return ele.Level;
                }
                else
                {
                    vAngle = 0;
                    return 0;
                }
            }
            else if (idx == PrivateElements.Count - 1)
            {
                ele = PrivateElements[PrivateElements.Count - 1];
                return ele.Level;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the length of the vertical alignment
        /// </summary>
        /// <returns>Returns the length</returns>
        public double LengthOfVerticalAlignment()
        {
            double length = 0;

            //Find consecutive pair of elements
            for(int i = 0; i < PrivateElements.Count - 1; i++)
                length += PrivateElements[i].LengthToElement(PrivateElements[i + 1]);

            return length;
        }

        /// <summary>
        /// Gets the length of the vertical alignment within two chainages
        /// </summary>
        /// <param name="startCh">The chainage to get the length from</param>
        /// <param name="endCh">The chainage to get the length to</param>
        /// <returns>Returns the length</returns>
        public double LengthOfVerticalAlignment(double startCh, double endCh)
        {
            double length = 0;

            var startChIndex = LocateVerticalElement(startCh);
            var endChIndex = LocateVerticalElement(endCh);

            //If either ch is not on the vertical alignment return a length of 0
            if (startChIndex == -1 || endChIndex == -1)
                return length;

            //Interate though each segment and add the element length
            for(int i = startChIndex; i <= endChIndex && i < PrivateElements.Count - 1; i++)
            {
                var startEle = PrivateElements[i];
                var endEle = PrivateElements[i + 1];
                var segmentLength = startEle.LengthToElement(endEle);

                //Calculate the length of the whole segment
                var chLength = endEle.Chainage - startEle.Chainage;
                double lengthToSubtract = 0;

                //Calculate the length of segment outside the chaiange bounds
                if(i == startChIndex)
                {
                    var distIntoEle = startCh - startEle.Chainage;
                    lengthToSubtract += (distIntoEle / chLength) * segmentLength;
                }

                if(i == endChIndex)
                {
                    var distIntoEle = endEle.Chainage - endCh;
                    lengthToSubtract += (distIntoEle / chLength) * segmentLength;
                }

                length += segmentLength - lengthToSubtract;
            }

            return length;
        }

        /// <summary>
        /// Calculate the chainage value for a given length on the vertical alignment
        /// </summary>
        /// <param name="length">The length along the vertical aligment</param>
        /// <returns>Returns the chainage value</returns>
        public double ChainageAtVerticalLength(double length)
        {
            double ch = 1E20;

            //If the length is negative its not valid
            if (length < 0)
                return ch;

            double startLength = 0;
            
            //Find consecutive pairs of elements
            for(int i = 0; i < PrivateElements.Count - 1; i++)
            {
                var startEle = PrivateElements[i];
                var endEle = PrivateElements[i + 1];

                var segmentLength = startEle.LengthToElement(endEle);

                if(length < startLength + segmentLength || Vector.FnEquals(length, startLength + segmentLength, 0.0000001))
                {
                    //The length value is found in this segment
                    var chLength = endEle.Chainage - startEle.Chainage;
                    var distToLength = length - startLength;
                    ch = startEle.Chainage + ((distToLength / segmentLength) * chLength);
                    break;
                }

                startLength += segmentLength;
            }

            return ch;
        }

        /// <summary>
        /// Calculates the length along the vertical alignment at a given chainage
        /// </summary>
        /// <param name="chainage">The chainage along the alignment</param>
        /// <returns>Returns the length along the vertical alignment</returns>
        public double VerticalLengthAtChainage(double chainage)
        {
            double length = 0;

            if (chainage <= PrivateElements.FirstOrDefault().Chainage)
                return length;

            //Find consecutive pairs of elements
            for(int i = 0; i < PrivateElements.Count - 1; i++)
            {
                var startEle = PrivateElements[i];
                var endEle = PrivateElements[i + 1];

                var segmentLength = startEle.LengthToElement(endEle);

                if(chainage < endEle.Chainage || Vector.FnEquals(chainage, endEle.Chainage, 0.0000001))
                {
                    //The chainage value is found in this segment
                    var chLength = endEle.Chainage - startEle.Chainage;
                    var distToChainage = chainage - startEle.Chainage;
                    length += ((distToChainage / chLength) * segmentLength);
                    break;
                }

                length += segmentLength;
            }

            return length;
        }

        #endregion
    }

    
    //Must be copied to alignment viewer 
    /// <summary>
    ///ChainageEquation - All code added by ES: 24.03.22
    ///It's basically a lookup 'if alignment chainage is this then report chainage as that.'
    /// </summary>
    public class ChainageEquation
    {       
        public string Description = "";
        public double AlignmentChainage = 0; //un-transformed chainage directly from the Hz Alignnent 
        public double ForwardChainage = 0;  //Chainage of alignmentChainage in the transformed system
                                      
        public double ChainageShift { get { return ForwardChainage - AlignmentChainage; } } //'Get' - only as it's calculated from input vars above

        /// <summary>
        /// TransformChainage - added by ES:25.03.22
        /// Takes a calculated chainage from the alignment and adds the equation shift
        /// </summary>
        /// <param name="InputChainage"></param>
        /// <returns>Chainage in forward chainage system</returns>
        public double TransformChainage(double InputChainage)
        {
            return InputChainage + ChainageShift;
        }

        /// <summary>
        /// UnTransformChainage - added by ES:25.03.22
        /// Takes a chainage from the forward system, negates the equation shift to move the chainage back to alignment chainage
        /// </summary>
        /// <param name="InputChainage"></param>
        /// <returns>Chainage in forward chainage system</returns>
        public double UnTransformChainage(double InputChainage)
        {
            return InputChainage - ChainageShift;
        }
    }

    /// <summary>
    /// ChainageEquationSet - All code written by ES:24.03.22
    /// Wrapper for a list of the above - with a few helpers for good measure
    /// </summary>
    public class ChainageEquationSet
    {
    
        private List <ChainageEquation> Equations = new List<ChainageEquation> ();

        /// <summary>
        /// Overides the below - work it out for yourself!
        /// </summary>
        /// <param name="Description"></param>
        /// <param name="AlignmentChainage"></param>
        /// <param name="ForwardChainage"></param>
        public void AddEquation(string Description, double AlignmentChainage, double ForwardChainage)
        {
            ChainageEquation e = new ChainageEquation();
            e.Description = Description;    
            e.AlignmentChainage = AlignmentChainage;   
            e.ForwardChainage= ForwardChainage;
            AddEquation(e);
        }
        /// <summary>
        /// Add an element to the list - base method
        /// </summary>
        /// <param name="newEquation"></param>
        public void AddEquation(ChainageEquation newEquation)
        {
            if (Equations == null) { Equations = new List<ChainageEquation>(); }
            Equations.Add(newEquation);

        }

        /// <summary>
        /// Sorts list by Alignment chainage - helps with the element search in the tranform method
        /// </summary>
        private void SortByAlignmentChainage()
        {
            Equations.Sort((x, y) => x.AlignmentChainage.CompareTo(y.AlignmentChainage));

        }

        /// <summary>
        /// Sorts list by forward chianages - can be used in the un-transform method, if we ever need one!
        /// </summary>
        private void SortByForwardChainage()
        {
            Equations.Sort((x, y) => x.ForwardChainage.CompareTo(y.ForwardChainage));
        }

        /// <summary>
        /// Transform - finds the first relevent equation in the equation List then returns the Chainage relative to the equation
        /// </summary>
        /// <param name="Chainage"> Actual alignment chainage</param>
        /// <returns>New chainage as calulated by the relevant chainage equation</returns>
        public double Transform(double Chainage)
        {

            if (Equations != null && Equations.Count > 0)
            {
                SortByAlignmentChainage();//sort the list by the alignment chainages - makes the following search easier as we don't need to check the next element

                foreach (ChainageEquation e in Equations)
                {
                    if (Chainage >= e.AlignmentChainage)
                    {
                        return e.TransformChainage(Chainage);//that'll do - requested chainage is great or equal to the alignment chainage. No need to check next.
                    }
                }
            }
            //Nothing was found, so return the input.
            return Chainage;
            
        }

        public double UnTransform(double Chainage)
        {
            if (Equations != null && Equations.Count > 0)
            {
                SortByForwardChainage();//sort the list by the forward chainages

                foreach (ChainageEquation e in Equations)
                {
                    if (Chainage >= e.ForwardChainage)
                    {
                        return e.UnTransformChainage(Chainage);//that'll do - requested chainage is greater or equal to the forward chainage. No need to check next.
                    }
                }
            }

            //effectively the default state if there's no equations or something went wrong then return the chainage passed into the method.
            return Chainage;
        }


    }

    //[/Must be copied to alignment viewer]

    public class RailwayCant
    {
        #region Properties

        private List<CantElement> Elements { get; set; } = new List<CantElement>();

        public void AddCant(double Chaiange, double Cant)
        {
            CantElement ele = new CantElement();
            ele.Chainage = Chaiange;
            ele.Cant = Cant;
            Elements.Add(ele);
        }

        public void AddCant(CantElement Cant)
        {
            Elements.Add(Cant);
        }

        public double GetCantAt(double Chainage)
        {
            if (Elements.Count == 0)
            {
                return 0;
            }

            int count = 0;

            foreach(CantElement ele in Elements)
            {
                count++;
                if (Chainage >= ele.Chainage)
                {
                    if (count == Elements.Count)
                    {
                        return Elements.ElementAt(count - 1).Cant;
                    }
                    else
                    {
                        CantElement nextEle =  Elements.ElementAt(count);
                        if(Chainage <= nextEle.Chainage)
                            return Interpolation.FnInterpolateY(ele.Chainage, ele.Cant, nextEle.Chainage, nextEle.Cant, Chainage);
                    }

                }

            }

            return 0;
        }

        #endregion
    }

    public class RoadCrossFall
    {
        private List<CrossFallElement> LeftElements { get; set; } = new List<CrossFallElement>();
        private List<CrossFallElement> RightElements { get; set; } = new List<CrossFallElement>();

        public void AddElement(double chainage, List<double> values, bool isLeft = true)
        {
            var element = new CrossFallElement();
            element.Chainage = chainage;
            element.Values = values;

            if (isLeft)
                LeftElements.Add(element);
            else
                RightElements.Add(element);
        }

        public void AddElement(CrossFallElement xFall, bool isLeft = true)
        {
            if (isLeft)
                LeftElements.Add(xFall);
            else
                RightElements.Add(xFall);
        }

        public double GetCantAt(double chainage)
        {
            int count = 0;

            //Return left if available otherwise return right
            if(LeftElements.Count <= 0)
            {
                if (RightElements.Count <= 0)
                    return 0;
                else
                {
                    foreach(var ele in RightElements)
                    {
                        count++;
                        if(chainage >= ele.Chainage)
                        {
                            if(count == RightElements.Count)
                            {
                                if (RightElements.Last().Values.Count <= 0)
                                    return 0;

                                return Alignment.XFallToCant(RightElements.Last().Values.First());
                            }
                            else
                            {
                                if (ele.Values.Count <= 0)
                                    return 0;

                                var nextEle = RightElements[count];

                                if (nextEle.Values.Count <= 0)
                                    return 0;

                                if (chainage <= nextEle.Chainage)
                                    return Interpolation.FnInterpolateY(ele.Chainage, Alignment.XFallToCant(ele.Values.First()), nextEle.Chainage, Alignment.XFallToCant(nextEle.Values.First()), chainage);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach(var ele in LeftElements)
                {
                    count++;
                    if(chainage >= ele.Chainage)
                    {
                        if(count == LeftElements.Count)
                        {
                            if (LeftElements.Last().Values.Count <= 0)
                                return 0;

                            return Alignment.XFallToCant(LeftElements.Last().Values.First());
                        }
                        else
                        {
                            if (ele.Values.Count <= 0)
                                return 0;

                            var nextEle = LeftElements[count];

                            if (nextEle.Values.Count <= 0)
                                return 0;

                            if (chainage <= nextEle.Chainage)
                                return Interpolation.FnInterpolateY(ele.Chainage, Alignment.XFallToCant(ele.Values.First()), nextEle.Chainage, Alignment.XFallToCant(nextEle.Values.First()), chainage);
                        }
                    }
                }
            }

            return 0;
        }
    } 

    public class HorizontalElementDisplay
    {
        #region Properties

        public HorizontalElementType ElementType { get; set; }
        public double Chainage { get; set; }
        public double East { get; set; }
        public double North { get; set; }
        public double Radius { get; set; }
        public double Length { get; set; } //for spirals: the length from the optimum radius to 0 or vis versa. 

        public HorizontalElementDisplay(HorizontalElementType elementType, double chainage, double east, double north, double radius, double length = 0)
        {
            this.ElementType = elementType;
            this.Chainage = chainage;
            this.East = east;
            this.North = north;
            this.Radius = radius;
            this.Length = length;
        }

        #endregion
    }

    public class HorizontalElement
    {
        #region Properties

        //User editable data
        public HorizontalElementType ElementType { get; set; }
        public Double Chainage { get; set; }
        public Double East { get; set; }
        public Double North { get; set; }
        public Double Radius { get; set; }
        public Double Length { get; set; } //for spirals: the length from the optimum radius to 0 or visa versa. 

        public double BrgError { get; set; } //used when bound to a grid to show the column "BearingError". Needs updating on any sort of change of the above parameters
        public string BrgErrorString { get { return CreateBearingErrorString(BrgError); } }

        //Extended data - this is calulated but never stored or displayed. Used for chainage & offset calulations
        public double ChainageOrigin { get; set; }
        public double EastOrigin { get; set; }
        public double NorthOrigin { get; set; }
        public double BrgOrigin { get; set; }
        public double RadiusOrigin { get; set; }

        public bool CenterPointSet { get; set; }
        public double CenterEast{ get; set; }
        public double CenterNorth{ get; set; }

        public double RL { get; set; }



        //Added by ES:08.11.22 - for special(ish) cases during LandXML import
        public bool EndPointSet { get; set; }
        public double EndEast { get; set; }
        public double EndNorth { get; set; }

        
        public HorizontalElementDisplay DisplayElement
        {
            set
            {
                ElementType = value.ElementType;
                Chainage = value.Chainage;
                East = value.East;
                North = value.North;
                Radius = value.Radius;
                Length = value.Length;
            }
            get
            {
                return new HorizontalElementDisplay(ElementType, Chainage, East, North, Radius, Length);
            }
        }


        #endregion

        #region Methods
        private string CreateBearingErrorString(double bearingError)
        {
            return NRG.MathsHelpers.Trig.RadToDMS(bearingError);
        }
        #endregion

    }

    public class VerticalElement
    {
        #region Properties

        public VerticalElementType ElementType { get; set; }
        public Double Chainage { get; set; }
        public Double Level { get; set; }
        public Double Radius { get; set; }

        #endregion

        #region Methods

        public double LengthToElement(VerticalElement nextElement)
        {
            try
            {
                var dist = (double)((decimal)(Vector.FnDistance(Chainage, Level, nextElement.Chainage, nextElement.Level)));

                if (Radius != 0)
                {
                    if (Vector.FnEquals(dist, 0))
                        return (Radius * 2) * Math.PI;
                    else
                    {
                        var temp = dist / (2 * Radius);
                        if (Vector.FnEquals(temp, 1, 0.0000001))
                            temp = 1;

                        return Radius * (2 * Math.Asin(temp));
                    }
                }
                else
                    return dist;
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }

    public class CantElement 
    {
        #region Properties

        public double Chainage { get; set; }
        public double Cant { get; set; }

        #endregion
    }

    public class CrossFallElement
    {
        public double Chainage { get; set; }
        public List<double> Values { get; set; }

        public CrossFallElement()
        {
            Values = new List<double>();
        }
    }

    public class NstHorizontalAlignment
    {
        public double ChStart { get; set; }
        public double ChOrigin { get; set; }
        public double Easting { get; set; }
        public double Northing { get; set; }
        public double Bearing { get; set; }
        public double Radius { get; set; }
        public double Rl { get; set; }
    }

    public class NstVerticalAlignment
    {
        public double ChStart { get; set; }
        public double Level { get; set; }
        public double Radius { get; set; }
    }

    public class NstCant
    {
        public double ChStart { get; set; }
        public List<double> Cant { get; set; }

        public NstCant()
        {
            Cant = new List<double>();
        }
    }

    public class AlignmentVector5
    {
        public double chainage { get; set; }
        public double offset { get; set; }
        public double bearing { get; set; }
        public double radius { get; set; }
        public int element { get; set; }
    }

	public class AlignmentDetails
	{
		public double HorizontalLength { get; set; }
		public double MinimumRadius { get; set; }
		public double MaximumRadius { get; set; }
		public double MinimumSprialLength { get; set; }
		public double MaximumSpriralLength { get; set; }
		public int NumberElements { get; set; }

		public AlignmentDetails()
		{
			HorizontalLength = 0;
			MinimumRadius = double.PositiveInfinity;
			MaximumRadius = double.NegativeInfinity;
			MinimumSprialLength = double.PositiveInfinity;
			MaximumSpriralLength = double.NegativeInfinity;
			NumberElements = 0;
		}
	}

    public class ChainageMarker
    {
        //public Rendering.SharedRenderItem RenderItem { get; set; }

        public List<Point2D> points = new List<Point2D>();

        //I have never seen anyone make such a meal of something so simple in my entire life!! ES:28.07.22

        public bool GenerateMarker(Point2D AlignmentPoistion, double tangentBearing, double Offset, double Flagsize)
        {


            if (AlignmentPoistion != null)
            {   
                double tang = tangentBearing + Math.PI / 2;
                double tang2 = tangentBearing - Math.PI / 2;
                
                if (tang > 2 * Math.PI)
                    tang -= Math.PI * 2;

                if (tang < 0)
                    tang += Math.PI * 2;


                Point2D sp = MathsHelpers.Trig.PRC(AlignmentPoistion.X, AlignmentPoistion.Y, tang, Math.Abs(Offset));
                points.Add(sp);

                Point2D ep = MathsHelpers.Trig.PRC(AlignmentPoistion.X, AlignmentPoistion.Y, tang2, Math.Abs(Offset));
                points.Add(ep);


                Point2D mp =  MathsHelpers.Trig.PRC(ep.X, ep.Y, tang, Math.Abs(Flagsize/2));

                Point2D ap = MathsHelpers.Trig.PRC(mp.X, mp.Y, tangentBearing, Math.Abs(Flagsize));

                points.Add(ap);

                ap = new Point2D();
                ap = MathsHelpers.Trig.PRC(ep.X, ep.Y, tang, Math.Abs(Flagsize ));

                points.Add(ap);

                ap = new Point2D();
                ap = MathsHelpers.Trig.PRC(sp.X, sp.Y, tang2, Math.Abs(Flagsize));
                points.Add(ap);

                mp = MathsHelpers.Trig.PRC(sp.X, sp.Y, tang2, Math.Abs(Flagsize/2));


                ap = new Point2D();

                ap = MathsHelpers.Trig.PRC(mp.X, mp.Y, tangentBearing, Math.Abs(Flagsize));
                points.Add(ap);

                points.Add(sp); 

                return true;
            }

            return false;
        }
    }

    public class CollectionOfAllChainageMarkers
    {
        public List<ChainageMarker> Markers { get; set; }
        public NRG.Models.Rendering.SharedRenderItem RenderItem { get; set; }

        public CollectionOfAllChainageMarkers()
        {
            Markers = new List<ChainageMarker>();
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.IO;
using NRG.Models;
using System.Windows.Forms;
using System.ComponentModel;


namespace NRG.Import
{
    
    
    public static class LandXMLImport
    {
        

        #region Setup
        private class XMLSetup
        {
            public StationPoint FromStation = new StationPoint(PointType.OccupiedStation);
            public BindingList<DTMPoint> Obs = new BindingList<DTMPoint>();

        }
        //Moved to general helpers by ES:15.06.22

        //public class ModelImport
        //{
        //    public List<DTM> Models = new List<DTM>();
        //    public List<Alignment> Geometry = new List<Alignment>();
        //}

        private class VIPAlignment
        {
            public List<VIP> Ips = new List<VIP>();

            /// <summary>
            /// Converts vertical intersection points stored in Ips into tangent points for a more 
            /// 'NRG' feel.
            /// </summary>
            /// <returns>an NRG vertical alignment</returns>
            /// There's 2 types on VIP that we support, circular and parabolic. There's a lot of
            /// cross over, but for ease I've used an if condition and some repeating code...sorry
            public VerticalAlignment ExtractVertical()
            {
                if (Ips != null && Ips.Count > 1)
                {
                    VerticalAlignment alignment = new VerticalAlignment();
                    int counter = 0;
                    foreach (VIP vip in Ips)
                    {
                        counter++;
                        if (vip.type == 0)
                        {
                            VerticalElement ele = new VerticalElement();
                            ele.Chainage = vip.Chainage;
                            ele.Level = vip.Level;
                            ele.Radius = vip.Radius;//we hope this is zero...I think!
                            alignment.AddElement(ele);
                        }
                        else
                        {
                            if (alignment.Elements == null)
                                continue;
                            if (alignment.Elements.Count > 0)
                            {
                                if (vip.Radius != 0)//circular curve - don't see many of these, so keep and eye on it
                                {
                                    VerticalElement lastele = alignment.Elements.Last();
                                    double ig = Math.Asin((vip.Level - lastele.Level) / (vip.Chainage - lastele.Chainage));
                                    double og = ig - (vip.Length / vip.Radius);

                                    double da = (vip.Length / vip.Radius) / 2; //deflection angle
                                    double tl = vip.Radius * Math.Tan(da); //Tangent length

                                    VerticalElement ele = new VerticalElement();
                                    ele.Chainage = vip.Chainage - (vip.Length / 2);

                                    ele.Radius = vip.Radius;
                                    ele.Level = vip.Level - (Math.Sin(ig) * tl);

                                    if (vip.Level < ele.Level)
                                        ele.Radius = Math.Abs(ele.Radius);
                                    else
                                        ele.Radius = Math.Abs(-ele.Radius);

                                    alignment.AddElement(ele);

                                    ele = new VerticalElement();

                                    ele.Chainage = vip.Chainage + vip.Length / 2;
                                    ele.Radius = 0;
                                    ele.Level = vip.Level + (Math.Sin(og) * tl);
                                    ele.ElementType = VerticalElementType.Straight;
                                    alignment.AddElement(ele);

                                }
                                else //parabola
                                {
                                    VerticalElement lastele = alignment.Elements.Last();

                                    VerticalElement ele = new VerticalElement();
                                    ele.Chainage = vip.Chainage - (vip.Length / 2);

                                    VIP nextvip = Ips.ElementAt(counter);
                                    VIP lastvip = Ips.ElementAt(counter - 2);

                                    //calc grade in  and grade out
                                    double ig = ((lastvip.Level - vip.Level) / (lastvip.Chainage - vip.Chainage));
                                    double og = ((nextvip.Level - vip.Level) / (nextvip.Chainage - vip.Chainage));

                                    //parabola doesn't specify radius, so it needs to be calculated...
                                    ele.Radius = vip.Length / (og - ig);

                                    double da = (vip.Length / ele.Radius) / 2; //deflection angle
                                    double tl = ele.Radius * Math.Tan(da); //Tangent length

                                    ele.Level = vip.Level - (Math.Sin(ig) * tl);
                                    //============================================================================================
                                    //Commented out by ES:07.12.22 - I don't really know how you determine if it's a sag or not!
                                    //Maybe it comes out in the wash? Anyhow try this for now, as it suits one of the A30 alignments - by Try This I mean comment it out!
                                    //I was making this up as I went along anyhow.
                                    //if (vip.Level < ele.Level)
                                    //    ele.Radius = Math.Abs(ele.Radius);
                                    //else
                                    //    ele.Radius = -Math.Abs(ele.Radius);
                                    //============================================================================================
                                    ele.ElementType = VerticalElementType.Parabola;


                                    //Added by ES:11.12.22 - occasionally getting repeating elements (usually at the beginning of the alignment). Often it would be a VIP followed by parabolic tangent points
                                    //this following seems somewhat of a hack, but appears to do a job. I think going forward we need the ability to view and edit the tabular data, rather than having to bring it into
                                    //NRGWin!

                                    bool addIt = true;

                                    if (alignment != null && alignment.Elements != null && alignment.Elements.Count  > 0 && lastele != null)
                                    {
                                        if (MathsHelpers.Vector.FnEquals(ele.Chainage, lastele.Chainage) & MathsHelpers.Vector.FnEquals(ele.Level, lastele.Level))
                                        {
                                            if (lastele.Radius == 0)
                                            {
                                                lastele.Radius = ele.Radius;
                                                lastele.ElementType = VerticalElementType.Parabola;
                                            }
                                            addIt = false;
                                        }
                                    }

                                    if (addIt)
                                    {
                                        alignment.AddElement(ele);//tangent point completed add it to the list
                                    }

                                    if (vip == Ips.Last())
                                    {
                                        //now stick a tangent point at the end of the element
                                        ele = new VerticalElement();
                                        ele.Chainage = vip.Chainage + (vip.Length / 2);
                                        ele.Level = vip.Level + (Math.Sin(og) * tl);
                                        ele.Radius = vip.Radius;
                                       
                                        alignment.AddElement(ele);
                                    }
                                }
                            }
                        }
                    }
                    return alignment;
                }
                return null;
            }
        }

        /// <summary>
        /// Vertical in
        /// </summary>
        public class VIP
        {
            public int type { get; set; } //0 = IP  - 1 = curve. we treat parabolic and circular the same as the calcs
            public double Length { get; set; }
            public double Chainage { get; set; }
            public double Level { get; set; }
            public double Radius { get; set; }
        }

        #endregion


        #region Methods
        public static GeneralHelpers.ModelImport Import(string filename)
        {


            if (!File.Exists(filename))
            {
                return null;
            }

            XDocument xml = XDocument.Load(filename);

            if (xml == null)
                return null;

            //DTM model = new DTM();
            var name = Path.GetFileNameWithoutExtension(filename);

            var features = ParsePlanFeatures(xml);
            features.Name = name + "_Plan_Features";
            var models = ParseSurfaces(xml);

            if (!features.IsEmpty)
            {
                models.Insert(0, features);
            }

            var obs = ParseObs(xml);
            if (!obs.IsEmpty)
            {
                obs.Name = name + "_Obs";
                models.Insert(0, obs);

            }


            var alignments = ParseAlignments(xml);

            models.ForEach(p =>
            {
                p.FilePath = filename;
                p.SaveRequired = true;
            });

            GeneralHelpers.ModelImport imported = new GeneralHelpers.ModelImport();



            foreach(DTM m in models)
            {
                imported.Models.Add(m);    
            }

            foreach (Alignment alg in alignments)
            {
                imported.Alignments.Add(alg);
            }




            if (imported.Models.Count > 0 || imported.Alignments.Count > 0)
                return imported;
            else
                return null;



        }

        private static DTM ParsePlanFeatures(XDocument xml)
        {
            var thisFeature = new DTM();
            XNamespace ns = xml.Root.Name.Namespace;
            if (xml.Descendants(ns + "PlanFeatures").Any())
            {
                foreach (XElement item2 in xml.Descendants(ns + "PlanFeatures"))
                {
                    if (item2.Descendants(ns + "PlanFeature").Any())

                    {
                        string stringname = "";
                        bool started = false;
                        foreach (XElement item in item2.Descendants(ns + "PlanFeature"))
                        {
                            if (item.Attributes("name").Any())
                            {
                                stringname = item.Attribute("name").Value.ToString().Replace(" ", "");
                            }
							

							if (item.Descendants(ns + "CoordGeom").Any())
                            {
                                foreach (XElement coords in item.Descendants(ns + "CoordGeom"))
                                {
                                    if (coords.Descendants(ns + "Line").Any())
                                    {
                                        started = false;
                                        foreach (XElement ln in coords.Descendants(ns + "Line"))
                                        {
                                            double x = 0;
                                            double y = 0;
                                            double z = 0;
                                            DTMPoint pt = new DTMPoint();

                                            if (started == false)
                                            {
                                                DTMPoint spt = new DTMPoint();

                                                spt.PointLabel = stringname + "/ST";
                                                var scrd = ln.Element(ns + "Start").Value.Split();
                                                double.TryParse(scrd[0], out y);
                                                double.TryParse(scrd[1], out x);
                                                double.TryParse(scrd[2], out z);
                                                spt.X = x; spt.Y = y; spt.Z = z;
                                                thisFeature.Points.Add(spt);
                                                started = true;

                                            }
                                            pt.PointLabel = stringname;
                                            var crd = ln.Element(ns + "End").Value.Split();
                                            double.TryParse(crd[0], out y);
                                            double.TryParse(crd[1], out x);
                                            double.TryParse(crd[2], out z);
                                            pt.X = x; pt.Y = y; pt.Z = z;
                                            
                                            thisFeature.Points.Add(pt);
                                        }
                                    }
                                }
                            }


                        }
                    }

                }

            }

            return thisFeature;
        }

        private static List<DTM> ParseSurfaces(XDocument xml)
        {
            List<DTM> DTMModels = new List<DTM>();

            XNamespace ns = xml.Root.Name.Namespace;
            var surfacesName = ns + "Surfaces";
            var surfaceName = ns + "Surface";
            var breakLinesName = ns + "Breaklines";
            var breakLineName = ns + "Breakline";
            var pntsName = ns + "Pnts";
            var facesName = ns + "Faces";

            //Read Surfaces
            foreach (XElement surfaces in xml.Descendants(surfacesName))
            {
                //Read Surface
                foreach (XElement surface in surfaces.Descendants(surfaceName))
                {
                    DTM thisSurface = new DTM();

                    thisSurface.Name = surface?.Attribute("name")?.Value;
                    
                    //Read Breaklines
                    foreach (XElement ln in surface.Descendants(breakLinesName))
                    {
                        //Read BreakLine
                        foreach (XElement brk in ln.Descendants(breakLineName))
                        {

                            string note = "", stringname = "";

                            note = brk.Attribute("desc")?.Value;
                            if (note == null)
                                note = "";

                            stringname = brk.Attribute("name")?.Value;
                            if (stringname != null)
                                stringname = stringname.Replace(" ", "");
                            else
                                stringname = note.Replace(" ", "");

							if (stringname != null || stringname == "")
							{

								


								if (brk.Elements(ns + "Feature").Any())
								{
								   
									var feature = brk.Element(ns + "Feature");

									if (feature.HasAttributes)
									{
										foreach (var att in feature.Attributes())
										{
											
											if (att.Name == "code")
											{
												stringname = att.Value.ToString();
											}
										}
									}

									
								}
	
							}
                            
                            var crds = brk.Value.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                            
                            for (int i = 0; i < crds.Length - 2;)
                            {
                                double z = -999;
                                double.TryParse(crds[i++], out double y);
                                double.TryParse(crds[i++], out double x);
                                double.TryParse(crds[i++], out z);

                                thisSurface.Points.Add(new DTMPoint(x, y, z) { PointLabel = (i == 3) ? stringname + "/ST" : stringname, Notes = new Dictionary<int, string> { { 1, note } } });
                            }
                        }
                    }

                    if (surface.Descendants(pntsName).Any())
                    {
                        #region Initial Points

                        var points = new Dictionary<int, DTMPoint>();

                        foreach (XElement pt in surface.Descendants(pntsName).Nodes())
                        {
                            int idx = -1;
                            int.TryParse(pt.Attribute("id")?.Value, out idx);

                            //If the ID is not valid we don't add the point
                            if (idx < 0)
                                continue;

                            var tmp = pt.Value.Split();

                            double z = -999;
                            double.TryParse(tmp[0], out double y);
                            double.TryParse(tmp[1], out double x);
                            double.TryParse(tmp[2], out z);

                            points[idx] = new DTMPoint(x, y, z);
                        }

                        #endregion initial points

                        #region Faces

                        if (surface.Descendants(facesName).Any())
                        {
                            var contSurface = thisSurface.GetSurface(1);
                            var nonContSurface = thisSurface.GetSurface(0);
                            TriangleSurface s = null;
                            foreach (XElement tri in surface.Descendants(facesName).Nodes())
                            {
                                var tmp = tri.Value.Split();
                                int pt1 = -1; int pt2 = -1; int pt3 = -1;
                                int.TryParse(tmp[0], out pt1);
                                int.TryParse(tmp[1], out pt2);
                                int.TryParse(tmp[2], out pt3);
                                if (pt1 >= 0 && pt2 >= 0 && pt3 >= 0)
                                {
                                    points.TryGetValue(pt1, out DTMPoint triPoint1);
                                    points.TryGetValue(pt2, out DTMPoint triPoint2);
                                    points.TryGetValue(pt3, out DTMPoint triPoint3);

                                    int.TryParse(tri.Attribute("i")?.Value, out int cont);
                                    s = new TriangleSurface();

                                    if (cont == 1)
                                        s = nonContSurface;
                                    else
                                        s = contSurface;

                                    if (MathsHelpers.Vector.FNRightOf(triPoint1.X, triPoint1.Y, triPoint3.X, triPoint3.Y, triPoint2.X, triPoint2.Y) == true)
                                    {
                                        var t = new Triangle(s, triPoint1, triPoint2, triPoint3);
                                        thisSurface.Triangles.Add(t);
                                    }
                                    else
                                    {
                                        var t = new Triangle(s, triPoint1, triPoint3, triPoint2);
                                        thisSurface.Triangles.Add(t);
                                        
                                    }
                                    
                                }

                            }
                        }

                        #endregion Faces

                        #region Add Points

                        foreach (var addpoint in points)
                        {
                            thisSurface.Points.Add(addpoint.Value);
                        }

                        #endregion Add Points

                        DTMModels.Add(thisSurface);



                    }
                    else if (thisSurface.Points.Count > 0)
                    {
                        DTMModels.Add(thisSurface);
                    }
                }
            }

            

            return DTMModels;
        }

        private static List<Alignment> ParseAlignments(XDocument xml, string AlignmentName = "")
        {
            XNamespace ns = xml.Root.Name.Namespace;

            var AlignmentsTag = ns + "Alignments";
            var AlignmentTag = ns + "Alignment";
            var CoordGeomTag = ns + "CoordGeom";
            var ProfileTag = ns + "Profile";
            var ProfileAlignTag = ns + "ProfAlign";
            var StationEquationTag = ns + "StaEquation";
            var CrossSectionsTag = ns + "CrossSects";

            List<Alignment> AlignmentOut = new List<Alignment>();
            

            foreach (XElement algs in xml.Descendants(AlignmentsTag))
            {
                foreach (XElement alg in algs.Descendants(AlignmentTag))
                {
                    Alignment ThisAlignment = new Alignment();

                    #region alignment name
                    if (alg.Attributes("name").Any())
                    {
                        string nme = alg.Attribute("name").Value.ToString();
                        if (nme != "")
                            ThisAlignment.StringName = nme;
                    }
                    double cumCh = 0;
                    if (alg.Attributes("staStart").Any())
                    {
                        if (!double.TryParse(alg.Attribute("staStart").Value, out cumCh))
                            cumCh = 0;
                    }
                    #endregion alignment name

                    if (ThisAlignment.StringName == "M_BJSR_RB03_01")
                    {
                        Console.WriteLine("");
                    }

                    //Added by ES:09.06.22 - Allow caller to import one alignment without parsing the whole file
                    if (AlignmentName != "")
                    {
                        if (ThisAlignment.StringName != AlignmentName)
                            continue;
                    }
                    
                    foreach (XElement cg in alg.Descendants(CoordGeomTag))
                    {
                        {
                            foreach (XElement ele in cg.Elements())
                            {
                                HorizontalElement hz = new HorizontalElement();
                                
                                #region element start coords
                                var startCoords = ele.Element(ns + "Start").Value.ToString().Trim().Split();
                                
                                double.TryParse(startCoords[0], out double y);
                                double.TryParse(startCoords[1], out double x);

                                double cx = 0;
                                double cy = 0;

                                hz.East = x;
                                hz.North = y;

                                hz.EastOrigin = x;
                                hz.NorthOrigin = y;


                                #endregion element start coords


                                if (ele.Element(ns + "Center") != null)
                                {
                                    var centerCoords = ele.Element(ns + "Center").Value.ToString().Trim().Split();
                                    {
                                        //var centerCoords = ele.Element(ns + "Center").Value.ToString().Trim().Split();
                                        double.TryParse(centerCoords[0], out double centy);
                                        double.TryParse(centerCoords[1], out double centx);

                                        hz.CenterEast = centx;
                                        hz.CenterNorth = centy;

                                        hz.CenterPointSet = true;

                                    }
                                }

                                if (ele.Element(ns + "End") != null)
                                {
                                    var endCoords = ele.Element(ns + "End").Value.ToString().Trim().Split();
                                    {
                                        //var centerCoords = ele.Element(ns + "Center").Value.ToString().Trim().Split();
                                        double.TryParse(endCoords[0], out double endy);
                                        double.TryParse(endCoords[1], out double endx);

                                        hz.EndEast = endx;
                                        hz.EndNorth = endy;

                                        hz.EndPointSet = true;

                                    }
                                }






                                #region element radius
                                if (ele.Attributes("radius").Any())
                                {
                                    if (double.TryParse(ele.Attribute("radius").Value, out double rad))
                                    {
                                        if (ele.Attribute("rot").Value.ToString() == "ccw")
                                            rad = -rad;
                                        hz.Radius = rad;
                                        hz.RadiusOrigin = rad;

                                       


                                    }
                                }
                                else if (ele.Attributes("radiusStart").Any())
                                {
                                    if (double.TryParse(ele.Attribute("radiusStart").Value, out double rad))
                                    {
                                        if (ele.Attribute("rot").Value.ToString() == "ccw")
                                            rad = -rad;
                                        hz.Radius = rad;
                                        hz.RadiusOrigin = rad;
                                    }
                                }
                                else
                                    hz.Radius = 0;
                                #endregion element radius
                                
                                #region start bearing
                                hz.BrgOrigin = 0;
                                if (ele.Attributes("dirStart").Any())
                                {
                                    if (double.TryParse(ele.Attribute("dirStart").Value, out double brg))
                                    {
                                        hz.BrgOrigin = brg /180 * Math.PI;
                                    }
                                }
                                else
                                {
                                    if (ele.Attributes("dir").Any())
                                    {
                                        if (double.TryParse(ele.Attribute("dir").Value, out double brg))
                                            hz.BrgOrigin = brg / 180 * Math.PI;
                                    }
                                }
                                #endregion start bearing 

                                
                                #region start chainage
                                if (ele.Attributes("staStation").Any())
                                {
                                    if (double.TryParse(ele.Attribute("staStart").Value, out double chainage))
                                    {
                                        if (chainage < cumCh) { chainage = cumCh; }
                                        hz.Chainage = chainage;
                                    }
                                }
                                else
                                {
                                    hz.Chainage = cumCh;
                                }
                                #endregion start chainage

                                #region element length
                                double length = 0;
                                if (ele.Attributes("length").Any())
                                {
                                    if (double.TryParse(ele.Attribute("length").Value, out length))
                                    {
                                        hz.Length = length;
                                    }
                                    cumCh = cumCh + length;
                                }
                                else
                                {

                                    length = hz.Chainage - cumCh;
                                }
                                #endregion element length

                                #region element specific - Line, Arc Spiral.
                                if (ele.Name.LocalName == "Line")
                                {
                                    hz.ElementType = HorizontalElementType.Straight;
                                    hz.Radius = 0;
                                    

                                }
                                else if (ele.Name.LocalName == "Curve" || ele.Name.LocalName == "Spiral")
                                {
                                    hz.ElementType = HorizontalElementType.Arc;

                                    if (ele.Name.LocalName == "Spiral")
                                    {
                                        hz.ElementType = HorizontalElementType.Spiral;
                                        hz.Length = length;

                                    }

                                    
                                }

                                #endregion element specific - Line, Arc Spiral.

                                

                                ThisAlignment.Horizontal.AddElement(hz);
                                bool midAdded = false;
                                //if we have a centre then chances are we're going to need a mid point as well for the NRG format to be able to cope in future.
                                if (hz.CenterPointSet && hz.ElementType == HorizontalElementType.Arc)
                                {
                                    if (MathsHelpers.Vector.FnDistance(hz.East, hz.North, hz.EndEast, hz.EndNorth) < 0.1)
                                    { 
                                        double abrg = MathsHelpers.Trig.RPC(hz.East, hz.North, hz.CenterEast, hz.CenterNorth );
                                       
                                        Point2D mid = MathsHelpers.Trig.PRC(hz.CenterEast, hz.CenterNorth, abrg, Math.Abs(hz.Radius));
                                        HorizontalElement mhz = new HorizontalElement();
                                        mhz.ElementType = HorizontalElementType.Arc;
                                        mhz.East = mid.X;
                                        mhz.North = mid.Y;
                                        mhz.Radius = hz.Radius;
                                        mhz.RadiusOrigin = mhz.Radius;
                                        mhz.Chainage = hz.Chainage + length / 2;
                                        mhz.ChainageOrigin = mhz.Chainage;

                                        mhz.CenterPointSet = true ;
                                        mhz.CenterEast = hz.CenterEast;
                                        mhz.CenterNorth = hz.CenterNorth;

                                        midAdded = true;
                                        ThisAlignment.Horizontal.AddElement(mhz);

                                    }
                                    else if (ele.Element(ns + "PI") != null)
                                    {
                                        var piCoords = ele.Element(ns + "PI").Value.ToString().Trim().Split();
                                        {
                                            HorizontalElement mhz = new HorizontalElement();
                                            double.TryParse(piCoords[0], out double piy);
                                            double.TryParse(piCoords[1], out double pix);
                                            double abrg = MathsHelpers.Trig.RPC(hz.CenterEast, hz.CenterNorth, pix, piy);

                                            Point2D mid = MathsHelpers.Trig.PRC(hz.CenterEast, hz.CenterNorth, abrg, Math.Abs(hz.Radius));
                                            mhz.ElementType = HorizontalElementType.Arc;
                                            mhz.East = mid.X;
                                            mhz.EastOrigin = mhz.East;
                                            mhz.North = mid.Y;
                                            mhz.NorthOrigin = mhz.North;
                                            mhz.Radius = hz.Radius;
                                            mhz.RadiusOrigin = mhz.Radius;
                                            //mhz.BrgOrigin = abrg;
                                            mhz.Chainage = hz.Chainage + length / 2;
                                            mhz.ChainageOrigin = mhz.Chainage;
                                            if (mhz.Radius < 0)
                                            {
                                                mhz.BrgOrigin -= Math.PI / 2;
                                                if (mhz.BrgOrigin < 0)
                                                {
                                                    mhz.BrgOrigin += Math.PI * 2;
                                                }
                                            }
                                            else
                                            {
                                                mhz.BrgOrigin += Math.PI / 2;
                                                if (mhz.BrgOrigin > Math.PI * 2)
                                                {
                                                    mhz.BrgOrigin -= Math.PI * 2;
                                                }
                                            }

                                            mhz.CenterPointSet = true;
                                            mhz.CenterEast = hz.CenterEast;
                                            mhz.CenterNorth = hz.CenterNorth;


                                            midAdded = true;
                                            ThisAlignment.Horizontal.AddElement(mhz);

                                        }
                                    }

                                }

                                

                                //Check for a straight element at the end...
                                if (ele == cg.LastNode)
                                {
                                

                                    hz = new HorizontalElement();

                                    
                                    
                                    if (ele.Attributes("length").Any() && double.TryParse(ele.Attribute("length").Value,  out double length2))
                                    {
                                        //if (midAdded) { length2 = length2 / 2; }
                                        hz.Chainage = cumCh; //+ length2;
                                        hz.ChainageOrigin = cumCh;// + length2;
                                    }
                                    startCoords = ele.Element(ns + "End").Value.ToString().Trim().Split();

                                    double.TryParse(startCoords[0], out y);
                                    double.TryParse(startCoords[1], out x);

                                    hz.East = x;
                                    hz.North = y;

                                    hz.EastOrigin = x;
                                    hz.NorthOrigin = y;

                                    ThisAlignment.Horizontal.AddElement(hz);
                                
                                
                                }
                            }



                            for (int i = 0; i < ThisAlignment.Horizontal.Elements.Count - 1; i++)
                            {
                                HorizontalElement thisele = ThisAlignment.Horizontal.Elements.ElementAt(i);
                                HorizontalElement nextele = ThisAlignment.Horizontal.Elements.ElementAt(i + 1);
								//if (thisele.CenterPointSet)
        //                        {
        //                            if (MathsHelpers.Vector.FNRightOf(thisele.East, thisele.North, nextele.East, nextele.North, thisele.CenterEast, thisele.CenterNorth))

        //                            {
        //                                thisele.Radius = Math.Abs(thisele.Radius);
        //                                thisele.RadiusOrigin = Math.Abs(thisele.Radius);

        //                            }
        //                            else
        //                            {
        //                                thisele.Radius = -Math.Abs(thisele.Radius);
        //                                thisele.RadiusOrigin = -Math.Abs(thisele.Radius);

        //                            }
        //                        }

                                AlignmentHelper.CalcOrigins(ref thisele, nextele);
                                
                                
                            }



                        }
                    }


                    #region station / chainage equations - Add to alignment viewer
                    //if (alg.Descendants(ProfileTag).Any())
                    //{
                    if (alg.Descendants(StationEquationTag).Any())
                    {
                        foreach (XElement eq in alg.Descendants(StationEquationTag))
                        {
                            if (eq.Attribute("staInternal") != null)
                            {
                                double algch = 0;
                                if (double.TryParse(eq.Attribute("staInternal").Value, out algch))
                                {
                                    if (eq.Attribute("staAhead") != null)
                                    {
                                        double aheadch = 0;
                                        if (double.TryParse(eq.Attribute("staAhead").Value, out aheadch))
                                        {
                                            ChainageEquation se = new ChainageEquation();
                                            se.AlignmentChainage= algch;
                                            se.ForwardChainage = aheadch;
                                            ThisAlignment.Horizontal.ChainageEquations.AddEquation(se);
                                        }

                                    }

                                }
                            }                        
                        }
                    }
                    //}
                    #endregion

                    #region CrossSections
                    if (alg.Descendants(CrossSectionsTag).Any())
                    { 
                        
                    }
                    #endregion CrossSections

                    #region vertical
                    if (alg.Descendants(ProfileTag).Any())
                    {
                        //VerticalAlignment va = new VerticalAlignment();
                        VIPAlignment vips = new VIPAlignment();
                        VIPAlignment ThisVert = new VIPAlignment();
                        foreach (XElement vp in alg.Descendants(ProfileTag))
                        {
                            foreach (XElement pv in vp.Descendants(ProfileAlignTag))
                            {

                                foreach (XElement ve in pv.Descendants())
                                {

                                    var arr = ve.Value.ToString().Trim().Split();

                                    if (double.TryParse(arr[0], out double ch))
                                    {
                                        if (double.TryParse(arr[1], out double lev))
                                        {
                                            VIP vip = new VIP();
                                            vip.Chainage = ch; vip.Level = lev;

                                            if (ve.Name.LocalName.ToString() == "PVI")
                                            {
                                                vip.type = 0;
                                            }
                                            else if (ve.Name.LocalName.ToString() == "CircCurve" || ve.Name.LocalName.ToString() == "ParaCurve")
                                            {
                                                vip.type = 1;
                                                if (ve.Attributes("length").Any())//it'll be a parabola
                                                {
                                                    if (double.TryParse(ve.Attribute("length").Value, out double l))
                                                    {
                                                        vip.Length = l;
                                                    }
                                                }
                                                else if (ve.Attributes("radius").Any())//probably circular
                                                {
                                                    if (double.TryParse(ve.Attribute("radius").Value, out double l))
                                                    {
                                                        vip.Radius = l;
                                                    }
                                                }

                                            }

                                            ThisVert.Ips.Add(vip);

                                        }

                                    }

                                }
                            }
                            ThisAlignment.Vertical = ThisVert.ExtractVertical();

                        }
                        #endregion vertical

                    }



                    AlignmentOut.Add(ThisAlignment);


                }
            }
            return AlignmentOut;
        }



        /// <summary>
        /// Given the XDocument from the XML - get out all the topo survey data from TS & GPS/GNSS
        /// </summary>
        /// <param name="xml"> the XML XDocument</param>
        /// <returns> NRG style DTM Model</returns>
        private static DTM ParseObs(XDocument xml)
        {
            XNamespace ns = xml.Root.Name.Namespace;

            var SurveyTag = ns + "Survey"; //assuming there's only going to be one in the file
            var CGTag = ns + "CgPoints";

            var Setups = ns + "InstrumentSetup"; //multiple intrument setups for the above survey
            var IntrumentPoint = ns + "InstrumentPoint"; //one intrument point for the above setup
            var ObservationGroup = ns + "ObservationGroup"; //I think there should be one per setup
            var TargetPoint = ns + "TargetPoint";//something I know not what the purpose is, for having its own tag
            var surveyModel = new DTM(); //the object we're going to return

            var XMLSetups = new Dictionary<string, XMLSetup>();
            var GPSSetup = ns + "GPSSetup";
            var GPSPosition = ns + "GPSPosition";


            var Commands = new Dictionary<string, XElement>();

            #region Leica HeXML
            XNamespace LeicaNS = "http://xml.hexagon.com/schema/HeXML-1.8";//not sure how to extract the actual name spaces out of the file so hard coding for a bit
            //I think a lot of this might depend on our code list - might have to be re-visited
            var uniqueID = "uniqueID";
            var PointCode = LeicaNS + "PointCode";
            var hxml = xml.Descendants(LeicaNS + "Point");

            foreach (XElement pts in hxml)
            {
                if (pts.Attributes(uniqueID).Any())
                {
                    string uid = pts.Attribute(uniqueID).Value.ToString();
                    if (pts.Descendants(PointCode).Any())
                    {
                        foreach (XElement code in pts.Descendants(PointCode))
                        {
                            if (Commands.ContainsKey(uid) == false)
                                Commands.Add(uid, code);
                        }
                    }
                }
            }
            #endregion HeXML

            #region non Leica specific
            var labels = new Dictionary<string, string>();
            //get feature codes
            var CGPoints = xml.Descendants(CGTag);

            foreach (XElement cg in CGPoints.Descendants())
            {
                if (cg.Attributes("code").Any())
                {
                    string code = cg.Attribute("code").Value.ToString();
					string id = "";
					if (cg.Attributes("oID").Any())
					{
						id = cg.Attribute("oID").Value.ToString();
					}
					else if (cg.Attributes("Name").Any())
					{
						id = cg.Attribute("Name").Value.ToString();
					}

					if (labels.ContainsKey(id) == false)
                    {
                        labels.Add(id, code);
                    }

                }
            }
            #endregion non Leica specific

            foreach (XElement Survey in xml.Descendants(SurveyTag))
            {
                #region GPS
                foreach (XElement gpss in Survey.Descendants(GPSSetup))
                {
                    foreach (XElement pos in gpss.Descendants(GPSPosition))
                    {
                        if (pos.Descendants(TargetPoint).Any())
                        {
                            XElement pt = pos.Element(TargetPoint);
                            if (pt.Attributes("pntRef").Any())
                            {
                                string id = pt.Attribute("pntRef").Value.ToString();
                                var coords = pt.Value.ToString().Split();


                                double x = 0, y = 0, z = 0;
                                if (double.TryParse(coords[0], out y))
                                {
                                    if (double.TryParse(coords[1], out x))
                                    {
                                        if (double.TryParse(coords[2], out z))
                                        {
                                            ObservationPoint ob = new ObservationPoint();
                                            ob.X = x; ob.Y = y; ob.Z = z;
                                            ob.PointID = id;

                                            ob.Type = PointType.Coordinate;

                                            if (labels.ContainsKey(id))
                                            {
                                                string la = "";
                                                labels.TryGetValue(id, out la);
                                                ob.PointLabel = la;
                                            }

                                            if (Commands.ContainsKey(id))
                                            {
                                                if (Commands.TryGetValue(id, out XElement xele))
                                                {
                                                    foreach (XElement nd in xele.Descendants())
                                                    {
                                                        foreach (XAttribute att in nd.Attributes())
                                                        {
                                                            string str = att.Value;
                                                            if (str == "Start Line")
                                                            {
                                                                ob.PointLabel = ob.PointLabel + "/ST";
                                                            }
                                                            else if (str == "Close Line")
                                                            {
                                                                ob.PointLabel = ob.PointLabel + "/CL";
                                                            }
                                                        }
                                                    }
                                                }

                                                surveyModel.Points.Add(ob);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion GPS
                #region Total Station
                foreach (XElement setup in Survey.Descendants(Setups))//isolate the survey or obs element
                {
                    StationSetup occstn = new StationSetup();
                    string occID = "";
                    if (setup.Attributes("id").Any())
                    {
                        occID = setup.Attribute("id").Value.ToString();
                    }
                    if (occID == "")
                    {
                        occID = setup.Attribute("stationName").Value.ToString();
                    }
                    if (setup.HasElements)
                    {
                        if (setup.Elements(IntrumentPoint).Any())
                        {
                            string instPt = setup.Element(IntrumentPoint).Value;

                            var coordArr = instPt.Split();
                            if (double.TryParse(coordArr[0], out double thisy))
                            {
                                if (double.TryParse(coordArr[1], out double thisx))
                                {
                                    if (double.TryParse(coordArr[2], out double thisz))
                                    {
                                        var os = new StationPoint(PointType.OccupiedStation);

                                        os.X = thisx; os.Y = thisy; os.Z = thisz;
                                        os.PointID = occID;
                                        os.PointLabel = occID;
                                        double instHt = 0;

                                        os.ObjectHeight = instHt;

                                        XMLSetup thisSetup = new XMLSetup();

                                        thisSetup.FromStation = os;

                                        XMLSetups.Add(occID, thisSetup);
                                    }
                                }
                            }
                        }
                    }
                }


                foreach (XElement obsgroups in Survey.Descendants(ObservationGroup)) //isolate the survey or obs element
                {
                    foreach (XElement obs in obsgroups.Descendants())
                    {

                        if (obs.Attributes("setupID").Any())
                        {
                            string id = obs.Attribute("setupID").Value.ToString();

                            if (XMLSetups.ContainsKey(id))
                            {
                                XMLSetup thissetup = XMLSetups[id];
                                ObservationPoint thisobs = GetObsFromElement(ns, obs);
                                if (thisobs != null)
                                {
                                    if (thisobs.Type == PointType.ReferenceObservation)
                                    {
                                        var ro = new StationPoint(PointType.ReferenceStation);
                                        ro.Type = PointType.ReferenceStation;
                                        ro.PointID = thisobs.PointID;
                                        ro.PointLabel = thisobs.PointLabel;
                                        ro.X = thisobs.X;
                                        ro.Y = thisobs.Y;
                                        ro.Z = thisobs.Z;
                                        
                                        thissetup.Obs.Add(ro);

                                        var roshot = new ReferenceObservation();
                                        roshot.PointID = thisobs.PointID;
                                        roshot.PointLabel = thisobs.PointLabel;
                                        roshot.Type = PointType.ReferenceObservation;
                                        roshot.X = thisobs.X;
                                        roshot.Y = thisobs.Y;
                                        roshot.Z = thisobs.Z;
                                        roshot.HCR = thisobs.HCR;
                                        roshot.VCR = thisobs.VCR;
                                        roshot.SD = thisobs.SD;
                                        roshot.TargetHeight = thisobs.TargetHeight;

                                        thissetup.Obs.Add(roshot);

                                    }

                                    if (thisobs.PointID != "")
                                    {
										if (thisobs.Type == PointType.ReferenceObservation) { continue; }
                                        thisobs.Type = PointType.Observation;
                                        if (labels.ContainsKey(thisobs.PointID))
                                        {
                                            string la = "";
                                            labels.TryGetValue(thisobs.PointID, out la);
                                            thisobs.PointLabel = la;
                                        }

                                        if (Commands.ContainsKey(thisobs.PointID))
                                        {
                                            if (Commands.TryGetValue(thisobs.PointID, out XElement xele))
                                            {
                                                foreach (XElement nd in xele.Descendants())
                                                {
                                                    foreach (XAttribute att in nd.Attributes())
                                                    {
                                                        string str = att.Value;
                                                        if (str == "Start Line")
                                                        {
                                                            thisobs.PointLabel = thisobs.PointLabel + "/ST";
                                                        }
                                                        else if (str == "Close Line")
                                                        {
                                                            thisobs.PointLabel = thisobs.PointLabel + "/CL";
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    thissetup.Obs.Add(thisobs);
                                }
                            }
                        }
                    }
                }

                foreach (KeyValuePair<string, XMLSetup> su in XMLSetups)
                {
                    surveyModel.Points.Add(su.Value.FromStation);

                    foreach (DTMPoint ob in su.Value.Obs)
                    {
                        surveyModel.Points.Add(ob);
                    }
                }
                #endregion total station
            }
            surveyModel.GenerateStationSetups();
            return surveyModel;
        }

        /// <summary>
        /// GetObsFromElement - Added by ES:08.05.20 
        /// </summary>
        /// <param name="ns"> default XML namespace</param>
        /// <param name="ele"> xml Xelement</param>
        /// <returns> a standard NRG Observation point if valid, null else</returns>
        private static ObservationPoint GetObsFromElement(XNamespace ns, XElement ele)
        {
            ObservationPoint thisObs = new ObservationPoint();

            if (ele.Name.LocalName.ToUpper().ToString() == "BACKSIGHT" || ele.Name.LocalName.ToUpper().ToString() == "RAWOBSERVATION")
            {
                double hcr = 0; double vcr = 0; double sd = 0; double tgtht = 0;
                //===========================================================================
                //Get a horizontal reading of some kind
                if (ele.Attributes("horizAngle").Any())
                {
                    double.TryParse(ele.Attribute("horizAngle").Value.ToString(), out hcr);
                }
                if (ele.Attributes("azimuth").Any())
                {
                    double.TryParse(ele.Attribute("azimuth").Value.ToString(), out hcr);
                }
                if (ele.Attributes("circle").Any())
                {
                    double.TryParse(ele.Attribute("circle").Value.ToString(), out hcr);
                }
                //===========================================================================
                //Get vertical circle reading 
                if (ele.Attributes("zenithAngle").Any())
                {
                    double.TryParse(ele.Attribute("zenithAngle").Value.ToString(), out vcr);
                }
                //===========================================================================
                //Slope distance
                if (ele.Attributes("slopeDistance").Any())
                {
                    double.TryParse(ele.Attribute("slopeDistance").Value.ToString(), out sd);
                }
                //===========================================================================
                //object height - target height or collumation height
                if (ele.Attributes("targetHeight").Any())
                {
                    double.TryParse(ele.Attribute("targetHeight").Value.ToString(), out tgtht);
                }

                thisObs.HCR = ConvertDDMtoDecimal(hcr.ToString()) / 180 * Math.PI; thisObs.VCR = ConvertDDMtoDecimal(vcr.ToString()) / 180 * Math.PI; thisObs.SD = sd; thisObs.TargetHeight = tgtht;

                //===========================================================================
                //Now we try to get some sort of point ID - this'll get used when we have to 
                //cross reference other sections of the file, especially the Leica HeXML stuff
                //which is going to be a pain in the arse, but there is some really good info 
                //to be had

                string subkey = "TargetPoint";

                if (ele.Name.LocalName.ToUpper().ToString() == "BACKSIGHT")
                {
                    subkey = "BacksightPoint";

                    thisObs.Type = PointType.ReferenceStation;
                }

                if (ele.Elements(ns + subkey).Any())
                {
                    XElement bsp = ele.Element(ns + subkey);
                    if (bsp.Attributes("name").Any())
                    {
                        thisObs.PointLabel = bsp.Attribute("name").Value.ToString();
                    }
                    if (bsp.Attributes("pntRef").Any())
                    {
                        thisObs.PointID = bsp.Attribute("pntRef").Value.ToString();
                    }

                    if (subkey == "BacksightPoint")
                    {
                        var thisArr = bsp.Value.Split();

                        if (thisArr != null)
                        {
                            double e = 0; double n = 0; double l = 0;
                            double.TryParse(thisArr[0], out n);
                            double.TryParse(thisArr[1], out e);
                            double.TryParse(thisArr[2], out l);
                            thisObs.X = e; thisObs.Y = n; thisObs.Z = l;
                            thisObs.Type = PointType.ReferenceObservation;
                        }
                    }
                }

                return thisObs;
            }

            return null;
        }

        /// <summary>
        /// Convert Degrees and decimal minutes to decimal degrees - there really has to be a better way of doing this
        /// </summary>
        /// <param name="inputAngle"> according to the landXML schema this is DD.MM.SS but it's not really!</param>
        /// <returns>double angle in decimal degrees</returns>

        private static double ConvertDDMtoDecimal(string inputAngle)
        {

            double zerocheck = 0;
            double.TryParse(inputAngle, out zerocheck);

            if (zerocheck == 0) return 0;

            double d = 0; double m = 0; double s = 0;

            string ia = inputAngle.Replace(".", " ");
            var arr = ia.Split();

            double.TryParse(arr[0], out d);

            double.TryParse(arr[1].Substring(0, 2), out m);

            double.TryParse(arr[1].Substring(2, 2) + "." + arr[1].Substring(4), out s);

            return d + (m / 60) + (s / 3600);
        }

        #endregion
    }

    public static class LandXMLHelper
    {
        /// <summary>
        /// GetAlignmentNames - Added by ES:09.06.22
        /// Given an input a file path (fileName), this function will return a list of geometric alignments(if any)  found in the file
        /// </summary>
        /// <param name="fileName">Duh, the file to open</param>
        /// <returns>List containing the alignments, null if there ain't none</returns>

        public static List<string> GetAlignmentNames(string fileName)
        {


            
            var resultList = new List<string>();

            try
            {

                XDocument xml = new XDocument();
                xml = XDocument.Load(fileName);
                XNamespace ns = xml.Root.Name.Namespace;

                XName AlignmentsTag = ns + "Alignments";
                XName  AlignmentTag = ns + "Alignment";


                foreach (XElement algs in xml.Descendants(AlignmentsTag))
                {
                    foreach (XElement alg in algs.Descendants(AlignmentTag))
                    {
                        resultList.Add(alg.Attribute("name").Value.ToString());
                    }
                }


                    
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return resultList;
        }

    }

}

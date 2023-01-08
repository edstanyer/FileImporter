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

namespace NRG.Import


{
  
    

    public class TrimbleTXLAlignment
    {

        public Alignment TXLAlignment = new Alignment();

        
        public bool ReadFile(string filename = "", string InitDirectory = "")
        {

            if (File.Exists(filename) == false)
            {
                OpenFileDialog ofd = new OpenFileDialog();

                if (Directory.Exists(InitDirectory))
                {
                    ofd.InitialDirectory = InitDirectory;
                }

                ofd.Filter = "Trimble Tunnel Alignments| *.TXL; *.TXL;)";
                ofd.Title = "Import Trimble Tunnel Alignmemt";
                ofd.ShowDialog();
                filename = ofd.FileName;
            }

            if (File.Exists(filename) == false)
            {
                return false;
            }

            XDocument txl = XDocument.Load(filename);
            //txl = xdoc(filename);

            if (txl == null)
            {
                return false;
            }

            if (ParseFile(txl) == false)
            {
                return false;
            }

            return true;
        }

        private bool ParseFile(XDocument txl)
        {
            if (txl == null) return false;


            XNamespace ns = txl.Root.Name.Namespace;

            var list = txl.Descendants("HorizontalAlignment");


            foreach (XElement item in txl.Descendants(ns + "HorizontalAlignment"))
            {
                if (item.HasElements)
                {
                    foreach (XElement he in item.Elements())
                    {
                        HorizontalElement hz = new HorizontalElement();
                        double val = 0;

                        if (he.Name == "Straight" || he.Name == "Arc" || he.Name == "EntrySpiral" || he.Name == "ExitSprial")
                        {
                            if (he.Descendants(ns + "StartStation").Any())
                            {
                                double.TryParse(he.Element(ns + "StartStation").Value.ToString(), out val);
                                hz.Chainage = val;
                                hz.ChainageOrigin = val;
                            }
                            if (he.Descendants(ns + "StartAzimuth").Any())
                            {
                                double.TryParse(he.Element(ns + "StartAzimuth").Value.ToString(), out val);
                                if (val != 0) { val = val / 180 * Math.PI; }
                                hz.BrgOrigin = val;
                            }
                            if (he.Descendants(ns + "StartCoordinate").Any())

                            {

                                double.TryParse(he.Element(ns + "StartCoordinate").Element(ns + "East").Value.ToString(), out val);
                                hz.East = val;
                                double.TryParse(he.Element(ns + "StartCoordinate").Element(ns + "North").Value.ToString(), out val);
                                hz.North = val;
                            }

                            if (he.Name == "Straight")
                            {
                                hz.ElementType = HorizontalElementType.Straight;
                            }
                            else if (he.Name == "Arc")
                            {
                                hz.ElementType = HorizontalElementType.Arc;
                                if (he.Descendants(ns + "Radius").Any())
                                {
                                    double.TryParse(he.Element(ns + "Radius").Value.ToString(), out val);
                                    if (he.Descendants(ns + "Direction").Any())
                                    {
                                        if (he.Element(ns + "Direction").Value.ToString() == "Left")
                                        {
                                            val = -Math.Abs(val);
                                        }
                                    }
                                    hz.Radius = val;
                                    hz.RadiusOrigin = val;

                                }
                            }
                            else if (he.Name == "EntrySpiral" || he.Name == "Exit Spiral")
                            {
                                double r = 0;
                                if (he.Name == "EntrySpiral")
                                {
                                    if (he.Descendants(ns + "EndRadius").Any())
                                    {
                                        double.TryParse(he.Element(ns + "EndRadius").Value.ToString(), out r);

                                    }
                                }
                                else
                                {
                                    if (he.Descendants(ns + "StartRadius").Any())
                                    {
                                        double.TryParse(he.Element(ns + "StartRadius").Value.ToString(), out r);

                                    }

                                }
                                if (he.Descendants(ns + "Direction").Any())
                                {
                                    if (he.Element(ns + "Direction").Value.ToString() == "Left")
                                    {
                                        val = -Math.Abs(r);
                                    }
                                }
                                hz.Radius = r;
                                if (he.Descendants(ns + "Length").Any())
                                {
                                    double.TryParse(he.Element(ns + "Length").Value.ToString(), out val);
                                }
                                hz.Length = val;
                                hz.RL = r * val;

                                hz.EastOrigin = hz.East;
                                hz.NorthOrigin = hz.North;
                                hz.ChainageOrigin = hz.Chainage;
                                if (he.Name == "EntrySpiral")
                                {
                                    hz.RadiusOrigin = 0;

                                }
                                else
                                {
                                    hz.RadiusOrigin = hz.Radius;
                                    if (he.Name == "ExitSpiral")
                                    {
                                        if (he.Descendants(ns + "EndAzimuth").Any())
                                        {
                                            double.TryParse(he.Element(ns + "Length").Value.ToString(), out val);
                                            if (val != 0)
                                            { val = val / 180 * Math.PI; }
                                            hz.BrgOrigin = val;

                                        }
                                        if (he.Descendants(ns + "EndStation").Any())
                                        {
                                            double.TryParse(he.Element(ns + "EndStation").Value.ToString(), out val);
                                            hz.ChainageOrigin = val;

                                        }
                                        if (he.Descendants(ns + "EndCoordinate").Any())
                                        {
                                            double.TryParse(he.Element(ns + "EndCoordinate").Element(ns + "East").Value.ToString(), out val);
                                            hz.EastOrigin = val;
                                            double.TryParse(he.Element(ns + "EndCoordinate").Element(ns + "North").Value.ToString(), out val);
                                            hz.NorthOrigin = val;

                                        }

                                    }
                                }


                            }


                            TXLAlignment.Horizontal.AddElement(hz);
                            if (he == item.LastNode)
                            {
                                if (he.Descendants(ns + "EndCoordinate").Any())
                                {
                                    HorizontalElement hz2 = new HorizontalElement();
                                    double.TryParse(he.Element(ns + "EndCoordinate").Element(ns + "East").Value.ToString(), out val);
                                    hz2.East = val;
                                    double.TryParse(he.Element(ns + "EndCoordinate").Element(ns + "North").Value.ToString(), out val);
                                    hz2.North = val;

                                    if (he.Descendants(ns + "EndStation").Any())
                                    {
                                        double.TryParse(he.Element(ns + "EndStation").Value.ToString(), out val);
                                        hz2.Chainage = val;
                                        hz2.ChainageOrigin = val;
                                    }

                                    if (he.Descendants(ns + "EndAzimuth").Any())
                                    {
                                        double.TryParse(he.Element(ns + "EndAzimuth").Value.ToString(), out val);
                                        if (val != 0)
                                        {
                                            hz2.BrgOrigin = val / 180 * Math.PI;
                                        }
                                    }

                                    TXLAlignment.Horizontal.AddElement(hz2);
                                }

                            }
                        }
                        else
                        {
                            Console.WriteLine("Something else - " + he.Name.ToString());

                        }


                    }
                }

            }

            double startCH = 0;
            double endCH = 0;
            List<VIP> vips = new List<VIP>();
            foreach (XElement item in txl.Descendants(ns + "VerticalAlignment"))
            {
                double val = 0;
                if (item.Descendants(ns + "StartStation").Any())
                {
                    double.TryParse(item.Element(ns + "StartStation").Value.ToString(), out val);
                    startCH = val;
                }
                if (item.Descendants(ns + "EndStation").Any())
                {
                    double.TryParse(item.Element(ns + "EndStation").Value.ToString(), out val);
                    endCH = val;
                }


                foreach (XElement he in item.Elements())
                {

                    VIP vi = new VIP();
                    if (he.Descendants(ns + "IntersectionPoint").Any())
                    {
                        double.TryParse(he.Element(ns + "IntersectionPoint").Element(ns + "Station").Value.ToString(), out val);
                        vi.chainage = val;
                        double.TryParse(he.Element(ns + "IntersectionPoint").Element(ns + "Elevation").Value.ToString(), out val);
                        vi.chainage = val;
                    }
                    if (he.Descendants(ns + "GradeIn").Any())
                    {
                        double.TryParse(he.Element(ns + "GradeIn").Value.ToString(), out val);
                        vi.gradein = val;
                    }
                    if (he.Descendants(ns + "GradeOut").Any())
                    {
                        double.TryParse(he.Element(ns + "GradeOut").Value.ToString(), out val);
                        vi.gradeout = val;
                    }

                }

            }

            return TXLAlignment.Horizontal.Elements.Count > 0;
        }

    }

}

public class VIP
    {
    public double chainage { get; set; }
    public double level { get; set; }
    public double gradein { get; set; }
    public double gradeout { get; set; }

    public double Radius()
    {

        return 0;
    }
}

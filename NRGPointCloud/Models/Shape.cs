using NRG.MathsHelpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NRG.Models
{
    public class Shape
    {
        public List<ShapeElement> Elements = new List<ShapeElement>();

        private  String ShapeLayer;

        public List<string> Layers = new List<string>() { "DEFAULT" };
        public string FileName = "";
        public double Gauge = 1.507;
        public TunnelConfig config = new TunnelConfig();
        public List<Point2D> LeftRail = new List<Point2D>();
        public List<Point2D> RightRail = new List<Point2D>();
        public Point2D ShapeCentre = new Point2D();
        public Bounds ShapeBounds = new Bounds();
        public List<Point2D> TrainPoints = new List<Point2D>();

        public Point2D TunnelAxis = new Point2D();

        public double DispXOffset = 0;
        public double DispYOffset = 0;

        public bool Changed;

        public string LayerName
        {
            get
            {
                if (ShapeLayer== "")
                {

                    ShapeLayer= System.IO.Path.GetFileNameWithoutExtension(this.FileName);

                }
                return ShapeLayer;
                
            }
            set
            {
                ShapeLayer = value;
            }
            
        }


        public double Length
        {
            get
            {
                        
                var length = 0D;

                if (Elements == null)
                    return length;

                foreach (var element in Elements)
                    length += element.Length;

                return length;
            }
        }



        public Shape()
        {

        }

        public Shape(string filepath, double gauge)
        {
            if (!File.Exists(filepath))
            {
                return;
            }

            if (filepath.ToUpper().EndsWith(".SHP"))
                ImportSHP(filepath);
            else if (filepath.ToUpper().EndsWith(".NRGSHP"))
                ReadShapeFile(filepath);

            this.Gauge = gauge;
        }

        
        public bool WriteShapeFile(string ShapeFileName = "", string initDirectory = "")
        {

            //string id = ApplicationSettings.Window.ProjectPath + "\\Pictures";

            if (ShapeFileName == "")
            {

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "NRG Shape Files|*.NRGShp";
                sfd.Title = "Save NRG Shape File";
                if (initDirectory != "" && File.Exists(initDirectory))
                {
                    sfd.InitialDirectory = initDirectory;
                }
                sfd.ShowDialog();
                ShapeFileName = sfd.FileName;
                if (ShapeFileName == "")
                {
                    return false;
                }
            }
            else if (ShapeFileName.Substring(ShapeFileName.Length-4).ToString().ToUpper() == ".SHP")
            {
                ShapeFileName = ShapeFileName.Replace(".shp", ".NRGShp");
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "NRG Shape Files|*.NRGShp";
                sfd.Title = "Save NRG Shape File";
                sfd.FileName = ShapeFileName;

                DialogResult res =  sfd.ShowDialog();
                ShapeFileName = sfd.FileName;
                if (ShapeFileName == "" | res == DialogResult.Cancel)
                {
                    return false;
                }

            }

            FileName = ShapeFileName;
            if (Elements.Count > 0)
            {
                if (File.Exists(ShapeFileName))
                {
                    File.Delete(ShapeFileName);
                }
                using (StreamWriter fs = new StreamWriter(ShapeFileName))
                {

                    string layerText = "Shape Layer=" + this.LayerName;
                    fs.WriteLine(layerText);
                    
                    if (Layers.Count > 0)
                    {
                        string str = "";
                        str = "Layers=";
                        for (int i = 0; i < Layers.Count; i++)
                        {
                            str = str + Layers.ElementAt(i).ToString() + ":" + i.ToString();
                            if (i < Layers.Count - 1)
                            {
                                str = str + ",";
                            }
                        }
                        fs.WriteLine(str);

                    }

                    for (int i = 0; i < Elements.Count; i++)
                    {
                        ShapeElement shp = Elements.ElementAt(i);
                        fs.WriteLine(shp.FileString());
                    }

                    string cantText = "";

                    cantText = "CANT=";

                    cantText = cantText + config.LeftHandX.ToString() + "," + config.LeftHandY.ToString() + "," + config.RightHandX.ToString() + "," + config.RightHandY.ToString() + "," + config.YFrom.ToString () + "," + config.MeasureX.ToString() + "," + config.MeasureY.ToString() + "," + config.YVert.ToString() + "," + config.CantPercent.ToString() + "," + config.IgnoreRotaition.ToString() + "," + config.CenterOffsetX.ToString() + "," + config.CenterOffsetY.ToString() + "," + config.ApplyCant.ToString();

                    

                    fs.WriteLine(cantText);

                    string dimText = "DIMS=" + config.CarrriageDim1.ToString() + "," + config.CarrriageDim2.ToString() + "," + config.CarrriageDim3.ToString();
                    fs.WriteLine(dimText);


                    fs.Close();

                }
            }
            Changed = false;
            //FileName = ShapeFileName;
            return true;
        }

        public bool ReadShapeFile(string ShapeFileName = "", string InitDirectory = "")
        {

            if (ShapeFileName == "")
            {
                OpenFileDialog ofd = new OpenFileDialog();

                if (InitDirectory != "")
                {
                    if (Directory.Exists(InitDirectory))
                    {
                        ofd.InitialDirectory = InitDirectory;
                    }
                }
                
                
                ofd.Filter = "NRG Shape Files| *.NRGShp; *.SHP;)";
                ofd.Title = "Open NRG Shape File";
                ofd.ShowDialog();
                ShapeFileName = ofd.FileName;
                if (ShapeFileName == "")
                {
                    return false;
                }
            }

           
            if (File.Exists(ShapeFileName) == false)
            {
                MessageBox.Show("The selected file does not exist.", "Open NRG Shape File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                return false;
            }
            else if (ShapeFileName.Length > 6)
            {
                string testExt = ShapeFileName.Substring(ShapeFileName.Length - 6).ToUpper();
                if (testExt != "NRGSHP")
                {
                    Changed = true;
                    FileName = ShapeFileName.ToUpper().Replace("SHP", "NRGSHP");
                    return ImportSHP(ShapeFileName);
                }

            }

            string[] lines = System.IO.File.ReadAllLines(ShapeFileName);

            foreach (string str in lines)               
            {
                if (str != "")
                {
                    string[] arr = str.Split('=');
                    int num = arr.GetUpperBound(0);

                    if (num > 0)
                    {

                        string[] param = arr[1].Split(',');

                        int numparam = param.GetUpperBound(0);

                        switch (arr[0].ToUpper().ToString())
                        {
                            case "SHAPE LAYER":
                                LayerName = param[0];
                                break;
                            case "LAYERS":
                                for (int j = 0; j <= numparam; j++)
                                {
                                    string[] lay = param[j].Split(':');
                                    if (lay.GetUpperBound(0) >= 0)
                                    {
                                        Layers.Add(lay[0]);
                                    }
                                }
                                break;
                            case "V":
                                if (param.GetUpperBound(0) >= 5)
                                {
                                    ShapeElement ele = new ShapeElement();
                                    ele.StartX = Convert.ToDouble(param[0]);
                                    ele.StartY = Convert.ToDouble(param[1]);
                                    ele.EndX = Convert.ToDouble(param[2]);
                                    ele.EndY = Convert.ToDouble(param[3]);
                                    ele.Radius = Convert.ToDouble(param[4]);
                                    ele.Layer = Convert.ToInt16(param[5]);
                                    Elements.Add(ele);
                                }
                                break;
                            case "CANT":

                                if (numparam >= 10)
                                {
                                    config.ApplyCant = 0;
                                    config.LeftHandX = Convert.ToDouble(param[0]);
                                    config.LeftHandY = Convert.ToDouble(param[1]);
                                    config.RightHandX = Convert.ToDouble(param[2]);
                                    config.RightHandY = Convert.ToDouble(param[3]);
                                    config.YFrom = Convert.ToInt16(param[4]);
                                    config.MeasureX = Math.Abs( Convert.ToInt16(param[5]));
                                    config.MeasureY = Convert.ToInt16(param[6]);
                                    config.YVert = Convert.ToInt16(param[7]);
                                    config.CantPercent = Convert.ToDouble(param[8]);
                                    if (numparam >= 9)
                                    {
                                        config.IgnoreRotaition = Convert.ToBoolean(param[9]);
                                    }
                                    else
                                    {
                                        config.IgnoreRotaition = false;
                                    }
                                    if (numparam >= 11)
                                    {
                                        config.CenterOffsetX = Convert.ToDouble(param[10]);
                                        config.CenterOffsetY = Convert.ToDouble(param[11]);

                                    }
                                    if (numparam >= 12)
                                    {
                                        if (param[12] == "1")
                                        {
                                            config.ApplyCant = 1;
                                        }
                                    }
                                }

                                break;
                            case "DIMS":
                                if (numparam >= 2)
                                {
                                    config.CarrriageDim1 = Convert.ToDouble(param[0]);
                                    config.CarrriageDim2 = Convert.ToDouble(param[1]);
                                    config.CarrriageDim3 = Convert.ToDouble(param[2]);
                                }
                                    break;
                            default:
                                break;

                        }
                    }

                }
            }

            Changed = false;
            FileName = ShapeFileName;
            return Elements.Count >0;

        }

        public bool ImportSHP(string ShapeFileName = "")
        {

            try
            {

                if (ShapeFileName == "")
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Filter = "NRG Shape Files|*.Shp";
                    ofd.Title = "Import NRG Shape File";
                    ofd.ShowDialog();
                    ShapeFileName = ofd.FileName;
                    if (ShapeFileName == "")
                    {
                        return false;
                    }
                }
                if (File.Exists(ShapeFileName) == false)
                {
                    MessageBox.Show("The selected file does not exist.", "Open NRG Shape File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                    return false;
                }
                double firstBrg = 0;
                int count = 0;
                string[] lines = System.IO.File.ReadAllLines(ShapeFileName);
                Int32 lay = 0;
                foreach (string line in lines)
                {
                    string[] arr = line.Split(',');
                    int num = arr.GetUpperBound(0);
                    if (line.Substring(0, 1) != ";")
                    {

                        if (line.ToUpper().Substring(0, 5) == "LAYER")
                        {
                            Layers.Clear();
                            string str = line.Substring(13);
                            //Layers.Add(str);
                            LayerName = str;
                            //lay = Layers.Count - 1;
                        }
                        else if (line.Substring(0, 4) == "CANT")
                        {
                            //Do cant stuff here

                        }
                        else
                        {
                            if (num == 6)
                            {
                                count = count + 1;
                                //must be an element
                                ShapeElement ele = new ShapeElement();

                                if (count == 1)
                                {
                                    firstBrg = Convert.ToDouble(arr[4]);
                                }

                                ele.StartX = Convert.ToDouble(arr[2]);
                                ele.StartY = Convert.ToDouble(arr[3]);
                                ele.Radius = Convert.ToDouble(arr[5]);
                                ele.Layer = lay;
                                Elements.Add(ele);
                                if (Elements.Count > 1)
                                {
                                    ShapeElement lastele = Elements.ElementAt(Elements.Count - 2);
                                    lastele.EndX = ele.StartX;
                                    lastele.EndY = ele.StartY;
                                }


                            }
                        }

                    }


                }


                if (Elements.Count > 1)

                {
                    ShapeElement ele = Elements.ElementAt(Elements.Count - 1);
                    if (ele.Radius == 0)
                    {
                        ShapeElement firstele = Elements.ElementAt(0);
                        ele.EndX = firstele.StartX;
                        ele.EndY = firstele.StartY;
                    }
                    else
                    {
                        Elements.RemoveAt(Elements.Count - 1);
                    }
                }

                //==================================================================================================================
                //Keep and eye on the code below - this is a quick and dirty fix, but I can't see another way of doing it.

                if (Elements.Count == 2)
                {
                    ShapeElement first = Elements.ElementAt(0);
                    ShapeElement second = Elements.ElementAt(1);
                    if (MathsHelpers.Vector.FnDistance(first.StartX, first.StartY, second.StartX, second.StartY) < 0.01f)
                    {
                        double brg = firstBrg + Math.PI / 2;
                        if (brg > 2 * Math.PI) { brg = brg - 2 * Math.PI; }
                        double x = 0;
                        double y = 0;
                        MathsHelpers.Trig.PRC(first.StartX, first.StartY, brg, first.Radius * 2, ref x, ref y);

                        first.EndX = x;
                        first.EndY = y;
                        second.StartX = x;
                        second.StartY = y;
                        if (second.Radius == 0)
                        {
                            second.Radius = first.Radius;
                        }
                    }
                }
                else
                {
                    if (Elements.Count == 1)
                    {
                        if (Elements.ElementAt(0).Radius != 0)
                        {
                            ShapeElement ele = new ShapeElement();
                            double brg = firstBrg + Math.PI / 2;
                            if (brg > 2 * Math.PI) { brg = brg - 2 * Math.PI; }
                            double x = 0;
                            double y = 0;
                            MathsHelpers.Trig.PRC(Elements.ElementAt(0).StartX, Elements.ElementAt(0).StartY, brg, Elements.ElementAt(0).Radius * 2, ref x, ref y);
                            Elements.ElementAt(0).EndX = x;
                            Elements.ElementAt(0).EndY = y;
                            ele.StartX = x;
                            ele.StartY = y;
                            ele.EndX = Elements.ElementAt(0).StartX;
                            ele.EndY = Elements.ElementAt(0).StartY;
                            ele.Radius = Elements.ElementAt(0).Radius;
                            Elements.Add(ele);
                        }


                    }

                }
                //==================================================================================================================

                CalcShapeBounds();
                var center = ShapeBounds.Center;
                ShapeCentre = new Point2D(center.X, center.Y);

                FileName = ShapeFileName;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<Point2D> ExplodeShape(double offsetX, double offsetY, double interval = 0)
        {
            var pointsList = new List<Point2D>();

            if (offsetY != 0 || offsetX != 0)
            {

            }

            try
            {
                foreach (var element in Elements)
                {
                    if (element.Radius != 0)
                    {
                        if (interval == -1)
                        {
                            pointsList.AddRange(Geometry.GenerateArc(element.StartX + offsetX, element.StartY + offsetY, element.EndX + offsetX, element.EndY + offsetY, element.Radius, -1));
                        }
                        else if (interval != 0)
                        {
                            var eleDist = element.Length;
                            var segments = (int)Math.Max(1, eleDist / Math.Abs(interval));
                            pointsList.AddRange(Geometry.GenerateArc(element.StartX + offsetX, element.StartY + offsetY, element.EndX + offsetX, element.EndY + offsetY, element.Radius, segments));
                        }
                        else
                        {
                            var eleDist = element.Length;
                            interval = eleDist / 100;
                            if (interval <= 0.1)
                                interval = 0.1;
                            else if (interval >= 1.0)
                                interval = 1.0;

                            var segments = (int)Math.Max(1, eleDist / Math.Abs(interval));
                            pointsList.AddRange(Geometry.GenerateArc(element.StartX + offsetX, element.StartY + offsetY, element.EndX + offsetX, element.EndY + offsetY, element.Radius, segments));
                        }
                    }
                    else
                    {
                        pointsList.Add(new Point3D(element.StartX + offsetX, element.StartY + offsetY, 0));
                        pointsList.Add(new Point3D(element.EndX + offsetX, element.EndY + offsetY, 0));
                    }
                }
                return pointsList;
            }
            catch
            {
                return pointsList;
            }
        }

        /// <summary>
        /// Calculates a series of points around the perimeter of the shape at a given step size
        /// </summary>
        /// <param name="stepSize">The distance between each point</param>
        /// <param name="shapeElementIndex">Returns a list of indecies relating to the location of each elements points with the list</param>
        /// <returns>Returns a List containing <see cref="Point2D"/>s which represent the shape</returns>
        public List<Point2D> ExplodeShape(double stepSize, out List<(int StartIndex, int EndIndex)> shapeElementIndex, double xOffset = 0, double yOffset = 0)
        {
            var points = new List<Point2D>();
            shapeElementIndex = new List<(int StartIndex, int EndIndex)>();

            for(int i = 0; i < Elements.Count; i++)
            {
                var element = Elements[i];

                int startIndex = 0, endIndex = 0;
                startIndex = points.Count;
                points.AddRange(element.ExplodeElement(xOffset, yOffset, stepSize));

                //Removes the duplicate element start point from the list
                if(i != 0 && element.Radius == 0)
                {
                    points.RemoveAt(startIndex);
                    startIndex--;
                }

                endIndex = points.Count - 1;
                shapeElementIndex.Add((startIndex, endIndex));
            }

            return points;
        }

        /// <summary>
        /// Calculates a series of points around the perimeter of the shape at a given step size
        /// </summary>
        /// <param name="stepSize">The distance between each point</param>
        /// <returns>Returns a List containing <see cref="Point2D"/>s which represent the shape</returns>
        public List<Point2D> ExplodeShape(double stepSize)
        {
            var points = new List<Point2D>();
            int startIndex = 0;

            for(int i = 0; i < Elements.Count; i++)
            {
                startIndex = points.Count;
                points.AddRange(Elements[i].ExplodeElement(0, 0, stepSize));

                //Removes the duplicate element start point from the list
                if (i != 0 && Elements[i].Radius == 0)
                    points.RemoveAt(startIndex);
            }

            return points;
        }

        /// <summary>
        /// Calculate the points for a series of dashed lines around the perimeter of the shape
        /// </summary>
        /// <param name="interval">The distance between each dash and the length of the dashed line</param>
        /// <returns>Returns a List of <see cref="Point2D"/>s representing the shape</returns>
        public List<Point2D> ExplodeDashedShape(double interval = 0.2)
        {
            var pointsList = new List<Point2D>();

            try
            {
                if (interval == 0)
                    interval = 0.2;

                foreach (var element in Elements)
                {
                    if (element.Radius != 0)
                    {
                        var eleDist = element.Length;
                        var segments = (int)Math.Max(1, eleDist / Math.Abs(interval));
                        pointsList.AddRange(Geometry.GenerateArc(element.StartX, element.StartY, element.EndX, element.EndY, element.Radius, segments));
                    }
                    else
                    {
                        //Straight
                        double dist = 0, curDist = 0, brg = 0, newX = 0, newY = 0;
                        Trig.RPC(element.StartX, element.StartY, element.EndX, element.EndY, ref brg, ref dist);

                        while (curDist <= dist)
                        {
                            Trig.PRC(element.StartX, element.StartY, brg, curDist, ref newX, ref newY);
                            pointsList.Add(new Point3D(newX, newY, 0));
                            curDist += Math.Abs(interval);
                        }

                        //Add last segment
                        if (pointsList.Count % 2 == 1)
                        {
                            pointsList.Add(new Point3D(element.EndX, element.EndY, 0));
                        }
                    }
                }

                return pointsList;
            }
            catch
            {
                return pointsList;
            }
        }

        /// <summary>
        /// Performs a flat shift in the direction of the center x and y
        /// </summary>
        /// <param name="xCenter">The X Center of the fitted shape</param>
        /// <param name="yCenter">The Y Center of the fitted shape</param>
        /// <returns></returns>
        public Shape FitShape(double xCenter, double yCenter)
        {
            var newShape = new Shape()
            {
                config = this.config,
                FileName = this.FileName,
                Gauge = this.Gauge
            };

            //Shift each element
            foreach (var ele in Elements)
            {
                var newEle = new ShapeElement()
                {
                    StartX = ele.StartX + xCenter,
                    StartY = ele.StartY + yCenter,
                    EndX = ele.EndX + xCenter,
                    EndY = ele.EndY + yCenter,
                    Radius = ele.Radius,
                    Layer = ele.Layer
                };
                newShape.Elements.Add(newEle);
            }

            //Shift each rail
            newShape.LeftRail = new List<Point2D>();
            foreach(var point in LeftRail)
                newShape.LeftRail.Add(new Point2D(point.X + xCenter, point.Y + yCenter));

            newShape.RightRail = new List<Point2D>();
            foreach (var point in RightRail)
                newShape.RightRail.Add(new Point2D(point.X + xCenter, point.Y + yCenter));

            return newShape;
        }

        public Shape GetTansformedShape(double AlignmentLevel, double Cant, double vAngle = 0)
        {
            return Transform(AlignmentLevel, Cant, vAngle);
        }

        public Shape GetTansformedShape(Alignment Alg, double Chainage)
        {
            double lev = Alg.Vertical.GradeLevel(Chainage,out double vang);
            double cant = Alg.GetCantAtChainage(Chainage);

            return Transform(lev, cant, vang);
        }

        private Shape Transform(double baseLevel, double cant, double VerticalAngle = Math.PI/2)
        {
            double xOffset = 0;
            double yOffset = 0;

            //=======================================================================================================
            //Added by ES:15.07.19 - we're doing away with left and right rail so now it's either low rail or string.
            if (config.YFrom > 1)
            { config.YFrom = 0;}
            //=======================================================================================================

            Shape newShape = new Shape()
            {
                config = this.config,
                FileName = this.FileName,
                Gauge = this.Gauge
            };

            //=====================================================
            //Added by ES:18.06.19
            if (config.CantPercent == 0)
            {
                cant = 0;
            }
            else if (config.CantPercent != 100)
            {
                cant = cant * (config.CantPercent / 100);
            }
            //====================================================

            if (cant < 0)
            {
                if (config.YFrom == 1 || config.YFrom==0)
                {
                    baseLevel = baseLevel + Math.Abs(cant) / 2;
                }
                else if (config.YFrom == 3)
                {
                    baseLevel = baseLevel + Math.Abs(cant);
                }
            }
            else if (cant > 0)
            {
                if (config.YFrom == 1)
                {
                    baseLevel = baseLevel + Math.Abs(cant) / 2;
                }
                else if (config.YFrom == 2)
                {
                    baseLevel = baseLevel + Math.Abs(cant);
                }

            }

            foreach (ShapeElement ele in Elements)
            {

                ShapeElement newele = new ShapeElement();
            
                newele.StartX = ele.StartX;
                newele.StartY = ele.StartY;

                newele.EndX = ele.EndX;
                newele.EndY = ele.EndY;

                newele.Radius = ele.Radius;
                newele.Layer = ele.Layer;


                newele.StartY = newele.StartY + baseLevel;
                newele.EndY = newele.EndY + baseLevel;
                newele.StartX = newele.StartX;
                newele.EndX = newele.EndX;
                newShape.Elements.Add(newele);
            }

            //=========================================================================================================
            double offsetYCorrected = config.CenterOffsetY;
            //if (config.YVert != 0)
            //{
            //    if (!MathsHelpers.Vector.FnEquals(VerticalAngle, 0) & !MathsHelpers.Vector.FnEquals(VerticalAngle, 0))
            //    {
            //        offsetYCorrected = offsetYCorrected * Math.Cos(VerticalAngle); //should be plus!
            //    }
            //}

            if (config.YFrom > 0)
            {
                //I think this is wrong, but...
                offsetYCorrected = offsetYCorrected + Math.Abs( cant) / 2;
            }
           
            double angle = -Math.Asin(cant / Gauge);
            if (config.ApplyCant == 0)
            {
                if (config.MeasureY == 0)
                {
                    if (config.MeasureX == 0)
                    {
                        xOffset = config.CenterOffsetX - offsetYCorrected * Math.Tan(angle);
                        yOffset = offsetYCorrected;
                    }
                    else
                    {
                        xOffset = config.CenterOffsetX * Math.Cos(angle) - offsetYCorrected * Math.Tan(angle);
                        yOffset = offsetYCorrected;
                    }
                }
                else
                {
                    if (config.MeasureX == 0)
                    {
                        xOffset = config.CenterOffsetX - offsetYCorrected * Math.Sin(angle);
                        yOffset = offsetYCorrected * Math.Cos(angle) + config.CenterOffsetX * Math.Tan(angle);
                    }
                    else
                    {
                        xOffset = config.CenterOffsetX * Math.Cos(angle) - offsetYCorrected * Math.Sin(angle);
                        yOffset = config.CenterOffsetX * Math.Sin(angle) + offsetYCorrected * Math.Cos(angle);
                    }

                }
            }
            else
            {
                if (cant <= 0)
                {
                    xOffset = config.CenterOffsetX + (config.LeftHandX * Math.Abs(cant));
                    yOffset = config.CenterOffsetY + (config.LeftHandY * cant);
                }
                else
                {
                    xOffset = config.CenterOffsetX + (config.RightHandX * Math.Abs(cant));
                    yOffset = config.CenterOffsetY + (config.RightHandY * cant);
                }


            }
            //=========================================================================================================

            foreach (ShapeElement ele in newShape.Elements)
            {
                ele.StartX = ele.StartX + xOffset;
                ele.EndX = ele.EndX + xOffset;
                ele.StartY = ele.StartY + yOffset - config.CenterOffsetY;
                ele.EndY = ele.EndY + yOffset - config.CenterOffsetY;
            }

            double rx = 0, ry = 0;
            CalcRotationOrigin(baseLevel, cant, ref rx, ref ry);
            //==================================================================================================

            double railRotation = angle;

            foreach (ShapeElement ele in newShape.Elements)
            {
                Point2D startPoint = new Point2D();
                Point2D endPoint = new Point2D();

                
                if (config.IgnoreRotaition == true)
                { angle = 0; }

                if (cant > 0)
                {
                    startPoint = Trig.RotateXYClockWiseAroundOrigin(ele.StartX, ele.StartY, -angle, rx, ry);
                    endPoint = Trig.RotateXYClockWiseAroundOrigin(ele.EndX, ele.EndY, -angle, rx, ry);
                }
                else if (cant <= 0)
                {

                    
                    startPoint = Trig.RotateXYClockWiseAroundOrigin(ele.StartX, ele.StartY, -angle, rx, ry);
                    endPoint = Trig.RotateXYClockWiseAroundOrigin(ele.EndX, ele.EndY, -angle, rx, ry);

                }

                ele.StartX = startPoint.X;
                ele.StartY = startPoint.Y;
                ele.EndX = endPoint.X;
                ele.EndY = endPoint.Y;

            }

            newShape.TunnelAxis.X = xOffset;
            newShape.TunnelAxis.Y = yOffset;

            //===========================================================================
            //Rails
            LeftRail = new List<Point2D>();
            RightRail = new List<Point2D>();

            foreach (Point2D pt in Rail.Coords)
            {
                Point2D lr = new Point2D();
                lr.X = pt.X - Gauge / 2;
                lr.Y = pt.Y + baseLevel;
                lr = Trig.RotateXYClockWiseAroundOrigin(lr.X, lr.Y, -railRotation , rx, ry);
                newShape.LeftRail.Add(lr);
                lr = new Point2D();
                lr.X = pt.X + Gauge / 2;
                lr.Y = pt.Y + baseLevel;
                lr = Trig.RotateXYClockWiseAroundOrigin(lr.X, lr.Y, -railRotation, rx, ry);
                newShape.RightRail.Add(lr);
            }

            foreach (Point2D pt in Train.Coords)
            {
                Point2D lr = new Point2D();
                lr.X = pt.X;
                lr.Y = pt.Y;
                lr = Trig.RotateXYClockWiseAroundOrigin(lr.X, lr.Y, -railRotation, rx, ry);
                newShape.TrainPoints.Add(lr);
            }

            //============================================================================
            newShape.DispXOffset = xOffset;
            newShape.DispYOffset = yOffset;

            newShape.CalcShapeBounds();
            var center = ShapeBounds.Center;
            newShape.ShapeCentre = new Point2D(center.X, center.Y);
            newShape.LayerName = this.LayerName;
            return newShape;
        }

        /// <summary>
        /// Calculates the Origin point of rotation based on a given level and cant
        /// </summary>
        /// <param name="baseLevel">The base level for the rotation</param>
        /// <param name="cant">The cant to be applied</param>
        /// <param name="rx">A <see cref="double"/> containing the X coordinate of the rotation origin</param>
        /// <param name="ry">A <see cref="double"/> containing the Y coordinate of the rotation origin</param>
        public void CalcRotationOrigin(double baseLevel, double cant, ref double rx, ref double ry)
        {
            rx = 0;
            ry = baseLevel;

            if (cant < 0)
            {
                if (config.YFrom == 0 | config.YFrom == 2)
                {
                    //Left or low rail
                    rx = -Math.Abs(Math.Sqrt((Gauge * Gauge) - (cant * cant)) / 2);

                }
                else if (config.YFrom == 3)
                {
                    //right
                    rx = Math.Abs(Math.Sqrt((Gauge * Gauge) - (cant * cant)) / 2);

                }
            }
            else if (cant > 0)
            {
                if (config.YFrom == 3 | config.YFrom == 0)
                {
                    //right rail or low rail
                    rx = Math.Abs(Math.Sqrt((Gauge * Gauge) - (cant * cant)) / 2);
                }
                else if (config.YFrom == 2)
                {
                    //left rail 
                    rx = -Math.Abs(Math.Sqrt((Gauge * Gauge) - (cant * cant)) / 2);
                }
            }
        }

        /// <summary>
        /// Transforms to rails by this given baselevel and cant
        /// </summary>
        /// <param name="baseLevel">The base level for the rails</param>
        /// <param name="cant">The cant to be applied</param>
        public void TransformRails(double baseLevel, double cant)
        {
            LeftRail.Clear();
            RightRail.Clear();

            double rx = 0, ry = 0;
            CalcRotationOrigin(baseLevel, cant, ref rx, ref ry);

            double angle = -Math.Asin(cant / Gauge);
            foreach (Point2D pt in Rail.Coords)
            {
                Point2D lr = new Point2D();
                lr.X = pt.X - Gauge / 2;
                lr.Y = pt.Y + baseLevel;
                lr = Trig.RotateXYClockWiseAroundOrigin(lr.X, lr.Y, -angle, rx, baseLevel);
                LeftRail.Add(lr);
                lr = new Point2D();
                lr.X = pt.X + Gauge / 2;
                lr.Y = pt.Y + baseLevel;
                lr = Trig.RotateXYClockWiseAroundOrigin(lr.X, lr.Y, -angle, rx, baseLevel);
                RightRail.Add(lr);
            }
        }

        public void CalcShapeBounds()
        {
            ShapeBounds = new Bounds(double.MaxValue, double.MaxValue, double.MaxValue, double.MinValue, double.MinValue, double.MinValue);
            //var testBounds = new Bounds();
            foreach (var element in Elements)
            {
                if (element.Radius == 0)
                {
                    //Its straight
                    ShapeBounds.Update(element.StartX, element.StartY);
                    ShapeBounds.Update(element.EndX, element.EndY);
                }
                else
                {
                    double arcCenterX = 0, arcCenterY = 0;
                    Trig.CalcCenterOfArc(element.StartX, element.StartY, element.EndX, element.EndY, element.Radius, ref arcCenterX, ref arcCenterY);

                    //Arc or circle
                    if (element.StartX == element.EndX && element.StartY == element.EndY)
                    {
                        //Its a circle so add the 2 furthest corners of the bounds
                        ShapeBounds.Update(arcCenterX - element.Radius, arcCenterY - element.Radius);
                        ShapeBounds.Update(arcCenterX + element.Radius, arcCenterY + element.Radius);
                    }
                    else
                    {
                        //Its an Arc add the bounds
                        double eleCenterX = (element.StartX + element.EndX) / 2, eleCenterY = (element.StartY + element.EndY) / 2, brg = 0;

                        if(Vector.FnEquals(eleCenterX, arcCenterX) && Vector.FnEquals(eleCenterY, arcCenterY))
                        {
                            brg = Trig.RPC(element.StartX, element.StartY, element.EndX, element.EndY);
                            brg -= Math.PI / 2;

                            if (brg < 0)
                                brg += (Math.PI * 2);
                        }
                        else
                            brg = Trig.RPC(arcCenterX, arcCenterY, eleCenterX, eleCenterY);

                        var arcMidPoint = Trig.PRC(arcCenterX, arcCenterY, brg, Math.Abs(element.Radius));

                        ShapeBounds.Update(element.StartX, element.StartY);
                        ShapeBounds.Update(element.EndX, element.EndY);
                        ShapeBounds.Update(arcMidPoint);
                    }
                }
            }
        }

        /// <summary>
        /// DistanceAlongShape - given X & Y (which I imagine will always translate as offset and level) retruns the cumulative distance around the shape
        /// </summary>
        /// <param name="X">vector X or offset</param>
        /// <param name="Y">vector Y or level</param>
        /// <returns>the distance of the above vector to the closest offset around the shape</returns>
        public double DistanceAlongShape(double X, double Y)
        {
            double dist = 0;
            double off = 0;
            DistanceAndOffsetFromShape(X, Y, ref dist, ref off);
            return dist;
        }

        /// <summary>
        /// DistanceAndOffsetFromShape - returns the distance along and the offset from a shape of a given vector (X & Y - think offset and level)
        /// </summary>
        /// <param name="X">the X coord of the point to query</param>
        /// <param name="Y">the Y coord fo the point to query</param>
        /// <param name="Distance">Distance of the point along the shape</param>
        /// <param name="Offset">Offset from the shape</param>
        public void DistanceAndOffsetFromShape(double X, double Y, ref double Distance, ref double Offset)
        {
            double nearestOffset = double.PositiveInfinity;
            double nearestDistance = 0;
            double culmDistance = 0;
            double thisdist = 0;
            double thisoffset = 0;
            double splayOff = 0;
            foreach (ShapeElement ele in this.Elements)
            {
                NRG.MathsHelpers.Trig.DistanceAndOffsetFromLine(ele.StartX, ele.StartY, ele.EndX, ele.EndY, ele.Radius, X, Y, ref thisdist, ref thisoffset);
                double centreX = 0, centreY = 0;
                NRG.MathsHelpers.Trig.CalcCenterOfArc(ele.StartX, ele.StartY, ele.EndX, ele.EndY, ele.Radius, ref centreX, ref centreY);
                if (thisdist >= 0 & thisdist <= ele.Length)//check to make sure the  point is within the element
                {
                    if (Math.Abs(thisoffset) < Math.Abs(nearestOffset)) { nearestOffset = thisoffset; nearestDistance = culmDistance + thisdist; }
                }
                //also check to see if we're closest to a tangent point
                //Start tangent
                splayOff = NRG.MathsHelpers.Vector.FnDistance(ele.StartX, ele.StartY, X, Y);
                if (Math.Abs(splayOff) < Math.Abs(nearestOffset))
                {
                    nearestOffset = splayOff;
                    nearestDistance = culmDistance;
                    if (NRG.MathsHelpers.Vector.FnOnLine3(ele.StartX, ele.StartY, ele.EndX, ele.EndY, X, Y) < 0) { nearestOffset = -nearestOffset; }
                }
                //End tangent
                splayOff = NRG.MathsHelpers.Vector.FnDistance(ele.EndX, ele.EndY, X, Y);
                if (Math.Abs(splayOff) < Math.Abs(nearestOffset))
                {
                    nearestOffset = splayOff;
                    nearestDistance = culmDistance + ele.Length;
                    if (NRG.MathsHelpers.Vector.FnOnLine3(ele.StartX, ele.StartY, ele.EndX, ele.EndY, X, Y) < 0) { nearestOffset = -nearestOffset; }
                }

                culmDistance = culmDistance + ele.Length;
            }
            Distance = nearestDistance;
            Offset = Math.Abs(nearestOffset);
        }

        /// <summary>
        /// FindShapeElement - Finds the element a given chainage/distance lands within
        /// Added by ES:07.06.19
        /// </summary>
        /// <param name="Chainage"> distance around the shape</param>
        /// <param name="StartChainage">retruns the distance around the shape to the start of the found element</param>
        /// <returns>Index to the found element or -1 if no element was found</returns>
        public int FindShapeElement(double Chainage, ref double StartChainage)
        {
            double culmDistance = 0;

            for (int i = 0; i < Elements.Count; i++)
            {
                ShapeElement ele = Elements.ElementAt(i);
                if (Chainage >= culmDistance & Chainage <= culmDistance + ele.Length) { StartChainage = culmDistance; return i; }

                culmDistance = culmDistance + ele.Length;

            }
            return -1; //didn't find one

        }

        /// <summary>
        /// CoordsOnShape - calculates the coordinates of a distance around the shape 
        /// Added by ES:07.06.19
        /// </summary>
        /// <param name="Chainage"> the distance along or around a shape. This is in respect to the entire shape</param>
        /// <param name="X">calculated x coordinate</param>
        /// <param name="Y">calculated y coordinate</param>
        /// <param name="Tangent">tangent bearing at the calculated point</param>
        /// <returns>Index to the shape element on which the chainage falls</returns>
        public int CoordsOnShape(double Chainage, ref double X, ref double Y, ref double Tangent)
        {
            int eleIdx = 0;
            double startChainage = 0;
            double brg = 0;
            double dist = 0;
            double distance = 0;
            eleIdx = FindShapeElement(Chainage, ref startChainage);

            if (eleIdx >= 0 & eleIdx < Elements.Count)

            {
                ShapeElement ele = Elements.ElementAt(eleIdx);
                NRG.MathsHelpers.Trig.RPC(ele.StartX, ele.StartY, ele.EndX, ele.EndY, ref brg, ref dist);
                distance = Chainage - startChainage;
                if (ele.Radius == 0)
                {
                    //Straight
                    Tangent = brg;
                }
                else
                {
                    //Arc
                    double j = distance / (ele.Radius * 2);
                    double entryBrg = 0; double exitBrg = 0; double len = 0;
                    NRG.MathsHelpers.Trig.CalcEntryAndExitBrg(ele.StartX, ele.StartY, ele.EndX, ele.EndY, 0, 0, ele.Radius, ref entryBrg, ref exitBrg, ref len); //Added 0 and 0 as parameters. If this if used at all, this ShapeElement will need rethinking TN 03/08/22
                    brg = entryBrg + j;
                    Tangent = entryBrg + (2 * j);
                    distance = (2 * ele.Radius) * Math.Sin(j);
                }
                NRG.MathsHelpers.Trig.PRC(ele.StartX, ele.StartY, brg, distance, ref X, ref Y);
            }

            return eleIdx;

        }

        /// <summary>
        /// CoordsOnShape - calculates the coordinates of a given distance and offset around a shape
        /// Added by ES:07.06.19
        /// </summary>
        /// <param name="Chainage"> the distance along or around a shape. This is in respect to the entire shape</param>
        /// <param name="Offset">Offset from the shape</param>
        /// <param name="X">calculated x coordinate</param>
        /// <param name="Y">calculated y coordinate</param>
        /// <returns>Index to the shape element on which the chainage falls</returns>
        public int CoordsOnShape(double Chainage, double Offset, ref double X, ref double Y)
        {
            int eleIdx = -1;
            double px = 0; double py = 0; double tangent = 0;
            eleIdx = CoordsOnShape(Chainage, ref px, ref py, ref tangent);
            if (eleIdx >= 0)
            {
                tangent = tangent + (2 * Math.PI);
                if (tangent > 0) { tangent = tangent + (2 * Math.PI); }
                NRG.MathsHelpers.Trig.PRC(px, py, tangent, Offset, ref X, ref Y);
            }

            return eleIdx;
        }

        public void DrawShape(PaintEventArgs e, Control canvas, int selectedElement = -1, bool DrawAlignmentAxis = true, bool DrawTrain = true, List<Point2D> surveyCoords = null, DTMPoint CuttingHeardCoords = null, MachineGraphics TBM = null, double xOffset = 0, double yOffset = 0, int InputPad = 75, bool DrawScale = false)
        {
            if (Elements.Count > 0)
            {
                Pen bluePen = new Pen(Color.Black, 3.01f);
                double x1 = 0, y1 = 0;
                double x2 = 0, y2 = 0, x3 = 0, y3 = 0;

                Pen yellowpen = new Pen(Color.Ivory, 1.01f);
                Pen drawpen;


                int currentIdx = -1;

                Bounds bnd;
                CalcShapeBounds();
                bnd = ShapeBounds;

                int pad = 75;
                pad = InputPad;
                foreach (ShapeElement ele in Elements)
                {


                    currentIdx++;


                    if (selectedElement == -1 | selectedElement != currentIdx)
                    {
                        drawpen = bluePen;
                    }
                    else
                    {
                        drawpen = yellowpen;
                    }


                    if (ele.Radius == 0)
                    {
                        //Straight
                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, ele.StartX, ele.StartY, bnd, ref x1, ref y1, pad, xOffset, yOffset);
                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, ele.EndX, ele.EndY, bnd, ref x2, ref y2, pad, xOffset, yOffset);

                        e.Graphics.DrawLine(drawpen, (float)x1, (float)y1, (float)x2, (float)y2);
                    }
                    else
                    {
                        //Arc
                        List<Point2D> pnts = Geometry.GenerateArc(ele.StartX, ele.StartY, ele.EndX, ele.EndY, ele.Radius, 50);

						Font f = new Font("Arial", 10);
						SolidBrush b = new SolidBrush(Color.Black);
						
                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, ele.EndX, ele.EndY, bnd, ref x1, ref y1, pad, xOffset, yOffset);
						e.Graphics.DrawString((ele.Radius * 1000).ToString("0"), f, b, (float)x1-10, (float)y1);


						int count = 0;
                        Point2D last = new Point2D();
                        foreach (Point2D pnt in pnts)
                        {
                            count++;
                            if (count > 1)
                            {
                                last = pnts.ElementAt(count - 2);

                                Vector.CalcScaledCoords(canvas.Width, canvas.Height, pnt.X, pnt.Y, bnd, ref x1, ref y1, pad, xOffset, yOffset);
                                Vector.CalcScaledCoords(canvas.Width, canvas.Height, last.X, last.Y, bnd, ref x2, ref y2, pad, xOffset, yOffset);
                                e.Graphics.DrawLine(drawpen, (float)x1, (float)y1, (float)x2, (float)y2);

								



							}
                        }

                    }
                }
                //drawpen.Dispose();
                Pen redPen = new Pen(Color.Red, 2.0f);


                Vector.CalcScaledCoords(canvas.Width, canvas.Height, 0, -0.1, bnd, ref x1, ref y1, pad, xOffset, yOffset);
                Vector.CalcScaledCoords(canvas.Width, canvas.Height, 0, 0.1, bnd, ref x2, ref y2, pad, xOffset, yOffset);

                e.Graphics.DrawLine(redPen, (float)x1, (float)y1, (float)x2, (float)y2);

                Vector.CalcScaledCoords(canvas.Width, canvas.Height, -0.1, 0, bnd, ref x1, ref y1, pad, xOffset, yOffset);
                Vector.CalcScaledCoords(canvas.Width, canvas.Height, 0.1, 0, bnd, ref x2, ref y2, pad, xOffset, yOffset);

                e.Graphics.DrawLine(redPen, (float)x1, (float)y1, (float)x2, (float)y2);

                if (DrawAlignmentAxis)
                {
                    redPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    redPen.Width = 0.001f;
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, -100, 0, bnd, ref x1, ref y1, pad, xOffset, yOffset);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, 100, 0, bnd, ref x2, ref y2, pad, xOffset, yOffset);
                    e.Graphics.DrawLine(redPen, (float)x1, (float)y1, (float)x2, (float)y2);


                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, 0, -100, bnd, ref x1, ref y1, pad, xOffset, yOffset);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, 0, 100, bnd, ref x2, ref y2, pad, xOffset, yOffset);
                    e.Graphics.DrawLine(redPen, (float)x1, (float)y1, (float)x2, (float)y2);
                }
                redPen.Color = Color.Black;

                Vector.CalcScaledCoords(canvas.Width, canvas.Height, TunnelAxis.X - 0.2, TunnelAxis.Y, bnd, ref x1, ref y1, pad, xOffset, yOffset);
                Vector.CalcScaledCoords(canvas.Width, canvas.Height, TunnelAxis.X + 0.2, TunnelAxis.Y, bnd, ref x2, ref y2, pad, xOffset, yOffset);
                e.Graphics.DrawLine(redPen, (float)x1, (float)y1, (float)x2, (float)y2);

                Vector.CalcScaledCoords(canvas.Width, canvas.Height, TunnelAxis.X, TunnelAxis.Y - 0.2, bnd, ref x1, ref y1, pad, xOffset, yOffset);
                Vector.CalcScaledCoords(canvas.Width, canvas.Height, TunnelAxis.X, TunnelAxis.Y + 0.2, bnd, ref x2, ref y2, pad, xOffset, yOffset);
                e.Graphics.DrawLine(redPen, (float)x1, (float)y1, (float)x2, (float)y2);
                redPen.Dispose();

                //Rails
                for (int i = 0; i < LeftRail.Count - 1; i++)
                {
                    Pen bp = new Pen(Color.Black, 0.1F);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, LeftRail.ElementAt(i).X, LeftRail.ElementAt(i).Y, bnd, ref x1, ref y1, pad, xOffset, yOffset);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, LeftRail.ElementAt(i + 1).X, LeftRail.ElementAt(i + 1).Y, bnd, ref x2, ref y2, pad, xOffset, yOffset);
                    e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, RightRail.ElementAt(i).X, RightRail.ElementAt(i).Y, bnd, ref x1, ref y1, pad, xOffset, yOffset);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, RightRail.ElementAt(i + 1).X, RightRail.ElementAt(i + 1).Y, bnd, ref x2, ref y2, pad, xOffset, yOffset);
                    e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);
                    bp.Dispose();
                }


                if (DrawTrain)
                {

                    for (int i = 0; i < TrainPoints.Count - 1; i = i + 2)

                    {
                        Pen bp = new Pen(Color.Black, 0.1F);
                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, TrainPoints.ElementAt(i).X, TrainPoints.ElementAt(i).Y, bnd, ref x1, ref y1, pad, xOffset, yOffset);
                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, TrainPoints.ElementAt(i + 1).X, TrainPoints.ElementAt(i + 1).Y, bnd, ref x2, ref y2, pad, xOffset, yOffset);
                        e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);
                        bp.Dispose();

                    }
                }

                if (surveyCoords != null)
                {
                    foreach (Point2D pt in surveyCoords)
                    {
                        Pen bp = new Pen(Color.Black, 0.1F);

                        double Cx1 = 0;
                        double Cy1 = 0;
                        double Cx2 = 0;
                        double Cy2 = 0;





						//Center coords
						Vector.CalcScaledCoords(canvas.Width, canvas.Height, pt.X, pt.Y, bnd, ref Cx1, ref Cy1, pad);
						Vector.CalcScaledCoords(canvas.Width, canvas.Height, pt.X, pt.Y, bnd, ref Cx2, ref Cy2, pad);


                        //if (DrawScale)//added by ES:01.11.20
                        //{
                        //    //we assume a the radius of the first element
                        //    //Brush brownBrush = new Brush(Color.Brown);

                        //    Pen brownPen = new Pen(Color.Brown);
                        //    double cx3 = 0; double cy3 = 0;
                        //    Vector.CalcScaledCoords(canvas.Width, canvas.Height, pt.X, pt.Y + +Elements.ElementAt(0).Radius, bnd, ref cx3, ref cy3 , pad);
                        //    e.Graphics.DrawLine(brownPen, (float)Cx1, (float)Cy1, (float)cx3, (float)cy3);
                        //}

						double Prism = 0.060;
                        double halfPrism = 0.030;
                        double diag = 1.4142;

                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, pt.X - halfPrism, pt.Y + halfPrism, bnd, ref x1, ref y1, pad);
                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, pt.X + halfPrism, pt.Y - halfPrism, bnd, ref x2, ref y2, pad);

                        var b = new System.Drawing.SolidBrush(Color.PeachPuff);

                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, pt.X - 0.06, pt.Y, bnd, ref x1, ref y1, pad);
                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, pt.X + 0.06, pt.Y, bnd, ref x2, ref y2, pad);


                        //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);
                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, pt.X, pt.Y + 0.06, bnd, ref x1, ref y1, pad);
                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, pt.X, pt.Y - 0.06, bnd, ref x2, ref y2, pad);
                        //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);
                        bp.Dispose();
                    }

                }
            

                if (TBM != null)
                {

                    if (DrawScale)//added by ES:01.11.20
                    {
                        //we assume a the radius of the first element
                        //Brush brownBrush = new Brush(Color.Brown);


                        double centX = 0; double centY = 0;

                        centX= (Elements.ElementAt(0).StartX + Elements.ElementAt(0).EndX)/ 2;
                        centY = (Elements.ElementAt(0).StartY + Elements.ElementAt(0).EndY) / 2;

                        Pen brownPen = new Pen(Color.Brown);
                        double cx3 = 0; double cy3 = 0;
                        double rad = Elements.ElementAt(0).Radius;

                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, centX,  centY-rad, bnd, ref cx3, ref cy3, pad);

                        double cx4 = centX;
                        double cy4 = centY + Elements.ElementAt(0).Radius/2;
                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, centX, centY+rad, bnd, ref cx4, ref cy4, pad);
                        e.Graphics.DrawLine(brownPen, (float)cx3, (float)cy3, (float)cx4, ((float)cy4));

                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, centX - rad, centY , bnd, ref cx3, ref cy3, pad);

                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, centX + rad, centY , bnd, ref cx4, ref cy4, pad);
                        e.Graphics.DrawLine(brownPen, (float)cx3, (float)cy3, (float)cx4, ((float)cy4));

                        double curdist = 0;
                        while (curdist < rad)
                        {
                            curdist = curdist = curdist + 0.02;
                            Vector.CalcScaledCoords(canvas.Width, canvas.Height, centX + curdist, centY, bnd, ref cx4, ref cy4, pad);
                            e.Graphics.DrawLine(brownPen, (float)cx4, (float)cy3+4, (float)cx4, ((float)cy4)-4);


                        }
                        curdist = 0;
                        while (curdist < rad)
                        {
                            curdist = curdist = curdist + 0.02;
                            Vector.CalcScaledCoords(canvas.Width, canvas.Height, centX - curdist, centY, bnd, ref cx4, ref cy4, pad);
                            e.Graphics.DrawLine(brownPen, (float)cx4, (float)cy3 + 4, (float)cx4, ((float)cy4) - 4);


                        }

                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, centX , centY, bnd, ref cx3, ref cy3, pad);

                        curdist = 0;
                        while (curdist < rad)
                        {
                            curdist = curdist = curdist + 0.02;
                            Vector.CalcScaledCoords(canvas.Width, canvas.Height, centX , centY + curdist, bnd, ref cx4, ref cy4, pad);
                            e.Graphics.DrawLine(brownPen, (float)cx3-4, (float)cy4 , (float)cx3+4, ((float)cy4) );
                        }

                        curdist = 0;
                        while (curdist < rad)
                        {
                            curdist = curdist = curdist + 0.02;
                            Vector.CalcScaledCoords(canvas.Width, canvas.Height, centX, centY - curdist, bnd, ref cx4, ref cy4, pad);
                            e.Graphics.DrawLine(brownPen, (float)cx3-4, (float)cy4 , (float)cx3+4, ((float)cy4) );
                        }


                        //e.Graphics.DrawLine(brownPen, (float)canvas.Width / 2, (float)canvas.Height / 2, canvas.Width / 2, ((float)canvas.Height / 2 )- (float)cy3);
                    }

                    Pen bp = new Pen(Color.Black, 0.1F);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.RearVector.X, TBM.RearVector.Y, bnd, ref x2, ref y2, pad);
                    e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    double frontVecX = x1; double frontVecY = y1;




                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.LeftVector.X, TBM.LeftVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    double leftvecX = x2; double leftvecY = y2;

                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.RightVector.X, TBM.RightVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    double rightvecX = x2; double rightvecY = y2;

                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.BottomVector.X, TBM.BottomVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    double bottomvecX = x2; double bottomvecY = y2;


                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.TopVector.X, TBM.TopVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    double topvecX = x2; double topvecY = y2;


                    #region testing 
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.TopVector.X, TBM.TopVector.Y, bnd, ref x1, ref y1, pad);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.BottomVector.X, TBM.BottomVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.LeftVector.X, TBM.LeftVector.Y, bnd, ref x1, ref y1, pad);
                    Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.RightVector.X, TBM.RightVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);
                    #endregion testing


                    PointF[] MachineVertsHz = { new PointF((float)frontVecX, (float)frontVecY), new PointF((float)leftvecX, (float)leftvecY), new PointF((float)rightvecX, (float)rightvecY) };


                    if (!MathsHelpers.Vector.FnEquals(leftvecX, rightvecX) && !MathsHelpers.Vector.FnEquals(topvecX, topvecY))
                    {

                        LinearGradientBrush blueBrush = new LinearGradientBrush(new Point((int)leftvecX, (int)leftvecY), new Point((int)rightvecX, (int)(rightvecY)),
                        Color.LightGreen,
                        Color.DarkGray);

						//LinearGradientBrush vertBrush = new LinearGradientBrush(new Point((int)leftvecX, (int)leftvecY), new Point((int)rightvecX, (int)(rightvecY)),
						//Color.DarkRed,
						//Color.DarkBlue);


						e.Graphics.FillPolygon(blueBrush, MachineVertsHz);

                        PointF[] MachineVertsVert = { new PointF((float)frontVecX, (float)frontVecY), new PointF((float)topvecX, (float)topvecY), new PointF((float)bottomvecX, (float)bottomvecY) };
                        //SolidBrush redBrush = new SolidBrush(Color.Red);

                        LinearGradientBrush vertBrush = new LinearGradientBrush(new Point((int)bottomvecX, (int)bottomvecY), new Point((int)topvecX, (int)(topvecY)),
                        Color.LightGreen,
                        Color.DarkGray);
                        e.Graphics.FillPolygon(vertBrush, MachineVertsVert);


                        e.Graphics.DrawLine(bp, (float)bottomvecX, (float)bottomvecY, (float)topvecX, (float)topvecY);
                        e.Graphics.DrawLine(bp, (float)leftvecX, (float)((bottomvecY + topvecY)/2) , (float)rightvecX, (float)((bottomvecY + topvecY)) / 2);
                        e.Graphics.DrawLine(bp, (float)frontVecX, (float)frontVecY, (float)topvecX, (float)topvecY);
                        e.Graphics.DrawLine(bp, (float)frontVecX, (float)frontVecY, (float)rightvecX, (float)rightvecY);
                        e.Graphics.DrawLine(bp, (float)frontVecX, (float)frontVecY, (float)leftvecX, (float)leftvecY);

                        //e.Graphics.FillPolygon(blueBrush, MachineVertsHz);


                        bp = new Pen(Color.Black, 2.2F);
                        //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                        //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.RearVector.X, TBM.RearVector.Y, bnd, ref x2, ref y2, pad);
                        e.Graphics.DrawLine(bp, (float)frontVecX, (float)frontVecY, (float)(topvecX + bottomvecX)/2, (float)(topvecY +bottomvecY)/2);
                        e.Graphics.DrawLine(bp, (float)frontVecX, (float)frontVecY, (float)(topvecX + bottomvecX) / 2, (float)(topvecY + bottomvecY) / 2);



                        blueBrush.Dispose();
                        vertBrush.Dispose();
                        //redBrush.Dispose();

                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.ForwardVector.X, TBM.ForwardVector.Y, bnd, ref x1, ref y1, pad);
                        Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.BackwardVector.X, TBM.BackwardVector.Y, bnd, ref x2, ref y2, pad);
                        e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                        



                    }
                    //#region sort this...

                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.RearVector.X, TBM.RearVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);


                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.LeftVector.X, TBM.LeftVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);
                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.RightVector.X, TBM.RightVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.BottomVector.X, TBM.BottomVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.TopVector.X, TBM.TopVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.FrontVector.X, TBM.FrontVector.Y, bnd, ref x1, ref y1, pad);
                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.RearVector.X, TBM.RearVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    #region testing 
                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.TopVector.X, TBM.TopVector.Y, bnd, ref x1, ref y1, pad);
                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.BottomVector.X, TBM.BottomVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);

                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.LeftVector.X, TBM.LeftVector.Y, bnd, ref x1, ref y1, pad);
                    //Vector.CalcScaledCoords(canvas.Width, canvas.Height, TBM.RightVector.X, TBM.RightVector.Y, bnd, ref x2, ref y2, pad);
                    //e.Graphics.DrawLine(bp, (float)x1, (float)y1, (float)x2, (float)y2);
                    #endregion testing
                    //#endregion sort this
                }
            }
        }


		/// <summary>
		/// Calculates x and y coords relative to the center of the canvas with optional padding
		/// </summary>
		/// <param name="pictureWidth"></param>
		/// <param name="pictureHeight"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="shapeBounds"></param>
		/// <param name="scaledX"></param>
		/// <param name="scaledY"></param>
		/// <param name="padding">Number of pixels between the model and the edge of the canvas</param>
		void CalcScaledCoords(int pictureWidth, int pictureHeight, double xPos, double yPos, NRG.Models.Bounds shapeBounds, ref double scaledX, ref double scaledY, int padding = 0)
		{
			var fracX = (pictureWidth - (padding * 2)) / shapeBounds.Size.X;
			var fracY = Math.Max((pictureHeight - (padding * 2)) / shapeBounds.Size.Y, 0.1);
			var frac = Math.Min(fracX, fracY);
			double xOffset = 0, yOffset = 0;

			if (fracX < fracY)
				yOffset = ((pictureHeight - (padding * 2)) - ((shapeBounds.Size.Y) * frac)) / 2;
			else
				xOffset = ((pictureWidth - (padding * 2)) - ((shapeBounds.Size.X) * frac)) / 2;

			scaledX = padding + xOffset + ((xPos - shapeBounds.Min.X) * frac);
			scaledY = pictureHeight - (padding + yOffset + ((yPos - shapeBounds.Min.Y) * frac));
		}


	//}

        public List<Point2D> ExplodeShapeByBearing(double originX, double originY, List<double> brgList)
        {
            originX = ShapeCentre.X;
            originY = ShapeCentre.Y;
            var points = new List<Point2D>();
            CalcShapeBounds();
            var distToCorner = Vector.FnDistance(originX, originY, ShapeBounds.Max.X, ShapeBounds.Max.Y);

            for(int i = 0; i < brgList.Count; i++)
            {
                double distance = double.MaxValue, brg = brgList[i];
                var intersectionPoint = new Point2D();


                foreach(var element in Elements)
                {
                    if (element.Radius == 0)
                    {
                        //Straight element
                        var segBrg = Trig.RPC(element.StartX, element.StartY, element.EndX, element.EndY);
                        double newX = 0, newY = 0, ipx = 0, ipy = 0;
                        Trig.PRC(originX, originY, brg, distToCorner, ref newX, ref newY);

                        if (Intersection.IntersectLines(element.StartX, element.StartY, element.EndX, element.EndY, originX, originY, newX, newY, ref ipx, ref ipy))
                        {
                            //Get distance to intersection point
                            var dist = Vector.FnDistance(originX, originY, ipx, ipy);
                            if (dist < distance)
                            {
                                distance = dist;
                                intersectionPoint = new Point2D(ipx, ipy);
                            }
                        }
                    }
                    else
                    {
                        var eleStart = new Point2D(element.StartX, element.StartY);
                        var eleEnd = new Point2D(element.EndX, element.EndY);

                        var lineStart = new Point2D(originX, originY);
                        var lineEnd = Trig.PRC(originX, originY, brg, distToCorner);
                        var iPoints = new List<Point2D>();


                        if (Intersection.LineIntersectsArc(eleStart, eleEnd, element.Radius, lineStart, lineEnd, ref iPoints))
                        {
                            //There is an intersection
                            for (int j = 0; j < iPoints.Count; j++)
                            {
                                var iPoint = iPoints[j];
                                if (Intersection.FnInBox(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, iPoint.X, iPoint.Y))
                                {
                                    var dist = Vector.FnDistance(originX, originY, iPoint.X, iPoint.Y);
                                    if (dist < distance)
                                    {
                                        distance = dist;
                                        intersectionPoint = new Point2D(iPoint.X, iPoint.Y);
                                    }
                                }
                            }
                        }
                    }
                }

                points.Add(intersectionPoint);

                //Calc point at current brg and dist to corner
                //double newX = 0, newY = 0, dist = 0, offset = 0;
                //Trig.PRC(originX, originX, brgList[i], distToCorner, ref newX, ref newY);

                ////Get distance along shape
                //DistanceAndOffsetFromShape(newX, newY, ref dist, ref offset);

                ////Calc point coord at 0 offset
                //CoordsOnShape(dist, 0, ref newX, ref newY);

                //points.Add(new Point2D(newX, newY));
            }

            return points;
        }
    }


    public class TunnelConfig
    {
        #region Properties

        public double CenterOffsetX { get; set; }
        public double CenterOffsetY { get; set; }
        /// <summary>Values: 0, 1 <para>0: Apply Cant</para><para>1: Apply Shift</para> </summary>
        public int ApplyCant { get; set; }
        public bool IgnoreRotaition { get; set; }
        public double CantPercent { get; set; }

        public double LeftHandX { get; set; }
        public double LeftHandY { get; set; }

        public double RightHandX { get; set; }
        public double RightHandY { get; set; }

        /// <summary>Values: 0, 1<para>0: Horizontally</para><para>1: Perpendicular to cant</para></summary>
        public int MeasureX { get; set; }
        /// <summary>Values: 0, 1<para>0: Vertically</para><para>1: Perpendicular to cant</para></summary>
        public int MeasureY { get; set; }

        /// <summary>Values: 0, 1, 2, 3<para>0: Low Rail</para><para>1: String</para><para>2: Left Hand Rail</para><para>3: Right Hand Rail</para></summary>
        public int YFrom { get; set; }
        public int YVert { get; set; }

        public double CarrriageDim1 { get; set; }
        public double CarrriageDim2 { get; set; }
        public double CarrriageDim3 { get; set; }

        #endregion

        #region Setup

        public TunnelConfig()
        {
            CantPercent = 100;
        }

        #endregion

        #region Methods

        public bool Equals(TunnelConfig config)
        {
            if (CenterOffsetX == config.CenterOffsetX && CenterOffsetY == config.CenterOffsetY && ApplyCant == config.ApplyCant && IgnoreRotaition == config.IgnoreRotaition && CantPercent == config.CantPercent && LeftHandX == config.LeftHandX && LeftHandY == config.LeftHandY && RightHandX == config.RightHandX && RightHandY == config.RightHandY && MeasureX == config.MeasureX && MeasureY == config.MeasureY && YVert == config.YVert && YFrom == config.YFrom)

            {
                //got bored!
                if (CarrriageDim1 == config.CarrriageDim1 && CarrriageDim2 == config.CarrriageDim2 && CarrriageDim3 == config.CarrriageDim3)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }

    public class ShapeElement
    {
        #region Properties

        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double Radius { get; set; }
        public Int32 Layer;

        public string FileString()
        {
            string str = "V=" + StartX.ToString() + "," + StartY.ToString() + "," + EndX.ToString() + "," + EndY.ToString() + "," + Radius.ToString() + "," + Layer.ToString();
            return str;
        }

        public double Length
        {
            get
            {
                var dist = (double)((decimal)(Vector.FnDistance(StartX, StartY, EndX, EndY)));

                if (Radius != 0)
                {
                    if (Vector.FnEquals((double)dist, 0))
                        return (Radius * 2) * Math.PI;
                    else
                    {
                        var temp = dist / (2 * Radius);
                        if (MathsHelpers.Vector.FnEquals(temp, 1, 0.00001))
                            temp = 1;

                        var val = Math.Asin(1);
                        return Radius * (2 * Math.Asin(temp));
                    }
                }
                else
                    return dist;
            }
        }

        #endregion

        #region Methods

        public List<Point2D> ExplodeElement(double offsetX, double offsetY, double interval = 0)
        {
            var pointsList = new List<Point2D>();

            try
            {
                if (Radius != 0)
                {
                    if (interval != 0)
                    {
                        var segments = (int)Math.Max(1, Length / Math.Abs(interval));
                        pointsList.AddRange(Geometry.GenerateArc(StartX + offsetX, StartY + offsetY, EndX + offsetX, EndY + offsetY, Radius, segments));
                    }
                    else
                    {
                        var eleDist = Length;
                        interval = Length / 100;

                        if (interval < 0.1)
                            interval = 0.1;

                        else if (interval > 1.0)
                            interval = 1.0;

                        var segments = (int)Math.Max(1, eleDist / Math.Abs(interval));
                        pointsList.AddRange(Geometry.GenerateArc(StartX + offsetX, StartY + offsetY, EndX + offsetX, EndY + offsetY, Radius, segments));
                    }
                }
                else
                {
                    pointsList.Add(new Point2D(StartX + offsetX, StartY + offsetY));
                    pointsList.Add(new Point2D(EndX + offsetX, EndY + offsetY));
                }

                return pointsList;
            }
            catch
            {
                return pointsList;
            }
        }

        #endregion
    }

    public static class Rail
    {
        #region Properties

        public static List<Point2D> Coords = new List<Point2D>();

        #endregion

        #region Methods

        //Modified by WA 04.07.19 to add default rails in the event there is no rail file
        public static void ReadRailFile(string FileName)
        {
            Coords = new List<Point2D>();
            string[] lines;

            try
            {
                try
                {
                    if(File.Exists(FileName))
                    {
                        lines = File.ReadAllLines(FileName);
                    }
                    else
                    {
                        //Try to create default file
                        File.WriteAllText(FileName, Properties.Resources.Rail);
                        lines = File.ReadAllLines(FileName);
                    }
                }
                catch
                {
                    //Failed to read existing or create default file - most likely permissions issue. Generate rails from default resource
                    lines = Properties.Resources.Rail.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                }

                if (lines == null || lines.Length <= 0)
                    return;

                //Convert the lines to coords
                foreach (var str in lines)
                {
                    if (str != "")
                    {
                        string[] arr = str.Split(',');
                        if (arr.Length >= 2)
                        {
                            //This will skip lines where there isn't a valid x & y. Not sure if this is preferable over just failing to load the file at all.
                            if (double.TryParse(arr[0], out double x) && double.TryParse(arr[1], out double y))
                                Coords.Add(new Point2D(x, y));
                        }
                    }
                }
            }
            catch
            {

            }
        }

        #endregion
    }

    public static class Train
    {
        #region Properties

        public static List<Point2D> Coords = new List<Point2D>();

        #endregion

        #region Methods

        public static void ReadTrainFile(string FileName)
        {
            Coords = new List<Point2D>();
            string[] lines;

            try
            {
                try
                {
                    if(File.Exists(FileName))
                    {
                        lines = File.ReadAllLines(FileName);
                    }
                    else
                    {
                        //Try to create default file
                        File.WriteAllText(FileName, Properties.Resources.Train);
                        lines = File.ReadAllLines(FileName);
                    }
                }
                catch
                {
                    //Failed to read existing or create default file. Generate train from default resource
                    lines = Properties.Resources.Train.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                }

                if (lines == null || lines.Length <= 0)
                    return;

                //Convert the lines to coords
                foreach(var str in lines)
                {
                    if(str != "")
                    {
                        string[] arr = str.Split(',');
                        if(arr.Length >= 2)
                        {
                            //This will skip lines where there isn't a valid x & y.
                            if (double.TryParse(arr[0], out double x) && double.TryParse(arr[1], out double y))
                                Coords.Add(new Point2D(x, y));
                        }
                    }
                }
            }
            catch
            {

            }
        }

        #endregion
    }

    public enum ShapeType
    {
        Design,
        DynamicProfile
    }

    public class ShapeControl
    {
        #region Properties

        public string LayerName { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }
        public List<ShapeControlItem> ShapeItems { get; set; }
        public List<ShapeControlSegment> DesignShapeSegments { get; set; }
        public List<ShapeControlSegment> DynamicShapeSegments { get; set; }
        public Dictionary<string, Shape> Shapes { get; set; }

        #endregion

        #region Setup

        public ShapeControl()
        {
            LayerName = "";
            X = 0;
            Y = 0;
            Angle = 0.08727;
            ShapeItems = new List<ShapeControlItem>();
            Shapes = new Dictionary<string, Shape>();
        }

        /// <summary>
        /// Updates the list of available shapes and updates those currently used
        /// </summary>
        /// <param name="directoryPath">The path to the default shape directory</param>
        public void SetupShapes(string directoryPath, double gauge)
        {
            //First remove all shapes that no longer exits
            for(int i = 0; i < Shapes.Keys.Count; i++)
            {
                var key = Shapes.Keys.ElementAt(i);
                var shape = Shapes[key];

                if(!File.Exists(shape.FileName))
                {
                    Shapes.Remove(key);
                    i--;
                }
            }

            //Find all shapes and add them if they are new
            if(Directory.Exists(directoryPath))
            {
                var files = Directory.GetFiles(directoryPath);
                foreach(var file in files)
                {
                    var fileUpper = file.ToUpper();
                    if(fileUpper.EndsWith(".SHP") || fileUpper.EndsWith(".NRGSHP"))
                    {
                        var fileName = Path.GetFileName(file);
                        if (Shapes.Keys.Contains(file))
                            continue;

                        if (Shapes.ContainsKey(fileName))
                            continue;
                        else
                        {
                            var newShape = new Shape();
                            newShape.Gauge = gauge;
                            if(newShape.ReadShapeFile(file))
                                Shapes.Add(fileName, newShape);
                        }
                    }
                }
            }
        }

        public void SetupSegments()
        {
            var curSegment = new ShapeControlSegment();
            bool isStart = true;

            //Design Segments
            DesignShapeSegments = new List<ShapeControlSegment>();
            for(int i = 0; i < ShapeItems.Count; i++)
            {
                var shapeItem = ShapeItems[i];

                if(isStart)
                {
                    //Add the start of the segment
                    curSegment.StartCh = shapeItem.Chainage;
                    curSegment.StartShape = shapeItem.Design;
                    isStart = false;
                }
                else
                {
                    //Add the end of the segment
                    curSegment.EndCh = shapeItem.Chainage;
                    curSegment.EndShape = shapeItem.Design;
                    curSegment.Morph = shapeItem.MorphShape;

                    //Check next shape item to check if we should merge the segments
                    bool merge = true;
                    while(merge && i + 1 < ShapeItems.Count)
                    {
                        var nextItem = ShapeItems[i + 1];
                        if (nextItem.Design == curSegment.StartShape && nextItem.Design == curSegment.EndShape)
                        {
                            //Merge the next shape with this segment
                            curSegment.EndCh = nextItem.Chainage;
                            i++;
                            merge = true;
                        }
                        else
                            merge = false;
                    }

                    //If the shape is the same there is no need to morph
                    if (curSegment.StartShape == curSegment.EndShape)
                        curSegment.Morph = false;

                    DesignShapeSegments.Add(curSegment);
                    curSegment = new ShapeControlSegment()
                    {
                        StartCh = curSegment.EndCh,
                        StartShape = curSegment.EndShape
                    };
                }
            }

            if(curSegment.StartShape != "None")
            {
                curSegment.EndShape = curSegment.StartShape;
                curSegment.EndCh = 1E20;
                curSegment.Morph = false;
                DesignShapeSegments.Add(curSegment);
            }

            isStart = true;
            curSegment = new ShapeControlSegment();

            //Dynamic Segments
            DynamicShapeSegments = new List<ShapeControlSegment>();
            for(int i = 0; i < ShapeItems.Count; i++)
            {
                var shapeItem = ShapeItems[i];

                if(isStart)
                {
                    //Add the start of the segment
                    curSegment.StartCh = shapeItem.Chainage;
                    curSegment.StartShape = shapeItem.DynamicProfile;
                    isStart = false;
                }
                else
                {
                    //Add the ned of the segment
                    curSegment.EndCh = shapeItem.Chainage;
                    curSegment.EndShape = shapeItem.DynamicProfile;
                    curSegment.Morph = shapeItem.MorphShape;

                    //Check next shape item to check if we should merge the segments
                    bool merge = true;
                    while(merge && i + 1 < ShapeItems.Count)
                    {
                        var nextItem = ShapeItems[i + 1];
                        if (nextItem.DynamicProfile == curSegment.StartShape && nextItem.DynamicProfile == curSegment.EndShape)
                        {
                            //Merge the next shape with this segment
                            curSegment.EndCh = nextItem.Chainage;
                            i++;
                            merge = true;
                        }
                        else
                            merge = false;
                    }

                    //If the shape is the same there is no need to morph
                    if (curSegment.StartShape == curSegment.EndShape)
                        curSegment.Morph = false;

                    DynamicShapeSegments.Add(curSegment);
                    curSegment = new ShapeControlSegment()
                    {
                        StartCh = curSegment.EndCh,
                        StartShape = curSegment.EndShape
                    };
                }
            }

            if (curSegment.StartShape != "None")
            {
                curSegment.EndShape = curSegment.StartShape;
                curSegment.EndCh = 1E20;
                curSegment.Morph = false;
                DynamicShapeSegments.Add(curSegment);
            }
        }

        #endregion

        #region Methods

        public Shape GetShape(double chainage, Alignment alignment, double grade, double cant, double vAngle, ShapeType type)
        {
            try
            {
                if (!alignment.IsOnHzAlignment(chainage))
                    return null;

                if (type == ShapeType.Design && (DesignShapeSegments == null || DesignShapeSegments.Count <= 0))
                    return null;

                if (type == ShapeType.DynamicProfile && (DynamicShapeSegments == null || DynamicShapeSegments.Count <= 0))
                    return null;

                foreach (var segment in (type == ShapeType.Design) ? DesignShapeSegments : DynamicShapeSegments)
                {
                    double startCh = segment.StartCh, endCh = segment.EndCh;

                    if (startCh == 1E20)
                        startCh = alignment.Horizontal.MinChainage;

                    if (endCh == 1E20)
                        endCh = alignment.Horizontal.MaxChainage;

                    if (chainage >= startCh && chainage < endCh)
                    {
                        //If there is no shape we can't show anything
                        if (segment.StartShape == "None" && segment.EndShape == "None")
                            return null;
                        else if (segment.StartShape == "None")
                            return null;
                        else if (segment.EndShape == "None" || segment.StartShape == segment.EndShape || !segment.Morph)
                            return (Shapes.ContainsKey(segment.StartShape)) ? Shapes[segment.StartShape].GetTansformedShape(Shapes[segment.StartShape].config.YVert == 0 ? grade : 0, cant, vAngle) : null;
                        else
                        {
                            if (Shapes.ContainsKey(segment.StartShape) && Shapes.ContainsKey(segment.EndShape))
                            {
                                //There are two different shapes so interpolate and return the new shape
                                var temp = alignment.Vertical.GradeLevel(startCh, out double vAngleStart);
                                temp = alignment.Vertical.GradeLevel(endCh, out double vAngleEnd);
                                var startShape = Shapes[segment.StartShape].GetTansformedShape(0, cant, vAngle);
                                var endShape = Shapes[segment.EndShape].GetTansformedShape(0, cant, vAngle);

                                bool isPerpVA = false;
                                if (startShape.config.YVert != 0 || endShape.config.YVert != 0)
                                    isPerpVA = true;

                                var shape = Interpolation.InterpolateShape(X, Y, Angle, startShape, endShape, startCh, endCh, chainage).GetTansformedShape(isPerpVA ? 0 : grade, 0, 0);
                                shape.TransformRails(isPerpVA ? 0 : grade, cant);
                                return shape;
                            }
                            else
                                return null;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public Shape GetShapeFromSegment(ShapeControlSegment segment, double chainage, Alignment alignment, double grade, double cant, double vAngle)
        {
            try
            {
                if (!alignment.IsOnHzAlignment(chainage))
                    return null;

                double startCh = segment.StartCh, endCh = segment.EndCh;

                if (startCh == 1E20)
                    startCh = alignment.Horizontal.MinChainage;

                if (endCh == 1E20)
                    endCh = alignment.Horizontal.MaxChainage;

                if(chainage >= startCh && chainage <= endCh)
                {
                    //If there is no shape we can't show anything
                    if (segment.StartShape == "None" && segment.EndShape == "None")
                        return null;
                    else if (segment.StartShape == "None")
                        return null;
                    else if (segment.EndShape == "None" || segment.StartShape == segment.EndShape || !segment.Morph)
                        return (Shapes.ContainsKey(segment.StartShape)) ? Shapes[segment.StartShape].GetTansformedShape(grade, cant, vAngle) : null;
                    else
                    {
                        if(Shapes.ContainsKey(segment.StartShape) && Shapes.ContainsKey(segment.EndShape))
                        {
                            var startShape = Shapes[segment.StartShape].GetTansformedShape(0, cant, vAngle);
                            var endShape = Shapes[segment.EndShape].GetTansformedShape(0, cant, vAngle);

                            var shape = Interpolation.InterpolateShape(X, Y, Angle, startShape, endShape, startCh, endCh, chainage).GetTansformedShape(grade, 0, 0);
                            shape.TransformRails(grade, cant);
                            return shape;
                        }
                    }
                }                

                return null;
            }
            catch
            {
                return null;
            }
        }

        public string GetShapeString(double chainage, Alignment alignment)
        {
            try
            {
                if (!alignment.IsOnHzAlignment(chainage))
                    return null;

                foreach(var segment in DesignShapeSegments)
                {
                    double startCh = segment.StartCh, endCh = segment.EndCh;

                    if (startCh == 1E20)
                        startCh = alignment.Horizontal.MinChainage;

                    if (endCh == 1E20)
                        endCh = alignment.Horizontal.MaxChainage;

                    if(chainage >= startCh && chainage < endCh)
                    {
                        //If there is no shape we can't show anything
                        if (segment.StartShape == "None" && segment.EndShape == "None")
                            return null;
                        else if (segment.StartShape == "None")
                            return null;
                        else if (segment.EndShape == "None" || segment.StartShape == segment.EndShape || !segment.Morph)
                            return (Shapes.ContainsKey(segment.StartShape)) ? Path.GetFileNameWithoutExtension(Shapes[segment.StartShape].FileName) : null;
                        else
                        {
                            if(Shapes.ContainsKey(segment.StartShape) && Shapes.ContainsKey(segment.EndShape))
                            {
                                //There are two different shapes we interpolate the shapes
                                return Path.GetFileNameWithoutExtension(Shapes[segment.StartShape].FileName) + " - " + Path.GetFileNameWithoutExtension(Shapes[segment.EndShape].FileName);
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Exports the current shape control configuration to the shape control (.SHC) format
        /// </summary>
        /// <param name="filePath">The path to the file to be saved</param>
        /// <param name="layerName">The name of the shape control layer</param>
        /// <returns>Return true if the export is successful</returns>
        public bool Export(string filePath)
        {
            try
            {
                //Remove existing file if it exists
                if (File.Exists(filePath))
                    File.Delete(filePath);

                //Add the Header
                var fileText = ";Shape Control" + "\r\n";
                fileText += "layer name = " + LayerName + "\r\n";
                fileText += "x = " + X.ToString() + "\r\n";
                fileText += "y = " + Y.ToString() + "\r\n";
                fileText += "angle = " + Angle.ToString() + "\r\n";
                fileText += "\r\n";

                //Add the chainage lines
                foreach(var shapeItem in ShapeItems)
                {
                    string designText = "None", dynamicText = "None";

                    if(shapeItem.Design != null && Shapes.ContainsKey(shapeItem.Design))
                        designText = Shapes[shapeItem.Design].FileName;

                    if (shapeItem.DynamicProfile != null && Shapes.ContainsKey(shapeItem.DynamicProfile))
                        dynamicText = Shapes[shapeItem.DynamicProfile].FileName;

                    fileText += shapeItem.Chainage == 1E20 ? "STOP" : shapeItem.Chainage.ToString() + ", " + designText;
                    fileText += ", " + dynamicText + ", " + (shapeItem.MorphShape ? "1" : "0") + "\r\n";
                }

                File.WriteAllText(filePath, fileText);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Imports a provided shape control file
        /// </summary>
        /// <param name="filePath">The path to the shape control file</param>
        /// <returns>Returns true if the import is successful</returns>
        public bool Import(string filePath, double gauge)
        {
            try
            {
                //Reset previous
                LayerName = "";
                ShapeItems = new List<ShapeControlItem>();

                if (!File.Exists(filePath))
                    return false;

                var lines = File.ReadAllLines(filePath);
                if (lines.Length <= 0 || lines[0] != ";Shape Control")
                    return false;

                foreach (var line in lines)
                {
                    if(line.Contains("="))
                    {
                        //Header Stuff
                        string[] temp = new string[0];

                        //Formatting...
                        if (line.Contains(" = "))
                            temp = line.Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                        else
                            temp = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                        if (temp.Length < 2)
                            continue;

                        switch (temp[0].ToUpper())
                        {
                            case "LAYER NAME":
                                LayerName = temp[1];
                                break;
                            case "X":
                                if (double.TryParse(temp[1], out double x))
                                    X = x;
                                else
                                    X = 0;

                                break;
                            case "Y":
                                if (double.TryParse(temp[1], out double y))
                                    Y = y;
                                else
                                    Y = 0;

                                break;
                            case "ANGLE":
                                if (double.TryParse(temp[1], out double angle))
                                    Angle = angle;
                                else
                                    Angle = 0.08727;

                                break;
                        }
                    }
                    else if(line.Contains(","))
                    {
                        //Chainage Line
                        string[] temp = new string[0];

                        //Formatting...
                        if (line.Contains(", "))
                            temp = line.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                        else
                            temp = line.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                        if (temp.Length < 2)
                            continue;

                        var newShapeItem = new ShapeControlItem();

                        //Chainage
                        if (double.TryParse(temp[0], out double ch))
                            newShapeItem.Chainage = ch;
                        else
                            newShapeItem.Chainage = 1E20;

                        //Design Shape
                        if (File.Exists(temp[1]))
                        {
                            //Load the shape
                            newShapeItem.Design = Path.GetFileName(temp[1]);

                            //If it is a new shape add to Shapes
                            var fileName = Path.GetFileName(temp[1]);
                            if (!Shapes.ContainsKey(fileName))
                            {
                                //Its important that we check the shape has loaded correctly
                                var newShape = new Shape();
                                newShape.Gauge = gauge;
                                if (newShape.ReadShapeFile(temp[1]))
                                    Shapes.Add(fileName, newShape);
                            }
                        }
                        else if(Shapes.ContainsKey(temp[1]))
                        {
                            //If file doesn't exist it may already be a filename
                            newShapeItem.Design = temp[1];
                        }

                        //This is adding a reference so any changes we make to newShapeItem are applied to the one in the list also. 
                        ShapeItems.Add(newShapeItem);                        

                        if (temp.Length < 3)
                        {
                            //Set Morph to true as this was the behavoir with the original shape control
                            newShapeItem.MorphShape = true;
                            continue;
                        }

                        //Dynamic Profile
                        if(File.Exists(temp[2]))
                        {
                            //Load the Shape
                            newShapeItem.DynamicProfile = Path.GetFileName(temp[2]);

                            //If it is a new shape add to Shapes
                            var fileName = Path.GetFileName(temp[2]);
                            if (!Shapes.ContainsKey(fileName))
                            {
                                var newShape = new Shape();
                                newShape.Gauge = gauge;
                                if (newShape.ReadShapeFile(fileName))
                                    Shapes.Add(fileName, newShape);
                            }
                        }
                        else if(Shapes.ContainsKey(temp[1]))
                        {
                            //If file doesn't exist it may already be a filename
                            newShapeItem.Design = temp[1];
                        }

                        if (temp.Length < 4)
                        {
                            //Set Morph to true as this was the behavoir with the original shape control
                            newShapeItem.MorphShape = true;
                            continue;
                        }

                        //Morph Shape
                        if (temp[3] == "1")
                            newShapeItem.MorphShape = true;
                        else
                            newShapeItem.MorphShape = false;
                    }
                }

                //Sensible setting of the morph
                if(ShapeItems.Count >= 1)
                {
                    //Doesn't make sense to morph first shape. doesn't do anything anyway
                    ShapeItems[0].MorphShape = false;

                    for(int i = 0; i < ShapeItems.Count - 1; i++)
                    {
                        //Check if shapes are different and there is a chainage difference
                        var firstShapeItem = ShapeItems[i];
                        var secondShapeItem = ShapeItems[i + 1];

                        if(firstShapeItem.Design == secondShapeItem.Design || Vector.FnEquals(firstShapeItem.Chainage, secondShapeItem.Chainage, 0.001))
                        {
                            secondShapeItem.MorphShape = false;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    public class ShapeControlItem
    {
        #region Properties

        public double Chainage { get; set; }
        public string Design { get; set; }
        public string DynamicProfile { get; set; }
        public bool MorphShape { get; set; }

        #endregion

        #region Setup

        public ShapeControlItem()
        {
            Chainage = 1E20;
            Design = null;
            DynamicProfile = null;
            MorphShape = false;
        }

        #endregion
    }

    public class ShapeControlSegment
    {
        #region Properties

        public double StartCh { get; set; }
        public double EndCh { get; set; }
        public string StartShape { get; set; }
        public string EndShape { get; set; }
        public bool Morph { get; set; }

        #endregion

        #region Setup

        public ShapeControlSegment()
        {
            StartCh = 1E20;
            EndCh = 1E20;
            StartShape = "None";
            EndShape = "None";
        }

        #endregion
    }
}

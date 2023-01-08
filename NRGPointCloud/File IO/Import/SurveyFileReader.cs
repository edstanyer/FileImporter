using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NRG.Models;
using System.Windows.Forms;
using IniParser.Parser;
using System.ComponentModel;

namespace NRG.Import
{
    public static class SurveyFileReader
    {


		public static DTM ImportASC(List<string> FileNames, DTM withinModel = null)
		{
			if (FileNames == null || FileNames.Count == 0) { return null; }

			DTM DTMOut = new DTM();

			foreach (string FileName in FileNames)
			{
				StreamReader sr = new StreamReader(FileName);
				int count = 0;
				int nCols = 0; int nRows = 0;
				double OriginX = 0; double OriginY = 0;	 double cellSize = 0; string  noData = "";
				bool cx = false; bool cy = false;
				double startX = 0; double startY = 0;
				bool started = false;

				int CurCol = 0; int CurRow = 0;

				while (sr.EndOfStream == false)
				{
					count++;
					string txt = sr.ReadLine();
					string[] arr = txt.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
					
					if (arr.Count() > 0)
					{
						if (count < 7)
						{
							switch (arr[0].ToUpper())
							{
								case "NCOLS":
									int.TryParse(arr[1], out nCols);
									break;
								case "NROWS":
									int.TryParse(arr[1], out nRows);
									break;
								case "XLLCORNER":
									double.TryParse(arr[1], out OriginX);
									cx = false;
									break;
								case "YLLCORNER":
									double.TryParse(arr[1], out OriginY);
									cy = false;
									break;
								case "XLLCENTER":
									double.TryParse(arr[1], out OriginX);
									cx = true;
									break;
								case "YLLCENTER":
									double.TryParse(arr[1], out OriginY);
									cy = true;
									break;

								case "CELLSIZE":
									double.TryParse(arr[1], out cellSize);
									break;
								case "NODATA_VALUE":
									noData = arr[1];
									break;
							}
						}
						else
						{
							if (started == false)
							{
								if (cx == false)
									startX = OriginX;
								else
									startX = OriginX + cellSize / 2;

								if (cy == false)
									startY = OriginY + ((cellSize * nRows) - cellSize);
								else
									startY = OriginY + ((cellSize * nRows)-(cellSize/2));

								
								
								for (int l = 0; l < arr.Count(); l++)
								{
									if (CurCol >= nCols)
									{
										CurCol = 0; CurRow++;
									}
									double curX = startX + (cellSize * CurCol);
									double curY = startY - (cellSize * CurRow);
									if (arr[l] != noData)
									{
										DTMPoint pt = new DTMPoint();
										pt.X = curX;
										pt.Y = curY;
										if (double.TryParse(arr[l], out double curZ))

										{
                                            bool ok = true;
                                            if (curX > 300000)
                                            {
                                                Console.WriteLine("");
                                            }
                                            if (withinModel != null)
                                            {
                                                if (MathsHelpers.Intersection.FnInBox(withinModel.Bounds.Min.X, withinModel.Bounds.Min.Y, withinModel.Bounds.Max.X, withinModel.Bounds.Max.Y, pt.X, pt.Y) == false)
                                                {
                                                    CurCol++;
                                                    continue;
                                                }
                                                else 
                                                {
                                                    if (withinModel.Triangles != null && withinModel.Triangles.Count > 0)
                                                    {
                                                        Triangle tri = withinModel.FindTriangle(pt.X, pt.Y);
                                                        if (tri == null || tri.Surface== null || tri.Surface.Contour == false)
                                                        {
                                                            CurCol++;
                                                            continue;
                                                        }
                                                    }
                                                }
                                                    
                                            }
                                           
                                            pt.Z = curZ;
                                            DTMOut.Points.Add(pt);
                                            
										}
									}

									CurCol++;
								}
								
							}

						}
					}

				}
			}
			return DTMOut;
		}


        /// <summary>
        /// ImportNRGPDF - imports data from NRG's legacy PDF format (PSION Data File)
        /// </summary>
        /// <param name="FileName">Name of the file to import</param>
        /// <returns>A list of DTMPoints made up of occupied stations, reference stations, reference observations and detail observations  </returns>
        public static DTM ImportNRGPDF(string FileName = "")
        {
            if (File.Exists(FileName) == false)
            {
                return null;
            }

            BindingList<DTMPoint> Obs = new BindingList<DTMPoint>();

            string[] lines = System.IO.File.ReadAllLines(FileName); //suck in the whole file
            int justRead = 0; //Describes the last record - 0= observation, 1=occupied, 2=reference object
            string PtNum = "";
            string Lab = "";
            foreach (string str in lines)
            {
                if (str != "")
                {
                    string[] arr = str.Split(',');
                    int num = arr.GetUpperBound(0);
                    
                    if (num >= 5)
                    {
                        ParsePDFPointNumber(arr[0], out PtNum,out Lab); //parse out the insane concatenated PDF point number and label
                        if (arr[0].Length >=3 && arr[0].ToUpper().Substring(0, 3) == "PGM") //Everything prefixed with a PGM is either a occupied or reference station
                        {
                            if (justRead == 0 || justRead==1)
                            {
                                //occupied or reference station
                                double e = 0;
                                double n = 0;
                                double l = 0;
                                double instHt = 0;

                                if (double.TryParse(arr[1], out e))
                                {
                                    if (double.TryParse(arr[2], out n))
                                    {
                                        if (double.TryParse(arr[3], out l ))
                                        {
                                            if (double.TryParse(arr[4], out instHt))
                                            {
                                                PointType type;

                                                if (justRead == 0)
                                                    type = PointType.OccupiedStation;   //last record was either a detail shot or nothing, this must be a new occupied(probably!)
                                                else
                                                    type = PointType.ReferenceStation;  //last record was an occupied station, this must be a reference station

                                                var os = new StationPoint(type);
                                                os.PointLabel = Lab;
                                                os.PointID = PtNum;
                                                os.X = e;//store station coordinates
                                                os.Y = n;
                                                os.Z = l;
                                                os.ObjectHeight = instHt;//actully target height is reference station
                                                Obs.Add (os);
                                                if (justRead == 0)
                                                { justRead = 1; } //tell the next record it must be a reference station
                                                else
                                                { justRead = 2; }// tell the next record it must be a reference observation
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            double hcr = 0;
                            double vcr = 0;
                            double sd = 0;
                            double tgtht = 0;
                            if (double.TryParse(arr[1], out hcr))
                            {
                                if (double.TryParse(arr[2], out vcr))
                                {
                                    if (double.TryParse(arr[3], out sd))
                                    {
                                        if (double.TryParse(arr[4], out tgtht))
                                        {
                                            ObservationPoint ob;
                                            if (justRead == 2)
                                                ob = new ReferenceObservation();    //last record was a reference station, this must be a reference observation
                                            else
                                                ob = new ObservationPoint();    //last record was nowt special, just store as a detail shot

                                            if (hcr < 0) { hcr = hcr + 2 * Math.PI; }//check for negative hz angles and correct
                                            if (hcr > 2 * Math.PI) { hcr = hcr - 2 * Math.PI; } //also check for angles > 360 and correct
                                            if (vcr < 0) { vcr = vcr - 2 * Math.PI; }
                                            if (vcr > 2 * Math.PI) { vcr = vcr - 2 * Math.PI; }
                                            ob.PointLabel = Lab;
                                            ob.PointID = PtNum;
                                            ob.HCR = hcr;//store the obs
                                            ob.VCR = vcr;
                                            ob.SD = sd;
                                            ob.TargetHeight = tgtht;
                                            justRead = 0; //all future records will be standard detail shots until we come to another occupied station
                                            Obs.Add (ob);//add record to list
                                        }
                                    }
                                }
                            }
                            
                        }

                    }

                }

            }

            var dtm = new DTM();
            dtm.FilePath = FileName;
            dtm.Name = Path.GetFileName(FileName);
            dtm.Points = Obs;
            dtm.GenerateStationSetups();
            dtm.SaveRequired = true;
            return dtm;
        }

        /// <summary>
        /// ParsePDFPointNumber - splits the concatinated point number and label apart. This is both a horrible way
        /// of storing the data and a horrible way of splitting it.
        /// </summary>
        /// <param name="Input">input the first field from each row</param>
        /// <param name="PointNumber">passes back the point number if there is one</param>
        /// <param name="Label">passes back the label if it exists</param>
        private static void ParsePDFPointNumber(string Input, out string PointNumber, out string Label)
        {
            PointNumber = "";
            Label = "";
            if (Input.Length >= 3)
            {
                if (Input.ToUpper().Substring(0, 3) == "PGM")
                {   //it's either a occupied or reference station record - no point number, station name as label
                    PointNumber = "";
                    Label = Input.ToUpper().Substring(3);
                    return;//that'll do, bug out
                }
                
            }
            bool doingLabel = false; 
            //detail or reference observation - there's got to be a more elegant way than this, but my head is a bit mashed at the moment
            char[] arr = Input.ToArray();
            for (int i = 0; i <= arr.GetUpperBound(0); i++)
            {
                if (doingLabel == false)
                {
                    int a = 0;
                    if (int.TryParse(arr[i].ToString(), out a) == false)
                    {
                        Label = Label + arr[i].ToString();
                        doingLabel = true;
                    }
                    else
                    {
                        PointNumber = PointNumber + arr[i].ToString();
                    }
                }
                else
                {
                    Label = Label + arr[i].ToString();
                }
            }
        }


        
        
        /// <summary>
        /// Generates a <see cref="DTM"/> object based on a given input GPF file.
        /// </summary>
        /// <param name="fileName">The full path of the GPF file to import</param>
        /// <returns>Returns a <see cref="DTM"/> object representing the GPF file. Returns null if the file is invalid"/></returns>
        public static DTM ImportNRGGPF(string fileName)
        {
            try
            {
                var dtm = new DTM();

                if(File.Exists(fileName))
                {
                    dtm.FilePath = fileName;
                    dtm.Name = Path.GetFileName(fileName);

                    using (var reader = new StreamReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)))
                    {
                        //Check that the file is a valid Ground Plot File
                        var line = reader.ReadLine();
                        if(line != null && line == ";NRG GROUND PLOT FILE VERSION 2")
                        {
                            try
                            {
                                double x = 0, y = 0, z = 0;
                                while((line = reader.ReadLine()) != null)
                                {
                                    //Check there is a valid number of parts
                                    var parts = line.Split(',');
                                    if(parts.Length >= 5)
                                    {
                                        var point = new DTMPoint();
                                        point.PointID = parts[0].Replace("\"", "");
                                        point.PointLabel = parts[1].Replace("\"", "");

                                        //Check X value is valid
                                        if (double.TryParse(parts[2], out x))
                                            point.X = x;

                                        //Check Y value is valid
                                        if (double.TryParse(parts[3], out y))
                                            point.Y = y;

                                        if (double.TryParse(parts[4], out z))
                                            point.Z = z;

                                        var notesCount = parts.Length - 5;
                                        if(notesCount > 0)
                                        {
                                            point.Notes = new Dictionary<int, string>();
                                            for (int i = 5; i < parts.Length; i++)
                                                point.Notes.Add(i - 5, parts[i].Replace("\"", ""));
                                        }

                                        dtm.Points.Add(point);
                                    }
                                }
                            }
                            catch
                            {

                            }
                        }
                    }

                    //Attempt to import the surfaces if they exist

                    var fileNameWithout = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);

                    ImportPRSFile(fileNameWithout + ".prs", ref dtm);

                    //Import the triangle file
                    if(File.Exists(fileNameWithout + ".tri"))
                    {
                        using (var reader = new StreamReader(File.Open(fileNameWithout + ".tri", FileMode.Open, FileAccess.Read, FileShare.Read)))
                        {
                            //Read the first line since its unimportant
                            var line = reader.ReadLine();

                            while((line = reader.ReadLine()) != null)
                            {
                                //Split the line by ',' and get the ID of each triange point to generate a triangle
                                var tmp = line.Split(',');

                                //If there are not atleast 7 parts then its not a valid triangle
                                if (tmp.Length < 7)
                                    continue;

                                if(int.TryParse(tmp[0], out int index1) && int.TryParse(tmp[1], out int index2) && int.TryParse(tmp[2], out int index3))
                                {
                                    //Get the Points
                                    index1--;
                                    index2--;
                                    index3--;

                                    //Make sure the index are valid
                                    if (index1 < 0 || index1 >= dtm.Points.Count || index2 < 0 || index2 >= dtm.Points.Count || index3 < 0  || index3 >= dtm.Points.Count)
                                        continue;

                                    var point1 = dtm.Points[index1];
                                    var point2 = dtm.Points[index2];
                                    var point3 = dtm.Points[index3];

                                    //Get the surface
                                    
                                    var surfaceTmp = tmp[6].Split('.');


                                    var surface = dtm.GetSurface(0);

                                    if(surfaceTmp.Length == 1)
                                    {
                                        if(int.TryParse(surfaceTmp[0], out int surfaceID))
                                        {
                                            if(surfaceID == -1)
                                            {
                                                //Take surface 1
                                                surface = dtm.GetSurface(1);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //Length must be greater than 2, this should have a value indicating the surface on the 2nd item
                                        if(double.TryParse(tmp[6], out double surfaceDecimal))
                                        {
                                            //Get the surface from the list of available
                                            var surfaceID = (int)Math.Abs(Math.Round(((surfaceDecimal + 1) * 1000)));
                                            surface = dtm.GetSurface(surfaceID);
                                        }
                                    }

                                    //Now we have points and a surface create the triangle
                                    var triangle = new Triangle(surface, point1, point2, point3);
                                    dtm.Triangles.Add(triangle);
                                }
                            }
                        }
                    }
                }

                return dtm;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Imports triangle surfaces from a PRS file. Will import layers and other stuff at some point
        /// </summary>
        /// <param name="fileName">The file to import surfaces from</param>
        /// <returns>Returns a list of <see cref="TriangleSurface"/></returns>
        public static void ImportPRSFile(string fileName, ref DTM dtm)
        {
            try
            {
                var surfaces = new List<TriangleSurface>();

                if(File.Exists(fileName))
                {
                    using (var reader = new StreamReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)))
                    {
                        //Check that the file is a valid Presentation File
                        var line = reader.ReadLine();
                        if(line != null && line == ";Presentation File - FORMAT V1.0")
                        {
                            while((line = reader.ReadLine()) != null)
                            {
                                //Check if the line is the start of the surfaces
                                if(line == "[Surfaces]")
                                {
                                    //Start reading the surface
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        //End the process if we've moved to the next section
                                        if(line.StartsWith("[") && line.EndsWith("]"))
                                            break;

                                        //Line must be a surface
                                        if (!line.Contains(","))
                                            continue;

                                        var args = line.Split(',');

                                        //If there are not enough params skip the surface item
                                        if (args.Length < 8)
                                            continue;

                                        var surface = new TriangleSurface();

                                        //ID
                                        if (int.TryParse(args[0], out int id))
                                        {
                                            surface.ID = id;
                                        }
                                        else
                                        {
                                            //Invalid ID so skip
                                            continue;
                                        }

                                        //Color
                                        if(long.TryParse(args[1], out long col))
                                        {
                                            int r = 0, g = 0, b = 0;
                                            NRG.Services.Conversions.LongIntToRGB(col, ref r, ref g, ref b);
                                            surface.R = r; surface.G = g; surface.B = b;
                                        }

                                        //Volume
                                        surface.Volume = (args[3] == "-1");

                                        //DIP
                                        if (double.TryParse(args[4], out double dip))
                                            surface.Dip = dip;
                                        else
                                            surface.Dip = 0;

                                        //Name
                                        surface.Name = args[5];

                                        //Fill Type
                                        if(int.TryParse(args[6], out int fillType))
                                        {
                                            if (fillType >= -1 && fillType <= 5)
                                                surface.FillType = (FillType)(fillType + 2);
                                            else
                                                surface.FillType = FillType.SOLIDFILL;
                                        }
                                        else
                                        {
                                            surface.FillType = FillType.SOLIDFILL;
                                        }

                                        //Contour
                                        surface.Contour = (args[7] == "-1");

                                        //Try to add the surface in the correct place in the list
                                        while (surfaces.Count <= surface.ID)
                                            surfaces.Add(new TriangleSurface());

                                        surfaces[surface.ID] = surface;
                                    }

                                    //Surfaces should have been read by now so return
                                    dtm.Surfaces = surfaces;
                                }
                                else if(line == "[Features]")
                                {
                                    //Read the next line
                                    if((line = reader.ReadLine()) != null && line.StartsWith("Features File="))
                                    {
                                        var tmp = line.Split('=');
                                        if(tmp.Length > 1)
                                        {
                                            var filePath = tmp[1];
                                            if(File.Exists(filePath))
                                            {
                                                //Get the features file and read it
                                                dtm.SetupFeaturesFromDictionary(new Dictionary<char, List<Feature>>(), filePath);

                                                var data = new IniDataParser().Parse(Helpers.GetSaneIniFile(filePath));

                                                var pointFeatures = new List<PointFeature>();
                                                var lineFeatures = new List<LineFeature>();
                                                
                                                if(data.Sections.ContainsSection("Points"))
                                                {
                                                    foreach(var feature in data.Sections["Points"])
                                                    {
                                                        pointFeatures.Add(new PointFeature(feature.KeyName, feature.Value.Split(',')));
                                                    }
                                                }

                                                if(data.Sections.ContainsSection("Lines"))
                                                {
                                                    foreach(var feature in data.Sections["Lines"])
                                                    {
                                                        lineFeatures.Add(new LineFeature(feature.KeyName, feature.Value.Split(',')));
                                                    }
                                                }

                                                if(data.Sections.ContainsSection("Break Lines"))
                                                {
                                                    foreach(var feature in data.Sections["Break Lines"])
                                                    {
                                                        lineFeatures.Add(new LineFeature(feature.KeyName, feature.Value.Split(','), true));
                                                    }
                                                }

                                                //Add point features to the dictionary
                                                foreach(var feature in pointFeatures)
                                                {
                                                    var firstChar = feature.FieldCode.ToUpper()[0];

                                                    //Add the feature to the dictionary
                                                    if (dtm.FeaturesDictionary.ContainsKey(firstChar))
                                                        dtm.FeaturesDictionary[firstChar].Add(feature);
                                                    else
                                                        dtm.FeaturesDictionary.Add(firstChar, new List<Feature> { feature });
                                                }

                                                //Add Line features to the dictionary
                                                foreach(var feature in lineFeatures)
                                                {
                                                    var firstChar = feature.FieldCode.ToUpper()[0];

                                                    //Add the feature to the dictionary
                                                    if (dtm.FeaturesDictionary.ContainsKey(firstChar))
                                                        dtm.FeaturesDictionary[firstChar].Add(feature);
                                                    else
                                                        dtm.FeaturesDictionary.Add(firstChar, new List<Feature> { feature });
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch
            {

            }
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using NRG.Services;

namespace NRG.Export
{
    public static class DTMExports
    {
        public static bool ExportDtmToGPF(DTM model, string filePathWithoutExtension)
        {
            string gpfPath = "", prsPath = "", triPath = "";
           
            try
            {
                if (model == null || filePathWithoutExtension == null)
                    return false;

                gpfPath = filePathWithoutExtension + ".gpf";
                prsPath = filePathWithoutExtension + ".prs";
                triPath = filePathWithoutExtension + ".tri";

                //Write the GPF File
                using (var writer = new StreamWriter(File.Create(gpfPath + "tmp")))
                {
                    //Add the header tex
                    writer.WriteLine(";NRG GROUND PLOT FILE VERSION 2");

                    foreach(var point in model.Points)
                    {
                        var line = "";

                        //Point ID
                        if (point.PointID != null && point.PointID != "")
                            line += "\"" + point.PointID + "\"";

                        line += ",";

                        //Point Label
                        if (point.PointLabel != null && point.PointLabel != "")
                            line += "\"" + point.PointLabel + "\"";

                        line += ",";

                        //X
                        line += point.X + ",";

                        //Y
                        line += point.Y + ",";

                        //Z
                        line += point.Z;

                        //Notes
                        if(point.Notes != null)
                        {
                            foreach(var note in point.Notes.Values)
                            {
                                line += "," + "\"" + note + "\"";
                            }
                        }

                        writer.WriteLine(line);
                        //writer.Close();
                    }
					writer.Close();
				}

                //Write the PRS File
                using (var writer = new StreamWriter(File.Create(prsPath + "tmp")))
                {
                    //Write the header
                    writer.WriteLine(";Presentation File - FORMAT V1.0");

                    if(model.Surfaces != null && model.Surfaces.Count > 0)
                    {
                        //Write the surface header
                        writer.WriteLine("[Surfaces]");

                        //Write each surface
                        for(int i = 0; i < model.Surfaces.Count; i++)
                        {
                            var surface = model.Surfaces[i];
                            var line = surface.ID + "," + Conversions.RGBtoInt(surface.R, surface.G, surface.B) + ",255," + (surface.Volume ? "-1" : "0") + "," + surface.Dip + "," + surface.Name + ",";
                            if (surface.FillType == FillType.SOLIDFILL || surface.FillType == FillType.NONE)
                                line += 0;
                            else
                                line += ((int)surface.FillType - 2);

                            line += "," + (surface.Contour ? "-1" : "0");

                            writer.WriteLine(line);
                            
                        }
                    }

                    if(model.FeaturesPath != null && File.Exists(model.FeaturesPath))
                    {
                        //Write the features header
                        writer.WriteLine("[Features]");

                        //Write the features path
                        writer.WriteLine("Features File=" + model.FeaturesPath);
                    }
                    writer.Close();
                }

                if(model.Triangles != null && model.Triangles.Count > 0)
                {
                    //Write the TRI File
                    using (var writer = new StreamWriter(File.Create(triPath + "tmp")))
                    {
                        //Write the header
                        writer.WriteLine(0 + " " + model.Triangles.Count);

                        //Generate IDs
                        DTMHelpers.RenumberPointIDs(model.Points.ToList());
                        int index = 0;
                        foreach(var tri in model.Triangles)
                        {
                            tri.ID = index;
                            index++;
                        }

                        //Write each triangle
                        foreach(var tri in model.Triangles)
                        {
                            if(tri.Point1 != null && tri.Point2 != null && tri.Point3 != null)
                            {
                                var line = "";

                                line += (tri.Point1.ID + 1);
                                line += "," + (tri.Point2.ID + 1);
                                line += "," + (tri.Point3.ID + 1);

                                //line += (model.Points.IndexOf(tri.Point1) + 1);
                                //line += "," + (model.Points.IndexOf(tri.Point2) + 1);
                                //line += "," + (model.Points.IndexOf(tri.Point3) + 1);

                                Triangle edge12 = null, edge23 = null, edge31 = null;

                                //Find triangle adjacent to side 1-2 and 3-1
                                foreach(var subTri in tri.Point1.Triangles)
                                {
                                    if (tri == subTri)
                                        continue;

                                    if (subTri.Point1 == tri.Point2 || subTri.Point2 == tri.Point2 || subTri.Point3 == tri.Point2)
                                        edge12 = subTri;

                                    if (subTri.Point1 == tri.Point3 || subTri.Point2 == tri.Point3 || subTri.Point3 == tri.Point3)
                                        edge31 = subTri;

                                    if (edge12 != null && edge31 != null)
                                        break;
                                }

                                //Add the index of edge 1-2
                                line += "," + ((edge12 == null) ? 0 : (edge12.ID + 1));

                                //Find triangle adjacent to side 2-3
                                foreach(var subTri in tri.Point2.Triangles)
                                {
                                    if (tri == subTri)
                                        continue;

                                    if(subTri.Point1 == tri.Point3 || subTri.Point2 == tri.Point3 || subTri.Point3 == tri.Point3)
                                    {
                                        edge23 = subTri;
                                        break;
                                    }
                                }

                                //Add the index of edge 2-3
                                line += "," + ((edge23 == null) ? 0 : (edge23.ID + 1));

                                //Add the index of edge 3-1
                                line += "," + ((edge31 == null) ? 0 : (edge31.ID + 1));

                                if(tri.Surface == null || tri.Surface.ID == 0)
                                {
                                    line += "," + 0;
                                }
                                else
                                {
                                    //This logic won't work if the surface id is greater than 999
                                    line += "," + "-1." + (tri.Surface.ID + 1000).ToString().Substring(1);
                                }

                                writer.WriteLine(line);
                            }
                            else
                            {

                            }
                        }
                        writer.Close();
                    }
                }

                //Export is complete

                if(File.Exists(gpfPath + "tmp"))
                {
                    //If the original gpf exists delete it
                    if (File.Exists(gpfPath))
                        File.Delete(gpfPath);

                    //Move the tmp gpf file to the original location
                    File.Move(gpfPath + "tmp", gpfPath);

                    //If the original prs file exits delete it
                    if (File.Exists(prsPath))
                        File.Delete(prsPath);

                    //Move the tmp prs file to the original location
                    if (File.Exists(prsPath + "tmp"))
                        File.Move(prsPath + "tmp", prsPath);

                    //If the original tri file exists delete it
                    if (File.Exists(triPath))
                        File.Delete(triPath);

                    //Move the tmp tri file to the original location
                    if (File.Exists(triPath + "tmp"))
                        File.Move(triPath + "tmp", triPath);
                }
                else
                {
                    //We have failed to save the gpf somehow so clean up and return false
                    if (File.Exists(gpfPath + "tmp"))
                        File.Delete(gpfPath + "tmp");

                    if (File.Exists(prsPath + "tmp"))
                        File.Delete(prsPath + "tmp");

                    if (File.Exists(triPath + "tmp"))
                        File.Delete(triPath + "tmp");
                }

                return true;
            }
            catch
            {
                //Crash during save so clean up the files
                if (File.Exists(gpfPath + "tmp"))
                    File.Delete(gpfPath + "tmp");

                if (File.Exists(prsPath + "tmp"))
                    File.Delete(prsPath + "tmp");

                if (File.Exists(triPath + "tmp"))
                    File.Delete(triPath + "tmp");
                    
                return false;                        
            }
        }
    }
}

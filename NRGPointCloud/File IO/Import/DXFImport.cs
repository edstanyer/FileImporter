
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netDxf;
using netDxf.Entities;
using System.IO;
using NRG.Models;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace NRG.Import
{
	public class DXFImport
	{

		#region properties

		public netDxf.DxfDocument DXFFile = null;
		public string DXFFileName;
		List<netDxf.Entities.Line> Lines = new List<netDxf.Entities.Line>();
		List<netDxf.Entities.Polyline> PolyLines = new List<netDxf.Entities.Polyline>();
		List<netDxf.Entities.Point> Points = new List<netDxf.Entities.Point>();
		List<netDxf.Entities.LwPolyline> LWPolyLines = new List<netDxf.Entities.LwPolyline>();
		List<netDxf.Entities.Arc> Arcs = new List<netDxf.Entities.Arc>();
		List<netDxf.Entities.Spline> Splines = new List<netDxf.Entities.Spline>();
		List<netDxf.Entities.Circle> Circles = new List<netDxf.Entities.Circle>();
		List<netDxf.Entities.Insert> BlockInserts = new List<netDxf.Entities.Insert>();
		List<netDxf.Entities.PolyfaceMesh> Meshes = new List<netDxf.Entities.PolyfaceMesh>();
		List<netDxf.Entities.Hatch> Hatches = new List<netDxf.Entities.Hatch>();
		List<netDxf.Blocks.Block> BlockDefs = new List<netDxf.Blocks.Block>();
		List<netDxf.Tables.Layer> Layers = new List<netDxf.Tables.Layer>();

		public double coorddiv = 1;
		#endregion properties

		public DXFImport(string FileName)
		{
			try
			{
				if (File.Exists(FileName))
				{
					DXFFileName = FileName;
					DXFFile = new DxfDocument();
					DXFFile = netDxf.DxfDocument.Load(FileName);



					if (DXFFile != null)
					{



						foreach (netDxf.Tables.UCS ucs in DXFFile.UCSs)
						{
							DXFFile.UCSs.Remove(ucs.ToString()); //no idea if this does anything, but was done as a test and still remains for better or worse
						}




						if (DXFFile.DrawingVariables.InsUnits == netDxf.Units.DrawingUnits.Millimeters)
						{
							coorddiv = 1;// 0.001; //none of this makes much sense to me either

						}

						//====================================================================================================================
						//Split the elements of the files into the bits we can import - probably a bit of a waste of time, but for neatness...
						foreach (netDxf.Entities.Line ln in DXFFile.Lines) { Lines.Add(ln); }

						foreach (netDxf.Entities.Polyline pl in DXFFile.Polylines) { PolyLines.Add(pl); }

						foreach (netDxf.Entities.LwPolyline pl in DXFFile.LwPolylines) { LWPolyLines.Add(pl); }

						foreach (netDxf.Entities.Point pt in DXFFile.Points) { Points.Add(pt); }

						foreach (netDxf.Entities.Arc arc in DXFFile.Arcs) { Arcs.Add(arc); }

						foreach (netDxf.Entities.Spline sp in DXFFile.Splines) { Splines.Add(sp); }

						foreach (netDxf.Entities.Circle circ in DXFFile.Circles) { Circles.Add(circ); }

						foreach (netDxf.Entities.Insert blk in DXFFile.Inserts) { BlockInserts.Add(blk); }

						foreach (netDxf.Entities.PolyfaceMesh msh in DXFFile.PolyfaceMeshes) { Meshes.Add(msh); }

						foreach (netDxf.Entities.Hatch htch in DXFFile.Hatches) { Hatches.Add(htch); }

						foreach (netDxf.Tables.Layer layer in DXFFile.Layers) { Layers.Add(layer); }

						//foreach(netDxf.Blocks.Block blockDef in  DXFFile.Blocks) { BlockDefs.Add(blockDef); } We currently only import blocks being used (BlockInserts)
						//=====================================================================================================================


					}
					else
					{
						//https://github.com/haplokuon/netDxf
						//"The library will never be able to read some entities like REGIONs, SURFACEs, and 3DSOLIDs, since they depend on undocumented proprietary data."
					}
				}
			}
			catch (netDxf.IO.DxfVersionNotSupportedException ex)

			{
				MessageBox.Show(ex.Message);
			}
		}

		#region DXF Helpers
		private string GetColourString(AciColor Colour)
		{

			System.Drawing.Color col = new System.Drawing.Color();
			//col = Colour.ToColor();
			//int col2 = Colour.ToColor().ToArgb();
			System.Drawing.ColorConverter conv = new System.Drawing.ColorConverter();

			col = Colour.ToColor();

			return "{" + col.ToString().Replace(" ", "") + "}";
		}
		#endregion DXF Helpers


		#region DXF Shape
		public void ExtractShape()
		{
			List<ShapeVector> shapeVectors = new List<ShapeVector>();
			foreach (netDxf.Entities.Line ln in Lines)
			{
				shapeVectors.Add(processLineForShape(ln));
			}

			foreach (netDxf.Entities.Arc ar in Arcs)
			{
				shapeVectors.Add(processArcForShape(ar));
			}
		}

		private ShapeVector processLineForShape(netDxf.Entities.Line line)
		{
			ShapeVector outvec = new ShapeVector();

			outvec.StartPoint.X = line.StartPoint.X;
			outvec.StartPoint.Y = line.StartPoint.Y;
			outvec.EndPoint.X = line.EndPoint.X;
			outvec.EndPoint.Y = line.EndPoint.Y;
			return outvec;
		}


		private ShapeVector processArcForShape(netDxf.Entities.Arc arc)
		{
			return new ShapeVector(MathsHelpers.Trig.PRC(arc.Center.X, arc.Center.Y, MathsHelpers.Trig.FNBearingToAngle(arc.StartAngle), arc.Radius),
								   MathsHelpers.Trig.PRC(arc.Center.X, arc.Center.Y, MathsHelpers.Trig.FNBearingToAngle(arc.EndAngle), arc.Radius),
								   arc.Radius);
		}

		//private List<ShapeVector> processPolylineForShape(netDxf.Entities.Polyline)
		//{
		//	return null;
		//}

		#endregion DXF Shape


		#region DXF DTM
		public DTM ExtractDTM(bool defaultModelImport = true) //defaultModelImport true uses the old way of extracting arcs (making hundreds of points out of them)													  //set to false to get Arcs in our new and improved™ NRG.Models.Arc format
		{
			DTM Model = new DTM();

			#region Points
			//Unlike NRG we don't see many CAD drawings with points
			if (Points != null && Points.Count > 0)
			{

				//Points -|- 
				//      X Y Z
				foreach (netDxf.Entities.Point pt in Points)
				{
					DTMPoint dp = new DTMPoint();
					dp.X = pt.Position.X * coorddiv;
					dp.Y = pt.Position.Y * coorddiv;
					dp.Z = pt.Position.Z * coorddiv;
					//dp.ManualLayers = new HashSet<string>(); //We do this here so we don't end up with a layer of "Default Points"
					dp.AddALayer(Model, pt.Layer.Name);
					Model.Points.Add(dp);

				}
			}
			#endregion

			#region Layers
			foreach (netDxf.Tables.Layer dxflayer in Layers)
			{
				//Name
				DrawingLayer newLayer = new DrawingLayer();
				newLayer.Name = dxflayer.Name;
				newLayer.Name = newLayer.Name.Replace(" ", null);
				//Lineweight
				int lineweight = LineweightConverter(dxflayer.Lineweight);
				if (lineweight < 1) { lineweight = 1; } //LineweightConverter returns "0" if the lineweight is ByBlock or ByLayer so we just set it to 1 as a default here
				newLayer.Lineweight = lineweight;
				newLayer.Draw = dxflayer.IsVisible;

				//Colour
				newLayer.Colour = dxflayer.Color.ToColor();

				Model.AddLayer(newLayer);
			}
			#endregion

			#region line

			//Line -|------------------|- 
			//    X Y Z(start)	     X Y Z(end)
			int i = -1;
			foreach (netDxf.Entities.Line ln in Lines)
			{

				string layer = "Default";
				if (!string.IsNullOrWhiteSpace(ln.Layer.ToString()))
				{
					layer = ln.Layer.ToString();
				}

				i++; //loop counter - bound to need this
				StandardLine modelLine = ProcessCADLine(ln);
				DTMPoint sp = new DTMPoint();//start point
				DTMPoint ep = new DTMPoint();//end point

				int lineweight = LineweightConverter(ln.Lineweight);
				if (lineweight < 1)
				{
					modelLine.LwByLayer = true;
					modelLine.Lineweight = 1;
				}
				else
				{
					modelLine.Lineweight = lineweight;
				}

				string entityName = ln.CodeName + ":" + ln.Handle;
				modelLine.StartPoint = Model.MatchPointFromPointsDictionary(ln.StartPoint.X, ln.StartPoint.Y, ln.StartPoint.Z, true, layer, false);
				modelLine.StartPoint.OriginalEntityNames.Add(entityName);

				modelLine.EndPoint = Model.MatchPointFromPointsDictionary(ln.EndPoint.X, ln.EndPoint.Y, ln.EndPoint.Z, true, layer, false);
				modelLine.EndPoint.OriginalEntityNames.Add(entityName);

				modelLine.OriginalEntityName = entityName;
				Model.AddStandardLine(modelLine, layer);

			}
			#endregion

			#region Polyline
			//PolyLine -|-----------------|------------------|------------------|- Made up of 2 or more points - maybe closed back to first point - also maybe 2D or 3D
			//        X Y Z(start)		X Y Z			   X Y Z			  X Y Z

			foreach (netDxf.Entities.Polyline pl in PolyLines)
			{
				i = -1;


				PolyLine poly = new PolyLine();
				//Model.PolyLines.Add(poly);
				poly.Colour = Color.FromArgb(1, pl.Color.R, pl.Color.G, pl.Color.B);




				string layer = "Default";
				if (!string.IsNullOrWhiteSpace(pl.Layer.ToString()))
				{
					layer = pl.Layer.ToString();
				}

				#region Colour stuff
				if (pl.Color.IsByLayer)
				{
					if (pl.Layer.Color.Equals(AciColor.Default))
					{
						poly.Colour = Color.Black;
					}
					else
					{
						poly.Colour = Color.FromArgb(1, pl.Layer.Color.R, pl.Layer.Color.G, pl.Layer.Color.B);
					}
				}
				else
				{
					if (pl.Color.Equals(AciColor.Default))
					{
						poly.Colour = Color.Black;
					}
					else
					{
						poly.Colour = Color.FromArgb(1, pl.Color.R, pl.Color.G, pl.Color.B);
					}
				}
				#endregion

				string entityName = pl.CodeName + ":" + pl.Handle;
				foreach (netDxf.Entities.PolylineVertex pv in pl.Vertexes)
				{

					DTMPoint pp = new DTMPoint();
					i++;



					//Vector3 v = MathHelper.Transform(new Vector3(pv.Position.X, pv.Position.Y, pp.Z), pl.Normal, MathHelper.CoordinateSystem.Object, MathHelper.CoordinateSystem.World);
					//Commented out by TN 01.06.22, wasn't being used for anything

					pp.X = pv.Position.X; //* coorddiv;
					pp.Y = pv.Position.Y; // * coorddiv;
					pp.Z = pv.Position.Z; // * coorddiv;


					//Model.Points.Add(pp);

					DTMPoint ip = Model.MatchPointFromPointsDictionary(pp.X, pp.Y, pp.Z, true, layer, false);
					ip.OriginalEntityNames.Add(entityName);

					if (ip.Notes == null)
						ip.Notes = new Dictionary<int, string>();

					string code = pl.Linetype.Name;

					if (string.IsNullOrWhiteSpace(code)) { continue; }

					if (ip.Notes.ContainsKey(1))
					{
						ip.Notes[1] += " " + code;
					}
					else
					{
						ip.Notes.Add(1, code);
					}

					poly.Nodes.Add(ip);

				}
				poly.IsClosed = pl.IsClosed;


				int lineweight = LineweightConverter(pl.Lineweight);
				if (lineweight < 1)
				{
					poly.LwByLayer = true;
					poly.Lineweight = 1;
				}
				else
				{
					poly.Lineweight = lineweight;
				}

				if (poly != null && poly.Nodes.Count > 0)
				{
					poly.OriginalEntityName = entityName;
					Model.AddPolyline(poly, layer);
				}

			}
			#endregion

			#region LWPolyline
			foreach (netDxf.Entities.LwPolyline LWPoly in LWPolyLines)
			{

				netDxf.Entities.LwPolyline pln = (netDxf.Entities.LwPolyline)LWPoly;
				PolyLine Poly = new PolyLine();

				ExplodedPolyline explodedPoly = ProcessLWPolyline(LWPoly);

				foreach (var nrgArc in explodedPoly.ArcList)
                {
					//====== new inhouse arc object creation
					//arcs should not match centre points DTMPoint cp = Model.MatchPointFromPointsDictionary(nrgArc.CentrePoint.X, nrgArc.CentrePoint.Y, nrgArc.CentrePoint.Z, true, nrgArc.Layer, false);
					Model.Points.Add(nrgArc.StartPoint);
					Model.Points.Add(nrgArc.EndPoint);
					Model.AddArcLine(nrgArc, nrgArc.Layer);
					//=======
				}


				foreach (var line in explodedPoly.StandardLineList)
                {
					line.StartPoint = Model.MatchPointFromPointsDictionary(line.StartPoint.X, line.StartPoint.Y, line.StartPoint.Z, true, line.Layer, false);
					line.StartPoint.OriginalEntityNames.Add(line.OriginalEntityName);

					line.EndPoint = Model.MatchPointFromPointsDictionary(line.EndPoint.X, line.EndPoint.Y, line.EndPoint.Z, true, line.Layer, false);
					line.EndPoint.OriginalEntityNames.Add(line.OriginalEntityName);

					Model.AddStandardLine(line, line.Layer);
				}

				//old way of importing lwpolyline (as one big PolyLine)

				//string layer = "Default";
				//if (!string.IsNullOrWhiteSpace(LWPoly.Layer.ToString()))
				//{
				//	layer = LWPoly.Layer.ToString();
				//}

				//PolyLine lwp = new PolyLine();
				//lwp.Colour = Poly.Colour;
				//string entityName = LWPoly.CodeName + ":" + LWPoly.Handle;
				//foreach (DTMPoint pt in Poly.Nodes)
				//{
				//	DTMPoint ip = Model.MatchPointFromPointsDictionary(pt.X, pt.Y, pt.Z, true, layer, false);
				//	ip.OriginalEntityNames.Add(entityName);

				//	lwp.Nodes.Add(ip);
				//}

				//int lineweight = LineweightConverter(LWPoly.Lineweight);
				//if (lineweight < 1)
				//{
				//	lwp.LwByLayer = true;
				//	lwp.Lineweight = 1;
				//}
				//else
				//{
				//	lwp.Lineweight = lineweight;
				//}


				//if (lwp != null && lwp.Nodes.Count > 0)
				//{
				//	lwp.OriginalEntityName = entityName;
				//	Model.AddPolyline(lwp, layer);
				//}
			}

			#endregion

			#region arc
			foreach (netDxf.Entities.Arc arc in Arcs)
			{


				//PolyLine ArcPoly = ProcessCADArc(arc);
				//if (ArcPoly == null) { continue; }
				//if (string.IsNullOrWhiteSpace(arc.Layer.ToString())) { ArcPoly.Layer = "Default"; }
				//else
				//{
				//	ArcPoly.Layer = arc.Layer.ToString();

				//}

				//====== new inhouse arc object creation
				Models.Arc nrgArc = ProcessCADArcNEW(arc);
				//DTMPoint cp = Model.MatchPointFromPointsDictionary(nrgArc.CentrePoint.X, nrgArc.CentrePoint.Y, nrgArc.CentrePoint.Z, true, nrgArc.Layer, false);
				Model.Points.Add(nrgArc.StartPoint);
				Model.Points.Add(nrgArc.EndPoint);
				Model.AddArcLine(nrgArc, nrgArc.Layer);
				//=======


				//PolyLine lwp = new PolyLine();
				//string entityName = arc.CodeName + ":" + arc.Handle;
				//foreach (DTMPoint pt in ArcPoly.Nodes)
				//{
				//	DTMPoint ip = Model.MatchPointFromPointsDictionary(pt.X, pt.Y, pt.Z, true, ArcPoly.Layer, false);
				//	ip.OriginalEntityNames.Add(entityName);
				//	lwp.Nodes.Add(ip);
				//}

				//int lineweight = LineweightConverter(arc.Lineweight);
				//if (lineweight < 1)
				//{
				//	lwp.LwByLayer = true;
				//	lwp.Lineweight = 1;
				//}
				//else
				//{
				//	lwp.Lineweight = lineweight;
				//}


				//if (lwp != null && lwp.Nodes.Count > 0)
				//{
				//	lwp.OriginalEntityName = entityName;
				//	Model.AddPolyline(lwp, ArcPoly.Layer);
				//}
			}
			#endregion region

			#region spline
			foreach (netDxf.Entities.Spline sp in Splines)
			{
				List<Vector3> vec = sp.PolygonalVertexes(500);

				PolyLine lwp = new PolyLine();

				string layer = "Default";
				if (!string.IsNullOrWhiteSpace(sp.Layer.ToString()))
				{
					layer = sp.Layer.ToString();
				}

				#region Colour stuff
				if (sp.Color.IsByLayer)
				{
					if (sp.Layer.Color.Equals(AciColor.Default))
					{
						lwp.Colour = Color.Black;
					}
					else
					{
						lwp.Colour = Color.FromArgb(1, sp.Layer.Color.R, sp.Layer.Color.G, sp.Layer.Color.B);
					}
				}
				else
				{
					if (sp.Color.Equals(AciColor.Default))
					{
						lwp.Colour = Color.Black;
					}
					else
					{
						lwp.Colour = Color.FromArgb(1, sp.Color.R, sp.Color.G, sp.Color.B);
					}
				}
				#endregion

				string entityName = sp.CodeName + ":" + sp.Handle;
				foreach (Vector3 v in vec)
				{
					DTMPoint pp = new DTMPoint();


					pp.X = v.X * coorddiv;
					pp.Y = v.Y * coorddiv;
					pp.Z = v.Z * coorddiv;
					//Model.Points.Add(pp);
					DTMPoint ip = Model.MatchPointFromPointsDictionary(pp.X, pp.Y, pp.Z, true, layer, false);
					ip.OriginalEntityNames.Add(entityName);

					lwp.Nodes.Add(ip);
				}


				int lineweight = LineweightConverter(sp.Lineweight);
				if (lineweight < 1)
				{
					lwp.LwByLayer = true;
					lwp.Lineweight = 1;
				}
				else
				{
					lwp.Lineweight = lineweight;
				}



				if (lwp != null && lwp.Nodes.Count > 0)
				{
					lwp.OriginalEntityName = entityName;
					Model.AddPolyline(lwp, layer);
				}
			}
			#endregion

			#region circle
			foreach (netDxf.Entities.Circle circ in Circles)
			{
				List<Vector2> vec = circ.PolygonalVertexes(100);

				PolyLine lwp = new PolyLine();

				string layer = "Default";
				if (!string.IsNullOrWhiteSpace(circ.Layer.ToString()))
				{
					layer = circ.Layer.ToString();
				}

				#region Colour stuff
				if (circ.Color.IsByLayer)
				{
					if (circ.Layer.Color.Equals(AciColor.Default))
					{
						lwp.Colour = Color.Black;
					}
					else
					{
						lwp.Colour = Color.FromArgb(1, circ.Layer.Color.R, circ.Layer.Color.G, circ.Layer.Color.B);
					}
				}
				else
				{
					if (circ.Color.Equals(AciColor.Default))
					{
						lwp.Colour = Color.Black;
					}
					else
					{
						lwp.Colour = Color.FromArgb(1, circ.Color.R, circ.Color.G, circ.Color.B);
					}
				}
				#endregion


				string entityName = circ.CodeName + ":" + circ.Handle;
				foreach (Vector2 v in vec)
				{
					DTMPoint pp = new DTMPoint();



					pp.X = (v.X * coorddiv) + circ.Center.X * coorddiv;
					pp.Y = (v.Y * coorddiv) + circ.Center.Y * coorddiv;
					pp.Z = -999;
					//Model.Points.Add(pp);
					DTMPoint ip = Model.MatchPointFromPointsDictionary(pp.X, pp.Y, pp.Z, true, layer, false);

					ip.OriginalEntityNames.Add(entityName);

					lwp.Nodes.Add(ip);
				}
				lwp.IsClosed = true;

				int lineweight = LineweightConverter(circ.Lineweight);
				if (lineweight < 1)
				{
					lwp.LwByLayer = true;
					lwp.Lineweight = 1;
				}
				else
				{
					lwp.Lineweight = lineweight;
				}



				if (lwp != null && lwp.Nodes.Count > 0)
				{
					lwp.OriginalEntityName = entityName;
					Model.AddPolyline(lwp, layer);
				}
			}
			#endregion region

			#region blocks
			foreach (netDxf.Entities.Insert blkIns in BlockInserts)
			{


				if (blkIns.Block.IsXRef) { continue; }

				//Create single insert point
				//DTMPoint insPoint = new DTMPoint();
				//insPoint.X = blkIns.Position.X;
				//insPoint.Y = blkIns.Position.Y;
				//insPoint.Z = blkIns.Position.Z;
				DTMPoint insPoint = Model.MatchPointFromPointsDictionary(blkIns.Position.X, blkIns.Position.Y, blkIns.Position.Z, true, "Default", false);
				string entityName = blkIns.CodeName + ":" + blkIns.Handle;
				insPoint.OriginalEntityNames.Add(entityName);


				Point3D parentOffset = new Point3D(); //The initial parent offset is 0,0,0

				//Process all definitions the insertpoint may contain. (nested blocks get added as seperate/same-level blockinserts to the point
				ProcessBlockDefinition(blkIns.Block, Model, insPoint, parentOffset);

				//-----------------------------------------------everything below gets done in ProcessBlockDefinition too so we eliminate the nested-ness and end up with 1 insPoint with loads of seperate block inserts
				//Create the new block inserts instance
				BlockInsert blockIns = new BlockInsert();

				//Setup the block inserts properties
				blockIns.Scale = new Point3D(blkIns.Scale.X, blkIns.Scale.Y, blkIns.Scale.Z);


				double rotation = (blkIns.Rotation / 180) * Math.PI;
				blockIns.Rotation = rotation;
				blockIns.Colour = blkIns.Color.ToColor();
				blockIns.BlockDefinitionHandle = blkIns.Block.Handle;


				if (insPoint.BlockInserts == null)
				{
					insPoint.BlockInserts = new List<BlockInsert>();
				}

				insPoint.BlockInserts.Add(blockIns);

				//-------------------------------------------------

				List<BlockInsert> listOfUnwantedBlockInserts = new List<BlockInsert>(); //We store all the possible block inserts during recursion. Then we check for undesired ones (duplicates) below and remove them.

				//Setup the blockinsert entities to be on the correct layers
				foreach (var blockInsert in insPoint.BlockInserts)
				{

					//Check the block is actually in our blockdefinitions. (Some DXF blocks are just parents for other blocks. In this case, we remove the parent block insert since its useless; it won't have a definition)
					if (!Model.GetBlockCollection().BlockDefinitions.ContainsKey(blockInsert.BlockDefinitionHandle))
					{
						listOfUnwantedBlockInserts.Add(blockInsert);

					}
					else
					{
						blockInsert.AddToLayer(Model, blockInsert.Layer); //We also only want the layers of the block inserts we want to keep. Easier to do that check here.
					}

				}

				foreach (var biToRemove in listOfUnwantedBlockInserts)
				{
					insPoint.BlockInserts.Remove(biToRemove);
				}

				//string blockname = blk.Block.Name.ToString();
				//insPoint.PointLabel = "[Entity= Block: " + blockname + ", " + blk.Block.Handle + "]";
				//Model.Points.Add(insPoint);


			}
			#endregion region

			#region 3dFaces
			List<netDxf.Entities.Face3d> Faces = DXFFile.Faces3d.ToList();
			if (Faces != null && Faces.Count > 0)
			{
				TriangleSurface surface = new TriangleSurface();

				List<netDxf.Tables.Layer> Layers = DXFFile.Layers.ToList();

				if (Layers != null && Layers.Count > 0)
				{
					//Create new surfaces from the DXF layers
					foreach (var layer in Layers)
					{

						string surfaceName = layer.Name;

						Color surfaceColour = Color.FromArgb(layer.Color.R, layer.Color.G, layer.Color.B);
						Model.CreateNewSurface(surfaceColour, surfaceName);
					}
				}
				else
				{
					surface.ID = 1;
					Model.Surfaces.Add(surface);
					surface = (Model.Surfaces[Model.Surfaces.Count - 1]);
					surface.Contour = true;
					surface.Volume = true;
					surface.Locked = false;
					surface.Name = "Unnamed";
				}

				foreach (netDxf.Entities.Face3d f in Faces)
				{
					DTMPoint pt1 = new DTMPoint(f.FirstVertex.X * coorddiv, f.FirstVertex.Y * coorddiv, f.FirstVertex.Z * coorddiv);
					DTMPoint pt2 = new DTMPoint(f.SecondVertex.X * coorddiv, f.SecondVertex.Y * coorddiv, f.SecondVertex.Z * coorddiv);
					DTMPoint pt3 = new DTMPoint(f.ThirdVertex.X * coorddiv, f.ThirdVertex.Y * coorddiv, f.ThirdVertex.Z * coorddiv);

					//Model.Points.Add(pt1);
					DTMPoint ip1 = Model.MatchPointFromPointsDictionary(pt1.X, pt1.Y, pt1.Z, true, null, false);
					pt1 = ip1;
					DTMPoint ip2 = Model.MatchPointFromPointsDictionary(pt2.X, pt2.Y, pt2.Z, true, null, false);
					pt2 = ip2;
					DTMPoint ip3 = Model.MatchPointFromPointsDictionary(pt3.X, pt3.Y, pt3.Z, true, null, false);
					pt3 = ip3;

					Triangle tr = new Triangle();



					//if (f.Color.IsByLayer == false)
					//{
					//	surface.R = f.Color.R;
					//	surface.G = f.Color.G;
					//	surface.B = f.Color.B;
					//}

					string searchName = f.Layer.Name;

					foreach (TriangleSurface s in Model.Surfaces)
					{
						if (searchName == s.Name)
						{
							surface = s;
							if (f.Color.IsByLayer == false)
							{
								s.R = f.Color.R;
								s.G = f.Color.G;
								s.B = f.Color.B;
							}
						}
					}

					if (MathsHelpers.Vector.FNRightOf(pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y))//added by ES:21.07.21 - ensure the triangle nodes are ordered CCW		
					{
						tr = new Triangle(surface, pt1, pt3, pt2);
					}
					else
					{
						tr = new Triangle(surface, pt1, pt2, pt3);
					}
					//Assign the triangle a surface
					foreach (var surf in Model.Surfaces)
						if (surf.Name == f.Layer.Name)
						{
							surf.AddTriangle(tr);
							tr.Surface = surf;
						}
					Model.Triangles.Add(tr);
				}

			}
			#endregion

			#region mesh
			//TODO write some fucking code
			foreach (netDxf.Entities.PolyfaceMesh mesh in Meshes)
			{

				List<netDxf.Entities.EntityObject> entityObjects = mesh.Explode();
				foreach (netDxf.Entities.Face3d face in entityObjects)
				{

				}


			}
			#endregion

			#region hatch
			foreach (netDxf.Entities.Hatch h in Hatches)
			{
				h.CreateBoundary(true);

				foreach (netDxf.Entities.HatchBoundaryPath bp in h.BoundaryPaths)
				{
					foreach (netDxf.Entities.EntityObject e in bp.Entities)
					{
						if (e.Type == netDxf.Entities.EntityType.Line)
						{

						}
						else if (e.Type == netDxf.Entities.EntityType.LwPolyline)
						{

						}
						else if (e.Type == netDxf.Entities.EntityType.Polyline)
						{

						}

					}

				}
			}
			#endregion


			return Model;
		}


		//private PolyLine ProcessCADArc(netDxf.Entities.Arc arc, double Level = -999, string Label = "[CADEntity: ARC]")
		//{
		//	PolyLine ArcPoly = new PolyLine();

		//	if (arc != null)
		//	{
		//		#region colour                
		//		if (arc.Color.IsByLayer)
		//		{
		//			if (arc.Layer.Color.Equals(AciColor.Default))
		//			{
		//				ArcPoly.Colour = Color.Black;
		//			}
		//			else
		//			{
		//				ArcPoly.Colour = Color.FromArgb(1, arc.Layer.Color.R, arc.Layer.Color.G, arc.Layer.Color.B);
		//			}
		//		}
		//		else
		//		{
		//			if (arc.Color.Equals(AciColor.Default))
		//			{
		//				ArcPoly.Colour = Color.Black;
		//			}
		//			else
		//			{
		//				ArcPoly.Colour = Color.FromArgb(1, arc.Color.R, arc.Color.G, arc.Color.B);
		//			}
		//		}
		//		#endregion

		//		//calc arc length
		//		double AL = MathsHelpers.Geometry.ArcLength(arc.StartAngle / 180 * Math.PI, arc.EndAngle / 180 * Math.PI, arc.Radius);
		//		// if (AL == 0) { return null; }
		//		if (AL < 1) { return null; ; }
		//		LwPolyline pl = arc.ToPolyline((int)AL * 10);//PolygonalVertexes((int)(AL *100 ));
		//		var test = arc.PolygonalVertexes((int)AL * 10);
		//		ArcPoly = ProcessLWPolyline(pl); //this will be fokt off
		//										 //foreach (Vector2 pt in pl)
		//										 //{

		//		//	DTMPoint p = new DTMPoint();	
		//		//	p.X = pt.X;
		//		//	p.Y = pt.Y;
		//		//	p.Z = Level;
		//		//	p.PointLabel = Label;	
		//		//	ArcPoly.Nodes.Add(p);
		//		//}
		//		return ArcPoly;
		//	}
		//	else
		//	{
		//		return null;
		//	}
		//}

		private Models.Arc ProcessCADArcNEW(netDxf.Entities.Arc cadArc)
		{
			DTMPoint cp = new DTMPoint(cadArc.Center.X, cadArc.Center.Y, cadArc.Center.Z);
			//cp.OriginalEntityNames.Add(cadArc.CodeName + ":" + cadArc.Handle);
			double startBearing = NRG.MathsHelpers.Trig.FNBearingToAngle(NRG.MathsHelpers.Trig.DegToRad(cadArc.StartAngle));
			double endBearing = NRG.MathsHelpers.Trig.FNBearingToAngle(NRG.MathsHelpers.Trig.DegToRad(cadArc.EndAngle));
			
			DTMPoint arcStart3D = new DTMPoint(MathsHelpers.Trig.PRC(cadArc.Center.X, cadArc.Center.Y, startBearing, cadArc.Radius), cadArc.Center.Z);
			DTMPoint arcEnd3D = new DTMPoint(MathsHelpers.Trig.PRC(cadArc.Center.X, cadArc.Center.Y, endBearing, cadArc.Radius), cadArc.Center.Z);

			bool centrePointPosition = !NRG.MathsHelpers.Vector.FNRightOf(arcStart3D.X, arcStart3D.Y, arcEnd3D.X, arcEnd3D.Y, cp.X, cp.Y);

			NRG.Models.Arc nrgArc = new Models.Arc(arcStart3D, arcEnd3D,-Math.Abs(cadArc.Radius), centrePointPosition);//Cad always draws anti-clockwise from start to end
			nrgArc.Layer = cadArc.Layer.ToString();
			nrgArc.OriginalEntityName = cadArc.CodeName + ":" + cadArc.Handle;
			return nrgArc;
		}


		private ExplodedPolyline ProcessLWPolyline(LwPolyline LWPoly, bool ignoreColour = false)
		{
			ExplodedPolyline explodedPolyline = new ExplodedPolyline();
			explodedPolyline.IsClosed = LWPoly.IsClosed;
			
			double Level = LWPoly.Elevation;//whole line should be at one elevation for contours and the like.
			Color colour = Color.Black;
			if (LWPoly != null)
			{
				#region Colour
				if (!ignoreColour)
				{
					if (LWPoly.Color.IsByLayer)
					{
						if (LWPoly.Layer.Color.Equals(AciColor.Default))
						{
							colour = Color.Black;
						}
						else
						{
							colour = Color.FromArgb(1, LWPoly.Layer.Color.R, LWPoly.Layer.Color.G, LWPoly.Layer.Color.B);
						}
					}
					else
					{
						if (LWPoly.Color.Equals(AciColor.Default))
						{
							colour = Color.Black;
						}
						else
						{
							colour = Color.FromArgb(1, LWPoly.Color.R, LWPoly.Color.G, LWPoly.Color.B);
						}
					}
				}
				#endregion

				List<netDxf.Entities.EntityObject> ents = LWPoly.Explode();
				foreach (netDxf.Entities.EntityObject e in ents)
				{
					if (e.Type == EntityType.Line)
					{
						StandardLine ln = ProcessCADLine((netDxf.Entities.Line)e, true);
						if (ln != null)
						{
							//Below is no longer needed. Previously we made one big nrgPolyLine which needed to be sorted into a linear order
							//Now we just make everything a single standardline or an arc (same thing), so the order of their start/ends doesn't matter.
							//Post sorting can be done for the standardlines if you really really want to.

							//PolyLine Poly = new PolyLine();

							//if (Poly.Nodes.Count > 0)
							//{
							//	//If the polyline already has nodes, we need to check if its in the correct order already

							//	//This method reverses the existing polyline if needed. Here we check if the next entity needs reversing or not
							//	if (NRG.MathsHelpers.Geometry.PolyLineReversal(ref Poly, ln.StartPoint, ln.EndPoint))
							//	{
							//		Poly.Nodes.Add(ln.StartPoint);
							//	}
							//	else
							//	{   //The next entity doesn't need reversing, we can add it normally. The existing polyline will have been reversed if needed
							//		Poly.Nodes.Add(ln.EndPoint);
							//	}
							//}
							//else
							//{
							//	Poly.Nodes.Add(ln.StartPoint);
							//	Poly.Nodes.Add(ln.EndPoint);
							//}
							ln.Colour = colour;
							explodedPolyline.StandardLineList.Add(ln);
							
						}
					}
					else if (e.Type == EntityType.Arc)
					{
						var cadArc = (netDxf.Entities.Arc)e;

						Models.Arc nrgArc = ProcessCADArcNEW(cadArc);
						nrgArc.Colour = colour;
						explodedPolyline.ArcList.Add(nrgArc);


						//Old way of importing arcs. (making points along the line etc)

						////Below import the arc as a polyline (sorts it out if the arc is part of a cad polyline too)
						//PolyLine arcpoly = new PolyLine();
						//arcpoly = ProcessCADArc(cadArc);
						//if (arcpoly != null)
						//{

						//	if (Poly.Nodes.Count > 0)
						//	{ //If the polyline already has nodes, we need to check if its in the correct order already

						//		//This method reverses the existing polyline if needed. Here we check if the next entity needs reversing or not
						//		if (NRG.MathsHelpers.Geometry.PolyLineReversal(ref Poly, arcpoly.Nodes.First(), arcpoly.Nodes.Last()))
						//		{
						//			//Reverse arcPolyList
						//			var reversedArcPoly = arcpoly.Nodes.Reverse().ToList();
						//			arcpoly.Nodes = new BindingList<DTMPoint>(reversedArcPoly);

						//			//Remove first point as its the same as the existing poly's last
						//			var arcNodesWithoutFirstPoint = arcpoly.Nodes.Except(new BindingList<DTMPoint> { arcpoly.Nodes[0] });

						//			//Add Nodes
						//			foreach (DTMPoint pt in arcNodesWithoutFirstPoint)
						//			{
						//				Poly.Nodes.Add(pt);
						//			}
						//		}
						//		else
						//		{   //The next entity doesn't need reversing, we can add it normally. The existing polyline will have been reversed if needed

						//			//Remove first point as its the same as the existing poly's last
						//			var arcNodesWithoutFirstPoint = arcpoly.Nodes.Except(new BindingList<DTMPoint> { arcpoly.Nodes[0] });
						//			//Add Nodes

						//			foreach (DTMPoint pt in arcNodesWithoutFirstPoint)
						//			{
						//				Poly.Nodes.Add(pt);
						//			}
						//		}
						//	}
						//	else
						//	{ //If this is the first entity being added we can just add each node normally.

						//		//Add Nodes
						//		foreach (DTMPoint pt in arcpoly.Nodes)
						//		{
						//			Poly.Nodes.Add(pt);
						//		}
						//	}
						//}
					}
				}
				

				return explodedPolyline;
			}
			else
			{
				return null;
			}
		}

		private StandardLine ProcessCADLine(netDxf.Entities.Line line, bool ignoreColour = false)
		{

			if (line == null)
			{
				return null;
			}
			StandardLine lineout = new StandardLine();
			lineout.Colour = Color.FromArgb(1, line.Layer.Color.R, line.Layer.Color.G, line.Layer.Color.B);

			#region Colour stuff
			if (!ignoreColour)
			{
				if (line.Color.IsByLayer)
				{
					if (line.Layer.Color.Equals(AciColor.Default))
					{
						lineout.Colour = Color.Black;
					}
					else
					{
						lineout.Colour = Color.FromArgb(1, line.Layer.Color.R, line.Layer.Color.G, line.Layer.Color.B);
					}
				}
				else
				{
					if (line.Color.Equals(AciColor.Default))
					{
						lineout.Colour = Color.Black;
					}
					else
					{
						lineout.Colour = Color.FromArgb(1, line.Color.R, line.Color.G, line.Color.B);
					}
				}
			}
			#endregion

			//Vector3 v1 = MathHelper.Transform(new Vector3(line.StartPoint.X, line.StartPoint.Y, line.StartPoint.Z), line.Normal, MathHelper.CoordinateSystem.Object, MathHelper.CoordinateSystem.World);
			//Vector3 v2 = MathHelper.Transform(new Vector3(line.EndPoint.X, line.EndPoint.Y, line.EndPoint.Z), line.Normal, MathHelper.CoordinateSystem.Object, MathHelper.CoordinateSystem.World);
			//Commented out by TN 01.06.22 - This is not needed as the line is already in world coordinates. 
			Vector3 v1 = new Vector3(line.StartPoint.X, line.StartPoint.Y, line.StartPoint.Z);
			Vector3 v2 = new Vector3(line.EndPoint.X, line.EndPoint.Y, line.EndPoint.Z);

			lineout.StartPoint.X = v1.X;
			lineout.StartPoint.Y = v1.Y;
			lineout.StartPoint.Z = v1.Z;
			lineout.EndPoint.X = v2.X;
			lineout.EndPoint.Y = v2.Y;
			lineout.EndPoint.Z = v2.Z;
			lineout.Layer = line.Layer.ToString();
			lineout.Layer = lineout.Layer.Replace(" ", null);
			lineout.OriginalEntityName = line.CodeName + ":" + line.Handle;

			return lineout;
		}



		public void ProcessBlockDefinition(netDxf.Blocks.Block BlockDef, DTM Model, DTMPoint insPoint, Point3D parentOffset)
		{
			//Check if we need to create the block definition. If true, no lines etc are added to block definition, but we still check for nested blocks so we can create seperate block inserts for each instance
			//This is needed to eliminate the nested relationship in all blocks. We want to end up with a single intertion point for all the blocks inside. TN 26.11.21
			bool alreadyProcessed = false;
			if (Model.GetBlockCollection().BlockDefinitions.ContainsKey(BlockDef.Handle))
			{
				alreadyProcessed = true;
			}


			BlockDefinition CurrentBlock = new BlockDefinition();
			CurrentBlock.Name = BlockDef.Handle;
			CurrentBlock.Handle = BlockDef.Handle;


			foreach (var ent in BlockDef.Entities)
			{

				switch (ent.Type)
				{
					case netDxf.Entities.EntityType.Line:
						if (!alreadyProcessed) //BlockDefiniton has already been processed, we don't need to bother with this
						{
							netDxf.Entities.Line ln = (netDxf.Entities.Line)ent;//cast into line object
							BlockLine newline = new BlockLine();
							newline.StartPoint.X = ln.StartPoint.X;
							newline.StartPoint.Y = ln.StartPoint.Y;
							newline.StartPoint.Z = ln.StartPoint.Z;
							newline.EndPoint.X = ln.EndPoint.X;
							newline.EndPoint.Y = ln.EndPoint.Y;
							newline.EndPoint.Z = ln.EndPoint.Z;
							newline.Radius = 0;
							CurrentBlock.BlockLines.Add(newline);
						}
						break;
					case netDxf.Entities.EntityType.Polyline:
						if (!alreadyProcessed) //BlockDefiniton has already been processed, we don't need to bother with this
						{
							netDxf.Entities.Polyline pln = (netDxf.Entities.Polyline)ent;


							bool startFound = false;
							Point3D startPt = new Point3D();
							Point3D endPt = new Point3D();
							foreach (netDxf.Entities.PolylineVertex pl in pln.Vertexes)
							{

								if (!startFound)
								{

									startPt.X = pl.Position.X;
									startPt.Y = pl.Position.Y;
									startPt.Z = pl.Position.Z;
									startFound = true;
								}
								else
								{
									endPt.X = pl.Position.X;
									endPt.Y = pl.Position.Y;
									endPt.Z = pl.Position.Z;

									BlockLine newline = new BlockLine();
									newline.StartPoint = startPt;
									newline.EndPoint = endPt;
									startPt = endPt;
									endPt = new Point3D();

									CurrentBlock.BlockLines.Add(newline);
								}


							}

							if (pln.IsClosed && pln.Vertexes.Count > 2)
							{
								BlockLine closingLine = new BlockLine();
								closingLine.StartPoint.X = pln.Vertexes[pln.Vertexes.Count - 1].Position.X;
								closingLine.StartPoint.Y = pln.Vertexes[pln.Vertexes.Count - 1].Position.Y;
								closingLine.StartPoint.Z = pln.Vertexes[pln.Vertexes.Count - 1].Position.Z;
								closingLine.EndPoint.X = pln.Vertexes[0].Position.X;
								closingLine.EndPoint.Y = pln.Vertexes[0].Position.Y;
								closingLine.EndPoint.Z = pln.Vertexes[0].Position.Z;
								CurrentBlock.BlockLines.Add(closingLine);
							}


						}
						break;
					case netDxf.Entities.EntityType.LwPolyline:
						if (!alreadyProcessed) //BlockDefiniton has already been processed, we don't need to bother with this
						{
							netDxf.Entities.LwPolyline lwpln = (netDxf.Entities.LwPolyline)ent;
							ExplodedPolyline explodedPolyline = ProcessLWPolyline(lwpln);
							foreach (var polyline in explodedPolyline.StandardLineList)
							{ 
								CurrentBlock.AddBlockLines(polyline); 
							}
							foreach(var arc in explodedPolyline.ArcList)
                            {
								CurrentBlock.AddBlockLines(arc);
                            }
						}
						break;
					case netDxf.Entities.EntityType.Arc:
						{
							var arc = (netDxf.Entities.Arc)ent;
							//Need to think how to process blocked arcs. maybe we just explode the arcs into the small points? TN 23.06.22
							//PolyLine ArcPoly = ProcessCADArc(arc);
							//if (ArcPoly == null) { continue; }
							//CurrentBlock.AddBlockLines(ArcPoly);
						}
						break;
					case netDxf.Entities.EntityType.Spline:
						{
							var sp = (netDxf.Entities.Spline)ent;
							List<Vector3> vec = sp.PolygonalVertexes(500);
							PolyLine sppoly = new PolyLine();
							foreach (Vector3 v in vec)
							{
								DTMPoint node = new DTMPoint();
								node.X = v.X;
								node.Y = v.Y;
								node.Z = v.Z;
								sppoly.Nodes.Add(node);
							}

							CurrentBlock.AddBlockLines(sppoly);

						}
						break;
					case netDxf.Entities.EntityType.Circle:
						{
							var circ = (netDxf.Entities.Circle)ent;
							List<Vector2> vec = circ.PolygonalVertexes(100);
							PolyLine poly = new PolyLine();
							foreach (Vector2 v in vec)
							{
								DTMPoint node = new DTMPoint();
								node.X = v.X;
								node.Y = v.Y;
								node.Z = -999;
								poly.Nodes.Add(node);
							}
							CurrentBlock.AddBlockLines(poly);

						}
						break;

					case netDxf.Entities.EntityType.Insert:
						{ //Regardless as to wether the BlockDefiniton has already been processed, we still need to find and log the other nested blocks and add them to our insertion point
							var blockInsert = (netDxf.Entities.Insert)ent;

							BlockInsert blockIns = new BlockInsert();

							//Calculate the InsertionOffset from this specific blockInsert's position + the cumalative parent offsets
							blockIns.InsertionOffset.X = blockInsert.Position.X + parentOffset.X;
							blockIns.InsertionOffset.Y = blockInsert.Position.Y + parentOffset.Y;
							blockIns.InsertionOffset.Z = blockInsert.Position.Z + parentOffset.Z;

							//We make a new parent offset for the next branch of nested blocks (new point3D to avoid pointer issues)
							Point3D newParentOffset = new Point3D();
							newParentOffset.X = blockIns.InsertionOffset.X;
							newParentOffset.Y = blockIns.InsertionOffset.Y;
							newParentOffset.Z = blockIns.InsertionOffset.Z;

							ProcessBlockDefinition(blockInsert.Block, Model, insPoint, newParentOffset);

							//Setup the block inserts properties
							blockIns.Scale = new Point3D(blockInsert.Scale.X, blockInsert.Scale.Y, blockInsert.Scale.Z);
							double roation = (blockInsert.Rotation / 180) * Math.PI;
							blockIns.Rotation = roation;

							blockIns.Layer = blockInsert.Layer.Name;
							blockIns.Layer = blockIns.Layer.Replace(" ", null);
							blockIns.Colour = blockInsert.Color.ToColor();
							blockIns.BlockDefinitionHandle = blockInsert.Block.Handle;

							if (insPoint.BlockInserts == null)
							{
								insPoint.BlockInserts = new List<BlockInsert>();
							}

							insPoint.BlockInserts.Add(blockIns);

						}
						break;

				}
			}
			//If block definition has already been processed we don't need to add it to our collection
			if (!alreadyProcessed)
			{ if (CurrentBlock.BlockLines.Count > 0) //If the block is just a wrapper for other blocks, we dont need to add it. If it has its own lines, we add it
				{
					Model.GetBlockCollection().BlockDefinitions.Add(CurrentBlock.Handle, CurrentBlock);
				}
			}

			return;

		}

		/// <summary>
		/// NOT USED ANYMORE. ProcessBlockDefinition used instead.
		/// </summary>
		/// <param name="Model"></param>
		/// <param name="Block"></param>
		/// <returns></returns>
		//private DTM ProcessCADBlock(DTM Model, netDxf.Entities.Insert Block)

		//{
		//	string testString = "";
		//	Color colour = new Color();//().from ( Block.Color.R, Block.Color.G, Block.Color.B);
		//	foreach(var ent in Block.Block.Entities)
		//	{
		//		ent.Type.ToString();
		//	}

		//	if (Block.Color.IsByLayer==false)

		//	{
		//		colour = Color.FromArgb(1, Block.Color.R, Block.Color.G, Block.Color.B);
		//	}
		//	else 
		//	{
		//		colour = Color.FromArgb(1, Block.Layer.Color.R, Block.Layer.Color.G, Block.Layer.Color.B);
		//	}


		//	List<netDxf.Entities.EntityObject> ents = Block.Explode();
		//	int ptCount = 0;
		//	foreach (netDxf.Entities.EntityObject obj in ents)
		//	{

		//		if (obj.Type == netDxf.Entities.EntityType.Line)
		//		{
		//			netDxf.Entities.Line ln = (netDxf.Entities.Line)obj;//cast into line object
		//			StandardLine blockline = new StandardLine();


		//			Vector3 v1 = MathHelper.Transform(new Vector3(ln.StartPoint.X, ln.StartPoint.Y, ln.StartPoint.Z), ln.Normal, MathHelper.CoordinateSystem.Object, MathHelper.CoordinateSystem.World);
		//			Vector3 v2 = MathHelper.Transform(new Vector3(ln.EndPoint.X, ln.EndPoint.Y, ln.EndPoint.Z), ln.Normal, MathHelper.CoordinateSystem.Object, MathHelper.CoordinateSystem.World);




		//			//start of the line
		//			DTMPoint pp = new DTMPoint();

		//			pp.X = v1.X;//ln.StartPoint.X * coorddiv;
		//			pp.Y = v1.Y;//ln.StartPoint.Y * coorddiv;
		//			pp.Z = v1.Z;//ln.StartPoint.Z * coorddiv;

		//			//pp.X = ln.StartPoint.X * coorddiv;
		//			//pp.Y = ln.StartPoint.Y * coorddiv;
		//			//pp.Z = ln.StartPoint.Z * coorddiv;
		//			//Model.Points.Add(pp);

		//			DTMPoint sp = Model.MatchPointFromPointsDictionary(pp.X, pp.Y, pp.Z, true);

		//			blockline.StartPoint = sp;
		//			//end of the line					
		//			DTMPoint ep = new DTMPoint();
		//			ep.X = v2.X;//ln.EndPoint.X * coorddiv;
		//			ep.Y = v2.Y;//ln.EndPoint.Y * coorddiv;
		//			ep.Z = v2.Z;//ln.EndPoint.Z * coorddiv;


		//			//ep.X = ln.EndPoint.X * coorddiv;
		//			//ep.Y = ln.EndPoint.Y * coorddiv;
		//			//ep.Z = ln.EndPoint.Z * coorddiv;
		//			//Model.Points.Add(ep);
		//			DTMPoint endp = Model.MatchPointFromPointsDictionary(ep.X, ep.Y, ep.Z, true);
		//			if (endp.PointLabel == null || endp.PointLabel.Contains(testString) == false)
		//			{
		//				endp.PointLabel = endp.PointLabel + testString;
		//			}


		//			blockline.EndPoint = endp;

		//			blockline.Colour = colour;//Color.FromArgb(1, obj.Color.R, obj.Color.G, obj.Color.B);
		//			//blockline.Colour = Color.Black;

		//			Model.AddStandardLine(blockline, obj.Layer.Name);
		//		}
		//		else if (obj.Type == netDxf.Entities.EntityType.Polyline)
		//		{

		//			netDxf.Entities.Polyline pln = (netDxf.Entities.Polyline)obj;
		//			PolyLine blockPolyLine = new PolyLine();
		//			blockPolyLine.Colour = colour;//Color.FromArgb(1, pln.Color.R, pln.Color.G, pln.Color.B);
		//			foreach (netDxf.Entities.PolylineVertex pv in pln.Vertexes)
		//			{
		//				DTMPoint pp = new DTMPoint();

		//				Vector3 v = MathHelper.Transform(new Vector3(pv.Position.X, pv.Position.Y, pv.Position.Z), pln.Normal, MathHelper.CoordinateSystem.Object, MathHelper.CoordinateSystem.World);
		//				pp.X = v.X;
		//				pp.Y = v.Y;
		//				pp.Z = v.Z;

		//				DTMPoint ip = Model.MatchPointFromPointsDictionary(pp.X, pp.Y, pp.Z, true);
		//				testString = "(Entity=Polyline Linetype=" + pln.Linetype + ")";
		//				if (ip.PointLabel== null || ip.PointLabel.Contains(testString) == false)
		//				{
		//					ip.PointLabel = ip.PointLabel + testString;
		//				}

		//				blockPolyLine.Nodes.Add(ip);

		//			}
		//			Model.AddPolyline(blockPolyLine, Block.Layer.Name);
		//		}
		//		else if (obj.Type == netDxf.Entities.EntityType.LwPolyline)
		//		{

		//			netDxf.Entities.LwPolyline pln = (netDxf.Entities.LwPolyline)obj;
		//			PolyLine blockPolyLine = new PolyLine();
		//			blockPolyLine.Colour = colour;//Color.FromArgb(1, pln.Color.R, pln.Color.G, pln.Color.B);
		//			foreach (netDxf.Entities.LwPolylineVertex pv in pln.Vertexes)
		//			{
		//				DTMPoint pp = new DTMPoint();
		//				pp.PointLabel = pln.Linetype.ToString().Replace(" ", "");

		//				pp.PointLabel = pln.Layer.ToString();
		//				Vector3 v = MathHelper.Transform(new Vector3(pv.Position.X, pv.Position.Y, pln.Elevation), pln.Normal, MathHelper.CoordinateSystem.Object, MathHelper.CoordinateSystem.World);
		//				pp.X = v.X;
		//				pp.Y = v.Y;
		//				pp.Z = v.Z;

		//				DTMPoint ip = Model.MatchPointFromPointsDictionary(pp.X, pp.Y, pp.Z, true);
		//				testString = "(Entity=LWPolyline Linetype=" + pln.Linetype + ")";
		//				if (ip.PointLabel== null ||ip.PointLabel.Contains(testString) == false)
		//				{
		//					ip.PointLabel = ip.PointLabel + testString;
		//				}
		//				blockPolyLine.Nodes.Add(ip);

		//			}
		//			Model.AddPolyline(blockPolyLine, Block.Layer.Name);
		//		}

		//		else if (obj.Type == netDxf.Entities.EntityType.Arc)
		//		{
		//			foreach (netDxf.Entities.Arc arc in Arcs)
		//			{

		//				arc.Radius = arc.Radius;
		//				List<Vector2> vec = arc.PolygonalVertexes(100);
		//				foreach (Vector2 v in vec)
		//				{

		//					DTMPoint pp2 = new DTMPoint();
		//					pp2.PointLabel = arc.Linetype.ToString().Replace(" ", "");
		//					if (v == vec.First())
		//					{
		//						pp2.PointLabel += "/ST";
		//					}
		//					else if (v == vec.Last())
		//					{
		//						pp2.PointLabel += "/CL";
		//					}

		//					pp2.X = (v.X * coorddiv) + (arc.Center.X * coorddiv);
		//					pp2.Y = (v.Y * coorddiv) + (arc.Center.Y * coorddiv);
		//					pp2.Z = -999;
		//					testString = "(Entity=Arc Linetype=" + arc.Linetype + ")";
		//					if (pp2.PointLabel == null || pp2.PointLabel.Contains(testString) == false)
		//                          {
		//                              pp2.PointLabel = pp2.PointLabel + testString;
		//                          }

		//					Model.Points.Add(pp2);

		//				}
		//			}
		//		}
		//		else if (obj.Type == netDxf.Entities.EntityType.Insert)
		//		{
		//			if (obj.Type == netDxf.Entities.EntityType.Insert)
		//				Model = ProcessCADBlock(Model, (netDxf.Entities.Insert)obj);
		//		}
		//		else
		//		{
		//			Console.WriteLine(""); 
		//		}


		//	}
		//	return Model;

		//}

		/// <summary>
		/// Converts DXF lineweights to our render system's pixel sizes. Can be modified to whichever sizes make the most sense. I've done it by eye :) TN 31.01.22
		/// </summary>
		/// <param name="dxfLineweight"></param>
		/// <returns></returns>
		/// 
		/// Return 0 if the Lineweight is decided by block or by layer.
		public int LineweightConverter(netDxf.Lineweight dxfLineweight)
		{
			switch (dxfLineweight)
			{
				case Lineweight.ByBlock:
				case Lineweight.ByLayer:
					return 0; //Look for cases of this being used in this import for how to handle a return of 0

				case Lineweight.Default:
				case Lineweight.W0:
				case Lineweight.W5:
				case Lineweight.W9:
				case Lineweight.W13:
				case Lineweight.W15:
				case Lineweight.W18:
				case Lineweight.W20:
				case Lineweight.W25:
				case Lineweight.W30:
				case Lineweight.W35:
				case Lineweight.W40:
				case Lineweight.W50:
				case Lineweight.W53:
				case Lineweight.W60:
					return 1;

				case Lineweight.W70:
				case Lineweight.W80:
				case Lineweight.W90:
					return 2;

				case Lineweight.W100:
				case Lineweight.W106:
				case Lineweight.W120:
					return 3;

				case Lineweight.W140:
					return 4;

				case Lineweight.W158:
					return 5;

				case Lineweight.W200:
					return 6;
				case Lineweight.W211:
					return 7;
			}


			return 1;
		}
		#endregion DXF DTM
	}

	public class ExplodedPolyline //this can be put somewhere better, just stuck it here because its only really relevant for the DXF import
	{
		public List<StandardLine> StandardLineList = new List<StandardLine>();
		public List<Models.Arc> ArcList = new List<Models.Arc>();
		public bool IsClosed = false;
	}

    public class MathHelper
	{
		#region CoordinateSystem enum

		/// <summary>
		/// Defines the coordinate system reference.
		/// </summary>
		public enum CoordinateSystem
		{
			/// <summary>
			/// World coordinates.
			/// </summary>
			World,
			/// <summary>
			/// Object coordinates.
			/// </summary>
			Object
		}

		#endregion

		/// <summary>
		/// Represents the smallest number.
		/// </summary>
		public const double Epsilon = 0.000000000001;
		/// <summary>
		/// Defines the max number of decimals of an angle. Trigonometric functions are very prone to round off errors.
		/// </summary>
		public const int MaxAngleDecimals = 12;
		/// <summary>
		/// Constant to transform an angle between degrees and radians.
		/// </summary>
		public const double DegToRad = Math.PI / 180.0;

		/// <summary>
		/// Constant to transform an angle between degrees and radians.
		/// </summary>
		public const double RadToDeg = 180.0 / Math.PI;

		/// <summary>
		/// PI/2 (90 degrees)
		/// </summary>
		public const double HalfPI = Math.PI * 0.5;

		/// <summary>
		/// 2*PI (360 degrees)
		/// </summary>
		public const double TwoPI = 2 * Math.PI;

		/// <summary>
		/// Checks if a number is close to one.
		/// </summary>
		/// <param name="number">Simple precision number.</param>
		/// <param name="threshold">Tolerance.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		public static bool IsOne(float number, float threshold)
		{
			return IsZero(number - 1, threshold);
		}

		/// <summary>
		/// Checks if a number is close to one.
		/// </summary>
		/// <param name="number">Simple precision number.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		/// <remarks>By default a tolerance of the constant float.Epsilon will be used.</remarks>
		public static bool IsOne(float number)
		{
			return IsZero(number - 1);
		}

		/// <summary>
		/// Checks if a number is close to one.
		/// </summary>
		/// <param name="number">Double precision number.</param>
		/// <param name="threshold">Tolerance.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		public static bool IsOne(double number, double threshold)
		{
			return IsZero(number - 1, threshold);
		}

		/// <summary>
		/// Checks if a number is close to one.
		/// </summary>
		/// <param name="number">Double precision number.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		/// <remarks>By default a tolerance of the constant double.Epsilon will be used.</remarks>
		public static bool IsOne(double number)
		{
			return IsZero(number - 1);
		}

		/// <summary>
		/// Checks if a number is close to zero.
		/// </summary>
		/// <param name="number">Simple precision number.</param>
		/// <param name="threshold">Tolerance.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		public static bool IsZero(float number, float threshold)
		{
			return (number >= -threshold && number <= threshold);
		}

		/// <summary>
		/// Checks if a number is close to zero.
		/// </summary>
		/// <param name="number">Simple precision number.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		/// <remarks>By default a tolerance of the constant float.Epsilon will be used.</remarks>
		public static bool IsZero(float number)
		{
			return IsZero(number, float.Epsilon);
		}

		/// <summary>
		/// Checks if a number is close to zero.
		/// </summary>
		/// <param name="number">Double precision number.</param>
		/// <param name="threshold">Tolerance.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		public static bool IsZero(double number, double threshold)
		{
			return number >= -threshold && number <= threshold;
		}

		/// <summary>
		/// Checks if a number is close to zero.
		/// </summary>
		/// <param name="number">Double precision number.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		/// <remarks>By default a tolerance of the constant double.Epsilon will be used.</remarks>
		public static bool IsZero(double number)
		{
			return IsZero(number, double.Epsilon);
		}

		/// <summary>
		/// Checks if a number is equal to another.
		/// </summary>
		/// <param name="a">Simple precision number.</param>
		/// <param name="b">Simple precision number.</param>
		/// <param name="threshold">Tolerance.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		public static bool IsEqual(float a, float b, float threshold)
		{
			return IsZero(a - b, threshold);
		}

		/// <summary>
		/// Checks if a number is equal to another.
		/// </summary>
		/// <param name="a">Double precision number.</param>
		/// <param name="b">Double precision number.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		/// <remarks>By default a tolerance of the constant float.Epsilon will be used.</remarks>
		public static bool IsEqual(float a, float b)
		{
			return IsZero(a - b);
		}

		/// <summary>
		/// Checks if a number is equal to another.
		/// </summary>
		/// <param name="a">Double precision number.</param>
		/// <param name="b">Double precision number.</param>
		/// <param name="threshold">Tolerance.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		public static bool IsEqual(double a, double b, double threshold)
		{
			return IsZero(a - b, threshold);
		}

		/// <summary>
		/// Checks if a number is equal to another.
		/// </summary>
		/// <param name="a">Double precision number.</param>
		/// <param name="b">Double precision number.</param>
		/// <returns>True if its close to one or false in anyother case.</returns>
		/// <remarks>By default a tolerance of the constant float.Epsilon will be used.</remarks>
		public static bool IsEqual(double a, double b)
		{
			return IsZero(a - b);
		}

		/// <summary>
		/// Transforms a point between coordinate systems.
		/// </summary>
		/// <param name="point">Point to transform.</param>
		/// <param name="zAxis">Object normal vector.</param>
		/// <param name="from">Point coordinate system.</param>
		/// <param name="to">Coordinate system of the transformed point.</param>
		/// <returns>Transormed point.</returns>
		public static Vector3 Transform(Vector3 point, Vector3 zAxis, CoordinateSystem from, CoordinateSystem to)
		{
			Matrix3 trans = ArbitraryAxis(zAxis);
			if (from == CoordinateSystem.World && to == CoordinateSystem.Object)
			{
				trans = trans.Transpose();
				return trans * point;
			}
			if (from == CoordinateSystem.Object && to == CoordinateSystem.World)
			{
				return trans * point;
			}
			return point;
		}

		/// <summary>
		/// Transforms a point list between coordinate systems.
		/// </summary>
		/// <param name="points">Points to transform.</param>
		/// <param name="zAxis">Object normal vector.</param>
		/// <param name="from">Points coordinate system.</param>
		/// <param name="to">Coordinate system of the transformed points.</param>
		/// <returns>Transormed point list.</returns>
		public static IList<Vector3> Transform(IList<Vector3> points, Vector3 zAxis, CoordinateSystem from, CoordinateSystem to)
		{
			Matrix3 trans = ArbitraryAxis(zAxis);
			List<Vector3> transPoints;
			if (from == CoordinateSystem.World && to == CoordinateSystem.Object)
			{
				transPoints = new List<Vector3>();
				trans = trans.Transpose();
				foreach (Vector3 p in points)
				{
					transPoints.Add(trans * p);
				}
				return transPoints;
			}
			if (from == CoordinateSystem.Object && to == CoordinateSystem.World)
			{
				transPoints = new List<Vector3>();
				foreach (Vector3 p in points)
				{
					transPoints.Add(trans * p);
				}
				return transPoints;
			}
			return points;
		}

		/// <summary>
		/// Gets the rotation matrix from the normal vector (extrusion direction) of an entity.
		/// </summary>
		/// <param name="zAxis">Normal vector.</param>
		/// <returns>Rotation matriz.</returns>
		public static Matrix3 ArbitraryAxis(Vector3 zAxis)
		{
			zAxis.Normalize();
			Vector3 wY = Vector3.UnitY;
			Vector3 wZ = Vector3.UnitZ;
			Vector3 aX;

			if ((Math.Abs(zAxis.X) < 1 / 64.0) && (Math.Abs(zAxis.Y) < 1 / 64.0))
				aX = Vector3.CrossProduct(wY, zAxis);
			else
				aX = Vector3.CrossProduct(wZ, zAxis);

			aX.Normalize();

			Vector3 aY = Vector3.CrossProduct(zAxis, aX);
			aY.Normalize();

			return new Matrix3(aX.X, aY.X, zAxis.X, aX.Y, aY.Y, zAxis.Y, aX.Z, aY.Z, zAxis.Z);
		}

		
	}


}


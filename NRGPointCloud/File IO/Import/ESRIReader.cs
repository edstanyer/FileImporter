using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcShapeFile;
using NRG.Models;

namespace NRG.Import
{
	public class ESRIReader
	{
		public DTM ReadFile(string filename)
		{
			DTM mod = new DTM();
			mod.Name = filename;
			ShapeFile shp = new ShapeFile();
			shp.Open(filename);
			shp.MoveFirst();
			while (!shp.EOF)
			{
				bool start = true; 
				foreach (Vertice v in shp.Vertices)
				{
					DTMPoint pt = new DTMPoint();
					if (start == true)
					{
						pt.PointLabel = "BRK/ST";
						start = false;
						pt.Z = 10.0f;
					}
					else
					{
						pt.PointLabel = "BRK";
					}
					pt.X = v.X_Cord;
					pt.Y = v.Y_Cord;
					pt.Z  = 0;//v.Z_Cord;
					mod.Points.Add(pt);
				}
				shp.MoveNext();
			}
			return mod;
		}

	}
}

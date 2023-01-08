using System;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Services;
using NRG.MathsHelpers;
using NRG.Models;
using System.Drawing;
using netDxf;

namespace NRG.Export
{
    public static class ExportLongSection
    {
		#region Methods


		public static bool ExportToDXF(string layerName, string filename, List<Models.SectionPoint> points, Models.Bounds sectionBounds, List<NRG.Models.SectionColour> Colours = null)
		{
			try
			{
				
				
				if (!PrintPointsToDXF(layerName, filename, points, Colours))
					return false;

				return true;
			}
			catch
			{
				return false;
			}
		}


		public static bool ExportToLongSection(string layerName, string filename, List<Models.SectionPoint> points, Models.Bounds sectionBounds, Models.LongSectionSort format)
        {
            try
            {
                switch (format)
                {
                    case Models.LongSectionSort.CHAINAGE:
                        if (!SortByChainage(ref points))
                            return false;
                        break;
                    case Models.LongSectionSort.HEIGHT:
                        if (!SortByHeight(ref points))
                            return false;
                        break;
                    case Models.LongSectionSort.RADIALLY:
                        if (!SortRadially(ref points, sectionBounds, 0))
                            return false;
                        break;
                }

                if (!PrintPointsToFile(layerName, filename, points))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SortRadially(ref List<Models.SectionPoint> points, Models.Bounds sectionBounds, double offset)
        {
            try
            {
                //Please note that z and y are swapped due to the way the section is shown (side on) 
                var centerX = (sectionBounds.Min.X + sectionBounds.Max.X) / 2;
                var centerY = (sectionBounds.Min.Y + sectionBounds.Max.Y) / 2;
                var centerZ = (sectionBounds.Min.Z + sectionBounds.Max.Z) / 2;

                //Calc and set the bearing for all points
                for (int i = 0; i < points.Count; i++)
                {
                    var point = points[i];
                    double brg = 0, dist = 0;
                    Trig.GetBrgAndDist(centerX, centerY, point.X, point.Y, ref brg, ref dist, 1.0);

                    brg += offset;
                    if (brg >= Math.PI * 2)
                        brg -= (Math.PI * 2);

                    point.Brg = brg;
                }

                //Now sort by bearing value
                points = MergeSort(points, Models.LongSectionSort.RADIALLY);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SortRadially(CrossSection section, double offsetBrg, ref BackgroundWorker worker, double centerX, double centerY, double offsetX = 0, double offsetY = 0)
        {
            try
            {
                //Calc and set the bearing for all points
                for (int i = 0; i < section.OriginalPoints.Count; i++)
                {
                    var point = section.OriginalPoints[i];
                    double brg = 0, dist = 0;
                    Trig.GetBrgAndDist(centerX + offsetX, centerY + offsetY, point.X, point.Y, ref brg, ref dist, 1.0);

                    brg += offsetBrg;
                    if (brg >= Math.PI * 2)
                        brg -= (Math.PI * 2);

                    point.Brg = brg;
                }

                if (worker.CancellationPending)
                    return false;


                //Now sort by bearing value
                var points = section.OriginalPoints;
                HeapSort(ref points, points.Count, ref worker);
                section.OriginalPoints = points;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SortByChainage(ref List<Models.SectionPoint> points)
        {
            try
            {
                points = MergeSort(points, Models.LongSectionSort.CHAINAGE);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SortByHeight(ref List<Models.SectionPoint> points)
        {
            try
            {
                points = MergeSort(points, Models.LongSectionSort.HEIGHT);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void HeapSort(ref List<SectionPoint> points, int n, ref BackgroundWorker worker)
        {
            for (int i = n / 2 - 1; i >= 0; i--)
            {
                if (worker.CancellationPending)
                    return;

                Heapify(ref points, n, i);
            }

            for (int i = n-1; i>=0; i--)
            {
                if (worker.CancellationPending)
                    return;

                var temp = points[0];
                points[0] = points[i];
                points[i] = temp;
                Heapify(ref points, i, 0);
            }
        }

        private static void Heapify(ref List<SectionPoint> points, int n, int i)
        {
            int largest = i;
            int left = 2 * i + 1;
            int right = 2 * i + 2;

            if (left < n && points[left].Brg > points[largest].Brg)
                largest = left;

            if (right < n && points[right].Brg > points[largest].Brg)
                largest = right;

            if(largest != i)
            {
                var temp = points[i];
                points[i] = points[largest];
                points[largest] = temp;
                Heapify(ref points, n, largest);
            }
        }


        private static List<Models.SectionPoint> MergeSort(List<Models.SectionPoint> points, Models.LongSectionSort sortOption)
        {
            if (points.Count <= 1)
                return points;

            var left = new List<Models.SectionPoint>();
            var right = new List<Models.SectionPoint>();
            for (int i = 0; i < points.Count; i++)
            {
                if (i < points.Count / 2)
                    left.Add(points[i]);
                else
                    right.Add(points[i]);
            }

            left = MergeSort(left, sortOption);
            right = MergeSort(right, sortOption);

            return Merge(left, right, sortOption);
        }

        private static List<Models.SectionPoint> Merge(List<Models.SectionPoint> left, List<Models.SectionPoint> right, Models.LongSectionSort sortOption)
        {
            var result = new List<Models.SectionPoint>();

            while (left.Count > 0 && right.Count > 0)
            {
                switch (sortOption)
                {
                    case Models.LongSectionSort.CHAINAGE:
                        if (left.First().X <= right.First().X)
                        {
                            result.Add(left.First());
                            left.RemoveAt(0);
                        }
                        else
                        {
                            result.Add(right.First());
                            right.RemoveAt(0);
                        }
                        break;
                    case Models.LongSectionSort.HEIGHT:
                        if (left.First().Z <= right.First().Z)
                        {
                            result.Add(left.First());
                            left.RemoveAt(0);
                        }
                        else
                        {
                            result.Add(right.First());
                            right.RemoveAt(0);
                        }
                        break;
                    case Models.LongSectionSort.RADIALLY:
                        if (left.First().Brg <= right.First().Brg)
                        {
                            result.Add(left.First());
                            left.RemoveAt(0);
                        }
                        else
                        {
                            result.Add(right.First());
                            right.RemoveAt(0);
                        }
                        break;
                }
            }

            //Either left or right have many elements left
            while (left.Count > 0)
            {
                result.Add(left.First());
                left.RemoveAt(0);
            }

            while (right.Count > 0)
            {
                result.Add(right.First());
                right.RemoveAt(0);
            }

            return result;
        }

        private static bool PrintPointsToFile(string layerName, string filename, List<Models.SectionPoint> sortedPoints)
        {
            try
            {
                var writer = new StreamWriter(filename);
                writer.WriteLine(";NRG Long Section File Version 2");
                writer.WriteLine(Environment.NewLine);
                writer.WriteLine("[layers]");
                writer.WriteLine(layerName + "=");
                writer.WriteLine(Environment.NewLine);
                writer.WriteLine("[points]");

                foreach (var point in sortedPoints)
                {
                    var pointString = "\"";
                    pointString += layerName + "\",";
                    pointString += point.Z + ",";
                    pointString += point.Y + ",\"";
                    pointString += layerName + "\", ";
                    pointString += point.X;
                    writer.WriteLine(pointString);
                }

                writer.Flush();
                writer.Close();
                writer = null;

                return true;
            }
            catch
            {
                return false;
            }
        }

		private static bool PrintPointsToDXF(string layerName, string filename, List<Models.SectionPoint> Points, List<SectionColour> Colours = null)
		{
			try
			{
				int idx = -1;
				netDxf.DxfDocument dxf = new DxfDocument();
				foreach (var point in Points)
				{
					idx++;
					netDxf.Entities.Point pt = new netDxf.Entities.Point(point.X, point.Y , point.Z);
					if (Colours== null || idx > Colours.Count)
					{
						pt.Color = new netDxf.AciColor(Color.FromArgb(point.R, point.G, point.B));
						
					}
					else
					{
						SectionColour col = Colours.ElementAt(idx);
						pt.Color = new netDxf.AciColor(Color.FromArgb((int)col.Red , (int)col.Green , (int)col.Blue ));

					}

					dxf.AddEntity(pt);
				}
				dxf.Save(filename);
				return true;
			}
			catch
			{
				return false;
			}
		}

		#endregion
	}
}

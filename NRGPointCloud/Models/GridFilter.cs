using System;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using NRG.Import;

namespace NRG.Models
{
    public class GridFilter
    {
        #region Properties

        private FilterType filterType;
        private object pointSource;
        private double[] args;
        private Dictionary<(int, int, int), (CloudPoint, double)> filteredGrid = new Dictionary<(int, int, int), (CloudPoint, double)>();
        private List<ZonePolygon> selectionZones;
        private List<ZonePolygon> deselectionZones;

        private Bounds filterBounds;
        private double xSp = 0, ySp = 0, zSp = 0;
        private double xSpF = 0, ySpF = 0, zSpF = 0;
        private double hXSp = 0, hYSp = 0, hZSp = 0;
        private int numX = 0, numY = 0, numZ = 0;

        #endregion

        #region Setup

        public GridFilter(FilterType filterTpye, Octree pointSource, double[] args, List<ZonePolygon> selectionZones, List<ZonePolygon> deselectionZones)
        {
            this.filterType = filterTpye;
            this.pointSource = pointSource;
            this.args = args;
            this.selectionZones = selectionZones;
            this.deselectionZones = deselectionZones;
            SetupFilter();
        }

        private void SetupFilter()
        {
            //Run through each selection zone to create the model bounds
            filterBounds = new Bounds(double.MaxValue, double.MaxValue, double.MaxValue, double.MinValue, double.MinValue, double.MinValue);

            foreach (var zone in selectionZones)
                filterBounds.Update(zone.Bounds);

            //Gather spacing info
            GetSpacingFromArgs();
        }

        private void GetSpacingFromArgs()
        {
            switch (filterType)
            {
                case FilterType.FILTER3D:
                    var sp = args[0];
                    xSp = sp;
                    ySp = sp;
                    zSp = sp;
                    hXSp = xSp / 2;
                    hYSp = ySp / 2;
                    hZSp = zSp / 2;
                    xSpF = 1 / xSp;
                    ySpF = 1 / ySp;
                    zSpF = 1 / zSp;
                    break;
                case FilterType.FILTER2D:
                    var x = args[0];
                    var y = args[0];
                    xSp = x;
                    ySp = y;
                    hXSp = xSp / 2;
                    hYSp = ySp / 2;
                    xSpF = 1 / xSp;
                    ySpF = 1 / ySp;
                    break;
                case FilterType.FILTERCUSTOM:
                    var x2 = args[0];
                    var y2 = args[1];
                    var z = args[2];
                    xSp = x2;
                    ySp = y2;
                    zSp = z;
                    hXSp = xSp / 2;
                    hYSp = ySp / 2;
                    hZSp = zSp / 2;
                    xSpF = 1 / xSp;
                    ySpF = 1 / ySp;
                    zSpF = 1 / zSp;
                    break;
            }

            if (xSp == 0)
                numX = 0;
            else
                numX = Math.Ceiling(filterBounds.Size.X / xSp).ToString().Length;

            if (ySp == 0)
                numY = 0;
            else
                numY = Math.Ceiling(filterBounds.Size.Y / ySp).ToString().Length;

            if (zSp == 0)
                numZ = 0;
            else
                numZ = Math.Ceiling(filterBounds.Size.Z / zSp).ToString().Length;
        }

        #endregion

        #region Methods

        public bool DoFilter(ref List<CloudPoint> filteredList, ref BackgroundWorker worker, ref DoWorkEventArgs args)
        {
            try
            {
                if (pointSource.GetType() == typeof(Octree))
                {
                    SetupFilter();
                    FindSelectedPoints(filterBounds, ((Octree)pointSource).Root, ref worker);

                    if (worker.CancellationPending)
                    {
                        args.Cancel = true;
                        return false;
                    }

                    filteredList = filteredGrid.Select(p => p.Value.Item1).ToList();
                    filteredGrid.Clear();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void FindSelectedPoints(Bounds visibleArea, OctreeNode node, ref BackgroundWorker worker)
        {
            if (!node.Bounds.Intersects2D(visibleArea))
                return;

            if (worker.CancellationPending)
                return;

            //Find Points
            FilterSelectedPointsFromNode(node, (Octree)pointSource);

            //Check all children
            if (node.Children != null)
                foreach (var child in node.Children)
                    if (child != null)
                        FindSelectedPoints(visibleArea, child, ref worker);
        }

        private void FilterSelectedPointsFromNode(OctreeNode node, Octree writer)
        {
            if (node == null)
                return;

            var name = node.FilePath;
            var filename = writer.WorkDir + "\\" + "data" + "\\" + "r" + "\\" + name + ".bin";

            if (File.Exists(filename))
            {
                //Load file in
                var polysToCheck = new List<ZonePolygon>();
                var deselectPolysToCheck = new List<ZonePolygon>();

                foreach (var poly in selectionZones)
                {
                    //If bounds of poly are intersect with bounds of node
                    if (node.Bounds.Intersects2D(poly.Bounds))
                        polysToCheck.Add(poly);
                }

                if (polysToCheck.Count <= 0)
                    return;

                foreach (var poly in deselectionZones)
                {
                    if (node.Bounds.Intersects2D(poly.Bounds))
                        deselectPolysToCheck.Add(poly);
                }

                var points = node.LoadIntoStore();

                //Code is duplicated here with the only difference being the last line for the purpose of optimization
                //We do one switch statement then process all points as opposed to processing each point then doing a switch statement before we add each point
                switch(filterType)
                {
                    case FilterType.FILTER3D:
                        foreach(var point in points)
                        {
                            bool cont = false;

                            //Check its selected
                            foreach(var poly in polysToCheck)
                            {
                                if(poly.InPoly2D(point.X, point.Y, poly.Points))
                                {
                                    cont = true;
                                    break;
                                }
                            }

                            if (!cont)
                                continue;

                            //Check its not deselected
                            foreach(var poly in deselectPolysToCheck)
                            {
                                if(poly.InPoly2D(point.X, point.Y, poly.Points))
                                {
                                    cont = false;
                                    break;
                                }
                            }

                            if (!cont)
                                continue;

                            Add3DFilterPoint(point);
                        }
                        return;
                    case FilterType.FILTER2D:
                        foreach(var point in points)
                        {
                            bool cont = false;

                            //Check its selected
                            foreach(var poly in polysToCheck)
                            {
                                if(poly.InPoly2D(point.X, point.Y, poly.Points))
                                {
                                    cont = true;
                                    break;
                                }
                            }

                            if (!cont)
                                continue;

                            //Check its not deselected
                            foreach(var poly in deselectPolysToCheck)
                            {
                                if(poly.InPoly2D(point.X, point.Y, poly.Points))
                                {
                                    cont = false;
                                    break;
                                }
                            }

                            if (!cont)
                                continue;

                            Add2DFilterPoint(point);
                        }
                        return;
                    case FilterType.FILTERCUSTOM:
                        foreach(var point in points)
                        {
                            bool cont = false;

                            //Check its selected
                            foreach(var poly in polysToCheck)
                            {
                                if(poly.InPoly2D(point.X, point.Y, poly.Points))
                                {
                                    cont = true;
                                    break;
                                }
                            }

                            if (!cont)
                                continue;

                            //Check its not deselected
                            foreach(var poly in deselectPolysToCheck)
                            {
                                if(poly.InPoly2D(point.X, point.Y, poly.Points))
                                {
                                    cont = false;
                                    break;
                                }
                            }

                            if (!cont)
                                continue;

                            AddCustomFilterPoint(point);
                        }
                        return;
                }
            }
        }

        private void ConvertDictionaryToList(ref List<CloudPoint> filteredPoints)
        {
            filteredPoints = filteredGrid.Select(p => p.Value.Item1).ToList();
            filteredGrid.Clear();
        }

        private void Add3DFilterPoint(CloudPoint point)
        {
            //Calc x, y and z index
            int xI = 0, yI = 0, zI = 0;

            if (numX != 0)
                xI = (int)Math.Round((point.X - filterBounds.Min.X) * xSpF);

            if (numY != 0)
                yI = (int)Math.Round((point.Y - filterBounds.Min.Y) * ySpF);

            if (numZ != 0)
                zI = (int)Math.Round((point.Z - filterBounds.Min.Z) * zSpF);

            var newX = ((xSp * xI) + hXSp) - (point.X - filterBounds.Min.X);
            var newY = ((ySp * yI) + hYSp) - (point.Y - filterBounds.Min.Y);
            var newZ = ((zSp * zI) + hZSp) - (point.Z - filterBounds.Min.Z);
            var newDist = newX * newX + newY * newY + newZ * newZ;

            var key = (xI, yI, zI);
            if (filteredGrid.TryGetValue(key, out (CloudPoint, double) value))
            {
                if (newDist < value.Item2)
                    filteredGrid[key] = (point, newDist);
            }
            else
                filteredGrid.Add(key, (point, newDist));
        }

        private void Add2DFilterPoint(CloudPoint point)
        {
            //Calc x and y index
            int xI = 0, yI = 0;

            if (numX != 0)
                xI = (int)Math.Round((point.X - filterBounds.Min.X) * xSpF);

            if (numY != 0)
                yI = (int)Math.Round((point.Y - filterBounds.Min.Y) * ySpF);

            var newX = ((xSp * xI) + hXSp) - (point.X - filterBounds.Min.X);
            var newY = ((ySp * yI) + hYSp) - (point.Y - filterBounds.Min.Y);
            var newDist = newX * newX + newY * newY;

            var key = (xI, yI, 0);
            if (filteredGrid.TryGetValue(key, out (CloudPoint, double) value))
            {
                if (newDist < value.Item2)
                    filteredGrid[key] = (point, newDist);
            }
            else
                filteredGrid.Add(key, (point, newDist));
        }

        private void AddCustomFilterPoint(CloudPoint point)
        {
            //Calc new x, y and z index
            int xI = 0, yI = 0, zI = 0;

            if (numX != 0)
                xI = (int)Math.Round((point.X - filterBounds.Min.X) * xSpF);

            if (numY != 0)
                yI = (int)Math.Round((point.Y - filterBounds.Min.Y) * ySpF);

            if (numZ != 0)
                zI = (int)Math.Round((point.Z - filterBounds.Min.Z) * zSpF);

            var newX = ((xSp * xI) + hXSp) - (point.X - filterBounds.Min.X);
            var newY = ((ySp * yI) + hYSp) - (point.Y - filterBounds.Min.Y);
            var newZ = ((zSp * zI) + hZSp) - (point.Z - filterBounds.Min.Z);
            double newDist = 0;

            if (numX != 0)
                newDist += (newX * newX);
            if (numY != 0)
                newDist += (newY * newY);
            if (numZ != 0)
                newDist += (newZ * newZ);

            var key = (xI, yI, zI);
            if (filteredGrid.TryGetValue(key, out (CloudPoint, double) value))
            {
                if (newDist < value.Item2)
                    filteredGrid[key] = (point, newDist);
            }
            else
                filteredGrid.Add(key, (point, newDist));
        }

        #endregion
    }
}

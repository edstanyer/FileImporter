using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using NRG.Settings;
using NRG.MathsHelpers;

namespace NRG.Models
{
	public class SectionColour
	{
		public float Red { get; set; }
		public float Green { get; set; }
		public float Blue { get; set; }

	}

	public class CrossSection
    {
        #region Properties

        public List<SectionPoint> OriginalPoints { get; set; }
        public List<SectionPoint> CalculationPoints { get; set; }
        public CloudPoint ClosestPoint { get; set; }
        public double Chainage { get; set; }
        public Point2D SectionCenterLeft { get; set; }
        public Point2D SectionCenterRight { get; set; }
        public Point2D SectionCenter { get; set; }
        public Point2D StringCenter { get; set; }
        public double Cant { get; set; }
        public double Bearing { get; set; }
        public ClippingBox ClippingBox { get; set; }
        public Bounds SectionBounds { get; set; }
        public Bounds MaxBounds { get; set; }
        public List<ClearancePolygon> Clearances { get; set; }
        public ColorType SectionColor { get; set; }
        public double SourceMinZ { get; set; }
        public double SourceMaxZ { get; set; }

        //New Properties for Shape Control
        public Shape DesignShape { get; set; }
        public Shape DynamicShape { get; set; }

        public List<Point2D> DesignPoints { get; set; }
        public List<Point2D> FittedDesignPoints { get; set; }

        public List<Point2D> DynamicPoints { get; set; }
        public List<Point2D> FittedDynamicPoints { get; set; }

        public List<double> DesignOffsets { get; set; }
        public List<double> FittedDesignOffsets { get; set; }

        public List<Point2D> LeftRail { get; set; }
        public List<Point2D> RightRail { get; set; }

        public List<Point2D> FittedLeftRail { get; set; }
        public List<Point2D> FittedRightRail { get; set; }

        public List<Point2D> BestFitCircle { get; set; }
        public Point2D BestFitCenter { get; set; }
        public double BestFitRadius { get; set; }
        public double CurScaleX { get; set; }
        public double CurScaleY { get; set; }
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public string Alignment { get; set; }
        public bool Print { get; set; }
        public string Name { get; set; }
        public string File { get; set; }
        public Dictionary<string , string> Data { get; set; }
        public Dictionary<(string String, string Fixed), string> Results { get; set; }

        bool RadialSortApplied { get; set; }

        #endregion

        #region Setup

        public CrossSection()
        {
            OriginalPoints = new List<SectionPoint>();
            CalculationPoints = new List<SectionPoint>();
            Chainage = 1E20;
            SectionCenterLeft = new Point2D();
            SectionCenterRight = new Point2D();
            SectionCenter = new Point2D();
            StringCenter = new Point2D();
            Cant = 0;
            Bearing = 0;
            ClippingBox = new ClippingBox();
            SectionBounds = new Bounds();
            MaxBounds = new Bounds();
            Clearances = new List<ClearancePolygon>();

            DesignPoints = new List<Point2D>();
            FittedDesignPoints = new List<Point2D>();

            DynamicPoints = new List<Point2D>();
            FittedDynamicPoints = new List<Point2D>();

            DesignOffsets = new List<double>();
            FittedDesignOffsets = new List<double>();

            BestFitCircle = new List<Point2D>();
            BestFitCenter = new Point2D();
            BestFitRadius = 0;
            Alignment = "N/A";
            Print = false;
            Name = "";
            File = "";

            SectionColor = ColorType.RGB;

            RadialSortApplied = false;
        }

        #endregion

        #region Methods

        public void GetSectionShapes(Alignment alignment, ShapeControl shapeControl, TunnelSettings settings)
        {
            var worker = new BackgroundWorker();
            GetSectionShapes(alignment, shapeControl, settings, ref worker);
        }

        public void GetSectionShapes(Alignment alignment, ShapeControl shapeControl, TunnelSettings settings, ref BackgroundWorker worker)
        {
            CalculationPoints = new List<SectionPoint>();
            Clearances = new List<ClearancePolygon>();
            DesignPoints = new List<Point2D>();
            DynamicPoints = new List<Point2D>();
            DesignOffsets = new List<double>();
            DesignShape = null;
            DynamicShape = null;
            MaxBounds = new Bounds();

            //If there are no section points we don't need to do anything
            if (OriginalPoints.Count <= 0)
                return;

            //Check if there is an alignement and a shape control
            if (alignment != null && shapeControl != null)
            {
                this.Alignment = alignment.StringName;

                //Find the offset of the center of the section
                var outVec = new AlignmentVector5();
                double sectionChX = 0, sectionChY = 0, gradeLevel = 0;
                if (alignment.Horizontal.GetChainageAndOffset(SectionCenter, ref outVec))
                {
                    if (Chainage == 1E20)
                        Chainage = outVec.chainage;

                    var centerCh = alignment.Horizontal.CoordsAtChAndOffset(Chainage, 0, out double tang);
                    Trig.DistanceAndOffsetFromLine(SectionCenter.X, SectionCenter.Y, Bearing, centerCh.X, centerCh.Y, ref sectionChX, ref sectionChY);
                    gradeLevel = alignment.Vertical.GradeLevel(Chainage, out double vAngle);
                    StringCenter = new Point2D(sectionChX, gradeLevel);
                    Cant = alignment.GetCantAtChainage(Chainage);

                    //Check if section is left to right
                    var leftOutvec = new AlignmentVector5();
                    var rightOutvec = new AlignmentVector5();

                    alignment.Horizontal.GetChainageAndOffset(SectionCenterLeft, ref leftOutvec);
                    alignment.Horizontal.GetChainageAndOffset(SectionCenterRight, ref rightOutvec);

                    if (leftOutvec.offset > rightOutvec.offset)
                        Cant *= -1;

                    DesignShape = shapeControl.GetShape(outVec.chainage, alignment, gradeLevel, Cant, vAngle, ShapeType.Design);
                    DynamicShape = shapeControl.GetShape(outVec.chainage, alignment, gradeLevel, Cant, vAngle, ShapeType.DynamicProfile);

                    //There are no shapes so we don't need to continue
                    if (DesignShape == null && DynamicShape == null)
                    {
                        CalculationPoints = OriginalPoints;
                        return;
                    }

                    //Sort section radially
                    if (!RadialSortApplied)
                        Export.ExportLongSection.SortRadially(this, Math.PI, ref worker, StringCenter.X + (DesignShape?.DispXOffset ?? 0), StringCenter.Y + (DesignShape?.DispYOffset ?? 0));

                    RadialSortApplied = true;

                    if (worker.CancellationPending)
                        return;

                    //Check if the section should be perpendicular to the vertical alignment
                    bool isPerpToVA = false;
                    if (DesignShape != null && DesignShape.config.YVert != 0)
                        isPerpToVA = true;
                    else if (DynamicShape != null && DynamicShape.config.YVert != 0)
                        isPerpToVA = true;

                    if (isPerpToVA)
                    {
                        //We assume since the perpToVA setting is set that points are found as such. So shift them to be perpendicular to horizontal alignment to match the shape
                        foreach(var point in OriginalPoints)
                        {
                            //Center is 0, grade
                            double startX = point.Z, startY = point.Y - gradeLevel, newX = 0, newY = 0;
                            double distFromCenter = Vector.FnDistance(0, 0, startX, startY);

                            if (startY >= 0)
                                Trig.PRC(0, 0, -vAngle, distFromCenter, ref newX, ref newY);
                            else
                                Trig.PRC(0, 0, Math.PI + vAngle, distFromCenter, ref newX, ref newY);

                            var newPoint = point.Copy();
                            newPoint.Z = newX;
                            newPoint.Y = newY;
                            CalculationPoints.Add(newPoint);
                        }
                    }
                    else
                        CalculationPoints = OriginalPoints;

                    if (worker.CancellationPending)
                        return;

                    //Calc Design
                    if (DesignShape != null)
                    {
                        LeftRail = DesignShape.LeftRail;
                        RightRail = DesignShape.RightRail;

                        if (worker.CancellationPending)
                            return;

                        //Calc Design Points
                        DesignPoints = DesignShape.ExplodeShape(settings.StepSize, out List<(int StartIndex, int EndIndex)> DesignShapeElementIndex);
                        foreach (var point in DesignPoints)
                            MaxBounds.Update(point);

                        if (DesignPoints.Count < 2)
                            return;

                        //Adjust the first point if its a duplicate
                        if (DesignPoints.First().X == DesignPoints.Last().X && DesignPoints.First().Y == DesignPoints.Last().Y)
                        {
                            DesignPoints.Remove(DesignPoints.Last());
                            DesignShapeElementIndex[DesignShapeElementIndex.Count - 1] = (DesignShapeElementIndex.Last().StartIndex, DesignPoints.Count - 1);
                        }

                        if (worker.CancellationPending)
                            return;

                        //Calc Design Offsets
                        DesignOffsets = new List<double>(CalculationPoints.Count);

                        foreach (var point in CalculationPoints)
                        {
                            //Update max bounds with each section point to display
                            MaxBounds.Update(point);
                            double dist = 0, offset = 0;
                            DesignShape.DistanceAndOffsetFromShape(point.X, point.Y, ref dist, ref offset);

                            if (Geometry.InPoly2D(DesignPoints, point.X, point.Y))
                                offset *= -1;

                            DesignOffsets.Add(offset);
                        }

                        if (worker.CancellationPending)
                            return;

                        CalculateClearances(DesignPoints, DesignShapeElementIndex, DesignShape, settings, ref worker);

                        if (worker.CancellationPending)
                            return;
                    }

                    //Calc Dynamic
                    if (DynamicShape != null)
                    {
                        if (worker.CancellationPending)
                            return;

                        //Calc Dynamic Points
                        DynamicPoints = DynamicShape.ExplodeShape(settings.StepSize);

                        if (DynamicPoints.Count >= 2)
                        {
                            //Adjust the first Point if its a duplicate
                            if (DynamicPoints.First().X == DynamicPoints.Last().X && DynamicPoints.First().Y == DynamicPoints.Last().Y)
                                DynamicPoints.Remove(DynamicPoints.Last());

                            if (worker.CancellationPending)
                                return;
                        }
                    }

                    CalculateBestFit(settings);
                    CalculateFittedShapes(alignment, shapeControl, settings);
                }
            }
            else
                CalculationPoints = OriginalPoints;
        }

		private int FindLineCircleIntersections(
		float cx, float cy, float radius,
		Point2D point1, Point2D point2,
		out Point2D intersection1, out Point2D intersection2)
		{
			double dx, dy, A, B, C, det, t;

			dx = point2.X - point1.X;
			dy = point2.Y - point1.Y;

			A = dx * dx + dy * dy;
			B = 2 * (dx * (point1.X - cx) + dy * (point1.Y - cy));
			C = (point1.X - cx) * (point1.X - cx) +
				(point1.Y - cy) * (point1.Y - cy) -
				radius * radius;

			det = B * B - 4 * A * C;
			if ((A <= 0.0000001) || (det < 0))
			{
				// No real solutions.
				intersection1 = new Point2D(double.NaN, double.NaN);
				intersection2 = new Point2D(double.NaN, double.NaN);
				return 0;
			}
			else if (det == 0)
			{
				// One solution.
				t = -B / (2 * A);
				intersection1 = new Point2D(point1.X + t * dx, point1.Y + t * dy);
				intersection2 = new Point2D(float.NaN, float.NaN);
				return 1;
			}
			else
			{
				// Two solutions.
				t = (double)((-B + Math.Sqrt(det)) / (2 * A));
				intersection1 =new Point2D(point1.X + t * dx, point1.Y + t * dy);
				t = (double)((-B - Math.Sqrt(det)) / (2 * A));
				intersection2 =new Point2D (point1.X + t * dx, point1.Y + t * dy);
				return 2;
			}
		}

		public void CalculateClearances(List<Point2D> calculationShapePoints, List<(int StartIndex, int EndIndex)> calcShapeElementIndex, Shape calculationShape, TunnelSettings settings, ref BackgroundWorker worker)
        {
            Clearances = new List<ClearancePolygon>();
            IEnumerable<Point2D> points;

            if (settings.ApplyCurveFitting && CalculationPoints.Count >= 3)
            {
                points = Geometry.CalculateSplineCoords(CalculationPoints, settings.StepSize, ref worker);
                foreach (var p in points)
                    MaxBounds.Update(p);
            }
            else
                points = CalculationPoints;

            if (points == null || points.Count() <= 1)
                return;

            ClearancePolygon currentClearance = null;
            var lastIntersectPoint = new Point2D();

            int startShapeIndex = 0;
            int endShapeIndex = 0;
            int lastEleID = 0, curEleID = 0;
            double ipX = 0, ipY = 0;

            bool first = true, last = false;
            for(int i = 0; i <= points.Count() - 1;)
            {
                if (worker.CancellationPending)
                    return;

                if (first)
                {
                    first = false;

                    //Setup new clearance zone
                    currentClearance = new ClearancePolygon(DesignOffsets[i] >= 0);

                    //First point so take intersection between point and shape

                    double dist = 0, brg = 0, offset = 0, newX = 0, newY = 0;

                    bool doesIntersect = false;
                    var currentPoint = points.ElementAt(i);
                    calculationShape.DistanceAndOffsetFromShape(currentPoint.X, currentPoint.Y, ref dist, ref offset);
                    calculationShape.CoordsOnShape(dist, 0, ref newX, ref newY);
                    var firstPoint = new Point2D(newX, newY);
                    double startCh = 0;
                    curEleID = calculationShape.FindShapeElement(dist, ref startCh);
                    lastEleID = curEleID;

                    brg = Trig.RPC(firstPoint.X, firstPoint.Y, currentPoint.X, currentPoint.Y);
                    dist = double.MaxValue;

                    //Check for intersection
                    int startIndex = calcShapeElementIndex[lastEleID].StartIndex, endIndex = calcShapeElementIndex[curEleID].EndIndex;
                    if(startIndex > endIndex)
                    {
                        //Loop from 0 to endIndex
                        for (int j = 0; j < endIndex; j++)
                        {
                            var segStart = calculationShapePoints[j];
                            var segEnd = calculationShapePoints[j + 1];
                            var segBrg = Trig.RPC(segStart.X, segStart.Y, segEnd.X, segEnd.Y);

                            var iPoint = Intersection.IntersectTwoBrgLines(currentPoint.X, currentPoint.Y, brg, segStart.X, segStart.Y, segBrg);
                            if (Intersection.FnInBox(segStart.X, segStart.Y, segEnd.X, segEnd.Y, iPoint.X, iPoint.Y))
                            {
                                var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, iPoint.X, iPoint.Y);
                                if (distToIntersection < dist && distToIntersection > 0.00001)
                                {
                                    doesIntersect = true;
                                    dist = distToIntersection;
                                    lastIntersectPoint = new Point2D(iPoint.X, iPoint.Y);
                                    endShapeIndex = j;
                                }
                            }
                        }

                        //Loop from startIndex to end of points
                        for (int j = startIndex; j < calculationShapePoints.Count - 1; j++)
                        {
                            var segStart = calculationShapePoints[j];
                            var segEnd = calculationShapePoints[j + 1];
                            var segBrg = Trig.RPC(segStart.X, segStart.Y, segEnd.X, segEnd.Y);

                            var iPoint = Intersection.IntersectTwoBrgLines(currentPoint.X, currentPoint.Y, brg, segStart.X, segStart.Y, segBrg);
                            if (Intersection.FnInBox(segStart.X, segStart.Y, segEnd.X, segEnd.Y, iPoint.X, iPoint.Y))
                            {
                                var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, iPoint.X, iPoint.Y);
                                if (distToIntersection < dist && distToIntersection > 0.00001)
                                {
                                    doesIntersect = true;
                                    dist = distToIntersection;
                                    lastIntersectPoint = new Point2D(iPoint.X, iPoint.Y);
                                    endShapeIndex = j;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int j = calcShapeElementIndex[lastEleID].StartIndex; j < calcShapeElementIndex[curEleID].EndIndex; j++)
                        {
                            var segStart = calculationShapePoints[j];
                            var segEnd = calculationShapePoints[j + 1];
                            var segBrg = Trig.RPC(segStart.X, segStart.Y, segEnd.X, segEnd.Y);

                            var iPoint = Intersection.IntersectTwoBrgLines(currentPoint.X, currentPoint.Y, brg, segStart.X, segStart.Y, segBrg);
                            if (Intersection.FnInBox(segStart.X, segStart.Y, segEnd.X, segEnd.Y, iPoint.X, iPoint.Y))
                            {
                                var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, iPoint.X, iPoint.Y);
                                if (distToIntersection < dist && distToIntersection > 0.00001)
                                {
                                    doesIntersect = true;
                                    dist = distToIntersection;
                                    lastIntersectPoint = new Point2D(iPoint.X, iPoint.Y);
                                    startShapeIndex = j;
                                }
                            }
                        }
                    }

                    //Intersect first and last
                    if(calculationShapePoints.Count >= 2)
                    {
                        var segStart = calculationShapePoints.Last();
                        var segEnd = calculationShapePoints.First();
                        var segBrg = Trig.RPC(segStart.X, segStart.Y, segEnd.X, segEnd.Y);

                        var iPoint = Intersection.IntersectTwoBrgLines(currentPoint.X, currentPoint.Y, brg, segStart.X, segStart.Y, segBrg);
                        if(Intersection.FnInBox(segStart.X, segStart.Y, segEnd.X, segEnd.Y, iPoint.X, iPoint.Y))
                        {
                            var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, iPoint.X, iPoint.Y);
                            if(distToIntersection < dist && distToIntersection > 0.00001)
                            {
                                doesIntersect = true;
                                dist = distToIntersection;
                                lastIntersectPoint = new Point2D(iPoint.X, iPoint.Y);
                                startShapeIndex = calculationShapePoints.Count - 1;
                            }
                        }
                    }

                    if (doesIntersect)
                        currentClearance.Points.Add(lastIntersectPoint.Copy());
                    else
                    {
                        lastIntersectPoint = firstPoint.Copy();
                        currentClearance.Points.Add(lastIntersectPoint);

                        //We must determine the startshapeindex
                        for(int j = calcShapeElementIndex[lastEleID].StartIndex; j < calcShapeElementIndex[curEleID].EndIndex; j++)
                        {
                            var segStart = calculationShapePoints[j];
                            var segEnd = calculationShapePoints[j + 1];

                            if(Vector.FnOnLine3(segStart.X, segStart.Y, segEnd.X, segEnd.Y, lastIntersectPoint.X, lastIntersectPoint.Y) == 0)
                            {
                                startShapeIndex = j;
                                break;
                            }
                        }
                    }
                }
                else if (last)
                {
                    bool doesIntersect = false;
                    var currentPoint = points.ElementAt(i);

                    currentClearance.IsOverBreak = !Geometry.InPoly2D(calculationShapePoints, currentPoint.X, currentPoint.Y);
                    currentClearance.Points.Add(new Point2D(currentPoint.X, currentPoint.Y));

                    double dist = 0, brg = 0, offset = 0, newX = 0, newY = 0;
                    calculationShape.DistanceAndOffsetFromShape(currentPoint.X, currentPoint.Y, ref dist, ref offset);
                    calculationShape.CoordsOnShape(dist, 0, ref newX, ref newY);

                    var lastPoint = new Point3D(newX, newY, 0);
                    double startCh = 0;
                    curEleID = calculationShape.FindShapeElement(dist, ref startCh);
                    brg = Trig.RPC(lastPoint.X, lastPoint.Y, currentPoint.X, currentPoint.Y);
                    dist = double.MaxValue;


                    //Check for intersection between points and shape
                    int startIndex = calcShapeElementIndex[lastEleID].StartIndex, endIndex = calcShapeElementIndex[curEleID].EndIndex;
                    if(startIndex > endIndex)
                    {
                        //Loop from 0 to endIndex
                        for(int j = 0; j < endIndex; j++)
                        {
                            var segStart = calculationShapePoints[j];
                            var segEnd = calculationShapePoints[j + 1];
                            var segBrg = Trig.RPC(segStart.X, segStart.Y, segEnd.X, segEnd.Y);

                            var iPoint = Intersection.IntersectTwoBrgLines(currentPoint.X, currentPoint.Y, brg, segStart.X, segStart.Y, segBrg);
                            if(Intersection.FnInBox(segStart.X, segStart.Y, segEnd.X, segEnd.Y, iPoint.X, iPoint.Y))
                            {
                                var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, iPoint.X, iPoint.Y);
                                if(distToIntersection < dist && distToIntersection > 0.00001)
                                {
                                    doesIntersect = true;
                                    dist = distToIntersection;
                                    lastIntersectPoint = new Point2D(iPoint.X, iPoint.Y);
                                    endShapeIndex = j;
                                }
                            }
                        }

                        //Loop from startIndex to end of points
                        for(int j = startIndex; j < calculationShapePoints.Count - 1; j++)
                        {
                            var segStart = calculationShapePoints[j];
                            var segEnd = calculationShapePoints[j + 1];
                            var segBrg = Trig.RPC(segStart.X, segStart.Y, segEnd.X, segEnd.Y);

                            var iPoint = Intersection.IntersectTwoBrgLines(currentPoint.X, currentPoint.Y, brg, segStart.X, segStart.Y, segBrg);
                            if(Intersection.FnInBox(segStart.X, segStart.Y, segEnd.X, segEnd.Y, iPoint.X, iPoint.Y))
                            {
                                var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, iPoint.X, iPoint.Y);
                                if(distToIntersection < dist && distToIntersection > 0.00001)
                                {
                                    doesIntersect = true;
                                    dist = distToIntersection;
                                    lastIntersectPoint = new Point2D(iPoint.X, iPoint.Y);
                                    endShapeIndex = j;
                                }
                            }
                        }
                    }
                    else
                    {
                        for(int j = startIndex; j < endIndex; j++)
                        {
                            var segStart = calculationShapePoints[j];
                            var segEnd = calculationShapePoints[j + 1];
                            var segBrg = Trig.RPC(segStart.X, segStart.Y, segEnd.X, segEnd.Y);

                            var iPoint = Intersection.IntersectTwoBrgLines(currentPoint.X, currentPoint.Y, brg, segStart.X, segStart.Y, segBrg);
                            if(Intersection.FnInBox(segStart.X, segStart.Y, segEnd.X, segEnd.Y, iPoint.X, iPoint.Y))
                            {
                                var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, iPoint.X, iPoint.Y);
                                if(distToIntersection < dist && distToIntersection > 0.00001)
                                {
                                    doesIntersect = true;
                                    dist = distToIntersection;
                                    lastIntersectPoint = new Point2D(iPoint.X, iPoint.Y);
                                    endShapeIndex = j;
                                }
                            }
                        }
                    }
                   
                    //Intersect first and last
                    if (calculationShapePoints.Count >= 2)
                    {
                        var segStart = calculationShapePoints.Last();
                        var segEnd = calculationShapePoints.First();
                        var segBrg = Trig.RPC(segStart.X, segStart.X, segEnd.X, segEnd.Y);

                        var iPoint = Intersection.IntersectTwoBrgLines(currentPoint.X, currentPoint.Y, brg, segStart.X, segStart.Y, segBrg);
                        if(Intersection.FnInBox(segStart.X, segStart.Y, segEnd.X, segEnd.Y, iPoint.X, iPoint.Y))
                        {
                            var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, iPoint.X, iPoint.Y);
                            if(distToIntersection < dist && distToIntersection > 0.00001)
                            {
                                doesIntersect = true;
                                dist = distToIntersection;
                                lastIntersectPoint = new Point2D(iPoint.X, iPoint.Y);
                                endShapeIndex = calculationShapePoints.Count - 1;
                            }
                        }
                    }

                    if (doesIntersect)
                        currentClearance.Points.Add(lastIntersectPoint.Copy());

                    //Add the final points
                    var firstPoint = currentClearance.Points.First().Copy();

                    if (startShapeIndex < endShapeIndex)
                    {
                        for (int j = endShapeIndex; j > startShapeIndex; j--)
                            currentClearance.Points.Add(calculationShapePoints[j]);
                    }
                    else if (startShapeIndex > endShapeIndex)
                    {
                        for (int j = endShapeIndex; j != startShapeIndex; j--)
                        {
                            if (j == -1)
                                j = calculationShapePoints.Count - 1;

                            if (j == startShapeIndex)
                                break;

                            currentClearance.Points.Add(calculationShapePoints[j]);
                        }
                    }
                    else if (startShapeIndex == endShapeIndex)
                    {

                        //Add seperate logic for if there is no intersection
                        if(Clearances.Count != 0)
                        {
                            //for (int j = endShapeIndex; j >= 0; j--)
                            //    currentClearance.Points.Add(calculationShapePoints[j]);
                        }
                        else
                        {
                            //There are no previous intersection so we must loop the whole shape
                            //Go from end index to 0
                            for (int j = endShapeIndex; j >= 0; j--)
                                currentClearance.Points.Add(calculationShapePoints[j]);

                            //Go from end of points to start index
                            for (int j = calculationShapePoints.Count - 1; j >= startShapeIndex; j--)
                                currentClearance.Points.Add(calculationShapePoints[j]);
                        }
                    }

                    //Close on the first point
                    currentClearance.Points.Add(firstPoint.Copy());

                    Clearances.Add(currentClearance.Copy());
                    currentClearance = null;
                    i++;
                }
                else
                {
                    double dist = 0, offset = 0;
                    bool doesIntersect = false;
                    var currentPoint = new Point2D();
                    var nextPoint = new Point2D();

                    if(currentClearance.Points.Count == 1 || i == points.Count() -1)
                    {
                        currentPoint = new SectionPoint(lastIntersectPoint.X, lastIntersectPoint.Y, 0, 0, 0, 0);
                        nextPoint = points.ElementAt(i);
                    }
                    else
                    {
                        currentPoint = points.ElementAt(i);
                        nextPoint = points.ElementAt(i + 1);
                    }

                    calculationShape.DistanceAndOffsetFromShape(nextPoint.X, nextPoint.Y, ref dist, ref offset);
                    double startCh = 0;
                    curEleID = calculationShape.FindShapeElement(dist, ref startCh);
                    dist = double.MaxValue;

                    //Check for intersection to next point
                    int startIndex = calcShapeElementIndex[lastEleID].StartIndex, endIndex = calcShapeElementIndex[curEleID].EndIndex;
                    if(startIndex > endIndex)
                    {
                        //Loop from 0 to endIndex
                        for(int j = 0; j < endIndex; j++)
                        {
                            var segStart = calculationShapePoints[j];
                            var segEnd = calculationShapePoints[j + 1];

                            if(Intersection.IntersectLines(currentPoint.X, currentPoint.Y, nextPoint.X, nextPoint.Y, segStart.X, segStart.Y, segEnd.X, segEnd.Y, ref ipX, ref ipY))
                            {
                                var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, ipX, ipY);
                                if(distToIntersection < dist && distToIntersection > 0.00001)
                                {
                                    doesIntersect = true;
                                    dist = distToIntersection;
                                    lastIntersectPoint = new Point2D(ipX, ipY);
                                    endShapeIndex = j;
                                }
                            }
                        }

                        //Loop from start to end of points
                        for(int j = startIndex; j < calculationShapePoints.Count - 1; j++)
                        {
                            var segStart = calculationShapePoints[j];
                            var segEnd = calculationShapePoints[j + 1];

                            if(Intersection.IntersectLines(currentPoint.X, currentPoint.Y, nextPoint.X, nextPoint.Y, segStart.X, segStart.Y, segEnd.X, segEnd.Y, ref ipX, ref ipY))
                            {
                                var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, ipX, ipY);
                                if(distToIntersection < dist && distToIntersection > 0.00001)
                                {
                                    doesIntersect = true;
                                    dist = distToIntersection;
                                    lastIntersectPoint = new Point2D(ipX, ipY);
                                    endShapeIndex = j;
                                }
                            }
                        }
                    }
                    else
                    {
                        for(int j = startIndex; j < endIndex; j++)
                        {
                            var segStart = calculationShapePoints[j];
                            var segEnd = calculationShapePoints[j + 1];

                            if(Intersection.IntersectLines(currentPoint.X, currentPoint.Y, nextPoint.X, nextPoint.Y, segStart.X, segStart.Y, segEnd.X, segEnd.Y, ref ipX, ref ipY))
                            {
                                var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, ipX, ipY);
                                if(distToIntersection < dist && distToIntersection > 0.00001)
                                {
                                    doesIntersect = true;
                                    dist = distToIntersection;
                                    lastIntersectPoint = new Point2D(ipX, ipY);
                                    endShapeIndex = j;
                                }
                            }

                        }
                    }

                    lastEleID = curEleID;

                    //Intersect first and last
                    if (calculationShapePoints.Count >= 2)
                    {
                        var segStart = calculationShapePoints.Last();
                        var segEnd = calculationShapePoints.First();

                        if (Intersection.IntersectLines(currentPoint.X, currentPoint.Y, nextPoint.X, nextPoint.Y, segStart.X, segStart.Y, segEnd.X, segEnd.Y, ref ipX, ref ipY))
                        {
                            var distToIntersection = Vector.FnDistance(currentPoint.X, currentPoint.Y, ipX, ipY);
                            if (distToIntersection < dist && distToIntersection > 0.00001)
                            {
                                doesIntersect = true;
                                dist = distToIntersection;
                                lastIntersectPoint = new Point2D(ipX, ipY);
                                endShapeIndex = calculationShapePoints.Count - 1;
                            }
                        }
                    }

                    if (doesIntersect)
                    {
                        if (currentClearance.Points.Count == 1)
                            currentClearance.Points.Add(lastIntersectPoint.Copy());
                        else
                        {
                            //currentClearance.Points.Add(new Point2D(currentPoint.X, currentPoint.Y));
                            currentClearance.Points.Add(lastIntersectPoint.Copy());
                            i++;
                        }

                        //Add the final points
                        var firstPoint = currentClearance.Points.First().Copy();

                        if(startShapeIndex < endShapeIndex)
                        {
                            for (int j = endShapeIndex; j > startShapeIndex; j--)
                                currentClearance.Points.Add(calculationShapePoints[j]);
                        }
                        else if(startShapeIndex > endShapeIndex)
                        {
                            for(int j = endShapeIndex; j != startShapeIndex; j--)
                            {
                                if (j == -1)
                                    j = calculationShapePoints.Count - 1;

                                if (j == startShapeIndex)
                                    break;

                                currentClearance.Points.Add(calculationShapePoints[j]);
                            }
                        }

                        //Close on the first point
                        currentClearance.Points.Add(firstPoint.Copy());

                        //Close clearance polygon and start the next one
                        Clearances.Add(currentClearance.Copy());

                        //Set overbreak to opposite of last polygon as we have an intersection and therefore swap
                        currentClearance = new ClearancePolygon(!Clearances.Last().IsOverBreak);

                        //Add last intersection point and set startShapeIndex from last intersection
                        currentClearance.Points.Add(lastIntersectPoint.Copy());
                        startShapeIndex = endShapeIndex;
                        endShapeIndex = 0;
                    }
                    else
                    {
                        if(i == points.Count() - 1)
                        {
                            last = true;
                            continue;
                        }

                        if (currentClearance.Points.Count != 1)
                        {
                            var p = points.ElementAt(i + 1);
                            currentClearance.IsOverBreak = !Geometry.InPoly2D(calculationShapePoints, p.X, p.Y);
                            i++;
                        }
                        else
                        {
                            var p = points.ElementAt(i);
                            currentClearance.IsOverBreak = !Geometry.InPoly2D(calculationShapePoints, p.X, p.Y);
                        }

                        currentClearance.Points.Add(new Point2D(nextPoint.X, nextPoint.Y));
                    }
                }
            }
        }

        public void CalculateBestFit(TunnelSettings settings, bool overrideSettings = false, int bestFitMethod = 0)
        {
            var radius = 0D;
            var center = new Point2D();
            BestFitCircle = new List<Point2D>();
            BestFitCenter = new Point2D();
            BestFitRadius = 0;

            if (!overrideSettings)
                bestFitMethod = settings.BestFitMethod;

            if(settings.ExcludeClearences == 0 || settings.ExcludeClearences == 1E20)
            {
                if(bestFitMethod == 0)
                {
                    //Best Fit Circle
                    if (Geometry.BestFitCircle(CalculationPoints, ref center, ref radius))
                    {
                        BestFitCenter = center.Copy();
                        BestFitCenter.Y -= StringCenter.Y;
                        BestFitRadius = radius;

                        BestFitCircle = Geometry.GenerateCircle(BestFitCenter.X, BestFitCenter.Y + StringCenter.Y, BestFitRadius, 60);
                    }
                }
                else if(bestFitMethod == 1)
                {
                    //Best Fit Clearances
                    CalculateBestFitPoints(CalculationPoints, settings);
                }
                else if(bestFitMethod == 1)
                {
                    //No Best Fit
                    BestFitCenter = new Point2D();
                }
            }
            else
            {
                var exludeValue = Math.Abs(settings.ExcludeClearences);
                var includedPoints = new List<SectionPoint>();

                for(int i = 0; i < CalculationPoints.Count && i < DesignOffsets.Count; i++)
                {
                    if (Math.Abs(DesignOffsets[i]) <= exludeValue)
                        includedPoints.Add(CalculationPoints[i]);
                }

                if(bestFitMethod == 0)
                {
                    //Best Fit Circle
                    if(Geometry.BestFitCircle(includedPoints, ref center, ref radius))
                    {
                        BestFitCenter = center.Copy();
                        BestFitCenter.Y -= StringCenter.Y;
                        BestFitRadius = radius;

                        BestFitCircle = Geometry.GenerateCircle(BestFitCenter.X, BestFitCenter.Y + StringCenter.Y, BestFitRadius, 60);
                    }
                }
                else if(bestFitMethod == 1)
                {
                    //Best Fit Clearances
                    CalculateBestFitPoints(includedPoints, settings);
                }
                else if(bestFitMethod == 2)
                {
                    //No Best Fit
                    BestFitCenter = new Point2D();
                }
            }
        }

        public void CalculateBestFitPoints(List<SectionPoint> points, TunnelSettings settings)
        {
            bool done = false;
            int increment = 0;
            BestFitCenter = new Point2D();

            if (DesignShape == null)
                return;

            while(!done)
            {
                double totalDX = 0, totalDY = 0, xWeight = 0, yWeight = 0;

                foreach (var p in points)
                {
                    var shape = DesignShape.FitShape(BestFitCenter.X, BestFitCenter.Y);
                    double dist = 0, offset = 0, shapePointX = 0, shapePointY = 0, dX = 0, dY = 0;
                    shape.DistanceAndOffsetFromShape(p.X, p.Y, ref dist, ref offset);
                    shape.CoordsOnShape(dist, 0, ref shapePointX, ref shapePointY);
                    var brg = Trig.RPC(shapePointX, shapePointY, p.X, p.Y);
                    Trig.PRC(0, 0, brg, Math.Abs(offset), ref dX, ref dY);

                    totalDX += dX;
                    if (settings.HoldVerticalAlignment)
                        dY = 0;
                    totalDY += dY;

                    if (Vector.FnEquals(dX, 0))
                        xWeight += 1;
                    else
                        xWeight += Math.Abs(Math.Sin(brg));

                    if (Vector.FnEquals(dY, 0))
                        yWeight += 1;
                    else
                        yWeight += Math.Abs(Math.Cos(brg));
                }

                var finalDX = totalDX / xWeight;
                var finalDy = totalDY / yWeight;

                BestFitCenter.X += finalDX;
                BestFitCenter.Y += finalDy;

                if (Vector.FnEquals(finalDX, 0) && Vector.FnEquals(finalDy, 0))
                    done = true;

                increment++;

                //Cancel if we have incremented too much
                if (increment >= 10)
                    done = true;
            }

            if (settings.HoldVerticalAlignment)
                BestFitCenter.Y = 0;
        }

        public void CalculateFittedShapes(Alignment alignment, ShapeControl shapeControl, TunnelSettings settings)
        {
            FittedDesignPoints = new List<Point2D>();
            FittedDynamicPoints = new List<Point2D>();

            FittedDesignOffsets = new List<double>();

            //Check if there is an alignment
            if(alignment != null && shapeControl != null && Chainage != 1E20 && settings.BestFitMethod != 2)
            {
                //Fitted Design Shape
                if(DesignShape != null)
                {
                    //Fitted Design Points
                    Point2D fittedDesignShapeCenter;

                    if (BestFitCircle.Count <= 0)
                        fittedDesignShapeCenter = new Point2D(BestFitCenter.X, BestFitCenter.Y);
                    else
                        fittedDesignShapeCenter = new Point2D(BestFitCenter.X - (DesignShape.ShapeCentre.X - StringCenter.X), (BestFitCenter.Y - StringCenter.Y) - (DesignShape.ShapeCentre.Y - StringCenter.Y));

                    if (settings.HoldVerticalAlignment)
                        fittedDesignShapeCenter.Y = 0;

                    var fittedDesignShape = DesignShape.FitShape(fittedDesignShapeCenter.X, fittedDesignShapeCenter.Y);
                    FittedDesignPoints = fittedDesignShape.ExplodeDashedShape();

                    FittedLeftRail = fittedDesignShape.LeftRail;
                    FittedRightRail = fittedDesignShape.RightRail;

                    //Fitted Design Offsets
                    FittedDesignOffsets = new List<double>(CalculationPoints.Count);
                    foreach(var point in CalculationPoints)
                    {
                        double dist = 0, offset = 0;
                        fittedDesignShape.DistanceAndOffsetFromShape(point.X, point.Y, ref dist, ref offset);
                        if (Geometry.InPoly2D(FittedDesignPoints, point.X, point.Y))
                            offset *= -1;

                        FittedDesignOffsets.Add(offset);
                    }
                }

                //Fitted Dynamic Shape
                if(DynamicShape != null)
                {
                    Point2D fittedDynamicShapeCenter;

                    //Fitted Dynamic Points
                    if (BestFitCircle.Count <= 0)
                        fittedDynamicShapeCenter = new Point2D(BestFitCenter.X, BestFitCenter.Y);
                    else
                        fittedDynamicShapeCenter = new Point2D(BestFitCenter.X - (DesignShape.ShapeCentre.X - StringCenter.X), (BestFitCenter.Y - StringCenter.Y) - (DesignShape.ShapeCentre.Y - StringCenter.Y));

                    if (settings.HoldVerticalAlignment)
                        fittedDynamicShapeCenter.Y = 0;

                    var fittedDynamicShape = DynamicShape.FitShape(fittedDynamicShapeCenter.X, fittedDynamicShapeCenter.Y);
                    FittedDynamicPoints = fittedDynamicShape.ExplodeDashedShape();
                }
            }
        }

        /// <summary>
        /// Finds the point within the <see cref="CrossSection"/> that falls closest to the <see cref="Ray"/>
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> to check each of the <see cref="CrossSection.CalculationPoints"/> distance from</param>
        /// <param name="x">A <see cref="double"/> to store the X value of the closest point</param>
        /// <param name="y">A <see cref="double"/> to store the Y value of the closest point</param>
        /// <param name="z">A <see cref="double"/> to store the Z value of the closest point</param>
        /// <param name="r">A <see cref="byte"/> to store the Red color channel of the closest point</param>
        /// <param name="g">A <see cref="byte"/> to store the Green color channel of the closest point</param>
        /// <param name="b">A <see cref="byte"/> to store the Blue color channel of the closest point</param>
        /// <param name="index">A <see cref="int"/> to store the index of the closest point in the <see cref="CrossSection.CalculationPoints"/> list</param>
        /// <returns>Returns true if a closest point is found, otherwise returns false</returns>
        public bool FindClosestPointOnRay(Ray ray, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref int index)
        {
            if (CalculationPoints == null || CalculationPoints.Count <= 0)
                return false;

            index = 0;
            var distance = double.MaxValue;

            //Iterate through all the points and calculate the distance from the ray
            for(int i = 0; i < CalculationPoints.Count; i++)
            {
                var point = CalculationPoints[i];
                var newDist = Vector.ClosestDistanceOnALine(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.End.X, ray.End.Y, ray.End.Z, point.X, point.Y, point.Z);

                //If the distance is smallest, set the closest point to be the current point
                if(newDist < distance)
                {
                    x = point.X;
                    y = point.Y;
                    z = point.Z;
                    r = point.R;
                    g = point.G;
                    b = point.B;
                    distance = newDist;
                    index = i;
                }
            }

            return true;
        }

        /// <summary>
        /// Finds the point within the <see cref="CrossSection"/> that falls closest to the <see cref="Ray"/>
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> to check each of the <see cref="CrossSection.CalculationPoints"/> distance from</param>
        /// <param name="x">A <see cref="double"/> to store the X value of the closest point</param>
        /// <param name="y">A <see cref="double"/> to store the Y value of the closest point</param>
        /// <param name="z">A <see cref="double"/> to store the Z value of the closest point</param>
        /// <param name="r">A <see cref="byte"/> to store the Red color channel of the closest point</param>
        /// <param name="g">A <see cref="byte"/> to store the Green color channel of the closest point</param>
        /// <param name="b">A <see cref="byte"/> to store the Blue color channel of the closest point</param>
        /// <returns>Returns true if a closest point is found, otherwise returns false</returns>
        public bool FindClosestPointOnRay(Ray ray, ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b)
        {
            int index = 0;
            return FindClosestPointOnRay(ray, ref x, ref y, ref z, ref r, ref g, ref b, ref index);
        }

        #endregion

        #region Bounds Generation

        /// <summary>
        /// Generate the bounds of the <see cref="CrossSection.ClippingBox"/> based on a give center point, width, height and bearing
        /// </summary>
        /// <param name="cX">The X value of the center of the bounds</param>
        /// <param name="cY">The Y value of the center of the bounds</param>
        /// <param name="width">The length from the center of the bounds</param>
        /// <param name="height">The height from the center of the bounds</param>
        /// <param name="bearing">The 2D bearing of the bounds</param>
        public void GenerateBoundsFromPoints(double cX, double cY, double width, double height, double bearing)
        {
            double deg90 = Math.PI / 2, deg180 = Math.PI;
            double ctx = 0, cty = 0, ltx = 0, lty = 0, rtx = 0, rty = 0;
            double cbx = 0, cby = 0, lbx = 0, lby = 0, rbx = 0, rby = 0;

            //Calculate the center top and bottom points
            Trig.PRC(cX, cY, bearing - deg90, height / 2, ref ctx, ref cty);
            Trig.PRC(cX, cY, bearing + deg90, height / 2, ref cbx, ref cby);

            //Calculate the left and right top and bottom points
            Trig.PRC(ctx, cty, bearing - deg180, width / 2, ref ltx, ref lty);
            Trig.PRC(cbx, cby, bearing - deg180, width / 2, ref lbx, ref lby);
            Trig.PRC(ctx, cty, bearing, width / 2, ref rtx, ref rty);
            Trig.PRC(cbx, cby, bearing, width / 2, ref rbx, ref rby);

            //Calculate the center left point and center right point of the bounds
            double clpx = 0, clpy = 0, crpx = 0, crpy = 0;
            Trig.PRC(cX, cY, bearing - deg180, width / 2, ref clpx, ref clpy);
            Trig.PRC(cX, cY, bearing, width / 2, ref crpx, ref crpy);

            //Set the cross section variables
            SectionCenterLeft = new Point2D(clpx, clpy);
            SectionCenterRight = new Point2D(crpx, crpy);
            SectionCenter = new Point2D(cX, cY);

            //Set the clipping box bounds to be those calculated
            ClippingBox.Points = new List<Point2D>();
            ClippingBox.Points.Add(new Point2D(rbx, rby));
            ClippingBox.Points.Add(new Point2D(rtx, rty));
            ClippingBox.Points.Add(new Point2D(ltx, lty));
            ClippingBox.Points.Add(new Point2D(lbx, lby));
        }
        
        /// <summary>
        /// Generate the bounds of the <see cref="CrossSection.ClippingBox"/> based on a given line and sweep distance
        /// </summary>
        /// <param name="lineStart">A <see cref="Point2D"/> representing the start of the line</param>
        /// <param name="lineEnd">A <see cref="Point2D"/> representing the end of the line</param>
        /// <param name="Sweep">The sweep distance to generate the bounds from</param>
		/// 
		/// Note  from ES:
		/// 							^
		/// 							|
		///	   Line Start				|Sweep								Line End
		///		+----------------------------------------------------------+
		///								|
		///								|
		///								^
		///	Sweep distance is the full depth of the bounding box, not half the depth as often assumed
		///	I've changed the parameter which was originally called distance to Sweep to avoid confusion

        public void GenerateBoundsFromLineAndSweepDistance(Point2D lineStart, Point2D lineEnd, double Sweep)
        {
            var center = (lineStart + lineEnd) / 2;
            double brg = 0, length = 0;

            //Get the bearing of the line
            Trig.RPC(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, ref brg, ref length);
            GenerateBoundsFromPoints(center.X, center.Y, length, Sweep, brg);
        }
        #endregion
    }
}

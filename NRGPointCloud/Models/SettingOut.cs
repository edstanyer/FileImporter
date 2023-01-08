using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRG.Models
{
    public class SettingOut
    {
        #region Internal Properties
        private Alignment CurrentAlignment { get; set; }
        private DTM BackgroundModel { get; set; }
        private DTMPoint CurrentPoint { get; set; }

        //Tunnel Settings
        private double AlignmentOffsetLevel { get; set; }
        private double Max3DDistance { get; set; }
        private bool Max3DDistanceEnabled { get; set; }
        //

        private DTMPoint _nearestBMPoint { get; set; } //Do not interact with this directly, use [DTMPoint] NearestBMPoint
        private DTMPoint _nearest3DBMPoint { get; set; } //Do not interact with this directly, use [DTMPoint] Nearest3DBMPoint
        private double _chainage { get; set; }//Do not interact with this directly, use "Chainage"
        private double _offset { get; set; }//Do not interact with this directly, use "Offset"
        private AlignmentVector5 _outVec { get; set; }//Do not interact with this directly, use "OutVec"

        private AlignmentVector5 OutVec { get { GetChainageAndOffset(false, forceOverride); return _outVec; } } //Call GetChainageAndOffset to see if Outvec needs updating
        private DTMPoint NearestBMPoint { get { return FindNearestBMPoint(); } set { _nearestBMPoint = value; } }
        private DTMPoint Nearest3DBMPoint { get { return FindNearest3DBMPoint(); } set { _nearest3DBMPoint = value; } }


        private bool recalc = false; //The main flag which decides wether we are going to attempt to perform a recalculation or not.
        private bool forceOverride = false; //Flag to forcefully cause a recalc regardless as to wether the point has moved previously
        private bool nearestPointUpdated = false; //Flags to ensure the nearest point and chainage&offset aren't calculated several times during the same operation
        private bool nearest3DPointUpdated = false; //Flags to ensure the nearest(3D/2D) point and chainage&offset aren't calculated several times during the same operation
        private bool chainageAndOffsetUpdated = false; //The flags are the secondary measures when the point is flagged as "Moved".


        private double _horizontalRadius { get; set; }
        private bool _horizontalRadiusCalculated { get; set; }
        private double _verticalAlignmentLevel { get; set; }
        private bool _verticalAlignmentLevelCalculated { get; set; }
        private double _horizontalVectorDistancetoNearestBMPoint { get; set; }
        private bool _horizontalVectorDistancetoNearestBMPointCalculated { get; set; }
        private double _heightDifferenceFromNearestBMPoint { get; set; }
        private bool _heightDifferenceFromNearestBMPointCalculated { get; set; }
        private double _3dVectorDistancetoNearestBMPoint { get; set; }
        private bool _3dVectorDistancetoNearestBMPointCalculated { get; set; }
        private double _nearestBMPointEasting { get; set; }
        private bool _nearestBMPointEastingCalculated { get; set; }
        private double _nearestBMPointNorthing { get; set; }
        private bool _nearestBMPointNorthingCalculated { get; set; }
        private double _nearestBMPointLevel { get; set; }
        private bool _nearestBMPointLevelCalculated { get; set; }
        private double _heightDifferencetoBM { get; set; }
        private bool _heightDifferencetoBMCalculated { get; set; }
        private double _bMLevel { get; set; }
        private bool _bMLevelCalculated { get; set; }
        private double _radialOffsettoAlignment { get; set; }
        private bool _radialOffsettoAlignmentCalculated { get; set; }
        private double _nearest3DBMPointRadialOffset { get; set; }
        private bool _nearest3DBMPointRadialOffsetCalculated { get; set; }
        private string _nearest3DMBMPointLabel { get; set; }
        private bool _nearest3DMBMPointLabelCalculated { get; set; }
        private double _slopeDistanceFromNearest3DBMPoint { get; set; }
        private bool _slopeDistanceFromNearest3DBMPointCalculated { get; set; }
        private double _lips { get; set; }
        private bool _lipsCalculated { get; set; }
        #endregion

        #region Public Properties
        public double HCR { get; set; }
        public double VCR { get; set; }
        public double SD { get; set; }
        public double TgtHt { get; set; }
        public double Chainage { get { return GetChainageAndOffset(false, forceOverride); } }
        public double Offset { get { return GetChainageAndOffset(true, forceOverride); } }
        public double HorizontalRadius { get{ return GetHorizontalRadius(forceOverride); } }
        public double VerticalAlignmentLevel { get { return GetVerticalAlignmentLevel(forceOverride); } }
        public double HorizontalVectorDistancetoNearestBMPoint { get { return GetHorizontalVectorDistancetoNearestBMPoint(forceOverride); } }
        public double Vector3DDistancetoNearestBMPoint { get { return Get3DVectorDistanceToNearestBMPoint(forceOverride); } }
        public double HeightDifferenceFromNearestBMPoint { get { return GetHeightDifferenceFromNearestBMPoint(forceOverride); } }
        public double HeightDifferencetoBMModel { get { return GetHeightDifferencetoBM(forceOverride); } }
        public double RadialOffsettoAlignment { get { return GetRadialOffsettoAlignment(forceOverride); } }
        public double Nearest3DBGPointRadialOffset { get { return GetRadialOffsettoAlignment(forceOverride, true); } }
        public string Nearest3DBGPointLabel { get { return GetNearest3DBMPointLabel(forceOverride); } }
        public double SlopeDistanceFromNearest3DBGPoint { get { return GetSlopeDistanceFromNearest3DBMPoint(forceOverride); } }
        public double Lips { get { return GetLips(forceOverride); } }
        public double BMLevel { get { return GetBMLevel(forceOverride); } }
        public double NearestBMPointEasting { get { return GetNearestBMPointEasting(forceOverride); } }
        public double NearestBMPointNorthing { get { return GetNearestBMPointNorthing(forceOverride); } }
        public double NearestBMPointLevel { get { return GetNearestBMPointLevel(forceOverride); } }
        #endregion
        #region Setup
        public SettingOut(DTMPoint currentPoint, DTM backgroundModel = null, Alignment currentAlignment = null)
        {
            CurrentPoint = currentPoint;
            BackgroundModel = backgroundModel;
            CurrentAlignment = currentAlignment;
            _chainage = -999;
            _offset = -999;
            AlignmentOffsetLevel = 0;
            Max3DDistance = 0;
            Max3DDistanceEnabled = false;
        }
        #endregion

        #region Methods

        public void UpdateAlignment(Alignment newAlignment)
        {
            if(CurrentAlignment == newAlignment) { return; }

            CurrentAlignment = newAlignment;
            chainageAndOffsetUpdated = false;
            _chainage = -999;
            _offset = -999;
        }
        
        public void UpdateBackgroundModel(DTM newBackgroundModel)
        {
            if (BackgroundModel == newBackgroundModel) { return; }
            
            BackgroundModel = newBackgroundModel;
            nearestPointUpdated = false;
            nearest3DPointUpdated = false;
            _nearestBMPoint = null;
            _nearest3DBMPoint = null;
        }

        public void UpdateAlignmentOffsetLevel(double algOffsetLevel)
        {
            AlignmentOffsetLevel = algOffsetLevel;
        }

        public void UpdateMax3DDistance(double max3DDist)
        {
            Max3DDistance = max3DDist;
        }

        public void UpdateMax3DDistanceEnabled(bool max3dDistEnabled)
        {
            Max3DDistanceEnabled = max3dDistEnabled;
        }

        public void InitialiseRecalc(bool ForceUpdateAll)
        {
            forceOverride = ForceUpdateAll;
            chainageAndOffsetUpdated = false;
            nearestPointUpdated = false;
            nearest3DPointUpdated = false;
            recalc = true;
        }
        //Inbetween calling these two, the grid should be interacted with (i.e LoadGrid)
        public void FinishRecalculating()
        {
            recalc = false;
            CurrentPoint.Moved = false;
            if (_nearestBMPoint != null) { NearestBMPoint.Moved = false; }
            if (_nearest3DBMPoint != null) { Nearest3DBMPoint.Moved = false; }
        }

        private DTMPoint FindNearestBMPoint()
        { 
            
            if (BackgroundModel == null) { return null; }
            
            if (_nearestBMPoint == null || _nearestBMPoint.Moved || CurrentPoint.Moved)
            {
                if (!nearestPointUpdated)
                {
                    _nearestBMPoint = BackgroundModel.GetClosestPoint(CurrentPoint, false, false, false);
                    nearestPointUpdated = true;
                }
            }

            return _nearestBMPoint;
        }

        private DTMPoint FindNearest3DBMPoint()
        {

            if (BackgroundModel == null) { return null; }

            if (_nearest3DBMPoint == null || _nearest3DBMPoint.Moved || CurrentPoint.Moved)
            {
                if (!nearest3DPointUpdated)
                {
                    _nearest3DBMPoint = BackgroundModel.GetClosestPoint3D(CurrentPoint, false, false, 0, false);
                    nearest3DPointUpdated = true;
                }
            }

            return _nearest3DBMPoint;
        }

        /// <summary>
        /// Attempts to update both the chainage and offset of the point if needed, returns only one value (depending on bool input). Returns -999 if no alignment assigned or invalid point
        /// </summary>
        /// <param name="returnOffset">True to return offset, false to return chainage</param>
        /// <returns></returns>
        private double GetChainageAndOffset(bool returnOffset, bool ForceRefresh)
        {
            double nullValue = -999;
            double currentValue = -999;
            if (returnOffset) { currentValue = _offset; } else { currentValue = _chainage; }

            if (CurrentAlignment == null)
            {
                if (CurrentPoint.PointLabel == "P001") { }
                chainageAndOffsetUpdated = true;
                _chainage = nullValue;
                _offset = nullValue;
                _outVec = null;
                return nullValue;
            }

            if (recalc)
            {
                if (currentValue == -999 || CurrentPoint.Moved || ForceRefresh)
                {
                    if (!chainageAndOffsetUpdated)
                    {
                        chainageAndOffsetUpdated = true;
                        AlignmentVector5 outvec = new AlignmentVector5();
                        if (CurrentAlignment.Horizontal.GetChainageAndOffset(CurrentPoint.X, CurrentPoint.Y, ref outvec))
                        {
                            _chainage = outvec.chainage;
                            _offset = outvec.offset;
                            _outVec = outvec;
                        }
                        else
                        {
                            _chainage = nullValue;
                            _offset = nullValue;
                            _outVec = null;
                        }
                    }
                }
            }

            if (returnOffset) { return _offset; } else { return _chainage; }
        }

        private double GetHorizontalRadius(bool ForceRefresh)
        {
            if (CurrentAlignment == null) { return _horizontalRadius = -999; }
            if (recalc)
            {
                if (CurrentPoint.Moved || ForceRefresh)
                {
                    _horizontalRadius = OutVec.radius;
                    
                }
            }

            return _horizontalRadius;
        }

        private double GetVerticalAlignmentLevel(bool ForceRefresh)
        {
            if (CurrentAlignment == null || CurrentAlignment.Vertical == null) { return _verticalAlignmentLevel = -999; }
            if (recalc)
            {
                if (CurrentPoint.Moved || ForceRefresh)
                {
                    _verticalAlignmentLevel = CurrentAlignment.Vertical.GradeLevel(Chainage);
                }
            }

            return _verticalAlignmentLevel;
        }


        private double GetHorizontalVectorDistancetoNearestBMPoint(bool ForceRefresh)
        {
            if (NearestBMPoint == null) { _horizontalVectorDistancetoNearestBMPoint = -999; }

            if (recalc)
            {
                if (CurrentPoint.Moved || NearestBMPoint.Moved || ForceRefresh)
                {
                    _horizontalVectorDistancetoNearestBMPoint = NRG.MathsHelpers.Vector.FnDistance(CurrentPoint.X, CurrentPoint.Y, NearestBMPoint.X, NearestBMPoint.Y);
                }
            }

            return _horizontalVectorDistancetoNearestBMPoint;
        }

        private double GetHeightDifferenceFromNearestBMPoint(bool ForceRefresh)
        {
            if (NearestBMPoint == null) { _heightDifferenceFromNearestBMPoint = -999; }

            if (recalc)
            {
                if (CurrentPoint.Moved || NearestBMPoint.Moved || ForceRefresh)
                {
                    _heightDifferenceFromNearestBMPoint = CurrentPoint.Z - NearestBMPoint.Z; ;
                }
            }

            return _heightDifferenceFromNearestBMPoint;
        }

        private double Get3DVectorDistanceToNearestBMPoint(bool ForceRefresh)
        {
            if (NearestBMPoint == null) { _3dVectorDistancetoNearestBMPoint = -999; }

            if (recalc)
            {
                if (CurrentPoint.Moved || NearestBMPoint.Moved || ForceRefresh)
                {
                    _3dVectorDistancetoNearestBMPoint = CurrentPoint.DistanceTo(NearestBMPoint);
                }
            }

            return _3dVectorDistancetoNearestBMPoint;
        }

        private double GetNearestBMPointEasting(bool ForceRefresh)
        {
            if (NearestBMPoint == null) { _nearestBMPointEasting = -999; }

            if (recalc)
            {
                if (CurrentPoint.Moved || NearestBMPoint.Moved || ForceRefresh)
                {
                    _nearestBMPointEasting = NearestBMPoint.X;
                }
            }

            return _nearestBMPointEasting;
        }

        private double GetNearestBMPointNorthing(bool ForceRefresh)
        {
            if (NearestBMPoint == null) { _nearestBMPointNorthing = -999; }

            if (recalc)
            {
                if (CurrentPoint.Moved || NearestBMPoint.Moved || ForceRefresh)
                {
                    _nearestBMPointNorthing = NearestBMPoint.Y;
                }
            }

            return _nearestBMPointNorthing;
        }

        private double GetNearestBMPointLevel(bool ForceRefresh)
        {
            if (NearestBMPoint == null) { _nearestBMPointLevel = -999; }

            if (recalc)
            {
                if (CurrentPoint.Moved || NearestBMPoint.Moved || ForceRefresh)
                {
                    _nearestBMPointLevel = NearestBMPoint.Y;
                }
            }

            return _nearestBMPointLevel;
        }

        private double GetHeightDifferencetoBM(bool ForceRefresh)
        {
            if (BackgroundModel == null) { _heightDifferencetoBM = -999; }

            if (recalc)
            {
                if (CurrentPoint.Moved || ForceRefresh)
                {
                    _heightDifferencetoBM = CurrentPoint.Z - BackgroundModel.CalcLevelFromTriangles(CurrentPoint.X, CurrentPoint.Y);
                }
            }

            return _heightDifferencetoBM;
        }



        private double GetBMLevel(bool ForceRefresh)
        {
            if (BackgroundModel == null) { _bMLevel = -999; }

            if (recalc)
            {
                if (CurrentPoint.Moved || ForceRefresh)
                {
                    _bMLevel = BackgroundModel.CalcLevelFromTriangles(CurrentPoint.X, CurrentPoint.Y);
                }
            }

            return _bMLevel;
        }


        private double GetRadialOffsettoAlignment(bool ForceRefresh, bool useNearest3DBackgroundPoint = false)
        {
            DTMPoint PointToUse = CurrentPoint;
            if (useNearest3DBackgroundPoint)
            {
                if (Nearest3DBMPoint == null) 
                {
                    return _nearest3DBMPointRadialOffset = -999;
                }
                else { PointToUse = Nearest3DBMPoint; }
            }


            if (CurrentAlignment == null || CurrentAlignment.Vertical == null) 
            { 
                if (useNearest3DBackgroundPoint) { return _radialOffsettoAlignment = -999; }
                else { return _nearest3DBMPointRadialOffset = -999; }
            }
            if (recalc)
            {
                if (PointToUse.Moved || ForceRefresh)
                {
                    //Apply offset
                    double offsetLevel = CurrentAlignment.Vertical.GradeLevel(PointToUse.Chainage) - AlignmentOffsetLevel;

                    //Calc radial offset to alignment
                    double brg = 0;
                    double dist = 0;
                    NRG.MathsHelpers.Trig.RPC(0, offsetLevel, Offset, PointToUse.Z, ref brg, ref dist);

                    if (useNearest3DBackgroundPoint) { return _radialOffsettoAlignment = dist; }
                    else { return _nearest3DBMPointRadialOffset = dist; }
                }
            }

            if (useNearest3DBackgroundPoint) { return _radialOffsettoAlignment; }
            else { return _nearest3DBMPointRadialOffset; }
        }

        private string GetNearest3DBMPointLabel(bool ForceRefresh)
        {
            if (Nearest3DBMPoint == null) { _nearest3DMBMPointLabel = ""; }

            if (recalc)
            {
                if (CurrentPoint.Moved || Nearest3DBMPoint.Moved || ForceRefresh)
                {
                    _nearest3DMBMPointLabel = Nearest3DBMPoint.PointLabel;
                }
            }

            return _nearest3DMBMPointLabel;
        }
        private double GetSlopeDistanceFromNearest3DBMPoint(bool ForceRefresh)
        {
            if (Nearest3DBMPoint == null) { _slopeDistanceFromNearest3DBMPoint = -999; }

            if (recalc)
            {
                if (CurrentPoint.Moved || Nearest3DBMPoint.Moved || ForceRefresh)
                {
                    _slopeDistanceFromNearest3DBMPoint = CurrentPoint.Distance3DTo(Nearest3DBMPoint);
                }
            }

            return _slopeDistanceFromNearest3DBMPoint;
        }

        private double GetLips(bool ForceRefresh)
        {
            if (Nearest3DBMPoint == null) { _lips = -999; }

            if (recalc)
            {
                if (CurrentPoint.Moved || Nearest3DBMPoint.Moved || ForceRefresh)
                {
                    _lips = RadialOffsettoAlignment - Nearest3DBGPointRadialOffset;
                }
            }

            return _lips;
        }
        #endregion
    }
}

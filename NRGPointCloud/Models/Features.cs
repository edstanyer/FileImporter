using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Services;
using NRG.Models.Rendering;
using NRG.Models;
using System.ComponentModel;

namespace NRG.Models
{
    public class LineFeature : Feature
    {
        #region Setup

        public LineFeature()
        {
            //Show Setting
            Show = true;
            LwByLayer = false;
        }

        public LineFeature(string code, string[] args, bool isBreakLine = false)
        {
            FieldCode = code;
            Code = code.Replace("?", "").Replace("*", "");

            //Plan Feature
            if(args.Length < 1)
            {
                PlanFeature = "";
            }
            else
            {
                PlanFeature = args[0];
            }

            //Color
            if (args.Length < 2)
            {
                R = 0; G = 0; B = 0;
            }
            else
            {
                if (long.TryParse(args[1], out long col))
                {
                    int r = 0, g = 0, b = 0;
                    Conversions.LongIntToRGB(col, ref r, ref g, ref b);
                    R = r; G = g; B = b;
                }
                else
                {
                    R = 0; G = 0; B = 0;
                }
            }

            //Draw
            if (args.Length < 4)
            {
                Draw = true;
            }
            else
            {
                if (args[3] == "-1")
                    Draw = true;
                else
                    Draw = false;
            }

            int breakLineOffset = 0;

            //BreakLineType
            if(isBreakLine)
            {
                if (args.Length < 13)
                {
                    BreakLineType = BreakLineType.BreakLine;
                }
                else
                {
                    if (args[12] == "4")
                        BreakLineType = BreakLineType.ExcludeFromModel;
                    else if (args[12] == "0")
                        BreakLineType = BreakLineType.None;
                    else
                        BreakLineType = BreakLineType.BreakLine;
                }

                breakLineOffset = 1;
            }
            else
            {
                BreakLineType = BreakLineType.None;
            }

            //Width
            if(args.Length < 14 + breakLineOffset)
            {
                Width = 0.2;
            }
            else
            {
                if (!double.TryParse(args[13 + breakLineOffset], out double width))
                    Width = 0.2;
                else
                    Width = width;
            }

            //Layer
            if(args.Length < 17 + breakLineOffset)
            {
                Layer = "";
            }
            else
            {
                Layer = args[16 + breakLineOffset];
            }

            //Show Setting
            Show = true;
            LwByLayer = false;
        }

        #endregion

        #region Methods


        public override Feature Copy()
        {
            return new LineFeature()
            {
                FieldCode = this.FieldCode,
                Code = this.Code,
                PlanFeature = this.PlanFeature,
                R = this.R,
                G = this.G,
                B = this.B,
                Draw = this.Draw,
                BreakLineType = this.BreakLineType,
                Width = this.Width,
                Layer = this.Layer
            };
        }

        #endregion
    }

    public class PointFeature : Feature
    {
        #region Setup

        public PointFeature()
        {
            //Show Setting
            Show = true;
            LwByLayer = false;
        }

        public PointFeature(string code, string[] args)
        {
            FieldCode = code;
            Code = code.Replace("?", "").Replace("*", "");

            //Plan Feature
            if (args.Length < 1)
            {
                PlanFeature = "";
            }
            else
            {
                PlanFeature = args[0];
            }

            //Color
            if (args.Length < 2)
            {
                R = 0; G = 0; B = 0;
            }
            else
            {
                if(!long.TryParse(args[1], out long col))
                {
                    R = 0; G = 0; B = 0;
                }
                else
                {
                    int r = 0, g = 0, b = 0;
                    Conversions.LongIntToRGB(col, ref r, ref g, ref b);
                    R = r; G = g; B = b;
                }
            }

            //Draw
            if(args.Length < 4)
            {
                Draw = true;
            }
            else
            {
                if (args[3] == "-1")
                    Draw = true;
                else
                    Draw = false;
            }

            //Width
            if (args.Length < 12)
            {
                Width = 0.2;
            }
            else
            {
                if (!double.TryParse(args[11], out double width))
                    width = 0.2;

                Width = width;
            }

            //Layer
            if (args.Length < 15)
            {
                Layer = "";
            }
            else
            {
                Layer = args[14];
            }

            //Exclude From Model
            if (args.Length < 23)
            {
                BreakLineType = BreakLineType.None;
            }
            else
            {
                if (args[22] == "-1")
                    BreakLineType = BreakLineType.ExcludeFromModel;
                else
                    BreakLineType = BreakLineType.None;
            }

            //Show Setting
            Show = true;
            LwByLayer = false;
        }

        #endregion

        #region Methods

        public override Feature Copy()
        {
            return new PointFeature()
            {
                FieldCode = this.FieldCode,
                Code = this.Code,
                PlanFeature = this.PlanFeature,
                R = this.R,
                G = this.B,
                B = this.B,
                Draw = this.Draw,
                Width = this.Width,
                Layer = this.Layer,
                BreakLineType = this.BreakLineType
            };
        }

        #endregion
    }

    public class Feature
    {
        #region Properties

        public string Code { get; set; }
        public double Width { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public string PlanFeature { get; set; }
        public string Layer { get; set; }
        public string FieldCode { get; set; }
        public BreakLineType BreakLineType { get; set; }
        public int LineWeight { get;set; }

        public bool LwByLayer { get; set; }
        public bool Draw { get; set; }
        public bool Show { get; set;  }

        #endregion


        #region Methods

        public override string ToString()
        {
            return Code;
        }

        //Added by TN: 20.09.21
        public bool AddToLayer(DTM dtm, string layerString = null)
        {
            //If the layer passed isn't valid, add object to the Default layer
            if (layerString == null || string.IsNullOrWhiteSpace(layerString))
                layerString = "Default";

            //If layer exists, it'll be set. If not, a new layer is made and then set.
            if (dtm.CheckIfLayerExists(layerString))
            {
                this.Layer = layerString;
                return true;
            }
            else
            {
                if (dtm.AddLayer(layerString))
                {
                    this.Layer = layerString;
                    return true;
                }

            }
            //If this returns false, something is really wrong.
            return false;
        }

        public virtual Feature Copy()
        {
            return null;
        }

        /// <summary>
        /// Determines if a provided code is considered to be a match for this <see cref="Feature"/>
        /// </summary>
        /// <param name="code">The code to check</param>
        /// <param name="matchScore">An <see cref="int"/> to store the value of the direct match</param>
        /// <param name="finalScore">An <see cref="int"/> to store the value of the final match</param>
        /// <returns>Returns true if the code is a valid match for the <see cref="Feature"/></returns>
        public bool IsCodeValid(string code, ref int matchScore, ref int finalScore)
        {
            matchScore = 0;
            finalScore = 0;

            //If the code to check is shorter then its invalid
            if (code.Length < Code.Length)
                return false;

            var upperCode = Code.ToUpper();
            for(int i = 0; i < upperCode.Length; i++)
            {
                //If the code segment doesn't match its not a valid code
                if (upperCode[i] != code[i])
                    return false;

                matchScore++;
            }

            //If the code is equal in length then its valid
            if (code.Length == Code.Length)
                return true;

            //If the field code contains ? it is only valid when the remaining characters are numbers
            if (FieldCode.Contains("?") && code.Length == FieldCode.Length)
            {
                finalScore = code.Length;
                return true;
            }

            //If the field code contains * its valid
            if (FieldCode.Contains("*"))
                return true;

            return false;
        }

        #endregion
    }

    public class LineFeatureCollection
    {
        #region Properties

        public LineFeature Feature { get; set; }
        public DTM DTM { get; set; }
        public Dictionary<string, List<LineFeatureItem>> FeatureItems { get; set; }
        public Dictionary<string, List<CADLineFeatureItem>> CADFeatureItems { get; set; }
        public SharedRenderItem RenderItem { get; set; }

        #endregion

        #region Setup

        public LineFeatureCollection(LineFeature feature, DTM dtm)
        {
            Feature = feature;
            DTM = dtm;
            FeatureItems = new Dictionary<string, List<LineFeatureItem>>();
            CADFeatureItems = new Dictionary<string, List<CADLineFeatureItem>>();
        }

        #endregion

        #region Method

        public override string ToString()
        {
            var code = Feature?.Code;
            if (code == null || code == "")
                return "Undefined Feature";
            else
                return code;
        }


        public int GetNumberOfPoints()
        {
            return DTM.Points.Count();
        }

        #endregion
    }

    public class PointFeatureCollection
    {
        #region Properties

        public PointFeature Feature { get; set; }
        public DTM DTM { get; set; }
        public BindingList<DTMPoint> Points { get; set; }
        public SharedRenderItem RenderItem { get; set; }

        #endregion

        #region Setup

        public PointFeatureCollection(PointFeature feature, DTM dtm)
        {
            Feature = feature;
            DTM = dtm;
            Points = new BindingList<DTMPoint>();
        }

        #endregion

        #region Methods

        public void AddPoint(DTMPoint point)
        {
            if (point.PointFeatures == null)
                point.PointFeatures = new List<PointFeatureCollection>();

            point.PointFeatures.Add(this);
            Points.Add(point);
        }

        public override string ToString()
        {
            var code = Feature?.Code;
            if (code == null || code == "" || code == "@")
                return "Undefined Feature";
            else
                return code;
        }

        #endregion
    }

    public class LineFeatureItem
    {
        #region Properties

        public string Code { get; set; }
        public BindingList<DTMPoint> Points { get; set; }
        public bool Closed { get; set; }
        public bool Started { get; set; }
        public string Layer { get; private set; }
        public LineFeatureCollection FeatureCollection { get; set; }
        public System.Drawing.Color OverrideColour = System.Drawing.Color.FromArgb(0); //Added by ES:26.04.21 
        public int ID { get; set; } //Added by TN 16.08.22. Use freely for counts etc
        #endregion

        #region Setup

        public LineFeatureItem(string code, LineFeatureCollection collection)
        {
            Code = code;
            Points = new BindingList<DTMPoint>();
            Closed = false;
            Started = false;
            FeatureCollection = collection;
    }
        #endregion

        #region Methods

        public void AddPoint(DTMPoint point)
        {
            if (point.LineFeatures == null)
                point.LineFeatures = new List<LineFeatureItem>();
            
            point.LineFeatures.Add(this);
            
            Points.Add(point);
        }

        public void InsertPoint(int index, DTMPoint point)
        {
            if (point.LineFeatures == null)
                point.LineFeatures = new List<LineFeatureItem>();

            point.LineFeatures.Add(this);
            Points.Insert(index, point);
        }

        public void RemovePoint(DTMPoint point)
        {
            if (point.LineFeatures != null && point.LineFeatures.Contains(this))
            {
                point.LineFeatures.Remove(this);
            }
                Points.Remove(point);
        }

        public void RemovePoint(DTMPoint point, bool BreakLine)
        {
            if (point.LineFeatures.IndexOf(this) < point.LineFeatures.Count-1)
            { 

            }

            if (point.LineFeatures != null && point.LineFeatures.Contains(this))
            {
                point.LineFeatures.Remove(this);
            }
            Points.Remove(point);
            
        }

        public override string ToString()
        {
            if (Code == null || Code == "")
                return "Undefined Feature";
            else
                return Code;
        }

        #endregion
    }

    public class CADLineFeatureItem
    {
        #region Properties

        public string Code { get; set; }
        public DTMPoint StartLine { get; set; }
        public DTMPoint EndLine { get; set; }
        public LineFeatureCollection FeatureCollection { get; set; }
        public System.Drawing.Color OverrideColour = System.Drawing.Color.FromArgb(0);
        #endregion

        #region Setup

        public CADLineFeatureItem(string code, LineFeatureCollection collection)
        {
            Code = code;
            FeatureCollection = collection;
        }

        #endregion

        #region Methods

        public void AddStartPoint(DTMPoint point)
        {
            if (point.CADFeatures == null)
                point.CADFeatures = new List<CADLineFeatureItem>();

            point.CADFeatures.Add(this);
            StartLine = point;
        }

        public void AddEndPoint(DTMPoint point)
        {
            if (point.CADFeatures == null)
                point.CADFeatures = new List<CADLineFeatureItem>();

            point.CADFeatures.Add(this);
            EndLine = point;
        }

        public override string ToString()
        {
            if (Code == null || Code == "")
                return "Undefined Feature";
            else
                return Code;
        }

        #endregion
    }
}

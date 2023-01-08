using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser.Parser;
using IniParser.Model;
using NRG.Models;
using NRG.Services;
using System.Drawing;

namespace NRG.Settings
{
    public class TunnelSettings
    {
        #region Properties

        public string OverbreakLayer;
        public string UnderbreakLayer;

        //Best Fit
        public bool HoldVerticalAlignment { get; set; }
        public double ExcludeClearences { get; set; }
        public double Tolerance { get; set; }
        public Color WithinTolerance { get; set; }
        public Color OutOfTolerance { get; set; }

        /// <summary>Values: 0, 1 ,2<para>0: Circle</para><para>1: Clearances</para><para>2: None</para></summary>
        public int BestFitMethod { get; set; }

        //Overbreak/Underbreak
        public bool ApplyCurveFitting { get; set; }
        public double StepSize { get; set; }
        public bool OverbreakOpen { get; set; }
        public Color OverbreakShadingColor { get; set; }
        public FillType OverbreakShadingType { get; set; }
        public Color UnderbreakShadingColor { get; set; }
        public FillType UnderbreakShadingType { get; set; }
        public bool DisplayOverbreak { get; set; }
        public bool DisplayUnderbreak { get; set; }      

        //Drawing shape stuff
        public Color ShapeColor { get; set; }
        public OpacityPercent ShapeOpacity { get; set; }
        public Color RailColor { get; set; }
        public Color IntervalColor { get; set; }
        public bool ShowRails { get; set; }
        public bool ShowIntervals { get; set; }
        public double ChainageInterval { get; set; }

        public bool IsSingleShape { get; set; }

        public double Gauge { get; set; }

        #endregion

        #region Setup

        public TunnelSettings()
        {
            GetSettingsFromDefault();
        }

        public TunnelSettings(string filepath)
        {
            var section = GetSectionDataFromFile(filepath);
            if (section != null)
                GetSettingsFromSection(section);
            else
                GetSettingsFromDefault();
        }

        public TunnelSettings(KeyDataCollection section)
        {
            if (section != null)
                GetSettingsFromSection(section);
            else
                GetSettingsFromDefault();
        }

        public SectionData CreateSectionForFile()
        {
            var section = new SectionData("Tunnel");
            var keyData = new KeyDataCollection();
            SetSections(ref keyData);
            section.Keys = keyData;
            return section;
        }

        private KeyDataCollection GetSectionDataFromFile(string filepath)
        {
            try
            {
                var data = new IniDataParser().Parse(Helpers.GetSaneIniFile(filepath));

                if (data.Sections.ContainsSection("Report"))
                    return data.Sections["Report"];

                return null;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private void SetSections(ref KeyDataCollection section)
        {
            section.AddKey("HoldVerticalAlignment", HoldVerticalAlignment ? "1" : "0");
            section.AddKey("ExcludeClearances", ExcludeClearences.ToString());
            section.AddKey("Tolerance", Tolerance.ToString());
            section.AddKey("WithinTolerance", Conversions.RGBToHex(WithinTolerance));
            section.AddKey("OutOfTolerance", Conversions.RGBToHex(OutOfTolerance));
            section.AddKey("BestFitMethod", BestFitMethod.ToString());

            section.AddKey("ApplyCurveFitting", ApplyCurveFitting ? "1" : "0");
            section.AddKey("StepSize", StepSize.ToString());
            section.AddKey("OverbreakOpen", OverbreakOpen ? "1" : "0");
            section.AddKey("OverbreakShadingColor", Conversions.RGBToHex(OverbreakShadingColor));
            section.AddKey("OverbreakShadingType", ((int)OverbreakShadingType).ToString());
            section.AddKey("UnderbreakShadingColor", Conversions.RGBToHex(UnderbreakShadingColor));
            section.AddKey("UnderbreakShadingType", ((int)UnderbreakShadingType).ToString());

            section.AddKey("ShapeColor", Conversions.RGBToHex(ShapeColor));
            section.AddKey("ShapeOpacity", ((int)ShapeOpacity).ToString());
            section.AddKey("RailColor", Conversions.RGBToHex(RailColor));
            section.AddKey("IntervalColor", Conversions.RGBToHex(IntervalColor));

            section.AddKey("ShowRails", ShowRails ? "1" : "0");
            section.AddKey("ShowIntervals", ShowIntervals ? "1" : "0");

            section.AddKey("ChainageInterval", ChainageInterval.ToString());

            section.AddKey("IsSingleShape", IsSingleShape ? "1" : "0");
        }

        private void GetSettingsFromSection(KeyDataCollection section)
        {
            GetHoldVerticalAlignment(section);
            GetExcludeClearences(section);
            GetTolerance(section);
            GetWithinTolerance(section);
            GetOutOfTolerance(section);
            GetBestFitMethod(section);

            GetApplyCurveFitting(section);
            GetStepSize(section);
            GetOverbreakOpen(section);
            GetOverbreakShadingColor(section);
            GetOverbreakShadingType(section);
            GetUnderbreakShadingColor(section);
            GetUnderbreakShadingType(section);
            GetDisplayOverbreak(section);
            GetDisplayUnderbreak(section);

            GetShapeColor(section);
            GetShapeOpacity(section);
            GetRailColor(section);
            GetIntervalColor(section);

            GetShowRails(section);
            GetShowIntervals(section);

            GetChainageInterval(section);

            GetIsSingleShape(section);
        }

        private void GetSettingsFromDefault()
        {
            HoldVerticalAlignment = false;
            ExcludeClearences = 1E20;
            Tolerance = 0.0;
            WithinTolerance = Color.FromArgb(0, 255, 0);
            OutOfTolerance = Color.FromArgb(255, 0, 0);
            BestFitMethod = 0;

            ApplyCurveFitting = false;
            StepSize = 0.1;
            OverbreakOpen = true;
            OverbreakShadingColor = Color.FromArgb(255, 0, 0);
            OverbreakShadingType = FillType.DIAGONALLEFT;
            UnderbreakShadingColor = Color.FromArgb(0, 255, 0);
            UnderbreakShadingType = FillType.DIAGONALRIGHT;
            DisplayOverbreak = true;
            DisplayUnderbreak = true;

            ShapeColor = Color.FromArgb(192, 192, 192);
            ShapeOpacity = OpacityPercent.Opacity100;
            //RailColor = Color.FromArgb(105, 105, 105);
            RailColor = Color.FromArgb(59, 68, 83);

            IntervalColor = Color.FromArgb(192, 192, 192);
            ShowRails = true;
            ShowIntervals = true;
            ChainageInterval = 5;

            IsSingleShape = true;
            Gauge = 1.507;
        }

        public TunnelSettings Copy()
        {
            return new TunnelSettings()
            {
                HoldVerticalAlignment = this.HoldVerticalAlignment,
                ExcludeClearences = this.ExcludeClearences,
                Tolerance = this.Tolerance,
                WithinTolerance = this.WithinTolerance,
                OutOfTolerance = this.OutOfTolerance,
                BestFitMethod = this.BestFitMethod,

                ApplyCurveFitting = this.ApplyCurveFitting,
                StepSize = this.StepSize,
                OverbreakOpen = this.OverbreakOpen,
                OverbreakShadingColor = this.OverbreakShadingColor,
                OverbreakShadingType = this.OverbreakShadingType,
                UnderbreakShadingColor = this.UnderbreakShadingColor,
                UnderbreakShadingType = this.UnderbreakShadingType,
                DisplayOverbreak = this.DisplayOverbreak,
                DisplayUnderbreak = this.DisplayUnderbreak,

                ShapeColor = this.ShapeColor,
                ShapeOpacity = this.ShapeOpacity,
                RailColor = this.RailColor,
                IntervalColor = this.IntervalColor,

                ShowRails = this.ShowRails,
                ShowIntervals = this.ShowIntervals,

                ChainageInterval = this.ChainageInterval,

                IsSingleShape = this.IsSingleShape,
                Gauge = this.Gauge
            };
        }

        #endregion

        #region Get Settings

        private void GetHoldVerticalAlignment(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("HoldVerticalAlignment"))
                    HoldVerticalAlignment = section["HoldVerticalAlignment"] == "1";
                else
                    HoldVerticalAlignment = false;
            }
            catch
            {
                HoldVerticalAlignment = false;
            }
        }

        private void GetExcludeClearences(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("ExcludeClearences"))
                    ExcludeClearences = Convert.ToDouble(section["ExcludeClearences"]);
                else
                    ExcludeClearences = 1E20;
            }
            catch
            {
                ExcludeClearences = 1E20;
            }
        }

        private void GetTolerance(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("Tolerance"))
                    Tolerance = Convert.ToDouble(section["Tolerance"]);
                else
                    Tolerance = 0.0;
            }
            catch
            {
                Tolerance = 0.0;
            }
        }

        private void GetWithinTolerance(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("WithinTolerance"))
                    WithinTolerance = ColorTranslator.FromHtml(section["WithinTolerance"]);
                else
                    WithinTolerance = Color.FromArgb(0, 255, 0);
            }
            catch
            {
                WithinTolerance = Color.FromArgb(0, 255, 0);
            }
        }

        private void GetOutOfTolerance(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("OutOfTolerance"))
                    OutOfTolerance = ColorTranslator.FromHtml(section["OutOfTolerance"]);
                else
                    OutOfTolerance = Color.FromArgb(255, 0, 0);
            }
            catch
            {
                OutOfTolerance = Color.FromArgb(255, 0, 0);
            }
        }

        private void GetBestFitMethod(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("BestFitMethod"))
                    BestFitMethod = Convert.ToInt32(section["BestFitMethod"]);
                else
                    BestFitMethod = 0;
            }
            catch
            {
                BestFitMethod = 0;
            }
        }

        private void GetApplyCurveFitting(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("ApplyCurveFitting"))
                    ApplyCurveFitting = section["ApplyCurveFitting"] == "1";
                else
                    ApplyCurveFitting = false;
            }
            catch
            {
                ApplyCurveFitting = false;
            }
        }

        private void GetStepSize(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("StepSize"))
                    StepSize = Convert.ToDouble(section["StepSize"]);
                else
                    StepSize = 0.1;
            }
            catch
            {
                StepSize = 0.1;
            }
        }

        private void GetOverbreakOpen(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("OverbreakOpen"))
                    OverbreakOpen = section["OverbreakOpen"] == "1";
                else
                    OverbreakOpen = true;
            }
            catch
            {
                OverbreakOpen = true;
            }
        }

        private void GetOverbreakShadingColor(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("OverbreakShadingColor"))
                    OverbreakShadingColor = ColorTranslator.FromHtml(section["OverbreakShadingColor"]);
                else
                    OverbreakShadingColor = Color.FromArgb(255, 0, 0);
            }
            catch
            {
                OverbreakShadingColor = Color.FromArgb(255, 0, 0);
            }
        }

        private void GetOverbreakShadingType(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("OverbreakShadingType"))
                    OverbreakShadingType = (FillType)Convert.ToInt32(section["OverbreakShadingType"]);
                else
                    OverbreakShadingType = FillType.DIAGONALLEFT;
            }
            catch
            {
                OverbreakShadingType = FillType.DIAGONALLEFT;
            }
        }

        private void GetUnderbreakShadingColor(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("UnderbreakShadingColor"))
                    UnderbreakShadingColor = ColorTranslator.FromHtml(section["UnderbreakShadingColor"]);
                else
                    UnderbreakShadingColor = Color.FromArgb(0, 255, 0);
            }
            catch
            {
                UnderbreakShadingColor = Color.FromArgb(0, 255, 0);
            }
        }

        private void GetUnderbreakShadingType(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("UnderbreakShadingType"))
                    UnderbreakShadingType = (FillType)Convert.ToInt32(section["UnderbreakShadingType"]);
                else
                    UnderbreakShadingType = FillType.DIAGONALRIGHT;
            }
            catch
            {
                UnderbreakShadingType = FillType.DIAGONALRIGHT;
            }
        }

        private void GetDisplayOverbreak(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("DisplayOverbreak"))
                    DisplayOverbreak = section["DisplayOverbreak"] == "1";
                else
                    DisplayOverbreak = true;
            }
            catch
            {
                DisplayOverbreak = true;
            }
        }

        private void GetDisplayUnderbreak(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("DisplayUnderbreak"))
                    DisplayUnderbreak = section["DisplayUnderbreak"] == "1";
                else
                    DisplayUnderbreak = true;
            }
            catch
            {
                DisplayUnderbreak = true;
            }
        }

        private void GetShapeColor(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("ShapeColor"))
                    ShapeColor = ColorTranslator.FromHtml(section["ShapeColor"]);
                else
                    ShapeColor = Color.FromArgb(192, 192, 192);
            }
            catch
            {
                ShapeColor = Color.FromArgb(192, 192, 192);
            }
        }

        private void GetShapeOpacity(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("ShapeOpacity"))
                    ShapeOpacity = (OpacityPercent)Convert.ToInt32(section["ShapeOpacity"]);
                else
                    ShapeOpacity = OpacityPercent.Opacity100;
            }
            catch
            {
                ShapeOpacity = OpacityPercent.Opacity100;
            }
        }

        private void GetRailColor(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("RailColor"))
                    RailColor = ColorTranslator.FromHtml(section["RailColor"]);
                else
                    //RailColor = Color.FromArgb(105, 105, 105);
                    RailColor = Color.FromArgb(59, 68, 83);
            }
            catch
            {
                //RailColor = Color.FromArgb(105, 105, 105);
                RailColor = Color.FromArgb(59, 68, 83);
            }
        }

        private void GetIntervalColor(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("IntervalColor"))
                    IntervalColor = ColorTranslator.FromHtml(section["IntervalColor"]);
                else
                    IntervalColor = Color.FromArgb(192, 192, 192);
            }
            catch
            {
                IntervalColor = Color.FromArgb(192, 192, 192);
            }
        }

        private void GetShowRails(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("ShowRails"))
                    ShowRails = section["ShowRails"] == "1";
                else
                    ShowRails = true;
            }
            catch
            {
                ShowRails = true;
            }
        }

        private void GetShowIntervals(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("ShowIntervals"))
                    ShowIntervals = section["ShowIntervals"] == "1";
                else
                    ShowIntervals = true;
            }
            catch
            {
                ShowIntervals = true;
            }
        }

        private void GetChainageInterval(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("ChainageInterval"))
                    ChainageInterval = Convert.ToDouble(section["ChainageInterval"]);
                else
                    ChainageInterval = 5;
            }
            catch
            {
                ChainageInterval = 5;
            }
        }

        private void GetIsSingleShape(KeyDataCollection section)
        {
            try
            {
                if (section.ContainsKey("IsSingleShape"))
                    IsSingleShape = section["IsSingleShape"] == "1";
                else
                    IsSingleShape = true;
            }
            catch
            {
                IsSingleShape = true;
            }
        }

        #endregion

        #region Methods

        public TunnelSettings DefaultDisplaySettings()
        {
            var settings = Copy();
            settings.ShapeColor = Color.FromArgb(192, 192, 192);
            settings.ShapeOpacity = OpacityPercent.Opacity100;
            //settings.RailColor = Color.FromArgb(105, 105, 105);
            settings.RailColor = Color.FromArgb(59, 68, 83);

            settings.IntervalColor = Color.FromArgb(192, 192, 192);
            settings.ShowRails = true;
            settings.ShowIntervals = true;
            settings.ChainageInterval = 5;
            return settings;
        }

        #endregion
    }
}

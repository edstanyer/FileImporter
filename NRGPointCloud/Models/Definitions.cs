using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRG.Models
{
    [Serializable]
    public enum OutPutFormat
    {
        BINARY,
        LAS,
        LAZ, 
        GPF,
        XYZ,
        DXF
    }

    public enum PointCloudFileFormat
    {
        LAS,
        LAZ,
        ASCII,
        E57,
        ALL
    }

    public enum StoreOption
    {
        ABORT_IF_EXISTS,
        OVERWRITE,
        INCREMENTAL
    }

    public enum ConversionQuality
    {
        FAST,
        DEFAULT,
        NICE
    }

    public enum Type
    {
        POINT,
        BOUNDRY
    }

    public enum SceneType
    {
        ORIGINAL,
        SELECTION,
        CROSSSECTION,
        DEFAULT
    }

    public enum FilterType
    {
        FILTER3D,
        FILTER2D,
        FILTERCUSTOM
    }

    public enum AsciiImport
    {
        CANCEL,
        APPLY,
        APPLYTOALL
    }

    public enum RenderMode
    {
        INCREMENTAL,
        IMMEDIATE
    }

    public enum ColorType
    {
        RGB,
        HeightMap,
        GrayScale,
        Intensity,
        Clearance
    }

    public enum LongSectionSort
    {
        RADIALLY,
        CHAINAGE,
        HEIGHT
    }

    public enum SectionReportType
    {
        SECTION,
        AREAABOVEDATUM,
        DIFFERENCEREPORT
    }

    public enum DataTypes
    {
        Line, 
        CodedPoint, 
        SelectionZone, 
        DeselectionZone,
        FilteredPoints
    }

    public enum SnapMode
    {
        UserData,
        PointCloud
    }

    public enum FillType
    {
        NONE,
        SOLIDFILL,
        HORIZONTALLINES,
        VERTICALLINES,
        DIAGONALRIGHT,
        DIAGONALLEFT,
        CROSSGRID,
        DIAGONALCROSSGRID
    }

    public enum OctreeFileVersion
    {
        FileVersion1,
        FileVersion2
    }

    public enum CrossSectionMode
    {
        LiveSection,
        TakeSections
    }

    public enum CrossSectionType
    {
        PointCloud,
        DTM
    }

    public enum OpacityPercent
    {
        Opacity100,
        Opacity75,
        Opacity50,
        Opacity25
    }

    public class AsciiFormat
    {
        public int SkipLines { get; set; }
        public string Delimiter { get; set; }
        public string Format { get; set; }

        public AsciiFormat()
        {
            SkipLines = 0;
            Delimiter = ",";
            Format = "";
        }

        public AsciiFormat(int skipLines, string delimiter, string format)
        {
            this.SkipLines = skipLines;
            this.Delimiter = delimiter;
            this.Format = format;
        }

        public AsciiFormat Copy()
        {
            return new AsciiFormat()
            {
                SkipLines = this.SkipLines,
                Delimiter = this.Delimiter,
                Format = this.Format
            };
        }
    }

    public enum UserDataFileVersion
    {
        FileVersion1,
        FileVersion1x1
    }

    public enum PointType
    {
        OccupiedStation,
        ReferenceStation,
        ReferenceObservation,
        Observation,
        Coordinate,
        AzimuthCorrection,
        FalsePoint,
        Steiner
    }

    public enum PointProperties
    {
        Command,
        PtNum,
        HCR,
        VCR,
        SD,
        TgtHt,
        Label,
        Notes1,
        Notes2,
        X,
        Y,
        Z,
        Chainage,
        Offset,
        HzRadius,
        VertAlignLevel,
        HzDistToBMPoint,
        HtDiffToBMPoint,
        ThreeDDistToBMPoint,
		EastBMPoint,
		NorthBMPoint,
		LevelBMPoint,
		HtDiffBMModel,
		BMModelLevel


    }

    public enum BreakLineType
    {
        None, 
        BreakLine,
        ExcludeFromModel
    }

    public enum ShowMenuItems
    {
        LineFeatures,
        BreakLines,
        Points,
        Surfaces
    }
}

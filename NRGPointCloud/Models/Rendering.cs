using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace NRG.Models.Rendering
{
    /// <summary>
    /// A struct containing all the information necessary to bind and draw a Vertex Buffer Array
    /// </summary>
    public class RenderItem
    {
        #region Properties

        public uint Vao { get; set; }
        public uint VboXyz { get; set; }
        public uint VboRgba { get; set; }
        public int Count { get; set; }
        public VertexBufferType Type { get; set; }

        #endregion

        #region Setup

        public RenderItem()
        {

        }

        #endregion
    }

    /// <summary>
    /// A <see cref="class"/> containing all the information necessary to bind and draw multiple Vertex Buffer Arrays
    /// </summary>
    public class SharedRenderItem
    {
        #region Properties
        public uint VboXyz { get; set; }
        public uint VboRgba { get; set; }
        public List<int> Count { get; set; }
        public Dictionary<object, uint> VaoList { get; set; }
        public VertexBufferType Type { get; set; }

        #endregion

        #region Setup

        public SharedRenderItem()
        {
            Count = new List<int>();
            VaoList = new Dictionary<object, uint>();
        }

        #endregion
    }

    public enum RenderItemType
    {
        TempLine,
        PointIndicator,
        PointIndicatorAxis,
        SelectionPolygon,
        SelectedPoints,
        FilteredPoints,
        ZoomWindow,
        HighlightedPoints,
        DirectionArrows,
        SectionLineList, //Redundant I think TN 02.02.2022
        SectionMarkerPoint, //Redundant
        VerticalSectionLine,
        SectionDirectionTriangle,
        SelectedPolyLines,
        HighlightedPolyLines,
        SelectedStandardLines,
        HighlightedStandardLines,
        SelectedLineFeatureItems,
        HighlightedLineFeatureItems,
        SelectedArcs,
        HighlightedArcs,
        SelectedSpiralNodes,
        HighlightedArcsDrawingLines,
        GuideLines,//Added by ES:08.07.22 for the crack for the time being!
        Null //Null equivalent. Not an actual render item


    }

    public enum VertexBufferType
    {
        XYZ,
        XYZRGB,
        XYZRGBA
    }

    public class ShowSettings
    {
        public bool ShowPoints { get; set; }
        public bool ShowLines { get; set; }
        public bool ShowBreakLines { get; set; }
        public bool ShowSurfaces { get; set; }
        public bool ShowTriangles { get; set; }
        public bool ShowNonControurableTriangles{ get; set; }

        public bool ShowSection { get; set; }
        public Color ShowColor { get; set; }
    }
}
